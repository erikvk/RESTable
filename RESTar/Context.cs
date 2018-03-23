using System;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.WebSockets;

namespace RESTar
{
    internal sealed class StarcounterContext : Context
    {
        public StarcounterContext(Client client) : base(client) { }
        internal StarcounterWebSocket GetScWebSocket() => (StarcounterWebSocket) GetWebSocket();
        internal void SetWebSocket(StarcounterWebSocket value) => base.SetWebSocket(value);
    }

    internal class WebSocketContext : Context
    {
        internal WebSocketContext(Client client) : base(client, false) => Client.IsInWebSocket = true;
    }

    internal class InternalContext : Context
    {
        internal InternalContext(Client client = null, bool autoDisposeClient = true) : base(client ?? Client.Internal, autoDisposeClient) { }
    }

    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// Requests are run from inside contexts. Contexts trace requests and responses and 
    /// keep track of internal requests and guards against infinite recursion.
    /// </summary>
    public abstract class Context : ITraceable, IDisposable
    {
        /// <inheritdoc />
        public string TraceId { get; }

        Context ITraceable.Context => this;

        /// <summary>
        /// Does this context have a WebSocket?
        /// </summary>
        public bool HasWebSocket => GetWebSocket() != null;

        /// <summary>
        /// The client of the context
        /// </summary>
        public Client Client { get; }

        private const int MaximumStackDepth = 300;
        private int StackDepth;
        private readonly bool AutoDisposeClient;

        private WebSocket webSocket;

        /// <summary>
        /// The ID of the websocket connected with this TCP connection
        /// </summary>
        protected WebSocket GetWebSocket() => webSocket;

        /// <summary>
        /// The ID of the websocket connected with this TCP connection
        /// </summary>
        protected void SetWebSocket(WebSocket value)
        {
            WebSocketController.Add(value);
            webSocket = value;
        }

        internal WebSocket WebSocket
        {
            get => GetWebSocket();
            set => SetWebSocket(value);
        }

        internal WebSocket WebSocketInternal => GetWebSocket();

        internal bool IsInWebSocket => GetWebSocket().Status == WebSocketStatus.Open;

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
        /// Creates a new context for a client
        /// </summary>
        /// <param name="client">The client of the context</param>
        /// <param name="autoDisposeClient">Should RESTar automatically dispose the client when the context
        /// stack depth reaches zero?</param>
        public Context(Client client, bool autoDisposeClient = true)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            TraceId = ConnectionId.Next;
            StackDepth = 0;
            AutoDisposeClient = autoDisposeClient;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Client?.Dispose();
            WebSocketInternal?.Dispose();
        }
    }
}