using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
    public class RESTableContext : IDisposable, IAsyncDisposable, ITraceable, IServiceProvider
    {
        public string TraceId { get; }
        private const int MaximumStackDepth = 500;
        private int StackDepth;
        internal bool IsBottomOfStack => StackDepth < 1;
        private IServiceProvider Services { get; }
        public object? GetService(Type serviceType) => Services.GetService(serviceType);

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

        private WebSocket? webSocket;

        /// <summary>
        /// The websocket connected with this context
        /// </summary>
        internal WebSocket? WebSocket
        {
            get => webSocket;
            set
            {
                if (value is null) return;
                Services.GetRequiredService<WebSocketManager>().Add(value);
                webSocket = value;
            }
        }

        #region Abstract

        /// <summary>
        /// Should return true if and only if the request is a WebSocket upgrade request
        /// </summary>
        protected virtual bool IsWebSocketUpgrade => false;

        /// <summary>
        /// Gets a WebSocket instance for a given Context
        /// </summary>
        protected virtual WebSocket CreateWebSocket() => throw new NotImplementedException();

        #endregion

        /// <summary>
        /// Does this context have a WebSocket connected?
        /// </summary>
        public bool HasWebSocket => WebSocket is not null;

        /// <summary>
        /// The client of the context
        /// </summary>
        public Client Client { get; }

        /// <summary>
        /// Creates a request in this context for a resource type, using the given method and optional protocol id and 
        /// view name. If the protocol ID is null, the default protocol will be used. T must be a registered resource type.
        /// </summary>
        /// <param name="method">The method to perform, for example GET</param>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        public virtual IRequest<T> CreateRequest<T>(Method method = GET, string protocolId = "restable", string? viewName = null) where T : class
        {
            var resourceCollection = Services.GetRequiredService<ResourceCollection>();
            var resource = resourceCollection.SafeGetResource<T>() ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());
            var parameters = new RequestParameters
            (
                context: this,
                method: method,
                resource: resource,
                protocolIdentifier: protocolId,
                viewName: viewName
            );
            parameters.SetBodyObject(null);
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
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            if (IsWebSocketUpgrade)
            {
                WebSocket = CreateWebSocket();
            }
            var parameters = new RequestParameters(this, method, uri, headers);
            parameters.SetBodyObject(body);
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
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, (Method) (-1), uri, null);
            uriComponents = null;
            if (parameters.Error is not null)
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
                // Will run synchronously since the request is not valid
                using var result = request.GetResult().Result;
                error = result as Error;
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
            if (method is < GET or > HEAD)
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
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, uri, headers);
            return Options.Create(parameters);
        }

        /// <summary>
        /// Creates a new context for a client, with scoped services
        /// </summary>
        /// <param name="client">The client of the context</param>
        /// <param name="services">The services to use in this context</param>
        public RESTableContext(Client client, IServiceProvider services)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Services = services ?? throw new ArgumentNullException(nameof(services));
            TraceId = NextId;
            StackDepth = 0;
        }

        private static ulong IdNr;

        private static string NextId
        {
            get
            {
                var id = IdNr += 1;
                var bytes = id switch
                {
                    < 1 << 8 => new[] {(byte) id},
                    < 2 << 8 => new[] {byte.MaxValue, (byte) (id - 1 << 8)},
                    < 3 << 8 => new[] {byte.MaxValue, byte.MaxValue, (byte) (id - 2 << 8)},
                    < 4 << 8 => new[] {byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte) (id - 3 << 8)},
                    < 5 << 8 => new[] {byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte) (id - 4 << 8)},
                    < 6 << 8 => new[] {byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte) (id - 5 << 8)},
                    < 7 << 8 => new[] {byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte) (id - 6 << 8)},
                    _ => BitConverter.GetBytes(IdNr)
                };
                return Convert.ToBase64String(bytes).TrimEnd('=');
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
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            if (Services is IDisposable disposable)
                disposable.Dispose();
        }
    }
}