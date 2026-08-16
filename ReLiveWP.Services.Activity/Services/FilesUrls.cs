namespace ReLiveWP.Services.Activity.Services;

public readonly record struct FilesUrls(string BaseUri, string Id)
{
    public static FilesUrls For(HttpRequest request, string id) => new($"{request.Scheme}://{request.Host}", id);

    public string Files => $"{BaseUri}/Users({Id})/Files";

    public string Album(string album) => $"{Files}/{album}";

    public string Folder(string folderId) => $"{Files}/folders('{folderId}')";

    public string Media(string resourceRef) => $"{Files}/files('{resourceRef}')";

    public string Thumbnail(string resourceRef, int size) => $"{Media(resourceRef)}/thumbnail/{size}";

    public string MediaContent(string resourceRef) => $"{Media(resourceRef)}/media";
}
