using System.Globalization;
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
public class SkyTriggerController(FindMyPhoneClient findMyPhone) : ControllerBase
{
    // bound the backend hop so a stalled Skybox can't hang the device's status POST
    private static readonly TimeSpan ReportTimeout = TimeSpan.FromSeconds(10);


    [HttpPost]
    [ActionName("RegisterChannel")]
    public async Task<RegisterChannelResponseModel> RegisterChannelAsync([FromBody] RegisterChannelRequestModel model)
    {
        var userId = User.Id();
        var deviceGuid = Request.Headers["X-WM-DeviceId"][0]!;
        if (deviceGuid is null)
            return new RegisterChannelResponseModel() { ResponseCode = 1, ResponseMessage = "Missing X-WM-DeviceId header" };

        var request = new RegisterChannelRequest() { UserId = userId, DeviceGuid = deviceGuid };
        if (!string.IsNullOrEmpty(model.NotificationUri))
            request.NotificationUri = model.NotificationUri;

        var resp = await findMyPhone.RegisterChannelAsync(request);
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
        var deviceGuid = Request.Headers["X-WM-DeviceId"][0]!;
        if (deviceGuid is null)
            return new UpdateCommandStatusResponseModel() { ResponseCode = 1, ResponseMessage = "Missing X-WM-DeviceId header" };

        await findMyPhone.ReportCommandStatusAsync(ToReport(deviceGuid, model), deadline: DateTime.UtcNow.Add(ReportTimeout));

        return new UpdateCommandStatusResponseModel()
        {
            ResponseCode = 0,
            ResponseMessage = "OK"
        };
    }

    [HttpPost]
    [ActionName("UpdateCommandStatusBatched")]
    public async Task<UpdateCommandStatusBatchedResponseModel> UpdateCommandStatusBatched([FromBody] UpdateCommandStatusBatchedRequestModel model)
    {
        var deviceGuid = Request.Headers["X-WM-DeviceId"][0]!;
        foreach (var request in model.Requests)
            await findMyPhone.ReportCommandStatusAsync(ToReport(deviceGuid, request), deadline: DateTime.UtcNow.Add(ReportTimeout));

        return new UpdateCommandStatusBatchedResponseModel()
        {
            Responses = model.Requests
                .Select(r => new CommandStatusResponseModel() { ResponseCode = 0, RequestId = r.RequestId })
                .ToList()
        };
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
