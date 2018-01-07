using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            WebSocket wsocket;

            Handle.WebSocket("wstest", (string data, WebSocket socket) =>
            {

                var s = data;

                socket.Send("Heej igen");
            });


            Handle.GET("/test", (Request request) =>
            {
                //if (!request.WebSocketUpgrade)
                //    return 204;

                wsocket = request.SendUpgrade("wstest");

                wsocket.Send("HEEELLLLOOOO!!!!");
                
                return HandlerStatus.Handled;
            });
        }
    }
}