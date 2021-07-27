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
    [Route("/billing/{action}")]
    [Route("/{version}/billing/{action}")]
    public class BillingController : Controller
    {
        public IActionResult GetCreditCards()
        {
            var str = new UTF8StringWriter();
            var resp = new GetCreditCardResponse();
            var serialiser = new XmlSerializer(typeof(GetCreditCardResponse), "http://schemas.zune.net/commerce/2009/01");
            serialiser.Serialize(str, resp);

            return Content(str.ToString(), "application/xml", Encoding.UTF8);
        }

        // map instance id -> app id (and i guess music id)
        public IActionResult VerifyPastPurchase()
        {
            var str = new UTF8StringWriter();
            var resp = new AcquisitionInfoResponse();
            var info = new AppAcquisitionInfoOutput() { AppId = new Guid("1572f5ac-5eca-df11-9eae-00237de2db9e") };
            resp.AppAcquisitionInfoOutputs = new List<AppAcquisitionInfoOutput>();
            resp.AppAcquisitionInfoOutputs.Add(info);

            var serialiser = new XmlSerializer(typeof(AcquisitionInfoResponse), "http://schemas.zune.net/commerce/2009/01");
            serialiser.Serialize(str, resp);

            return Content(str.ToString(), "application/xml", Encoding.UTF8);
        }

        public IActionResult PurchaseAsset()
        {
            var str = new UTF8StringWriter();
            var resp = new PurchaseResponse();

            var serialiser = new XmlSerializer(typeof(PurchaseResponse), "http://schemas.zune.net/commerce/2009/01");
            serialiser.Serialize(str, resp);

            return Content(str.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
