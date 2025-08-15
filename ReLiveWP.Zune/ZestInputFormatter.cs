using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace ReLiveWP.Zune;

// so, zune is kinda dumb, and will often send XML data as application/x-www-form-urlencoded.
// this input formatter is designed to handle that.
public class ZestInputFormatter : XmlSerializerInputFormatter
{
    public ZestInputFormatter(MvcOptions options) : base(options)
    {
        SupportedMediaTypes.Add("application/x-www-form-urlencoded");
    }
}
