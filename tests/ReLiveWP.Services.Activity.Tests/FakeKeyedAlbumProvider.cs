using ReLiveWP.ServiceDefaults.Media;
using ReLiveWP.Services.Activity.Providers;
using ReLiveWP.Services.Grpc;

namespace ReLiveWP.Services.Activity.Tests;

public class FakeKeyedAlbumProvider : SocialAlbumProviderBase
{
    public const string Token = "keyed";

    public const string AmyIdentity = "https://keyed.example/users/amy";
    public const string AmyKey = "keyed.example+amy";

    public const string BenIdentity = "https://keyed.example/users/ben";

    public Dictionary<string, string> KeysByIdentity { get; } = new() { [AmyIdentity] = AmyKey };

    public Uri? MediaSource { get; set; } = new("https://media.keyed.example/amy/photo.png");

    public List<Connection?> AlbumConnections { get; } = [];

    public override string Provider => Token;

    public override bool IsValidExternalId(string externalId) => externalId.Contains('+');

    public override bool IsValidMediaId(string mediaId) => mediaId.All(char.IsAsciiLetterOrDigit);

    public override Task<string?> GetAlbumKeyAsync(string identityId, CancellationToken ct = default)
        => Task.FromResult(KeysByIdentity.GetValueOrDefault(identityId));

    public override Task<string?> FindIdentityAsync(string albumKey, CancellationToken ct = default)
        => Task.FromResult(KeysByIdentity.FirstOrDefault(pair => pair.Value == albumKey).Key);

    public override Task<IReadOnlyList<SocialAlbum>> GetAlbumsAsync(
        string userId, IEnumerable<Connection> connections, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<SocialAlbum>>([]);

    public override Task<SocialAlbumContents> GetAlbumAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default)
    {
        AlbumConnections.Add(connection);
        return Task.FromResult(new SocialAlbumContents([], "amy@keyed.example"));
    }

    public override Task<string?> GetHandleAsync(
        string userId, string externalId, Connection? connection, CancellationToken ct = default)
        => Task.FromResult<string?>("amy@keyed.example");

    public int ResolveCalls { get; private set; }

    public override Task<Uri?> ResolveMediaSourceAsync(string albumKey, string mediaId, MediaSize size,
                                                       CancellationToken ct = default)
    {
        ResolveCalls++;
        return Task.FromResult(MediaSource);
    }
}
