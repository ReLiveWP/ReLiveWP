using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Catalog;

// Turns the checked-in manifest into catalog rows. The manifest is the source of truth; recompiling
// an unchanged one writes nothing.
public class CatalogCompiler(
    UpdatesDbContext db,
    IOptions<PackageOptions> packageOptions,
    ILogger<CatalogCompiler> logger)
{
    private readonly PackageOptions packages = packageOptions.Value;

    public async Task<int> CompileAsync(string manifestPath)
    {
        var loaded = ManifestLoader.Load(manifestPath);
        var manifest = loaded.Updates;
        if (manifest.Count == 0 && loaded.OsUpgradeBlocks.Count == 0)
        {
            logger.LogInformation("Nothing to compile in {Path}", manifestPath);
            return 0;
        }

        var allocator = new RevisionIdAllocator(db);
        var written = 0;

        foreach (var update in manifest)
        {
            var files = ReadFiles(update);
            var coreXml = CoreFragmentWriter.Write(update);
            var extendedXml = ExtendedFragmentWriter.Write(update, files);
            var hash = RevisionIdAllocator.HashContent(coreXml, extendedXml);

            var revisionId = await allocator.AllocateAsync(update.UpdateId, update.RevisionNumber, hash);

            if (await db.Updates.AnyAsync(u => u.RevisionId == revisionId))
            {
                // Where a payload is served from is deployment config, not content, so it is
                // deliberately outside the hash. That means moving it never bumps a revision, and
                // equally that a recompile has to refresh it rather than skip the whole revision.
                await RefreshPayloadUrlsAsync(revisionId, files);

                logger.LogDebug("{Slug} revision {Revision} is already compiled as {RevisionId}",
                    update.Slug, update.RevisionNumber, revisionId);
                continue;
            }

            var parsed = SyncUpdatesParser.ParseMetadataOnly(coreXml);
            parsed.RevisionId = revisionId;
            parsed.IsLeaf = update.IsLeaf;
            parsed.DeploymentAction = update.DeploymentAction;
            parsed.IsBundle = update.DeploymentAction == UpdateScopeFilter.BundleAction;
            parsed.LastChangeTime = DateTime.UtcNow.Date;

            var entity = CatalogWriter.BuildUpdate(parsed, UpdateOrigin.Authored);
            CatalogWriter.ApplyExtendedInfo(entity, new ParsedExtendedUpdate
            {
                RevisionId = revisionId,
                ExtendedMetadataXml = extendedXml,
                Fragments = ExtendedFragmentReader.ReadFragments(extendedXml),
                Files = files,
                Localizations = [.. update.Localizations.Select(l => new ParsedLocalization
                {
                    Language = l.Language,
                    Title = l.Title,
                    Description = l.Description,
                    MoreInfoUrl = l.MoreInfoUrl,
                    SupportUrl = l.SupportUrl,
                })],
            });

            db.Updates.Add(entity);
            written++;

            logger.LogInformation("Compiled {Slug} revision {Revision} as {RevisionId}",
                update.Slug, update.RevisionNumber, revisionId);
        }

        await db.SaveChangesAsync();
        await CatalogWriter.RecomputeLeafFlagsAsync(db);
        await ApplyOsUpgradeBlocksAsync(loaded.OsUpgradeBlocks);

        logger.LogInformation("Authored catalog compiled: {Written} new revisions of {Total} declared",
            written, manifest.Count);

        return written;
    }

    private async Task RefreshPayloadUrlsAsync(long revisionId, List<ParsedFile> files)
    {
        var stored = await db.Files.Where(f => f.UpdateRevisionId == revisionId).ToListAsync();

        foreach (var file in stored)
        {
            var current = files.FirstOrDefault(f => f.FileName == file.FileName);
            if (current is null || file.SourceUrl == current.SourceUrl)
                continue;

            file.SourceUrl = current.SourceUrl;
            file.LocalPath = file.FileName;

            logger.LogInformation("Repointed {FileName} at {Url}", file.FileName, current.SourceUrl);
        }
    }

    // Rebuilt from scratch every compile: the derivation is over the catalog, so a crawl that adds
    // updates has to be able to widen the block set without anything being removed by hand.
    private async Task ApplyOsUpgradeBlocksAsync(List<ManifestOsUpgradeBlock> blocks)
    {
        if (blocks.Count == 0)
            return;

        // Kept from the previous compile where the policy is unchanged, so a recompile does not
        // restamp every row and re-announce a change to devices that already applied it.
        var stamped = await db.Deployments
            .AsNoTracking()
            .ToDictionaryAsync(d => (d.Ring, d.UpdateId), d => d);

        await db.Deployments.ExecuteDeleteAsync();
        var now = DateTime.UtcNow.Date;

        foreach (var block in blocks)
        {
            var derived = await BlockSetDeriver.DeriveAsync(db, block.MinimumOsVersion);

            foreach (var updateId in derived.DeployableUpdateIds)
            {
                var previous = stamped.GetValueOrDefault((block.Ring, updateId));
                var unchanged = previous is not null
                    && previous.Action == block.Action
                    && previous.LastChangeTime != default;

                db.Deployments.Add(new UpdateDeployment
                {
                    Ring = block.Ring,
                    UpdateId = updateId,
                    Action = block.Action,
                    Reason = block.Reason,
                    LastChangeTime = unchanged ? previous!.LastChangeTime : now,
                });
            }

            logger.LogInformation(
                "Blocked {Count} deployable updates at or above OS {Version} for ring {Ring} (from {Payloads} payloads through {Bundles} bundles)",
                derived.DeployableUpdateIds.Count, block.MinimumOsVersion, block.Ring,
                derived.PayloadRevisions.Count, derived.BundleRevisions.Count);
        }

        await db.SaveChangesAsync();
    }

    // A payload the manifest names but that is not on the package volume is a compile error. The
    // serving path drops a file it cannot locate, so letting one through here would hand the device
    // an update it can never download.
    private List<ParsedFile> ReadFiles(ManifestUpdate update)
    {
        var files = new List<ParsedFile>();
        foreach (var file in update.Files)
        {
            var path = Path.IsPathRooted(file.Path) ? file.Path : Path.Combine(packages.ResolvedPath, file.Path);
            if (!File.Exists(path))
                throw new FileNotFoundException($"{update.Slug} references a payload that is not on the package volume.", path);

            files.Add(ExtendedFragmentWriter.ReadFile(path, packages.PublicUrlFor(Path.GetFileName(path))));
        }

        return files;
    }
}
