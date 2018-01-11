using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RESTar.Internal;

namespace RESTar
{
    internal class WebSocketComparer : IEqualityComparer<IWebSocket>
    {
        public bool Equals(IWebSocket x, IWebSocket y) => x?.Id == y?.Id;
        public int GetHashCode(IWebSocket obj) => obj.Id.GetHashCode();
    }

    internal static class WebSocketController
    {
        internal static ConcurrentDictionary<Type, ConcurrentBag<IWebSocket>> ActiveSockets { get; private set; }

        static WebSocketController()
        {
            ActiveSockets = new ConcurrentDictionary<Type, ConcurrentBag<IWebSocket>>();
        }

        internal static void Register<T>(IWebSocket socket) where T : ITarget
        {

        }

        internal static void Unregister<>
    }

    public static class WebSocketController<T> where T : IWebSocketController
    {
        public static IEnumerable<IWebSocket> OpenSockets { get; }
    }
}