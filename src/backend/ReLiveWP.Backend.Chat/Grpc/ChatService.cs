using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ReLiveWP.Backend.Chat.Data;
using ReLiveWP.Backend.Chat.Services;
using ReLiveWP.Services.Grpc.Chat;

namespace ReLiveWP.Backend.Chat.Grpc;

public class ChatService(PresenceService presence, MessageService messages) : ReLiveWP.Services.Grpc.Chat.Chat.ChatBase
{
    private const int MaxInstanceIdLength = 64;

    public override async Task<Empty> RegisterEndpoint(RegisterEndpointRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.EndpointId) || string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Address))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "endpoint_id, user_id and address are required"));

        if (request.SessionTimeoutSeconds <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "session_timeout_seconds must be positive"));

        if (!IsUsableInstanceId(request.InstanceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                $"instance_id must be at most {MaxInstanceIdLength} letters, digits, dashes or braces"));

        await presence.RegisterEndpointAsync(
            request.EndpointId,
            request.UserId,
            request.Address,
            request.Status,
            TimeSpan.FromSeconds(request.SessionTimeoutSeconds),
            request.InstanceId,
            context.CancellationToken);

        return new Empty();
    }

    public override async Task<EndpointStateResponse> SetPresence(SetPresenceRequest request, ServerCallContext context)
    {
        var state = await presence.SetPresenceAsync(request.EndpointId, request.Status, context.CancellationToken);
        return new EndpointStateResponse { State = state };
    }

    public override async Task<EndpointStateResponse> ReportActivity(ReportActivityRequest request, ServerCallContext context)
    {
        var state = await presence.ReportActivityAsync(request.EndpointId, context.CancellationToken);
        return new EndpointStateResponse { State = state };
    }

    public override async Task<Empty> EndEndpoint(EndEndpointRequest request, ServerCallContext context)
    {
        await presence.EndEndpointAsync(request.EndpointId, context.CancellationToken);
        return new Empty();
    }

    public override async Task<SendMessageResponse> SendMessage(SendMessageRequest request, ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.EndpointId) || string.IsNullOrEmpty(request.TransactionId) || string.IsNullOrEmpty(request.RecipientAddress))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "endpoint_id, transaction_id and recipient_address are required"));

        if (request.Kind == MessageKind.Unspecified)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "kind is required"));

        var outcome = await messages.SendAsync(
            request.EndpointId,
            request.TransactionId,
            request.RecipientAddress,
            request.RecipientInstanceId,
            request.Kind,
            request.Payload,
            request.StoreIfOffline,
            context.CancellationToken);

        return new SendMessageResponse { Outcome = outcome };
    }

    private static bool IsUsableInstanceId(string instanceId) =>
        instanceId.Length <= MaxInstanceIdLength
        && instanceId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '{' or '}');
}
