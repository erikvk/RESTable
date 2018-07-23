using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Internal.Auth;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using Starcounter;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Webhooks are used to generate POST request callbacks to external URIs when events are triggered.
    /// </summary>
    [RESTar, Database]
    public class Webhook : IValidatable
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

        [Transient] private bool DestinationHasChanged { get; set; }

        private string destination;

        /// <summary>
        /// The destination URL for this webhook
        /// </summary>
        public string Destination
        {
            get => destination;
            set
            {
                DestinationHasChanged = DestinationHasChanged || destination != value;
                destination = value;
            }
        }

        /// <summary>
        /// Does the destination refer to a local resource?
        /// </summary>
        [RESTarMember(ignore: true)] public bool DestinationIsLocal { get; private set; }

        /// <summary>
        /// The API key to use in requests to local destination resources
        /// </summary>
        [RESTarMember(ignore: true)] public string LocalDestinationAPIKey { get; private set; }

        /// <summary>
        /// Custom headers included in the POST request
        /// </summary>
        [JsonConverter(typeof(HeadersConverter<DbHeaders>), true)]
        public DbHeaders Headers { get; }

        [Transient] private bool EventHasChanged { get; set; }

        /// <summary>
        /// The underlying storage for Event
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
                EventHasChanged = EventHasChanged || EventName != value;
                EventName = value;
            }
        }

        /// <summary>
        /// Is this webhook currently paused?
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// The error message, if any, of this webhook
        /// </summary>
        [RESTarMember(hideIfNull: true)] public string ErrorMessage { get; private set; }

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

        /// <inheritdoc />
        public Webhook() => Headers = new DbHeaders();

        private bool CheckIfValid() => IsValid(out _);

        /// <inheritdoc />
        public bool IsValid(out string invalidReason)
        {
            #region Destination

            if (string.IsNullOrWhiteSpace(Destination))
            {
                invalidReason = "Invalid or missing Destination in webhook";
                return false;
            }
            if (Uri.TryCreate(Destination, UriKind.Absolute, out var _uri))
            {
                Destination = _uri.ToString();
                DestinationIsLocal = false;
            }
            else
            {
                if (!Context.Root.UriIsValid(Destination, out var error, out var resource, out var components))
                {
                    if (!DestinationHasChanged)
                    {
                        ErrorMessage = $"The Destination URL of webhook '{Label ?? Id}' is no longer valid, and has been changed to " +
                                       "protect against unsafe behavior. Please change the destination to a valid local URI " +
                                       "to repair the webhook. Previous Destination: " + Destination;
                        Destination = $"/{Resource<Echo>.ResourceSpecifier}/Info={WebUtility.UrlEncode(ErrorMessage)}";
                        IsPaused = true;
                        invalidReason = null;
                        return true;
                    }
                    ErrorMessage = null;
                    invalidReason = "Invalid Destination URI syntax. Was not an absolute URI, and failed validation " +
                                    "as a local URI. " + error.Headers.Info;
                    return false;
                }
                Destination = components.ToUriString();
                DestinationIsLocal = true;
                if (RESTarConfig.RequireApiKey)
                {
                    switch (Headers.Authorization)
                    {
                        case Authenticator.AuthHeaderMask: break;
                        case string _:
                            LocalDestinationAPIKey = Authenticator.GetAccessRights(Headers)?.ApiKey;
                            break;
                        default:
                            LocalDestinationAPIKey = null;
                            break;
                    }
                    var context = Context.Webhook(LocalDestinationAPIKey, out var authError);
                    if (authError != null)
                    {
                        Headers.Authorization = null;
                        LocalDestinationAPIKey = null;
                        invalidReason = "Missing or invalid 'Authorization' header. Webhooks with local destinations require a " +
                                        "valid API key to be included in the 'Authorization' header.";
                        return false;
                    }

                    if (!context.MethodIsAllowed(POST, resource, out var methodError))
                    {
                        invalidReason = $"Authorization error: {methodError.Headers.Info}";
                        return false;
                    }
                }
            }

            #endregion

            #region Event

            if (string.IsNullOrWhiteSpace(Event))
            {
                invalidReason = "Invalid or missing Event in webhook";
                return false;
            }
            if (Db.SQL<Event>(Admin.Event.ByName, Event).FirstOrDefault() is Event @event)
                Event = @event.Name;
            else
            {
                if (!EventHasChanged)
                {
                    ErrorMessage = $"The Event '{Event}' of webhook '{Label ?? Id}' is no longer available. Please change the 'Event' " +
                                   "property to a valid event to repair the webhook. To list available events, use the 'RESTar.Event' resource.";
                    IsPaused = true;
                    invalidReason = null;
                    return true;
                }
                invalidReason = $"Unknown event '{Event}'. To list available events, use the RESTar.Event resource";
                return false;
            }

            #endregion

            #region Custom request

            if (CustomRequest != null)
            {
                if (!CustomRequest.IsValid(this, out var _invalidReason, out var errorMessage))
                {
                    invalidReason = _invalidReason;
                    return false;
                }
                if (errorMessage != null)
                {
                    ErrorMessage = errorMessage;
                    invalidReason = null;
                    return true;
                }
            }

            #endregion

            invalidReason = null;
            return true;
        }

        private IRequest GetLocalPostRequest(out Results.Error error)
        {
            var context = Context.Webhook(LocalDestinationAPIKey, out error);
            return error != null ? null : context.CreateRequest(Destination, POST, headers: Headers.ToTransient());
        }

        private HttpRequestMessage GetRequestMessage(Stream body, ContentType? contentType)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, Destination);
            if (body != null)
            {
                var content = new StreamContent(body);
                content.Headers.ContentType = contentType.GetValueOrDefault();
                message.Content = content;
            }
            foreach (var header in Headers) message.Headers.TryAddWithoutValidation(header.Key, header.Value);
            message.Headers.Add("RESTar-webhook-id", Id);
            return message;
        }

        internal async Task Post<T>(IEventInternal<T> @event) where T : class
        {
            if (IsPaused) return;

            Stream body;
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
                            CheckIfValid();
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
                    (body, contentType) = (result.Body, result.Headers.ContentType.GetValueOrDefault());
                }
            }

            try
            {
                if (DestinationIsLocal)
                {
                    using (var request = GetLocalPostRequest(out var requestError))
                    {
                        request.SetBody(body, contentType);
                        if (requestError != null)
                        {
                            await WebhookLog.Log(this, true, requestError.LogMessage);
                            return;
                        }
                        using (var result = request.Evaluate())
                        {
                            switch (result)
                            {
                                case Results.Error error:
                                    await WebhookLog.Log(this, true, error.LogMessage);
                                    CheckIfValid();
                                    error.Dispose();
                                    break;
                                default:
                                    await WebhookLog.Log(this, false, result.LogMessage, request.GetBody().ContentLength.GetValueOrDefault());
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    using (var requestMessage = GetRequestMessage(body, contentType))
                    using (var response = await HttpClient.SendAsync(requestMessage))
                        await WebhookLog.Log(this, false, $"{response.StatusCode}: {response.ReasonPhrase}",
                            requestMessage.Content?.Headers.ContentLength ?? 0);
                }
            }
            catch (Exception e)
            {
                await WebhookLog.Log(this, true, e.ToString());
            }
        }

        private static string ForCustomRequest(string message) => $"Error when evaluating custom request: {message}";

        internal static void Check() => Db.TransactAsync(() => Db
            .SQL<Webhook>(All)
            .ForEach(wh => wh.CheckIfValid())
        );
    }
}