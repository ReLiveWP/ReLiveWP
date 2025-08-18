namespace ReLiveWP.Backend.Identity.Services;

public interface IClientAssertionService
{
    string CreateClientAssertion(string clientId, string issuer);
}