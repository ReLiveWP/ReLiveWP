namespace ReLiveWP.Services.Support.Services;

public static class ContentValidation
{
    public static void Run(IServiceProvider services, ILogger logger)
    {
        var articles = services.GetRequiredService<KbContentStore>();
        var links = services.GetRequiredService<FwlinkStore>();
        var areas = services.GetRequiredService<AreaStore>();

        foreach (var area in articles.All.Select(a => a.Area).Distinct().Where(a => !areas.IsNamed(a)))
            logger.LogWarning("Area {Area} has articles but no name in areas.yaml", area);

        foreach (var link in links.All.Where(l => articles.ClaimsFwlink(l.Id)))
        {
            logger.LogWarning(
                "Link {Id} is in fwlinks.yaml but KB{Article} also claims it; the article wins and the file entry is unreachable",
                link.Id, articles.FindByFwlink(link.Id)!.Id);
        }
    }
}
