using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Starcounter;

namespace TestProject
{
    public class Program
    {
        private const string groupName = "testgroup";
        private static readonly List<ulong> Added = new List<ulong>();

        public static void Main()
        {
            Handle.GET("/test?{?}", (string query, Request request) =>
            {
                if (!request.WebSocketUpgrade)
                    return HttpStatusCode.UpgradeRequired;

                Added.Add(request.GetWebSocketId());

                if (query == "schedule")
                    Scheduling.RunTask(() => request.SendUpgrade(groupName)).Wait();
                else if (query == "taskrun")
                    Task.Run(() => request.SendUpgrade(groupName)).Wait();
                else request.SendUpgrade(groupName);

                return HandlerStatus.Handled;
            });

            Handle.WebSocket(groupName, (string s, WebSocket socket) =>
            {
                // sorry, no reaction
            });

            Handle.WebSocket(groupName, (byte[] b, WebSocket socket) =>
            {
                // sorry, no reaction
            });

            Handle.WebSocketDisconnect(groupName, ws =>
            {
                var id = ws.ToUInt64();
                Debug.Assert(Added.Contains(id));
                Added.Remove(id);
            });
        }
    }
}