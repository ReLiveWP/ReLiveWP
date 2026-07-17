namespace ReLiveWP.Services.Exchange.Models;

public enum EasCommand : byte
{
    Sync = 0,
    SendMail = 1,
    SmartForward = 2,
    SmartReply = 3,
    GetAttachment = 4,
    FolderSync = 9,
    FolderCreate = 10,
    FolderDelete = 11,
    FolderUpdate = 12,
    MoveItems = 13,
    GetItemEstimate = 14,
    MeetingResponse = 15,
    Search = 16,
    Settings = 17,
    Ping = 18,
    ItemOperations = 19,
    Provision = 20,
    ResolveRecipients = 21,
    ValidateCert = 22,
}
