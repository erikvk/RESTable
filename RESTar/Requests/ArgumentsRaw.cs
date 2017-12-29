using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTar.Requests
{
    internal class ArgumentsRaw
    {
        internal string Uri;
        internal byte[] Body;
        internal Dictionary<string, string> Headers;
        internal MimeType ContentType;
        internal MimeType[] Accept;
        internal Origin Origin;
    }
}