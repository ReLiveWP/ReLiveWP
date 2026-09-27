
namespace ReLiveWP.Services.Login.Models;

public record SecurityTokenModel(
    string ServiceTarget,
    string Token,
    string TokenType,
    DateTimeOffset Created,
    DateTimeOffset Expires,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpires = null);
