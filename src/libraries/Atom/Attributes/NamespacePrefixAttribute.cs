using System;
using System.Collections.Generic;
using System.Text;

namespace Atom.Attributes
{
    /// <summary>
    /// Allows specifying XML namespace prefixes (xmlns:blah) in
    /// XML responses created by <see cref="ZestOutputFormatter"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public sealed class NamespacePrefixAttribute : Attribute
    {
        public string Prefix { get; }
        public string Namespace { get; }

        public NamespacePrefixAttribute(string prefix, string ns)
        {
            Prefix = prefix;
            Namespace = ns;
        }
    }
}
