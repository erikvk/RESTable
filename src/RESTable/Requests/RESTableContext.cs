using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RESTable.Meta;
using RESTable.Results;
using RESTable.WebSockets;
using static RESTable.Method;
using Options = RESTable.Results.Options;

namespace RESTable.Requests
{
    /// <summary>
    /// Represents a context in which an interaction with the local RESTable APIs can take place.
    /// Requests are run from inside contexts. Contexts guard against infinite recursion and define
    /// the root for each ITraceable tree.
    /// </summary>
    public class RESTableContext : IDisposable, IAsyncDisposable, ITraceable, IServiceProvider
    {
        public string TraceId { get; }
        private const int MaximumStackDepth = 500;
        private int StackDepth;
        internal bool IsBottomOfStack => StackDepth < 1;
        private IServiceProvider Services { get; }
        public object? GetService(Type serviceType) => Services.GetService(serviceType);
        private IOptionsMonitor<RESTableConfiguration> OptionsMonitor { get; }
        public RESTableConfiguration Configuration => OptionsMonitor.CurrentValue;

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

        /// <summary>
        /// Does this context have a websocket that is currently waiting to be opened?
        /// </summary>
        public bool HasWaitingWebSocket(out WebSocket? waitingWebSocket)
        {
            if (webSocket?.Status == WebSocketStatus.Waiting)
            {
                waitingWebSocket = webSocket;
                return true;
            }
            waitingWebSocket = null;
            return false;
        }

        #region Abstract

        /// <summary>
        /// Should return true if and only if the request is a WebSocket upgrade request
        /// </summary>
        protected virtual bool IsWebSocketUpgrade => false;

        /// <summary>
        /// Gets a WebSocket instance for a given Context
        /// </summary>
        protected virtual WebSocket CreateServerWebSocket() => throw new NotImplementedException();

        #endregion

        /// <summary>
        /// Does this context have a WebSocket connected?
        /// </summary>
        public bool HasWebSocket => WebSocket is not null;

        /// <summary>
        /// The client of the context
        /// </summary>
        public Client Client { get; }

#if !NETSTANDARD2_0
        #region Retreiving things

        public ValueTask<ReadOnlyMemory<T>> All<T>() where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).AsReadOnlyMemoryAsync();
        }

        public ValueTask<ReadOnlyMemory<T>> Get<T>(Range range) where T : class
        {
            var (offset, limit) = range.ToOffsetAndLimit();
            return new EntityBufferTask<T>(CreateRequest<T>(), offset, limit, System.Collections.Immutable.ImmutableList<Condition<T>>.Empty).AsReadOnlyMemoryAsync();
        }

        public ValueTask<T> First<T>() where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Entities.FirstAsync();
        }

        public ValueTask<T> Last<T>() where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Entities.LastAsync();
        }

        public ValueTask<T> Get<T>(int index) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).At(index);
        }

        public ValueTask<T> Get<T>(Index index) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).At(index);
        }

        #endregion

        #region Updating things

        public ValueTask<bool> Put<T>(Index index, T item) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Put(index, item);
        }

        public ValueTask<bool> Put<T>(int index, T item) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Put(index, item);
        }

        public ValueTask<bool> Insert<T>(T item) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Insert(item);
        }

        public ValueTask<ReadOnlyMemory<T>> Insert<T>(params T[] items) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Insert(items);
        }

        public ValueTask<ReadOnlyMemory<T>> Insert<T>(ReadOnlyMemory<T> buffer) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Insert(buffer);
        }

        public ValueTask<ReadOnlyMemory<T>> Insert<T>(ReadOnlySpan<T> buffer) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Insert(buffer);
        }

        public ValueTask<bool> Patch<T>(Index index, T item) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Patch(index, item);
        }

        public ValueTask<bool> Patch<T>(int index, T item) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Patch(index, item);
        }

        public ValueTask<ReadOnlyMemory<T>> Patch<T>(Range range, ReadOnlyMemory<T> updatedBuffer) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Patch(range, updatedBuffer);
        }

        public ValueTask<ReadOnlyMemory<T>> Patch<T>(Range range, ReadOnlySpan<T> updatedBuffer) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Patch(range, updatedBuffer);
        }

        #endregion

        #region Deleting things

        public ValueTask<long> Delete<T>(Index index) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Delete(index);
        }

        public ValueTask<long> Delete<T>(int index) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Delete(index);
        }

        public ValueTask<long> Delete<T>(Range range) where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>()).Delete(range);
        }

        #endregion

        #region Entities

        public EntityBufferTask<T> Entities<T>() where T : class
        {
            return new EntityBufferTask<T>(CreateRequest<T>());
        }

        public EntityBufferTask<T> Entities<T>(Range range) where T : class
        {
            var (offset, limit) = range.ToOffsetAndLimit();
            return new EntityBufferTask<T>(CreateRequest<T>(), offset, limit, System.Collections.Immutable.ImmutableList<Condition<T>>.Empty);
        }

        #endregion

