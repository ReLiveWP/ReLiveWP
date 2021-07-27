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
        public GetCreditCardResponse GetCreditCards()
        {
            return new GetCreditCardResponse();
        }

        // map instance id -> app id (and i guess music id)
        public AcquisitionInfoResponse VerifyPastPurchase()
        {
            var resp = new AcquisitionInfoResponse();
            var info = new AppAcquisitionInfoOutput() { AppId = new Guid("1572f5ac-5eca-df11-9eae-00237de2db9e") };
            resp.AppAcquisitionInfoOutputs = new List<AppAcquisitionInfoOutput>();
            resp.AppAcquisitionInfoOutputs.Add(info);

            return resp;
        }

        public PurchaseResponse PurchaseAsset()
        {
            return new PurchaseResponse();
        }
    }
}
