using ReLiveWP.Services.Docs.Dav;

namespace ReLiveWP.Services.Docs.Tests;

public class DavUrlsTests
{
    private const string Base = "https://docs.relivewp.net";

    [Theory]
    [InlineData("", "https://docs.relivewp.net/dav")]
    [InlineData("Documents", "https://docs.relivewp.net/dav/Documents")]
    [InlineData("Documents/Budget.xlsx", "https://docs.relivewp.net/dav/Documents/Budget.xlsx")]
    [InlineData("Shared Docs/Q4 Plan.docx", "https://docs.relivewp.net/dav/Shared%20Docs/Q4%20Plan.docx")]
    public void BuildEscapesSegmentsButKeepsSeparators(string path, string expected)
        => Assert.Equal(expected, DavUrls.Build(Base, path));

    [Theory]
    [InlineData("")]
    [InlineData("Documents")]
    [InlineData("Documents/Budget.xlsx")]
    [InlineData("Shared Docs/Q4 Plan.docx")]
    public void PathSurvivesARoundTrip(string path)
        => Assert.Equal(path, DavUrls.ToPath(DavUrls.Build(Base, path)));

    [Fact]
    public void ToPathAcceptsARelativeUrl()
        => Assert.Equal("Documents/Budget.xlsx", DavUrls.ToPath("/dav/Documents/Budget.xlsx"));

    [Fact]
    public void ToPathIsEmptyForTheRoot()
        => Assert.Equal("", DavUrls.ToPath($"{Base}/dav"));

    [Theory]
    [InlineData("Documents/Budget.xlsx", "Budget.xlsx")]
    [InlineData("Budget.xlsx", "Budget.xlsx")]
    [InlineData("", "")]
    public void NameTakesTheLastSegment(string path, string expected)
        => Assert.Equal(expected, DavUrls.Name(path));
}
