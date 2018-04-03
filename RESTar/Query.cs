using RESTar.Auth;
using RESTar.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar
{
    /// <summary>
    /// A factory class for generic request instances
    /// </summary>
    /// <typeparam name="T">The resource type to create a request against. This must be a registered RESTar 
    /// resource type, otherwise Create() will throw an UnknownResource exception</typeparam>
    public static class Request<T> where T : class
    {
        /// <summary>
        /// Creates a generic request using an internal trace, with a given method and optional protocol id. If the 
        /// protocol ID is null, the default protocol will be used.
        /// </summary>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        /// <returns>A generic request instance</returns>
        public static IRequest<T> Create(Method method, string protocolId = null, string viewName = null)
        {
            var resource = Resource<T>.SafeGet;
            if (resource == null)
                throw new UnknownResource(typeof(T).RESTarTypeName());
            var parameters = new RequestParameters(new InternalContext(), method, resource, protocolId);
            return new Requests.Request<T>(resource, parameters);
        }

        /// <summary>
        /// Creates a generic request using the given trace, method and optional protocol id. If the 
        /// protocol ID is null, the default protocol will be used.
        /// </summary>
        /// <param name="trace">A trace to use for this request, for example a Context (or another request 
        /// if this is a nested request).</param>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        /// <returns>A generic request instance</returns>
        public static IRequest<T> Create(ITraceable trace, Method method, string protocolId = null, string viewName = null)
        {
            var resource = Resource<T>.SafeGet;
            if (resource == null)
                throw new UnknownResource(typeof(T).RESTarTypeName());
            var parameters = new RequestParameters(trace.Context, method, resource, protocolId);
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
        internal static IRequest Construct<T>(IResource<T> r, RequestParameters p) where T : class => new Requests.Request<T>(r, p);

        /// <summary>
        /// Creates a new request instance.
        /// </summary>
        /// <param name="trace">A trace to use for this request, for example a Client (or another request 
        /// if this is a nested request). If this is the first request in the trace chain, use the 
        /// factory methods of Client to create a new Client object to use as trace.</param>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="uri">The URI if the request</param>
        /// <param name="body">A body to use in the request</param>
        /// <param name="headers">The headers to use in the request</param>
        public static IRequest Create(ITraceable trace, Method method, ref string uri, byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();
            if (trace == null) throw new Untraceable();
            var parameters = new RequestParameters(trace.Context, method, ref uri, body, headers);
            parameters.Authenticate();
            if (!parameters.IsValid)
                return new InvalidParametersRequest(parameters);
            return Construct((dynamic) parameters.IResource, parameters);
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
        /// <param name="resource">The resource referenced in the URI</param>
        public static bool IsValid(ITraceable trace, ref string uri, out Error error, out IResource resource)
        {
            var parameters = new RequestParameters(trace.Context, (Method) (-1), ref uri, null, null);
            parameters.Authenticate();
            if (parameters.Error != null)
            {
                error = Error.GetError(parameters.Error);
                resource = null;
                return false;
            }
            resource = parameters.IResource;
            IRequest request = Construct((dynamic) resource, parameters);
            if (request.IsValid)
            {
                error = null;
                return true;
            }
            error = request.Result as Error;
            return false;
        }
    }
}