namespace ReLiveWP.Dav;

public static class DavMethods
{
    public static readonly HttpMethod Propfind = new("PROPFIND");
    public static readonly HttpMethod Report = new("REPORT");
    public static readonly HttpMethod MkCol = new("MKCOL");
}
