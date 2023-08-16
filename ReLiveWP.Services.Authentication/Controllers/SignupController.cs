using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ReLiveWP.Backend.Identity;
using System.Xml;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Authentication.Controllers
{
    [Route("/ws/SignupService.asmx")]
    [ApiController]
    public class SignupController : ControllerBase
    {
        private readonly ILogger<SignupController> _logger;
        private readonly Login.LoginClient _loginClient;

        private const string SOAP_NS = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string SIGNUP_NS = "http://schemas.microsoft.com/Passport/Mobile/WebServices/Signup/V1";

        public SignupController(ILogger<SignupController> logger,
                                Login.LoginClient loginClient)
        {
            _logger = logger;
            _loginClient = loginClient;
        }

        [HttpPost]
        [Consumes("text/xml")]
        public async Task<IActionResult> PostAsync()
        {
            var reader = new StreamReader(Request.Body);
            // what the actual fuck.
            var data = (await reader.ReadToEndAsync()).TrimEnd('\0');
            var document = new XmlDocument();
            document.LoadXml(data);

            var nsManager = new XmlNamespaceManager(document.NameTable);
            nsManager.AddNamespace("signup", SIGNUP_NS);

            var signinName = document.SelectSingleNode("//signup:SigninName", nsManager)?.InnerText;
            if (signinName == null)
                return BadRequest();

            var resp = await _loginClient.UserExistsAsync(new UserExistsRequest() { Username = signinName });

            var doc = new XmlDocument();
            var root = doc.AppendChild(doc.CreateElement("soap", "Envelope", SIGNUP_NS));
            var body = root.AppendChild(doc.CreateElement("soap", "Body", SIGNUP_NS));

            var result = doc.CreateElement("Result", "");
            result.SetAttribute("status", resp.Exists ? "unavailable" : "available");

            result.AppendChild(doc.CreateElement("AltNames", ""));
            body.AppendChild(result);

            return Ok(doc);
        }
    }
}
