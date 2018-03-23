using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using static RESTar.Method;

namespace RESTar
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="JObject" />
    /// <summary>
    /// The Echo resource is a test and utility resource that returns the 
    /// request conditions as an object.
    /// </summary>
    [RESTar(GET, AllowDynamicConditions = true, Description = description)]
    public class Echo : JObject, ISelector<Echo>
    {
        private const string description = "The Echo resource is a test and utility resource that " +
                                           "returns the request conditions as an object.";

        private Echo()
        {
        }

        private Echo(object thing) : base(thing)
        {
        }

        /// <inheritdoc />
        public IEnumerable<Echo> Select(IRequest<Echo> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var echo = new[]
            {
                new Echo(request.Conditions.Select(c => new JProperty(c.Key, c.Value)))
            };
            Deflection.Dynamic.TypeCache.ClearTermsFor<Echo>();
            return echo;
        }
    }
}