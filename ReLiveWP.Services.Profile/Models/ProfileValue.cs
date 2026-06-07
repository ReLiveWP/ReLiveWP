using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ReLiveWP.Services.Profile.Models;

public class ProfileValue : IXmlSerializable
{
    public bool IsNil { get; set; }
    public string? XsdType { get; set; }
    public string? Value { get; set; }

    public ProfileValue()
    {
        IsNil = true;
    }

    public ProfileValue(long v)
    {
        XsdType = "long";
        Value = v.ToString();
    }

    public ProfileValue(string str)
    {
        XsdType = "string";
        Value = str ?? "";
    }

    public ProfileValue(DateTime dt)
    {
        XsdType = "dateTime";
        Value = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public XmlSchema? GetSchema() => null;

    public void ReadXml(XmlReader reader)
    {
        var nil = reader.GetAttribute("nil", ProfileConstants.Xsi);
        var type = reader.GetAttribute("type", ProfileConstants.Xsi);
        IsNil = nil is "true" or "1";
        if (type != null)
        {
            var i = type.IndexOf(':');
            XsdType = i >= 0 ? type[(i + 1)..] : type;
        }

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        Value = reader.ReadElementContentAsString();
    }

    public void WriteXml(XmlWriter writer)
    {
        if (IsNil)
        {
            writer.WriteAttributeString("nil", ProfileConstants.Xsi, "true");
            return;
        }

        if (XsdType != null)
        {
            // declare an xsd prefix so xsi:type resolves to a qualified name
            writer.WriteAttributeString("xmlns", "t", null, ProfileConstants.Xsd);
            writer.WriteAttributeString("type", ProfileConstants.Xsi, "t:" + XsdType);
        }

        if (Value != null)
            writer.WriteString(Value);
    }
}
