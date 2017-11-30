using System.Net;
using Newtonsoft.Json.Linq;
using RESTar.Operations;
using Starcounter;
using static RESTar.Requests.HandlerActions;
using static RESTar.Admin.Settings;
using static RESTar.Requests.Evaluator;

namespace RESTar.Requests
{
    internal static class StarcounterHandlers
    {
        internal static void RegisterRESTHandlers(bool setupMenu)
        {
            var uri = _Uri + "{?}";
            Handle.GET(_Port, uri, (Request request, string query) => Evaluate(GET, () => MakeArgs(request, query)));
            Handle.POST(_Port, uri, (Request request, string query) => Evaluate(POST, () => MakeArgs(request, query)));
            Handle.PUT(_Port, uri, (Request request, string query) => Evaluate(PUT, () => MakeArgs(request, query)));
            Handle.PATCH(_Port, uri, (Request request, string query) => Evaluate(PATCH, () => MakeArgs(request, query)));
            Handle.DELETE(_Port, uri, (Request request, string query) => Evaluate(DELETE, () => MakeArgs(request, query)));
            Handle.CUSTOM(_Port, "REPORT " + uri, (Request request, string query) => Evaluate(COUNT, () => MakeArgs(request, query)));
            Handle.OPTIONS(_Port, uri, (Request request, string query) => Evaluate(ORIGIN, () => MakeArgs(request, query)));
            if (!_ViewEnabled) return;
            Application.Current.Use(new HtmlFromJsonProvider());
            Application.Current.Use(new PartialToStandaloneHtmlProvider());
            var appName = Application.Current.Name;
            Handle.GET($"/{appName}{{?}}", (Request request, string query) => Evaluate(VIEW, () => MakeArgs(request, query)));
            Handle.GET("/__restar/__page", () => Evaluate(PAGE));
            if (!setupMenu) return;
            Handle.GET($"/{appName}", () => Evaluate(MENU));
        }

        private static Args MakeArgs(Request request, string uri)
        {
            var arrProxy = request.HeadersDictionary?.ContainsKey("X-ARR-LOG-ID") == true;
            var args = new Args(uri, arrProxy)
            {
                Origin = MakeOrigin(request),
                BodyBytes = request.BodyBytes,
                Headers = request.HeadersDictionary,
                ContentType = request.ContentType,
                Accept = request.PreferredMimeTypeString
            };
            if (args.Macro == null) return args;
            request.BodyBytes = request.BodyBytes ?? args.Macro.BodyBinary.ToArray();
            if (args.Macro.Headers == null) return args;
            foreach (var pair in JObject.Parse(args.Macro.Headers))
            {
                var currentValue = request.Headers[pair.Key];
                if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                    request.Headers[pair.Key] = pair.Value.Value<string>();
            }
            return args;
        }

        private static Origin MakeOrigin(Request request)
        {
            var origin = new Origin();
            if (request == null)
            {
                origin.Type = OriginType.Internal;
                origin.IP = null;
                origin.Proxy = null;
            }
            else
            {
                string ip = null;
                if (request.HeadersDictionary?.TryGetValue("X-Forwarded-For", out ip) == true && ip != null)
                {
                    origin.IP = IPAddress.Parse(ip.Split(':')[0]);
                    origin.Proxy = request.ClientIpAddress;
                }
                else
                {
                    origin.IP = request.ClientIpAddress;
                    origin.Proxy = null;
                }
                origin.Type = request.IsExternal ? OriginType.External : OriginType.Internal;
            }
            return origin;
        }

        internal static void UnRegisterRESTHandlers()
        {
            var uri = _Uri + "{?}";
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "POST", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "PUT", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "PATCH", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "DELETE", uri));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "OPTIONS", uri));
            var appName = Application.Current.Name;
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}{{?}}"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", "/__restar/__page"));
            Do.Try(() => Handle.UnregisterHttpHandler(_Port, "GET", $"/{appName}"));
        }
    }
}