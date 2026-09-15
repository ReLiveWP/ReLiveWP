using ReLiveWP.Backend.DeviceUpdate.Model;
using ReLiveWP.Backend.DeviceUpdate.Wsup;

namespace ReLiveWP.Backend.DeviceUpdate.Tests;

public class ExtendedFragmentReaderTests
{
    private const string Extended =
        """<ExtendedProperties DefaultPropertiesLanguage="en" Handler="cmd"><InstallationBehavior /></ExtendedProperties><Files><File Digest="abc" FileName="x.cab" Size="1" /></Files><HandlerSpecificData type="cmd:CommandLineInstallation" />""";

    private static string Localized(string language, string title) =>
        $"<LocalizedProperties><Language>{language}</Language><Title>{title}</Title></LocalizedProperties>";

    // Files and HandlerSpecificData are siblings of ExtendedProperties inside one Extended fragment,
    // not fragments of their own. Confirmed against fe2.
    [Fact]
    public void ExtendedFragmentKeepsFilesAndHandlerDataTogether()
    {
        var fragments = ExtendedFragmentReader.ReadFragments(Extended);

        var fragment = Assert.Single(fragments);
        Assert.Equal(FragmentTypes.Extended, fragment.FragmentType);
        Assert.Contains("<Files>", fragment.Xml, StringComparison.Ordinal);
        Assert.Contains("<HandlerSpecificData", fragment.Xml, StringComparison.Ordinal);
        Assert.Equal("", fragment.Language);
    }

    [Fact]
    public void EachLocalizedPropertiesBecomesItsOwnFragment()
    {
        var fragments = ExtendedFragmentReader.ReadFragments(
            Extended + Localized("en", "English") + Localized("de", "Deutsch") + Localized("ja", "Japanese"));

        Assert.Equal(4, fragments.Count);
        Assert.Equal(1, fragments.Count(f => f.FragmentType == FragmentTypes.Extended));

        var localized = fragments.Where(f => f.FragmentType == FragmentTypes.LocalizedProperties).ToList();
        Assert.Equal(["en", "de", "ja"], localized.Select(f => f.Language));
        Assert.All(localized, f => Assert.DoesNotContain("<ExtendedProperties", f.Xml, StringComparison.Ordinal));
    }

    // The catalog stored every fragment concatenated into one blob before fragments existed, so the
    // reader has to recover the boundaries for --reparse to work without a re-crawl.
    [Fact]
    public void RecoversBoundariesFromAConcatenatedBlob()
    {
        var blob = Extended + string.Concat(Enumerable.Range(0, 18).Select(i => Localized("l" + i, "t" + i)));

        var fragments = ExtendedFragmentReader.ReadFragments(blob);

        Assert.Equal(19, fragments.Count);
        Assert.Equal(18, fragments.Count(f => f.FragmentType == FragmentTypes.LocalizedProperties));
    }

    [Fact]
    public void EmptyMetadataYieldsNoFragments()
    {
        Assert.Empty(ExtendedFragmentReader.ReadFragments(""));
        Assert.Empty(ExtendedFragmentReader.ReadFragments("   "));
    }

    [Fact]
    public void RepeatedLanguagesGetDistinctOrdinals()
    {
        var fragments = ExtendedFragmentReader.ReadFragments(Localized("en", "one") + Localized("en", "two"));

        Assert.Equal([0, 1], fragments.Select(f => f.Ordinal));
    }
}
