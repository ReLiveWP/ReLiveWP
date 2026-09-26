namespace ReLiveWP.Backend.ConnectedServices.Services;

public interface IConnectedServicesContainer : IDictionary<string, ConnectedServiceDescription> { }

public class ConnectedServicesContainer : Dictionary<string, ConnectedServiceDescription>, IConnectedServicesContainer { }

public static class ConnectedServicesExtensions
{
    public static ConnectedServicesBuilder AddConnectedServices(this IServiceCollection services, IConfiguration configuration)
    {
        var container = new ConnectedServicesContainer();
        services.AddSingleton<IConnectedServicesContainer>(container);
        return new ConnectedServicesBuilder(services, configuration, container);
    }
}

public class ConnectedServicesBuilder(IServiceCollection services, IConfiguration configuration, IConnectedServicesContainer container)
{
    public ConnectedServicesBuilder AddConnectedService(ConnectedServiceDescription description)
    {
        description.IsEnabled = configuration.GetValue($"ConnectedServices:{description.ServiceId}:Enabled", description.IsEnabled);
        container.Add(description.ServiceId, description);
        return this;
    }

    public ConnectedServicesBuilder AddConnectedService(Func<IServiceCollection, ConnectedServiceDescription> description)
    {
        return AddConnectedService(description(services));
    }
}
