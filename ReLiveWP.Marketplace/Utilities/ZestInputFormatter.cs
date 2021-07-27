using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Server.IIS.Core;
using System.Xml.Serialization;

namespace ReLiveWP.Marketplace.Utilities
{
    public class ZestInputFormatter : XmlSerializerInputFormatter
    {
        public ZestInputFormatter(MvcOptions options) : base(options)
        {
            // so, zune is kinda dumb, and will often send XML data as application/x-www-form-urlencoded.
            // this input formatter is designed to handle that.
            SupportedMediaTypes.Add("application/x-www-form-urlencoded");
        }

        //public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        //{
        //    var request = context.HttpContext.Request;
        //    using var xmlReader = new StreamReader(request.Body);

        //    try
        //    {
        //        var serializer = new XmlSerializer(context.ModelType, "http://schemas.zune.net/commerce/2009/01");
        //        var deserializedObject = serializer.Deserialize(xmlReader);
        //        return await InputFormatterResult.SuccessAsync(deserializedObject);
        //    }
        //    catch (InvalidOperationException exception) when (exception.InnerException is FormatException || exception.InnerException is XmlException)
        //    {
        //        throw new InputFormatterException("ErrorDeserializingInputData", exception);
        //    }
        //}
    }
}
