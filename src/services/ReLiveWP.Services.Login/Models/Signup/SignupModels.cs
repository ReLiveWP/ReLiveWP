namespace ReLiveWP.Services.Login.Models.Signup;

public record SigninNameAvailabilityModel(bool IsAvailable)
{
    public string Status => IsAvailable ? "available" : "unavailable";
}
