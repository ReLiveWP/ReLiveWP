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
        public IActionResult Location()
        {
            var str = new UTF8StringWriter();
            var resp = new LocationResponse();

            var serialiser = new XmlSerializer(typeof(LocationResponse), "http://schemas.zune.net/commerce/2009/01");
            serialiser.Serialize(str, resp);

            return Content(str.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
