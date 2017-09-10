using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Methods;

// ReSharper disable UnusedParameter.Local

namespace RESTar
{
    /// <summary>
    /// The Counter resource is an operations resource that calculates the number 
    /// of entities returned from GET request, the URI of which is included in the 
    /// request data.
    /// </summary>
    [RESTar(GET, Singleton = true, Description = description)]
    public class Counter : ISelector<Counter>
    {
        private const string description = "The Counter resource is an operations resource that calculates " +
                                           "the number of entities returned from GET request, the URI of which " +
                                           "is included in the request data.";

        /// <summary>
        /// The result of the count operation 
        /// </summary>
        public long Count { get; private set; }

        /// <inheritdoc />
        public IEnumerable<Counter> Select(IRequest<Counter> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var args = new Args(JObject.Parse(request.Body).SafeGetNoCase("uri").Value<string>());
            IRequest innerRequest = MakeRequest((dynamic) args.IResource, args);
            var rights = RESTarConfig.AuthTokens[request.AuthToken];
            if (rights.SafeGet(innerRequest.Resource)?.Contains(GET) != true)
                throw new ForbiddenException(AbortedCount, $"Access denied to resource '{innerRequest.Resource.Name}'");
            return new[] {new Counter {Count = Evaluate((dynamic) innerRequest)}};
        }

        private static Request<T> MakeRequest<T>(IResource<T> _, Args args) where T : class
        {
            var request = new Request<T>();
            var conditions = Condition<T>.Parse(args.Conditions, request.Resource);
            request.Conditions = conditions;
            return request;
        }

        private static long Evaluate<T>(IRequest<T> request) where T : class => Evaluators<T>.COUNT(request);
    }
}