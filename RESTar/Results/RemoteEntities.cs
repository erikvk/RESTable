using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RESTar.Results
{
    public class RemoteEntities : Content
    {

        public RemoteEntities(IRequest request, Stream stream) : base(request)
        {
            Body = stream;
        }
    }
}