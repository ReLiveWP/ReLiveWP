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

    [HttpPost]
    [ActionName("RegisterChannel")]
    public async Task<RegisterChannelResponseModel> RegisterChannelAsync([FromBody] RegisterChannelRequestModel model)
    {
        var userId = User.Id();
        var deviceGuid = Request.Headers["X-WM-DeviceId"][0];

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
    public UpdateCommandStatusResponseModel UpdateCommandStatus([FromBody] UpdateCommandStatusRequestModel model)
    {
        return new UpdateCommandStatusResponseModel()
        {
            ResponseCode = 0,
            ResponseMessage = "OK"
        };
    }

    [HttpPost]
    [ActionName("UpdateCommandStatusBatched")]
    public UpdateCommandStatusBatchedResponseModel UpdateCommandStatusBatched([FromBody] UpdateCommandStatusBatchedRequestModel model)
    {
        return new UpdateCommandStatusBatchedResponseModel()
        {
            Responses = model.Requests
                .Select(r => new CommandStatusResponseModel() { ResponseCode = 0, RequestId = r.RequestId })
                .ToList()
        };
    }
}
