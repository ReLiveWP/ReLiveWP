namespace ReLiveWP.Services.MediaProxy.Utilities;

public static class KestrelEndpointPorts
{
    public const string PublicEndpoint = "Public";
    public const string InternalEndpoint = "Internal";

    public static int ReadEndpointPort(IConfiguration configuration, string endpointName)
    {
        var key = $"Kestrel:Endpoints:{endpointName}:Url";
        var url = configuration[key];
        if (string.IsNullOrEmpty(url))
            throw new InvalidOperationException($"{key} is not configured.");

        return BindingAddress.Parse(url).Port;
    }
}
