using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;
using Starcounter;
using Binary = Starcounter.Binary;

namespace RESTar.Admin
{
    /// <summary>
    /// Holds a log of webhook activity
    /// </summary>
    [RESTar(Method.GET, Method.DELETE), Database]
    public class WebhookLog
    {
        /// <summary>
        /// The webhook that did the request
        /// </summary>
        public Webhook Webhook { get; }

        /// <summary>
        /// The destination URL of the request
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// The number of bytes contained in the request body
        /// </summary>
        public long ByteCount { get; }

        /// <summary>
        /// The log message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Does this log entry encode an error?
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// The date and time when the request was sent
        /// </summary>
        public DateTime Time { get; }

        internal WebhookLog(Webhook webhook, long byteCount, string message, bool isError)
        {
            Webhook = webhook;
            Destination = webhook.Destination;
            ByteCount = byteCount;
            Message = message;
            IsError = isError;
            Time = DateTime.UtcNow;
        }

        internal static async Task Log(Webhook hook, bool isError, string message, long byteCount = 0)
        {
            await Scheduling.RunTask(() => Db.TransactAsync(() => new WebhookLog(hook, byteCount, message, isError)));
        }
    }

    /// <summary>
    /// Webhooks are used to generate POST request callbacks to external URIs when events are triggered.
    /// </summary>
    [RESTar, Database]
    public class Webhook
    {
        internal const string All = "SELECT t FROM RESTar.Admin.Webhook t";
        internal const string ByEventName = All + " WHERE t.EventName =?";
        private static HttpClient HttpClient { get; }
        static Webhook() => HttpClient = new HttpClient();

        /// <summary>
        /// A unique ID for this Webhook
        /// </summary>
        public string Id => this.GetObjectID();

        /// <summary>
        /// A descriptive label for this webhook
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The underlying storage for destination URLs
        /// </summary>
        [RESTarMember(ignore: true)] public string DestinationURL { get; set; }

        /// <summary>
        /// The destination URL for this webhook
        /// </summary>
        public string Destination
        {
            get => DestinationURL;
            set
            {
                switch (value)
                {
                    case "":
                    case null: break;
                    case var external when Uri.TryCreate(external, UriKind.Absolute, out var uri):
                        DestinationURL = uri.ToString();
                        break;
                    case var local when Context.Root.UriIsValid(local, out var error, out _, out var formatted) is var valid:
                        DestinationURL = valid ? formatted : local;
                        DestinationError = error?.LogContent;
                        break;
                }
            }
        }

        /// <summary>
        /// The underlying storage for content type
        /// </summary>
        [RESTarMember(ignore: true)] public string ContentTypeString { get; set; }

        /// <summary>
        /// The content type to use in the POST request, for example 'application/json'
        /// </summary>
        public ContentType ContentType
        {
            get => ContentTypeString ?? ContentType.DefaultOutput;
            set
            {
                ContentTypeIsValid = value.AnyType || ContentTypeController.OutputContentTypeProviders.TryGetValue(value.MediaType ?? "", out _);
                ContentTypeString = value.ToString();
            }
        }


        /// <summary>
        /// The underlying storage for event names
        /// </summary>
        [RESTarMember(ignore: true)] public string EventName { get; private set; }

