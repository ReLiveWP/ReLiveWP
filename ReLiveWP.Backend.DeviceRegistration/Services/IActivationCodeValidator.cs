namespace ReLiveWP.Backend.DeviceRegistration.Services;

public readonly record struct ActivationCodeCheck(bool IsValid, int? Serial);

public interface IActivationCodeValidator
{
    ActivationCodeCheck CheckActivationCode(string activationCode);
}
