namespace ReLiveWP.Services.Login.Models;

public record InlineLoginModel(
    string Id,
    string Mkt,
    string Lc,
    string Opid,
    string Uaid,
    string PostUrl,
    string AssetBase);
