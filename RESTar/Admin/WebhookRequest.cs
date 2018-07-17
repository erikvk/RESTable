using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Requests;
using RESTar.Resources;
using Starcounter;

namespace RESTar.Admin
{
    /// <summary>
    /// A request used for getting data from a local resource, to post in a WebHook
    /// </summary>
    [Database]
    public class WebhookRequest
    {
        /// <summary>
        /// The method of the WebHook request. Always GET
        /// </summary>
        public Method Method => Method.GET;

        /// <summary>
        /// The URI of the WebHook request. Must point to a local resource.
        /// </summary>
        public string URI { get; set; }

        /// <summary>
        /// The API key to use in the request
        /// </summary>
        [RESTarMember(ignore: true)] public string APIKey { get; internal set; }

        /// <summary>
        /// The underlying storage for headers of this WebHook request
        /// </summary>
        [RESTarMember(ignore: true)] public string HeadersString { get; private set; }

        /// <summary>
        /// The headers for this WebHook request
        /// </summary>
        [RESTarMember(replaceOnUpdate: true), JsonConverter(typeof(HeadersConverter))]
        public Headers Headers
        {
            get
            {
                if (HeadersString == null) return null;
                return JsonConvert.DeserializeObject<Headers>(HeadersString);
            }
            set => HeadersString = JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// The underlying storage for the body of this WebHook request
        /// </summary>
        [RESTarMember(ignore: true)] public Binary BodyBinary { get; set; }

        /// <summary>
        /// The body of this WebHook request
        /// </summary>
        public JToken Body
        {
            get => HasBody ? JToken.Parse(BodyUTF8) : null;
            set => BodyBinary = value != null ? new Binary(Encoding.UTF8.GetBytes(value.ToString())) : default;
        }

        /// <summary>
        /// Should the POST request be aborted if the custom request returns an error?
        /// </summary>
        public bool BreakOnError { get; set; }

        /// <summary>
        /// Should the POST request be aborted if the custom request returns 404: No content?
        /// </summary>
        public bool BreakOnNoContent { get; set; }

        internal IRequest CreateRequest(out Results.Error error)
        {
            var client = Client.Webhook;
            if (!client.TryAuthenticate(APIKey, out var forbidden))
            {
                error = forbidden;
                return null;
            }
            var context = Context.Webhook(client);
            if (!context.UriIsValid(URI, out error, out var resource, out _))
                return null;
            if (!context.MethodIsAllowed(Method, resource, out var methodNotAllowed))
            {
                error = methodNotAllowed;
                return null;
            }
            return Context.Webhook(client).CreateRequest(URI, Method, GetBody(), Headers);
        }

        private byte[] GetBody() => HasBody ? BodyBinary.ToArray() : new byte[0];
        private bool HasBody => !BodyBinary.IsNull && BodyBinary.Length > 0;
        private string BodyUTF8 => !HasBody ? "" : Encoding.UTF8.GetString(BodyBinary.ToArray());
    }
}