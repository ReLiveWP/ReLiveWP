using FilePath = System.IO.Path;

namespace ReLiveWP.Backend.DeviceUpdate;

public class PackageOptions
{
    public const string SectionName = "Packages";

    public string Path { get; set; } = "Packages";

    // Where a device reaches /Packages. Baked into an authored update's FileLocation at compile
    // time, so it has to be the address the phone can resolve, not the one we bind.
    public string PublicUrl { get; set; } = "http://localhost:10004/Packages";

    public string ResolvedPath => FilePath.GetFullPath(Path);

    public string PublicUrlFor(string fileName) => $"{PublicUrl.TrimEnd('/')}/{Uri.EscapeDataString(fileName)}";
}
