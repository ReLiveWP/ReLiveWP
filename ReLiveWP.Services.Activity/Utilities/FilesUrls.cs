namespace ReLiveWP.Services.Activity.Utilities;

public readonly record struct FilesUrls(string BaseUri, string Id)
{
    public static FilesUrls For(HttpRequest request, string id) => new($"{request.Scheme}://{request.Host}", id);

    public string Files => $"{BaseUri}/Users({Id})/Files";

    public string ForAlbum(string album) => $"{Files}/{album}";

    public string ForFolder(string folderId) => $"{Files}/folders('{folderId}')";

    public string ForItem(string resourceRef) => $"{Files}/files('{resourceRef}')";

    public string ForItemThumbnail(string resourceRef, int size) => $"{ForItem(resourceRef)}/thumbnail/{size}";

    public string ForItemContent(string resourceRef) => $"{ForItem(resourceRef)}/media";
}
