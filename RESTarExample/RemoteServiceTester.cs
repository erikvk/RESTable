using System;
using System.Text;
using RESTar;
using RESTar.Requests;

#pragma warning disable 1591

namespace RESTarExample
{
    [RESTar]
    public class RemoteServiceTester : ITerminal
    {
        public void Dispose() { }
        public IWebSocket WebSocket { private get; set; }
        public void Open() { }
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;

        public string ApiKey { private get; set; }
        public string Service { get; set; } = "http://localhost:9000/rest";
        public string Body { get; set; }
        public Headers Headers { get; set; } = new Headers();

        public void HandleTextInput(string input)
        {
            var (command, tail) = input.TSplit(' ');
            switch (command.ToUpperInvariant())
            {
                case var methodString when Enum.TryParse(methodString, out Method method):
                    var context = Context.Remote(Service, ApiKey);
                    var body = Body?.Length > 0 ? Encoding.UTF8.GetBytes(Body) : null;
                    var request = context.CreateRequest(method, tail, body, Headers);
                    // var res = request.Result.Serialize();
                    // WebSocket.SendResult(res, res.TimeElapsed, true);

                    var result = request.Result;
                    // foreach (var entity in result as IEntities)
                    // {
                    //     var json = JsonConvert.SerializeObject(entity);
                    // }
                    var ser = result.Serialize();
                    WebSocket.SendResult(ser);

                    break;
                case "BODY":
                    Body = tail;
                    break;
                case "SERVICE":
                    Service = tail;
                    break;
                case "APIKEY":
                    ApiKey = tail;
                    break;
                case var name:
                    Headers[name] = tail;
                    WebSocket.SendJson(Headers);
                    break;
            }
        }
    }
}