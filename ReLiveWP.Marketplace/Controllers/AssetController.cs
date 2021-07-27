using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Marketplace.Data;
using ReLiveWP.Marketplace.Utilities;

namespace ReLiveWP.Marketplace.Controllers
{
    [Route("/asset/{action}")]
    [Route("/{language}/asset/{action}")]
    public class AssetController : Controller
    {
        public LocationResponse Location()
        {
            var resp = new LocationResponse();
            var output = new LocationOutput()
            {
                KeyID = Guid.NewGuid(),
                DownloadUrl = "http://marketplaceedgeservice.windowsphone.com/ReLiveWP.xap",
                DownloadAcknowledgementUrl = "http://marketplaceedgeservice.windowsphone.com/asset/acknowledge"
            };

            resp.LocationOutputs.Add(output);
            return resp;
        }
    }
}
