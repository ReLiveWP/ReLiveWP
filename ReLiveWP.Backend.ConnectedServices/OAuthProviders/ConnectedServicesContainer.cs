namespace ReLiveWP.Backend.ConnectedServices.OAuthProviders;

public interface IConnectedServicesContainer : IDictionary<string, ConnectedServiceDescription> { }

public class ConnectedServicesContainer : Dictionary<string, ConnectedServiceDescription>, IConnectedServicesContainer { }

public static class ConnectedServicesExtensions
{
    public static ConnectedServicesBuilder AddConnectedServices(this IServiceCollection services)
    {
        var container = new ConnectedServicesContainer();
        services.AddSingleton<IConnectedServicesContainer>(container);
        return new ConnectedServicesBuilder(services, container);
    }
}

public class ConnectedServicesBuilder(IServiceCollection services, IConnectedServicesContainer container)
{
    public ConnectedServicesBuilder AddConnectedService(ConnectedServiceDescription description)
    {
        container.Add(description.ServiceId, description);
        return this;
    }

    public ConnectedServicesBuilder AddConnectedService(Func<IServiceCollection, ConnectedServiceDescription> description)
    {
        return AddConnectedService(description(services));
    }
}
