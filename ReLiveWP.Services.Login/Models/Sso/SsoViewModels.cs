using System.ComponentModel.DataAnnotations;

namespace ReLiveWP.Services.Login.Models.Sso;

public class SignInViewModel
{
    [Required]
    public string PendingId { get; set; } = default!;

    [Required(ErrorMessage = "Enter your ReLive ID.")]
    [Display(Name = "ReLive ID")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    // defaults false so an unchecked box, which posts nothing at all, binds false without needing
    // a hidden companion input. the sign-in page ticks it explicitly on first render.
    public bool RememberMe { get; set; }

    public string? Error { get; set; }
}

public record SsoErrorViewModel(string Title, string Detail);

public record TokenRequestModel(
    string Code,
    string ClientId,
    string RedirectUri,
    string? CodeVerifier);
