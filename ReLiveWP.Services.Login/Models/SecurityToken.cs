
namespace ReLiveWP.Services.Login.Models;

public record SecurityTokenModel(
    string ServiceTarget, 
    string Token,
    string TokenType, 
    DateTimeOffset Created,
    DateTimeOffset Expires);
