using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReLiveWP.Marketplace.Utilities
{
    // this is dumb
    public class UTF8StringWriter : StringWriter
    {
        private static Encoding UTF8NoBom = new UTF8Encoding(false);
        public override Encoding Encoding => UTF8NoBom;
    }
}
