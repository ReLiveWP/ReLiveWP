using ReLiveWP.Services.Messenger.Services;

namespace ReLiveWP.Services.Messenger.Tests;

internal sealed class RecordingSessionWaker : IMsnpSessionWaker
{
    public List<string> Woken { get; } = [];

    public void WakeSessionLater(string sessionId) => Woken.Add(sessionId);
}
