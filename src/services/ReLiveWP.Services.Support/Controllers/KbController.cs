using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ReLiveWP.ServiceDefaults;
using ReLiveWP.Services.Support.Models;
using ReLiveWP.Services.Support.Services;

namespace ReLiveWP.Services.Support.Controllers;

public class KbController(
    KbContentStore articles,
    AreaStore areas,
    IOptions<SupportOptions> options) : Controller
{
    private readonly SupportOptions _options = options.Value;

    [HttpGet("/")]
    public IActionResult Home() => View("Home", BuildIndex());

    [HttpGet("/kb")]
    public IActionResult Index() => View(BuildIndex());

    [HttpGet("/kb/{id:int}")]
    [HttpGet("/kb/{id:int}/{slug}")]
    public IActionResult Article(int id, string? slug)
    {
        var article = articles.Find(id);
        if (article is null)
            return NotFoundArticle(id);

        var seeAlso = article.SeeAlso.Select(articles.Find).OfType<KbArticle>().ToList();

        // no lookup: the front matter is what allocates the link, so declaring it is enough
        return View(new KbArticleViewModel(article, seeAlso, article.Fwlink, _options));
    }

    [HttpGet("/search")]
    public IActionResult Search([FromQuery] string? q)
    {
        var query = q?.Trim() ?? "";
        return View(new KbSearchViewModel(query, string.IsNullOrEmpty(query) ? [] : articles.Search(query)));
    }

    [HttpGet("/errors/{code}")]
    public IActionResult Error(string code)
    {
        var article = articles.FindByErrorCode(code);
        if (article is not null)
            return RedirectToAction(nameof(Article), new { id = article.Id, slug = article.Slug });

        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("ErrorNotFound", new KbErrorNotFoundViewModel(code, articles.Search(code)));
    }

    [HttpGet("/en-us/help/{id:int}")]
    [HttpGet("/en-us/kb/{id:int}")]
    [HttpGet("/kb/{id:int}/en-us")]
    public IActionResult LegacyArticle(int id) => RedirectPermanent($"/kb/{id}");

    [HttpGet("/default.aspx")]
    public IActionResult LegacyScid([FromQuery] string? scid)
    {
        var last = scid?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
        return int.TryParse(last, out var id) ? RedirectPermanent($"/kb/{id}") : RedirectPermanent("/kb");
    }

    private IActionResult NotFoundArticle(int id)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("ArticleNotFound", id);
    }

    private KbIndexViewModel BuildIndex()
    {
        var grouped = articles.All
            .GroupBy(a => a.Area)
            .OrderBy(g => g.Key)
            .Select(g => new KbAreaGroup(g.Key, areas.NameFor(g.Key), g.OrderBy(a => a.Id).ToList()))
            .ToList();

        return new KbIndexViewModel(articles.All.Count, grouped, RecentlyReviewed());
    }

    private IReadOnlyList<KbArticle> RecentlyReviewed() =>
        articles.All.OrderByDescending(a => a.LastReview).ThenByDescending(a => a.Id).Take(5).ToList();
}

public record KbAreaGroup(int Key, string Name, IReadOnlyList<KbArticle> Articles);

public record KbIndexViewModel(int Count, IReadOnlyList<KbAreaGroup> Areas, IReadOnlyList<KbArticle> RecentlyReviewed);

public record KbArticleViewModel(KbArticle Article, IReadOnlyList<KbArticle> SeeAlso, int? Fwlink, SupportOptions Options);

public record KbSearchViewModel(string Query, IReadOnlyList<KbArticle> Results);

public record KbErrorNotFoundViewModel(string Code, IReadOnlyList<KbArticle> Suggestions);