        /// <summary>
        /// The event used to trigger this webhook
        /// </summary>
        public string Event
        {
            get => EventName;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && Db.SQL<Event>(Admin.Event.ByName, value).FirstOrDefault() is Event @event)
                {
                    EventName = @event.Name;
                    EventIsValid = true;
                }
                else
                {
                    EventName = value;
                    EventIsValid = false;
                }
            }
        }

        /// <summary>
        /// Is this webhook currently paused?
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Is this webhook currently valid?
        /// </summary>
        public bool IsValid => DestinationIsValid && ContentTypeIsValid && EventIsValid && CustomRequestIsValid;

        /// <summary>
        /// Is this webhook currently active?
        /// </summary>
        public bool IsActive => IsValid && !IsPaused;

        /// <summary>
        /// The error (if any) for this webhook
        /// </summary>
        public string Error
        {
            get
            {
                if (!DestinationIsValid)
                    return DestinationError;
                if (!ContentTypeIsValid)
                    return $"Could not find a content type provider for content type '{ContentType}'. " +
                           "To list available content types, use the RESTar.Admin.Protocol resource";
                if (!EventIsValid)
                    return $"Unknown event '{Event}'. To list available events, use the RESTar.Event resource";
                if (!CustomRequestIsValid)
                    return CustomRequestError;
                return null;
            }
        }

        /// <summary>
        /// Is the destination URL valid?
        /// </summary>
        [RESTarMember(ignore: true)] public bool DestinationIsValid => DestinationError == null;

        /// <summary>
        /// The error (if any) of the destination
        /// </summary>
        [RESTarMember(ignore: true)] public string DestinationError { get; private set; }

        /// <summary>
        /// Is the content type valid?
        /// </summary>
        [RESTarMember(ignore: true)] public bool ContentTypeIsValid { get; private set; }

        /// <summary>
        /// Is the event valid?
        /// </summary>
        [RESTarMember(ignore: true)] public bool EventIsValid { get; private set; }

        /// <summary>
        /// Is the custom request valid?
        /// </summary>
        [RESTarMember(ignore: true)] public bool CustomRequestIsValid => CustomRequestError == null;

        /// <summary>
        /// The error (if any) of the custom request
        /// </summary>
        [RESTarMember(ignore: true)] public string CustomRequestError { get; private set; }

        private WebhookRequest customRequest;

        /// <summary>
        /// A custom request to use to generate the body of the webhook's POST requests
        /// </summary>
        [RESTarMember(replaceOnUpdate: true)] public WebhookRequest CustomRequest
        {
            get => customRequest;
            set
            {
                customRequest?.Delete();
                customRequest = value;
            }
        }

        private HttpRequestMessage GetRequestMessage(Stream body, ContentType? contentType, string customRequestInfo = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, DestinationURL);
            if (body != null)
            {
                var content = new StreamContent(body);
                content.Headers.ContentType = contentType.GetValueOrDefault();
                message.Content = content;
            }
            if (customRequestInfo != null)
                message.Headers.Add("RESTar-webhook-custom-request-info", customRequestInfo);
            message.Headers.Add("RESTar-webhook-id", Id);
            return message;
        }

        internal async Task Post(IEventInternal @event)
        {
            if (!IsActive) return;
            await Scheduling.RunTask(async () =>
            {
                HttpRequestMessage requestMessage;

                if (CustomRequest == null)
                {
                    var (_body, _contentType) = @event.Payload.ToBodyStream(@event.NativeContentType);
                    requestMessage = GetRequestMessage(_body, _contentType);
                }
                else
                {
                    string ForCustomRequest(string message) => $"Error when evaluating custom request: {message}";
                    var client = Client.Webhook;
                    if (!client.TryAuthenticate(CustomRequest.ApiKeyHash, out var error))
                    {
                        await WebhookLog.Log(this, true, ForCustomRequest(error.LogMessage));
                        return;
                    }
                    using (var request = CustomRequest.CreateRequest(client))
                    {
                        var result = request.Evaluate().Serialize(ContentType);
                        switch (result)
                        {
                            case Results.Error _:
                                await WebhookLog.Log(this, true, ForCustomRequest(result.LogMessage));
                                if (CustomRequest.BreakOnError)
                                {
                                    result.Dispose();
                                    return;
                                }
                                break;
                            case NoContent _:
                                result.Dispose();
                                if (customRequest.BreakOnNoContent) return;
                                break;
                        }
                        requestMessage = GetRequestMessage(result.Body, result.Headers.ContentType, result.LogMessage);
                    }
                }
                var byteCount = requestMessage.Content?.Headers.ContentLength ?? 0;
                try
                {
                    using (requestMessage)
                    using (var response = await HttpClient.SendAsync(requestMessage))
                    {
                        var status = $"{response.StatusCode}: {response.ReasonPhrase}";
                        await WebhookLog.Log(this, false, status, byteCount);
                    }
                }
                catch (Exception e)
                {
                    await WebhookLog.Log(this, true, e.ToString(), byteCount);
                }
            });
        }
    }

    /// <summary>
    /// A request used for getting data to post in a WebHook
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
        /// A stored API key hash
        /// </summary>
        [RESTarMember(ignore: true)] public string ApiKeyHash { get; private set; }

        /// <summary>
        /// The API key to use in the WebHook request
        /// </summary>
        public string APIKey
        {
            get => ApiKeyHash == null ? null : "*******";
            set => ApiKeyHash = string.IsNullOrWhiteSpace(value) ? null : value.SHA256();
        }

        /// <summary>
        /// The underlying storage for headers of this WebHook request
        /// </summary>
        [RESTarMember(ignore: true)] public string HeadersString { get; private set; }

        /// <summary>
        /// The headers for this WebHook request
        /// </summary>
        [RESTarMember(replaceOnUpdate: true)] public Headers Headers
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

        internal IRequest CreateRequest(Client client) => Context.Webhook(client).CreateRequest
        (
            uri: URI,
            method: Method,
            body: GetBody(),
            headers: Headers
        );

        private byte[] GetBody() => HasBody ? BodyBinary.ToArray() : new byte[0];
        private bool HasBody => !BodyBinary.IsNull && BodyBinary.Length > 0;
        private string BodyUTF8 => !HasBody ? "" : Encoding.UTF8.GetString(BodyBinary.ToArray());
    }
}