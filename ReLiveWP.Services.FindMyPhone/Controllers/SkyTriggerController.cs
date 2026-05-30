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
public class SkyTriggerController(FindMyPhoneClient findMyPhone) : ControllerBase
{

    [HttpPost]
    [ActionName("RegisterChannel")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/xml")]
    public async Task<RegisterChannelResponseModel> RegisterChannelAsync([FromBody] RegisterChannelRequestModel model)
    {
        return new RegisterChannelResponseModel()
        {
            ResponseCode = 0,
            ResponseMessage = "OK"
        };
    }
}
