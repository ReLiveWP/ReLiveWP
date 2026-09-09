using System.Globalization;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Identity;
using ReLiveWP.Services.FindMyPhone.Models;
using ReLiveWP.Services.Grpc.FindMyPhone;

using FindMyPhoneClient = ReLiveWP.Services.Grpc.FindMyPhone.FindMyPhone.FindMyPhoneClient;

namespace ReLiveWP.Services.FindMyPhone.Controllers;

[Authorize]
[ApiController]
[Route("Services/Device/SkyTrigger/[action]")]
[Consumes("application/xml", "text/xml")]
[Produces("application/xml")]
public class SkyTriggerController(FindMyPhoneClient findMyPhone, ILogger<SkyTriggerController> logger) : ControllerBase
{
    // bound the backend hop so a stalled Skybox can't hang the device's status POST
    private static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(10);

    private const int CodeOk = 0;
    private const int CodeFailed = 1;

    private string? DeviceGuid => Request.Headers["X-WM-DeviceId"].FirstOrDefault();

    [HttpPost]
    [ActionName("RegisterChannel")]
    public async Task<RegisterChannelResponseModel> RegisterChannelAsync([FromBody] RegisterChannelRequestModel model)
    {
        var userId = User.Id();
        var deviceGuid = DeviceGuid;
        if (string.IsNullOrEmpty(deviceGuid))
        {
            FindMyPhoneMetrics.RecordDeviceResponse("RegisterChannel", CodeFailed);
            return new RegisterChannelResponseModel() { ResponseCode = CodeFailed, ResponseMessage = "Missing X-WM-DeviceId header" };
        }

        var request = new RegisterChannelRequest() { UserId = userId, DeviceGuid = deviceGuid };
        if (!string.IsNullOrEmpty(model.NotificationUri))
            request.NotificationUri = model.NotificationUri;

        var resp = await findMyPhone.RegisterChannelAsync(request);
        FindMyPhoneMetrics.RecordDeviceResponse("RegisterChannel", resp.Code);
        return new RegisterChannelResponseModel()
        {
            ResponseCode = resp.Code,
            ResponseMessage = resp.Message
        };
    }

    [HttpPost]
    [ActionName("UpdateCommandStatus")]
    public async Task<UpdateCommandStatusResponseModel> UpdateCommandStatus([FromBody] UpdateCommandStatusRequestModel model)
    {
        var code = await ReportAsync(DeviceGuid, model);
        FindMyPhoneMetrics.RecordDeviceResponse("UpdateCommandStatus", code);

        return new UpdateCommandStatusResponseModel()
        {
            ResponseCode = code,
            ResponseMessage = code == CodeOk ? "OK" : "Failed"
        };
    }

    [HttpPost]
    [ActionName("UpdateCommandStatusBatched")]
    public async Task<UpdateCommandStatusBatchedResponseModel> UpdateCommandStatusBatched([FromBody] UpdateCommandStatusBatchedRequestModel model)
    {
        var deviceGuid = DeviceGuid;
        FindMyPhoneMetrics.CommandStatusBatchSize.Record(model.Requests.Count);

        var responses = new List<CommandStatusResponseModel>(model.Requests.Count);
        foreach (var request in model.Requests)
        {
            var code = await ReportAsync(deviceGuid, request);
            FindMyPhoneMetrics.RecordDeviceResponse("UpdateCommandStatusBatched", code);
            responses.Add(new CommandStatusResponseModel() { ResponseCode = code, RequestId = request.RequestId });
        }

        return new UpdateCommandStatusBatchedResponseModel() { Responses = responses };
    }

    private async Task<int> ReportAsync(string? deviceGuid, UpdateCommandStatusRequestModel model)
    {
        if (string.IsNullOrEmpty(deviceGuid))
            return CodeFailed;

        try
        {
            await findMyPhone.ReportCommandStatusAsync(ToReport(deviceGuid, model), deadline: DateTime.UtcNow.Add(ReportTimeout));
            return CodeOk;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "Command status report {RequestId} for {DeviceId} failed", model.RequestId, deviceGuid);
            return CodeFailed;
        }
    }

    private static ReportCommandStatusRequest ToReport(string deviceGuid, UpdateCommandStatusRequestModel model) => new()
    {
        DeviceGuid = deviceGuid,
        RequestId = ParseId(model.RequestId),
        Result = ParseResult(model.Result),
        Final = model.Final,
        Data = model.Data ?? ""
    };

    private static uint ParseId(string value) =>
        uint.TryParse(value, out var id) ? id : 0;

    // Result comes as a hex HRESULT string, e.g. "0x00000000".
    private static uint ParseResult(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        var digits = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
