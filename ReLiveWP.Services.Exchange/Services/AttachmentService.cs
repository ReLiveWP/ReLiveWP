using Grpc.Core;
using ReLiveWP.Services.Grpc.Mailbox;

namespace ReLiveWP.Services.Exchange.Services;

public enum AttachmentResolveStatus
{
    Success,
    NotFound,       // FileReference doesn't resolve to anything the caller owns
    InvalidRange,   // malformed or out-of-bounds Range
    Empty,          // zero-byte attachment
    IoFailure,      // non-NotFound RPC failure talking to the mailbox store
}

public sealed record AttachmentResult(
    string DisplayName,
    string ContentType,
    byte[] Content,
    int TotalSize,
    int RangeStart,
    int RangeEnd)
{
    public string RangeHeader => $"{RangeStart}-{RangeEnd}";
}

public readonly record struct AttachmentResolveResult(AttachmentResolveStatus Status, AttachmentResult? Data);

// resolves a FileReference (+ optional byte Range) to attachment bytes; shared by ItemOperations Fetch and GetAttachment
public class AttachmentService(MailboxStore.MailboxStoreClient mailbox, ILogger<AttachmentService> logger)
{
    public async Task<AttachmentResolveResult> ResolveAsync(
        string userId, string? fileReference, string? range, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(fileReference))
            return new(AttachmentResolveStatus.NotFound, null);

        AttachmentContent content;
        try
        {
            content = await mailbox.GetAttachmentContentAsync(
                new GetAttachmentContentRequest { UserId = userId, AttachmentId = fileReference },
                cancellationToken: ct);
        }
        catch (RpcException e) when (e.StatusCode == StatusCode.NotFound)
        {
            return new(AttachmentResolveStatus.NotFound, null);
        }
        catch (RpcException e)
        {
            logger.LogWarning(e, "GetAttachmentContent RPC failure for {FileReference}", fileReference);
            return new(AttachmentResolveStatus.IoFailure, null);
        }

        var bytes = content.Content.ToByteArray();
        if (bytes.Length == 0)
            return new(AttachmentResolveStatus.Empty, null);

        int start = 0, end = bytes.Length - 1;
        if (!string.IsNullOrEmpty(range))
        {
            if (!TryParseRange(range, bytes.Length, out start, out end))
                return new(AttachmentResolveStatus.InvalidRange, null);
        }

        var slice = start == 0 && end == bytes.Length - 1 ? bytes : bytes[start..(end + 1)];

        return new(AttachmentResolveStatus.Success, new AttachmentResult(
            content.HasDisplayName ? content.DisplayName : fileReference,
            content.HasContentType ? content.ContentType : "application/octet-stream",
            slice,
            bytes.Length,
            start,
            end));
    }

    // "m-n", zero-indexed, inclusive; m<=n, both within [0, length)
    private static bool TryParseRange(string range, int length, out int start, out int end)
    {
        start = 0;
        end = 0;

        var dash = range.IndexOf('-');
        if (dash <= 0 || dash == range.Length - 1)
            return false;

        if (!int.TryParse(range.AsSpan(0, dash), out start) || !int.TryParse(range.AsSpan(dash + 1), out end))
            return false;

        if (start < 0 || end < start || start >= length)
            return false;

        // best-effort: server clamps rather than rejecting an over-long upper bound
        end = Math.Min(end, length - 1);
        return true;
    }
}
