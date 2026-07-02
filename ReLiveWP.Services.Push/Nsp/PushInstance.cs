namespace ReLiveWP.Services.Push.Nsp;

public sealed class PushInstance
{
    // global ID for this instance of push
    public string Id { get; } = Guid.NewGuid().ToString("N");
}
