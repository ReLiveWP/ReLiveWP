namespace ReLiveWP.Backend.DeviceRegistration.Services;

public class PermissiveActivationCodeValidator : IActivationCodeValidator
{
    public ActivationCodeCheck CheckActivationCode(string activationCode) => new(true, null);
}
