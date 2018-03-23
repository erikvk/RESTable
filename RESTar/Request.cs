using System;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Error.NotFound;
using RESTar.Results.Success;
using static RESTar.Methods;
using static RESTar.RESTarConfig;

namespace RESTar
{
    /// <summary>
    /// A factory class for generic request instances
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Request<T> where T : class
    {
        /// <summary>
        /// Creates a generic request using an internal trace, with a given method and optional protocol id. If the 
        /// protocol ID is null, the default protocol will be used.
        /// </summary>
        /// <param name="method">The method to perfor, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        /// <returns>A generic request instance</returns>
        public static IRequest<T> Create(Methods method, string protocolId = null, string viewName = null)
        {
            return Create(new InternalContext(), method, protocolId, viewName);
        }

        /// <summary>
        /// Creates a generic request using the given trace, method and optional protocol id. If the 
        /// protocol ID is null, the default protocol will be used.
        /// </summary>
        /// <param name="trace">A trace to use for this request, for example a Context (or another request 
        /// if this is a nested request).</param>
        /// <param name="method">The method to perfor, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        /// <returns>A generic request instance</returns>
        public static IRequest<T> Create(ITraceable trace, Methods method, string protocolId = null, string viewName = null)
        {
            if (trace is Client client)
                trace = new InternalContext(client, false);
            var resource = Resource<T>.SafeGet;
            if (resource == null)
                throw new UnknownResource(typeof(T).RESTarTypeName());
            var parameters = new RequestParameters(trace, method, resource, protocolId);
            return new Requests.Request<T>(resource, parameters);
        }
    }

    /// <summary>
    /// A factory class for non-generic request instances
    /// </summary>
    public static class Request
    {
        /// <summary>
        /// Directs the call to the Request class constructor, from a dynamic binding for the generic IResource parameter.
        /// </summary>
        private static IRequest Construct<T>(IResource<T> r, RequestParameters p) where T : class => new Requests.Request<T>(r, p);

        /// <summary>
        /// Creates a new request instance.
        /// </summary>
        /// <param name="trace">A trace to use for this request, for example a Client (or another request 
        /// if this is a nested request). If this is the first request in the trace chain, use the 
        /// factory methods of Client to create a new Client object to use as trace.</param>
        /// <param name="method">The method to perfor, for example GET</param>
        /// <param name="uri">The URI if the request</param>
        /// <param name="body">A body to use in the request</param>
        /// <param name="headers">The headers to use in the request</param>
        public static IRequest Create(ITraceable trace, Methods method, ref string uri, byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();
            if (trace == null) throw new Untraceable();
            var parameters = new RequestParameters(trace, method, ref uri, body, headers);
            parameters.Authenticate();
            if (!parameters.IsValid)
                return new InvalidParametersRequest(parameters);
            return Construct((dynamic) parameters.IResource, parameters);
        }

        /// <summary>
        /// Use this method to check the origin of an incoming OPTIONS request. This will check the contents
        /// of the Origin header against allowed CORS origins.
        /// </summary>
        /// <param name="trace">A trace to use for this request, for example a Client (or another request 
        /// if this is a nested request). If this is the first request in the trace chain, use the 
        /// factory methods of Client to create a new Client object to use as trace.</param>
        /// <param name="uri">The URI if the request</param>
        /// <param name="headers">The headers contained in the request</param>
        /// <returns></returns>
        public static IFinalizedResult CheckOrigin(ITraceable trace, ref string uri, Headers headers)
        {
            if (uri == null) throw new MissingUri();
            if (trace == null) throw new Untraceable();
            var parameters = new RequestParameters(trace, OPTIONS, ref uri, null, headers);
            var origin = parameters.Headers.Origin;
            if (!parameters.IsValid || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
                return new InvalidOrigin();
            if (AllowAllOrigins || AllowedOrigins.Contains(originUri))
                return new AcceptOrigin(origin, parameters);
            return new InvalidOrigin();
        }

        /// <summary>
        /// Validates a trace and URI and returns true if valid. If invalid, the error is returned in the 
        /// out parameter.
        /// </summary>
        /// <param name="trace">A trace to use for this request, for example a Client (or another request 
        /// if this is a nested request). If this is the first request in the trace chain, use the 
        /// factory methods of Client to create a new Client object to use as trace.</param>
        /// <param name="uri">The URI if the request</param>
        /// <param name="error">A RESTarError describing the error, or null if valid</param>
        public static bool IsValid(ITraceable trace, ref string uri, out RESTarError error)
        {
            var parameters = new RequestParameters(trace, (Methods) (-1), ref uri, null, null);
            parameters.Authenticate();
            if (parameters.Error != null)
            {
                error = RESTarError.GetError(parameters.Error);
                return false;
            }
            IRequest request = Construct((dynamic) parameters.IResource, parameters);
            if (request.IsValid)
            {
                error = null;
                return true;
            }
            error = request.GetResult() as RESTarError;
            return false;
        }
    }
}