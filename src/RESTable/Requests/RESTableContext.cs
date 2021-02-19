using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.Internal.Auth;
using RESTable.Meta;
using RESTable.Results;
using RESTable.WebSockets;
using static RESTable.Method;

namespace RESTable.Requests
{
    /// <summary>
    /// Requests are run from inside contexts. Contexts guard against infinite recursion 
    /// and define the root for each ITraceable tree. They also hold WebSocket connections
    /// and Client access rights.
    /// </summary>
    public abstract class RESTableContext : IDisposable, IAsyncDisposable, ITraceable
    {
        public string TraceId { get; }
        private const int MaximumStackDepth = 500;
        private WebSocket webSocket;
        private int StackDepth;
        internal bool IsBottomOfStack => StackDepth < 1;
        public IServiceProvider Services { get; }

        public RESTableContext Context => this;

        internal void IncreaseDepth()
        {
            if (StackDepth == MaximumStackDepth)
                throw new InfiniteLoop();
            StackDepth += 1;
        }

        internal void DecreaseDepth()
        {
            StackDepth -= 1;
        }

        /// <summary>
        /// The websocket connected with this context
        /// </summary>
        internal WebSocket WebSocket
        {
            get => webSocket;
            set
            {
                WebSocketController.Add(value);
                webSocket = value;
            }
        }

        #region Abstract

        /// <summary>
        /// Should return true if and only if the request is a WebSocket upgrade request
        /// </summary>
        protected abstract bool IsWebSocketUpgrade { get; }

        /// <summary>
        /// Gets a WebSocket instance for a given Context
        /// </summary>
        protected abstract WebSocket CreateWebSocket();

        #endregion

        /// <summary>
        /// Does this context have a WebSocket connected?
        /// </summary>
        public bool HasWebSocket => WebSocket != null;

        /// <summary>
        /// The client of the context
        /// </summary>
        public Client Client { get; }

        /// <summary>
        /// Returns true if and only if this client is considered authenticated. This is a necessary precondition for 
        /// being included in a context. If false, a NotAuthorized result object is returned in the out parameter, that 
        /// can be returned to the client.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <param name="error">The error result, if not authenticated</param>
        /// <returns></returns>
        public bool TryAuthenticate(ref string uri, out Unauthorized error, Headers headers = null)
        {
            Client.AccessRights = RESTableConfig.RequireApiKey switch
            {
                true => Authenticator.GetAccessRights(ref uri, headers),
                false => AccessRights.Root
            };
            if (Client.AccessRights == null)
            {
                error = new Unauthorized();
                error.SetContext(this);
                if (headers?.Metadata == "full")
                    error.Headers.Metadata = error.Metadata;
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>
        /// Creates a request in this context for a resource type, using the given method and optional protocol id and 
        /// view name. If the protocol ID is null, the default protocol will be used. T must be a registered resource type.
        /// </summary>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        public virtual IRequest<T> CreateRequest<T>(Method method = GET, string protocolId = "restable", string viewName = null) where T : class
        {
            var resource = Resource<T>.SafeGet ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());
            var parameters = new RequestParameters
            (
                context: this,
                method: method,
                resource: resource,
                protocolIdentifier: protocolId,
                viewName: viewName
            );
            return new Request<T>(resource, parameters);
        }

        /// <summary>
        /// Creates a request in this context using the given parameters.
        /// </summary>
        /// <param name="method">The method to perform</param>
        /// <param name="uri">The URI of the request</param>
        /// <param name="body">The body of the request</param>
        /// <param name="headers">The headers of the request</param>
        public virtual IRequest CreateRequest(Method method = GET, string uri = "/", object body = null, Headers headers = null)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (IsWebSocketUpgrade)
            {
                WebSocket = CreateWebSocket();
            }
            var parameters = new RequestParameters(this, method, uri, headers);
            parameters.SetBody(body);
            if (!parameters.IsValid) return new InvalidParametersRequest(parameters);
            return DynamicCreateRequest((dynamic) parameters.IResource, parameters);
        }

        /// <summary>
        /// Validates a request URI and returns true if valid. If invalid, the error is returned in the 
        /// out parameter.
        /// </summary>
        /// <param name="uri">The URI if the request</param>
        /// <param name="error">A RESTableError describing the error, or null if valid</param>
        /// <param name="resource">The resource referenced in the URI</param>
        /// <param name="uriComponents">The URI components of the uri, if valid. Otherwise null</param>
        public bool UriIsValid(string uri, out Error error, out IResource resource, out IUriComponents uriComponents)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, (Method) (-1), uri, null);
            uriComponents = null;
            if (parameters.Error != null)
            {
                var invalidParametersRequest = new InvalidParametersRequest(parameters);
                error = parameters.Error.AsResultOf(invalidParametersRequest);
                resource = null;
                return false;
            }
            resource = parameters.IResource;
            IRequest request = DynamicCreateRequest((dynamic) resource, parameters);
            if (!request.IsValid)
            {
                error = request.Evaluate().Result as Error;
                return false;
            }
            uriComponents = request.UriComponents;
            error = null;
            return true;
        }

        /// <summary>
        /// Returns true if and only if the current context can be used to make a request with the given method,
        /// to the given resource.
        /// </summary>
        /// <param name="method">The method the check access for</param>
        /// <param name="resource">The resource to check access to</param>
        /// <param name="error">An object describing the error, if the method is not allowed</param>
        /// <returns></returns>
        public bool MethodIsAllowed(Method method, IResource resource, out MethodNotAllowed error)
        {
            if (method < GET || method > HEAD)
                throw new ArgumentException($"Invalid method value {method} for request");
            if (resource?.AvailableMethods.Contains(method) != true)
            {
                error = new MethodNotAllowed(method, resource, false);
                return false;
            }
            if (Client.AccessRights[resource]?.Contains(method) == true)
            {
                error = null;
                return true;
            }
            error = new MethodNotAllowed(method, resource, true);
            return false;
        }

        private static IRequest DynamicCreateRequest<T>(IResource<T> r, RequestParameters p) where T : class => new Request<T>(r, p);

        /// <summary>
        /// Use this method to check the origin of an incoming OPTIONS request. This will check the contents
        /// of the Origin header against allowed CORS origins. If the URI is valid, a body is included with the
        /// response, describing the selected resource.
        /// </summary>
        /// <param name="uri">The URI of the request</param>
        /// <param name="headers">The headers of the request</param>
        /// <returns></returns>
        public IResult GetOptions(string uri, Headers headers)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, uri, headers);
            return Options.Create(parameters);
        }

        /// <summary>
        /// Creates a new context for a client, with scoped services
        /// </summary>
        /// <param name="client">The client of the context</param>
        /// <param name="services">The services to use in this context</param>
        protected RESTableContext(Client client, IServiceProvider services = null)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Services = services ?? new ServiceCollection().BuildServiceProvider();
            TraceId = NextId;
            StackDepth = 0;
        }

        /// <summary>
        /// The context of internal root-level access requests
        /// </summary>
        public static RESTableContext Root => new InternalContext();

        private static ulong IdNr;

        private static string NextId
        {
            get
            {
                var bytes = BitConverter.GetBytes(IdNr += 1);
                return Convert.ToBase64String(bytes);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is RESTableContext context && context.TraceId == TraceId;
        }

        public override int GetHashCode()
        {
            return TraceId.GetHashCode();
        }

        public void Dispose()
        {
            if (Services is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().Wait();
            if (Services is IDisposable disposable)
                disposable.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Services is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            if (Services is IDisposable disposable)
                disposable.Dispose();
        }
    }
}