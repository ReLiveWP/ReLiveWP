using System.IO;
using System.Security.Cryptography;

namespace ReLiveWP.Backend.Identity.Services;

public class AvatarStoreOptions
{
    public string Root { get; set; } = "/data/avatars";
}

public class AvatarStore
{
    private readonly string root;

    public AvatarStore(IConfiguration configuration)
    {
        root = configuration["Storage:AvatarRoot"] ?? new AvatarStoreOptions().Root;
        Directory.CreateDirectory(root);
    }

    public string ComputeEtag(byte[] data) => Convert.ToHexString(SHA256.HashData(data))[..32].ToLowerInvariant();

    public async Task<string> WriteAsync(string etag, string extension, byte[] data, CancellationToken ct = default)
    {
        var name = etag + extension;
        var path = Path.Combine(root, name);

        // content-addressed, so an existing file with this name already holds these bytes
        if (!File.Exists(path))
            await File.WriteAllBytesAsync(path, data, ct);

        return name;
    }

    public async Task<byte[]?> ReadAsync(string fileName, CancellationToken ct = default)
    {
        var path = Resolve(fileName);
        if (path is null || !File.Exists(path))
            return null;

        return await File.ReadAllBytesAsync(path, ct);
    }

    public void Delete(string fileName)
    {
        var path = Resolve(fileName);
        if (path is not null && File.Exists(path))
            File.Delete(path);
    }

    private string? Resolve(string fileName)
    {
        var full = Path.GetFullPath(Path.Combine(root, fileName));
        return full.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal) ? full : null;
    }
}
