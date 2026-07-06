using System.Collections.Concurrent;

namespace ReLiveWP.Services.Push.Nsp;

public class PushPresence
{
    private readonly ConcurrentDictionary<string, NspSession> sessions = new();

    public void Add(string deviceId, NspSession session) => sessions[deviceId] = session;

    public void Remove(string deviceId, NspSession session)
    {
        sessions.TryRemove(new (deviceId, session));
    }

    public bool TryGet(string deviceId, out NspSession session) => sessions.TryGetValue(deviceId, out session);

    public IEnumerable<string> LocalDevices => sessions.Keys;
}