#endif

        /// <summary>
        /// Creates a request in this context for a resource type, using the given method and optional protocol id and 
        /// view name. If the protocol ID is null, the default protocol will be used. T must be a registered resource type.
        /// </summary>
        /// <param name="protocolId">An optional protocol ID, defining the protocol to use for the request. If the 
        /// protocol ID is null, the default protocol will be used.</param>
        /// <param name="viewName">An optional view name to use when selecting entities from the resource</param>
        public IRequest<T> CreateRequest<T>(string protocolId = "restable", string? viewName = null) where T : class
        {
            var resourceCollection = Services.GetRequiredService<ResourceCollection>();
            var resource = resourceCollection.SafeGetResource<T>() ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());
            var parameters = new RequestParameters
            (
                context: this,
                method: GET,
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
        public IRequest CreateRequest(Method method = GET, string? uri = "/", Headers? headers = null, object? body = null)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            if (IsWebSocketUpgrade)
            {
                WebSocket = CreateServerWebSocket();
            }
            var parameters = new RequestParameters(this, method, uri, headers, body);
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
        public bool UriIsValid(string uri, out Error? error, out IResource? resource, out IUriComponents? uriComponents)
        {
            if (uri is null) throw new ArgumentNullException(nameof(uri));
            var parameters = new RequestParameters(this, (Method) (-1), uri, null, null);
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
                var resultTask = request.GetResult();
                if (resultTask.IsCompleted)
                    error = resultTask.GetAwaiter().GetResult() as Error;
                else error = resultTask.AsTask().Result as Error;
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
        public bool MethodIsAllowed(Method method, IResource resource, out MethodNotAllowed? error)
        {
            if (method is < GET or > HEAD)
                throw new ArgumentException($"Invalid method value {method} for request");
            if (resource.AvailableMethods.Contains(method) != true)
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
        public IResult GetOptions(string? uri, Headers headers)
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

            // Ensures that RESTable is initialized. If it fails, the service provider
            // does not contain the RESTable services
            services.GetRequiredService<RESTableInitializer>();

            OptionsMonitor = services.GetRequiredService<IOptionsMonitor<RESTableConfiguration>>();

            TraceId = NextId;
            StackDepth = 0;
        }

        private static ulong IdNr;

        private static string NextId
        {
            get
            {
                var id = IdNr += 1;
                var neededBytes = id switch
                {
                    < 256 => 1,
                    < 65536 => 2,
                    < 16777216 => 3,
                    < 4294967296 => 4,
                    < 1099511627776 => 5,
                    < 281474976710656 => 6,
                    < 72057594037927940 => 7,
                    _ => 8
                };
                var bytes = BitConverter.GetBytes(id);
                return Convert.ToBase64String(bytes, 0, neededBytes).TrimEnd('=');
            }
        }

        public override bool Equals(object? obj)
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