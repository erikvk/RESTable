using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets
{
    /// <inheritdoc cref="IWebSocket" />
    /// <inheritdoc cref="IWebSocketInternal" />
    /// /// <inheritdoc cref="IDisposable" />
    /// <summary>
    /// A WebSocket wrapper that puts all void returning operations in a queue for later execution
    /// </summary>
    internal class WebSocketQueue : IWebSocket, IWebSocketInternal
    {
        internal ConcurrentQueue<Func<Task>> ActionQueue { get; }
        internal IWebSocketInternal ToQueueFor { get; }
        public string TraceId { get; }
        public RESTableContext Context { get; }
        public WebSocketStatus Status => ToQueueFor.Status;
        public Headers Headers => ToQueueFor.Headers;
        public ReadonlyCookies Cookies => ToQueueFor.Cookies;

        /// <inheritdoc />
        public void SetStatus(WebSocketStatus status) => ActionQueue.Enqueue(() =>
        {
            ToQueueFor.SetStatus(status);
            return Task.CompletedTask;
        });

        public WebSocketQueue(IWebSocketInternal webSocket)
        {
            ActionQueue = new ConcurrentQueue<Func<Task>>();
            ToQueueFor = webSocket;
            TraceId = webSocket.TraceId;
            Context = webSocket.Context;
        }

        public Task SendTextRaw(string text)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendTextRaw(text));
            return Task.CompletedTask;
        }

        public Task SendText(string d)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendText(d));
            return Task.CompletedTask;
        }

        public Task SendText(byte[] d, int o, int l)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendText(d, o, l));
            return Task.CompletedTask;
        }

        public Task SendBinary(byte[] d, int o, int l)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendBinary(d, o, l));
            return Task.CompletedTask;
        }
        
        public Task SendJson(object i, bool a = false, bool? p = null, bool ig = false)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendJson(i, a, p, ig));
            return Task.CompletedTask;
        }

        public Task SendException(Exception exception)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendException(exception));
            return Task.CompletedTask;
        }

        public Task SendResult(IResult r, TimeSpan? t = null, bool w = false, bool d = true)
        {
            ActionQueue.Enqueue(() => ToQueueFor.SendResult(r, t, w, d));
            return Task.CompletedTask;
        }

        public Task StreamResult(ISerializedResult r, int m, TimeSpan? t = null, bool w = false, bool d = true)
        {
            ActionQueue.Enqueue(() => ToQueueFor.StreamResult(r, m, t, w, d));
            return Task.CompletedTask;
        }

        public void DirectToShell(IEnumerable<Condition<Shell>> a = null)
        {
            ActionQueue.Enqueue(() =>
            {
                ToQueueFor.DirectToShell(a);
                return Task.CompletedTask;
            });
        }

        public void DirectTo<T>(ITerminalResource<T> t, ICollection<Condition<T>> a = null) where T : class, ITerminal
        {
            ActionQueue.Enqueue(() =>
            {
                ToQueueFor.DirectTo(t, a);
                return Task.CompletedTask;
            });
        }

        public ValueTask DisposeAsync()
        {
            ActionQueue.Enqueue(() =>
            {
                var webSocket = (WebSocket) ToQueueFor;
                return webSocket.DisposeAsync().AsTask();
            });
            return default;
        }
    }
}