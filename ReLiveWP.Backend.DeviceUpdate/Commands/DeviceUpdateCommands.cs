using System.CommandLine;
using Microsoft.EntityFrameworkCore;
using ReLiveWP.Backend.DeviceUpdate.Catalog;
using ReLiveWP.Backend.DeviceUpdate.Data;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Commands;

public static class DeviceUpdateCommands
{
    private static readonly string[] HelpArguments = ["--help", "-h", "-?", "--version"];

    public static bool IsCommandInvocation(string[] args) =>
        args.Length > 0 && (!args[0].StartsWith('-') || HelpArguments.Contains(args[0]));

    public static Task<int> RunAsync(WebApplication app, string[] args) =>
        BuildRootCommand(app).Parse(args).InvokeAsync();

    private static RootCommand BuildRootCommand(WebApplication app)
    {
        var root = new RootCommand("WP7 device update catalog maintenance. Run with no arguments to serve.");
        var download = new Option<bool>("--download")
        {
            Description = "Fetch the package CABs once the catalog is up to date",
        };

        var crawl = new Command("crawl", "Pull the full WP7 catalog from the upstream update service") { download };
        crawl.SetAction((result, ct) => Scoped(app, async services =>
        {
            await services.GetRequiredService<Crawler>().CrawlAsync();
            if (result.GetValue(download))
                await services.GetRequiredService<PackageStore>().DownloadPackagesAsync();
        }));

        var harvest = new Command("harvest",
            "Run only the OEM/carrier matrix harvest against an already-discovered catalog, then fill extended info") { download };
        harvest.SetAction((result, ct) => Scoped(app, async services =>
        {
            var crawler = services.GetRequiredService<Crawler>();
            var upstream = services.GetRequiredService<WsusUpstreamClient>();
            var db = services.GetRequiredService<UpdatesDbContext>();

            await crawler.HarvestMatrixAsync();
            await crawler.FetchExtendedInfoAsync(await upstream.GetFreshCookieAsync(),
                db.Updates.Where(u => u.ExtendedMetadata == null).Select(u => u.RevisionId));
            await crawler.ResolveFileLocationsAsync(await upstream.GetFreshCookieAsync());

            if (result.GetValue(download))
                await services.GetRequiredService<PackageStore>().DownloadPackagesAsync();
        }));

        var downloadCommand = new Command("download", "Fetch every package CAB the catalog references");
        downloadCommand.SetAction((result, ct) => Scoped(app, services =>
            services.GetRequiredService<PackageStore>().DownloadPackagesAsync()));

        var verify = new Command("verify", "Check every downloaded package against its stored SHA1");
        verify.SetAction((result, ct) => Scoped(app, services =>
            services.GetRequiredService<PackageStore>().VerifyPackagesAsync()));

        var reparse = new Command("reparse", "Rebuild relationships and fragments from the stored metadata, without crawling");
        reparse.SetAction((result, ct) => Scoped(app, services =>
            services.GetRequiredService<CatalogReparser>().ReparseAsync()));

        var manifestPath = new Option<string>("--manifest")
        {
            Description = "Directory holding the authored catalog XML",
            DefaultValueFactory = _ => "Catalog",
        };

        var author = new Command("author", "Compile the authored catalog manifest into the database") { manifestPath };
        author.SetAction((result, ct) => Scoped(app, services =>
            services.GetRequiredService<CatalogCompiler>().CompileAsync(result.GetValue(manifestPath)!)));

        root.Subcommands.Add(crawl);
        root.Subcommands.Add(author);
        root.Subcommands.Add(harvest);
        root.Subcommands.Add(downloadCommand);
        root.Subcommands.Add(verify);
        root.Subcommands.Add(reparse);

        return root;
    }

    private static async Task<int> Scoped(WebApplication app, Func<IServiceProvider, Task> action)
    {
        using var scope = app.Services.CreateScope();
        await action(scope.ServiceProvider);
        return 0;
    }
}
