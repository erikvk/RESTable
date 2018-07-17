using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Internal.Auth;
using RESTar.Linq;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Results;
using Starcounter;
using static RESTar.Method;

namespace RESTar.Admin
{
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
                    case null:
                        DestinationURL = null;
                        DestinationIsLocal = false;
                        break;
                    case var external when Uri.TryCreate(external, UriKind.Absolute, out var uri):
                        DestinationURL = uri.ToString();
                        DestinationIsLocal = false;
                        break;
                    case var other when Context.Root.UriIsValid(other, out var error, out _, out var formatted) is var valid:
                        DestinationURL = valid ? formatted : other;
                        DestinationIsLocal = valid;
                        DestinationError = error?.LogContent;
                        break;
                }
            }
        }

        /// <summary>
        /// Does the destination URL refer to a local resource?
        /// </summary>
        public bool DestinationIsLocal { get; private set; }

        /// <summary>
        /// The API key to use in requests to local destination resources
        /// </summary>
        [RESTarMember(ignore: true)] public string LocalDestinationAPIKey { get; private set; }

        /// <summary>
        /// The underlying storage for headers of this WebHook request
        /// </summary>
        [RESTarMember(ignore: true)] public string HeadersString { get; private set; }

        /// <summary>
        /// Custom headers included in the POST request
        /// </summary>
        [RESTarMember(replaceOnUpdate: true), JsonConverter(typeof(HeadersConverter), "*")]
        public Headers Headers
        {
            get => HeadersString == null ? null : JsonConvert.DeserializeObject<Headers>(HeadersString);
            set
            {
                switch (value.Authorization)
                {
                    case null:
                        value.Authorization = LocalDestinationAPIKey = null;
                        break;
                    case Authenticator.AuthHeaderMask: break;
                    case var localAuthHeader when DestinationIsLocal:
                        LocalDestinationAPIKey = Authenticator.GetAccessRights(localAuthHeader)?.ApiKey;
                        value.Authorization = Authenticator.AuthHeaderMask;
                        break;
                }
                HeadersString = JsonConvert.SerializeObject(value);
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
        public bool IsValid => DestinationIsValid && EventIsValid && CustomRequestIsValid;

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
                if (!EventIsValid)
                    return $"Unknown event '{Event}'. To list available events, use the RESTar.Event resource";
                if (!CustomRequestIsValid)
                    return CustomRequestError;
                if (!IsAuthorized)
                    return AuthorizationError;
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
        /// Is the event valid?
        /// </summary>
        [RESTarMember(ignore: true)] public bool EventIsValid { get; private set; }

        /// <summary>
        /// Is this webhook properly authorized (as far as we can know)?
        /// </summary>
        [RESTarMember(ignore: true)] public bool IsAuthorized => AuthorizationError == null;

        /// <summary>
        /// The error (if any) regarding this webhook's authorization
        /// </summary>
        [RESTarMember(ignore: true)] public string AuthorizationError { get; private set; }

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

        private IRequest GetLocalPostRequest(out Results.Error error)
        {
            var client = Client.Webhook;
            if (!client.TryAuthenticate(Headers.Authorization, out var forbidden))
            {
                error = forbidden;
                return null;
            }
            var context = Context.Webhook(client);
            if (!context.UriIsValid(DestinationURL, out error, out var resource, out _))
                return null;
            if (!context.MethodIsAllowed(POST, resource, out var methodNotAllowed))
            {
                error = methodNotAllowed;
                return null;
            }
            return Context.Webhook(client).CreateRequest(DestinationURL, POST, headers: Headers);
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

        private static int Check(IRequest<Webhook> request)
        {
            var count = 0;
            Db.TransactAsync(() => request.GetInputEntities().ForEach(webhook =>
            {
                if (webhook.DestinationIsLocal)
                    count += 1;
            }));
            return count;
        }

        internal async Task Post<T>(IEventInternal<T> @event) where T : class
        {
            if (!IsActive) return;

            Stream body;
            var info = default(string);
            var contentType = Headers.ContentType ?? ContentType.JSON;

            if (CustomRequest == null)
                (body, contentType) = @event.Payload.ToBodyStream(@event.NativeContentType ?? contentType);
            else
            {
                using (var request = CustomRequest.CreateRequest(out var error))
                {
                    if (error != null)
                    {
                        await WebhookLog.Log(this, true, ForCustomRequest(error.LogMessage));
                        return;
                    }
                    var result = request.Evaluate().Serialize(contentType);
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
                    (body, contentType, info) = (result.Body, result.Headers.ContentType ?? contentType, result.LogMessage);
                }
            }

            try
            {
                if (DestinationIsLocal) { }
                else
                {
                    using (var requestMessage = GetRequestMessage(body, contentType, info))
                    {
                        if (Headers is Headers _headers)
                            foreach (var header in _headers)
                                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        using (var response = await HttpClient.SendAsync(requestMessage))
                        {
                            var status = $"{response.StatusCode}: {response.ReasonPhrase}";
                            await WebhookLog.Log(this, false, status, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                await WebhookLog.Log(this, true, e.ToString(), 0);
            }
        }

        private static string ForCustomRequest(string message) => $"Error when evaluating custom request: {message}";
    }
}