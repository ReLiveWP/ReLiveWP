using System;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;

namespace Atom
{
    /// <remarks>
    /// The default value is <see cref="TimeSpan.MinValue"/>. This is a value
    /// type and has the same hash code as <see cref="TimeSpan"/>! Implicit
    /// assignment from <see cref="TimeSpan"/> is neither implemented nor desirable!
    /// </remarks>
    public struct SerializableTimeSpan : IXmlSerializable
    {
        private TimeSpan value;

        public SerializableTimeSpan(TimeSpan value)
        {
            this.value = value;
        }

        public static implicit operator SerializableTimeSpan(TimeSpan value)
        {
            return new SerializableTimeSpan(value);
        }

        public static implicit operator TimeSpan(SerializableTimeSpan instance)
        {
            return instance.value;
        }

        public static bool operator ==(SerializableTimeSpan a, SerializableTimeSpan b)
        {
            return a.value == b.value;
        }

        public static bool operator !=(SerializableTimeSpan a, SerializableTimeSpan b)
        {
            return a.value != b.value;
        }

        public static bool operator <(SerializableTimeSpan a, SerializableTimeSpan b)
        {
            return a.value < b.value;
        }

        public static bool operator >(SerializableTimeSpan a, SerializableTimeSpan b)
        {
            return a.value > b.value;
        }

        public override bool Equals(object o)
        {
            if (o is SerializableTimeSpan span)
                return value.Equals(span.value);
            else if (o is TimeSpan span1)
                return value.Equals(span1);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var text = reader.ReadContentAsString();
            value = XmlConvert.ToTimeSpan(text);
        }

        public override string ToString()
        {
            return value.ToString();
        }

        public string ToString(string format)
        {
#if NET40_OR_GREATER || NETSTANDARD
            return value.ToString(format);
#else
            return ToString();
#endif
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteString(XmlConvert.ToString(value));
        }
    }
}
