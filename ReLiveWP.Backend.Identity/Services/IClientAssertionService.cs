namespace ReLiveWP.Backend.Identity.Services;

public interface IClientAssertionService
{
    Task<string> CreateClientAssertionAsync(string clientId, string issuer);
}