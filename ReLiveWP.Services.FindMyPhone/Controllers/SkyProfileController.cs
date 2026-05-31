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
[Route("Services/Device/SkyProfile/[action]")]
public class SkyProfileController(FindMyPhoneClient findMyPhone) : ControllerBase
{
    [HttpPost]
    [ActionName("RegisterDevice")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/xml")]
    public async Task<RegisterDeviceResponseModel> RegisterDeviceAsync([FromBody] RegisterDeviceRequestModel model)
    {
        var userId = User.Id();
        var dict = new Dictionary<string, string>();
        foreach (var item in model.Properties)
            dict[item.Name] = item.Value;

        if (!dict.TryGetValue("SkyProfile.WinMoDeviceGuid", out var deviceGuid))
            throw new BadHttpRequestException("Missing SkyProfile.WinMoDeviceGuid");

        var request = new RegisterDeviceRequest()
        {
            UserId = userId,
            DeviceGuid = deviceGuid
        };

        request.DeviceProps.Add(dict);

        var response = await findMyPhone.RegisterDeviceAsync(request);
        return new RegisterDeviceResponseModel()
        {
            ResponseCode = response.Code,
            EnabledForMarket = response.Enabled,
            ResponseMessage = response.Message
        };
    }

    [HttpPost]
    [ActionName("UpdateDeviceInfo")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/xml")]
    public async Task<UpdateDeviceInfoResponseModel> RegisterDeviceAsync([FromBody] UpdateDeviceInfoRequestModel model)
    {
        var userId = User.Id();
        var deviceGuid = Request.Headers["X-WM-DeviceId"][0];

        var dict = new Dictionary<string, string>();
        foreach (var item in model.Properties)
            dict[item.Name] = item.Value;

        var request = new UpdateDeviceInfoRequest()
        {
            UserId = userId,
            DeviceGuid = deviceGuid
        };

        request.DeviceProps.Add(dict);

        var response = await findMyPhone.UpdateDeviceInfoAsync(request);
        return new UpdateDeviceInfoResponseModel()
        {
            ResponseCode = response.Code,
            ResponseMessage = response.Message
        };
    }
}
