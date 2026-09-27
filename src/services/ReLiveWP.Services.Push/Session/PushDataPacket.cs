namespace ReLiveWP.Services.Push.Session;

public readonly record struct PushDataPacket(byte[] Payload, string Binding);
