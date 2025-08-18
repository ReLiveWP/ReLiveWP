namespace ReLiveWP.Backend.Identity.ConnectedServices;

public interface IConnectedServicesContainer : IDictionary<string, ConnectedServiceDescription> { }

public class ConnectedServicesContainer : Dictionary<string, ConnectedServiceDescription>, IConnectedServicesContainer
{

}

public static class ConnectedServicesExtensions
{
    public static ConnectedServicesBuilder AddConnectedServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var container = new ConnectedServicesContainer();
        services.AddSingleton<IConnectedServicesContainer>(container);

        return new ConnectedServicesBuilder(services, container);
    }
}

public class ConnectedServicesBuilder(IServiceCollection services, IConnectedServicesContainer container)
{
    public void AddConnectedService(ConnectedServiceDescription description)
    {
        container.Add(description.ServiceId, description);
    }

    public void AddConnectedService(Func<IServiceCollection, ConnectedServiceDescription> description)
    {
        AddConnectedService(description(services));
    }
}
