#if DEBUG

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTar
{
    /// <summary>
    /// A resource for creating arbitrary aggregated groupings from multiple
    /// internal requests.   
    /// </summary>
    [RESTar(Methods.GET, Description = description)]
    public class Grouper : JObject, ISelector<Grouper>
    {
        private const string description = "Create arbitrary aggregated groupings from multiple internal requests";

        /// <inheritdoc />
        public IEnumerable<Grouper> Select(IRequest<Grouper> request)
        {
            throw new NotImplementedException();
        }
    }
}

#endif