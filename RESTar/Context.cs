using System;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Success;
using RESTar.WebSockets;

namespace RESTar
{
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Requests are run from inside contexts. Contexts trace requests and responses and 
    /// keep track of internal requests and guards against infinite recursion.
    /// </summary>
    public abstract class Context : IDisposable
    {
        internal string InitialTraceId { get; }
        internal bool Used { get; set; }

        /// <summary>
        /// Should return true if and only if the request is a WebSocket upgrade request
        /// </summary>
        protected abstract bool IsWebSocketUpgrade { get; }

        /// <summary>
        /// Gets a WebSocket instance for a given Context
        /// </summary>
        protected abstract WebSocket GetWebSocket();

        /// <summary>
        /// The client of the context
        /// </summary>
        public Client Client { get; }

        private const int MaximumStackDepth = 300;
        private int StackDepth;
        private readonly bool AutoDisposeClient;

        private WebSocket webSocket;

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

        internal void IncreaseDepth()
        {
            if (StackDepth == MaximumStackDepth)
                throw new InfiniteLoop();
            StackDepth += 1;
        }

        internal void DecreaseDepth()
        {
            StackDepth -= 1;
            if (StackDepth == 0 && AutoDisposeClient)
                Client.Dispose();
        }

        /// <summary>
        /// Creates a new request instance
        /// </summary>
        /// <param name="method"></param>
        /// <param name="uri"></param>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public IRequest MakeRequest(Method method, ref string uri, byte[] body = null, Headers headers = null)
        {
            if (uri == null) throw new MissingUri();
            if (Used) throw new ReusedContext();
            Used = true;
            if (IsWebSocketUpgrade)
                WebSocket = GetWebSocket();
            var parameters = new RequestParameters(this, method, ref uri, body, headers);
            parameters.Authenticate();
            if (!parameters.IsValid)
                return new InvalidParametersRequest(parameters);
            return Request.Construct((dynamic) parameters.IResource, parameters);
        }

        /// <summary>
        /// Use this method to check the origin of an incoming OPTIONS request. This will check the contents
        /// of the Origin header against allowed CORS origins.
        /// </summary>
        /// <param name="uri">The URI if the request</param>
        /// <param name="headers">The headers contained in the request</param>
        /// <returns></returns>
        public IFinalizedResult CheckOrigin(ref string uri, Headers headers)
        {
            if (uri == null) throw new MissingUri();
            var parameters = new RequestParameters(this, Method.OPTIONS, ref uri, null, headers);
            var origin = parameters.Headers.Origin;
            if (!parameters.IsValid || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
                return new InvalidOrigin();
            if (RESTarConfig.AllowAllOrigins || RESTarConfig.AllowedOrigins.Contains(originUri))
                return new AcceptOrigin(origin, parameters);
            return new InvalidOrigin();
        }

        /// <summary>
        /// Creates a new context for a client
        /// </summary>
        /// <param name="client">The client of the context</param>
        /// <param name="autoDisposeClient">Should RESTar automatically dispose the client when the context
        /// is disposed?</param>
        protected Context(Client client, bool autoDisposeClient = true)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            InitialTraceId = ConnectionId.Next;
            StackDepth = 0;
            AutoDisposeClient = autoDisposeClient;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (AutoDisposeClient)
                Client.Dispose();
        }
    }
}