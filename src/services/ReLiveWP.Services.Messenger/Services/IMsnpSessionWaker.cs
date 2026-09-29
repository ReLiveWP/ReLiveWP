namespace ReLiveWP.Services.Messenger.Services;

public interface IMsnpSessionWaker
{
    void WakeSessionLater(string sessionId);
}
