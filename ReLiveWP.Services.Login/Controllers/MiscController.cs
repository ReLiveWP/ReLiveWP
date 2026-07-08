using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Services.Login.Controllers;

public class MiscController : Controller
{
    [Route("/getuserrealm.srf")]
    public IActionResult Index([FromQuery] string login)
    {
        return Content(@$"<RealmInfo Success=""true"">
<State>4</State>
<UserState>1</UserState>
<Login>{HttpUtility.HtmlEncode(login)}</Login>
</RealmInfo>", "text/xml");
    }
}
