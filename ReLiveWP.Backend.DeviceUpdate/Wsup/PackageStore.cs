using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.DeviceUpdate.Data;

namespace ReLiveWP.Backend.DeviceUpdate.Wsup;

public class PackageStore(UpdatesDbContext db, HttpClient http, IOptions<PackageOptions> options, ILogger<PackageStore> logger)
{
    private const int MaxParallelVerifications = 8;

    private readonly string packagesPath = options.Value.ResolvedPath;

    public async Task DownloadPackagesAsync()
    {
        // Download each referenced package CAB that isn't already on the local volume.
        Directory.CreateDirectory(packagesPath);

        var files = await db.Files
            .Where(f => f.SourceUrl != null && f.LocalPath != null)
            .Select(f => new { f.SourceUrl, f.LocalPath })
            .Distinct()
            .ToListAsync();

        var downloaded = 0;
        await Parallel.ForEachAsync(files, async (file, ct) =>
        {
            var target = Path.Combine(packagesPath, file.LocalPath!);
            if (File.Exists(target))
                return;

            try
            {
                logger.LogInformation("Downloading package {Package}", Path.GetFileName(file.LocalPath));

                using var response = await http.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, file.SourceUrl), HttpCompletionOption.ResponseHeadersRead, ct);
                using var content = await response.Content.ReadAsStreamAsync(ct);
                using var destination = File.OpenWrite(target);

                await content.CopyToAsync(destination, ct);

                Interlocked.Increment(ref downloaded);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to download {Url}", file.SourceUrl);
            }
        });

        logger.LogInformation("Downloaded {Count} package(s)", downloaded);
    }

    public async Task VerifyPackagesAsync()
    {
        // Verify every downloaded package against its stored SHA1 digest. 

        var files = await db.Files
            .Where(f => f.LocalPath != null && f.DigestSha1 != null)
            .Select(f => new { f.LocalPath, f.DigestSha1, f.Size })
            .Distinct()
            .ToListAsync();

        logger.LogInformation("Verifying {Count} package(s)", files.Count);

        var ok = 0;
        var missing = new ConcurrentBag<string>();
        var mismatched = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = MaxParallelVerifications }, async (file, ct) =>
        {
            var path = Path.Combine(packagesPath, file.LocalPath!);
            if (!File.Exists(path))
            {
                missing.Add(file.LocalPath!);
                return;
            }

            await using var stream = File.OpenRead(path);
            var hash = await SHA1.HashDataAsync(stream, ct);

            if (Convert.ToBase64String(hash) == file.DigestSha1)
                Interlocked.Increment(ref ok);
            else
                mismatched.Add(file.LocalPath!);
        });

        foreach (var file in missing)
            logger.LogWarning("Missing: {File}", file);
        foreach (var file in mismatched)
            logger.LogError("Digest mismatch: {File}", file);

        logger.LogInformation("Verify complete: {Ok} ok, {Missing} missing, {Mismatched} mismatched (of {Total})",
            ok, missing.Count, mismatched.Count, files.Count);
    }
}
