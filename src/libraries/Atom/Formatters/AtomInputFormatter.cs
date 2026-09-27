using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Atom.Formatters;

public class AtomInputFormatter : XmlSerializerInputFormatter
{
    public AtomInputFormatter(MvcOptions options) : base(options)
    {
        SupportedMediaTypes.Add("application/atom+xml");
    }
}
