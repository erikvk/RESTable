using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RESTar.ContentTypeProviders.NativeJsonProtocol;
using RESTar.Internal.Auth;
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

        static Webhook()
        {
            HttpClient = new HttpClient();
            Event<Webhook>.OnInsert += Check;
            Event<Webhook>.OnUpdate += Check;
        }

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
                        Headers.Authorization = null;
                        break;
                    case var external when Uri.TryCreate(external, UriKind.Absolute, out var uri):
                        DestinationURL = uri.ToString();
                        if (DestinationIsLocal)
                            Headers.Authorization = null;
                        DestinationIsLocal = false;
                        DestinationError = null;
                        break;
                    case var local when Context.Root.UriIsValid(local, out var error, out _, out var components) is var isValid:
                        if (isValid)
                        {
                            DestinationURL = components.ToUriString();
                            if (!DestinationIsLocal)
                                Headers.Authorization = null;
                            DestinationIsLocal = true;
                            DestinationError = null;
                        }
                        else
                        {
                            DestinationError = local;
                            DestinationError = error.LogMessage;
                        }
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
        [JsonConverter(typeof(HeadersConverter<Headers>), "*")]
        public DbHeaders Headers
        {
            get;

            //get => HeadersString == null ? new Headers() : JsonConvert.DeserializeObject<Headers>(HeadersString);
            //set
            //{
            //    switch (value.Authorization)
            //    {
            //        // No header present. If was removed, make sure api key is null
            //        case null:
            //            LocalDestinationAPIKey = null;
            //            break;

            //        // Has been set to authheadermask manually. Set header to null.
            //        case Authenticator.AuthHeaderMask when LocalDestinationAPIKey == null:
            //            value.Authorization = null;
            //            break;

            //        // No change
            //        case Authenticator.AuthHeaderMask: break;

            //        // Has value, and request destination is local
            //        case var localAuthHeader when DestinationIsLocal:
            //            LocalDestinationAPIKey = Authenticator.GetAccessRights(localAuthHeader)?.ApiKey;
            //            value.Authorization = Authenticator.AuthHeaderMask;
            //            break;
            //    }
            //    HeadersString = JsonConvert.SerializeObject(value);
            //}
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

        /// <inheritdoc />
        public Webhook() => Headers = new DbHeaders();

        private static IEnumerable<Webhook> Check(IEnumerable<Webhook> entities) => RESTarConfig.RequireApiKey
            ? entities.Select(webhook =>
            {
                // Check destination auth
                if (webhook.DestinationIsLocal && webhook.DestinationIsValid)
                {
                    AccessRights accessRights;

                    if (webhook.LocalDestinationAPIKey == null)
                    {
                        accessRights = Authenticator.GetAccessRights(webhook.Headers);
                        webhook.LocalDestinationAPIKey = accessRights?.ApiKey;
                        if (accessRights == null)
                        {
                            webhook.Headers.Authorization = null;
                            webhook.AuthorizationError = "The destination is a local resource, but found no valid " +
                                                         "'Authorization' header in the webhook headers";
                        }

                        if (webhook.Headers.Authorization is string header && header != Authenticator.AuthHeaderMask)
                        {
                            webhook.Headers.Authorization = Authenticator.AuthHeaderMask;
                        }
                        else { }
                    }
                    else
                    {
                        accessRights = Authenticator.ApiKeys.SafeGet(webhook.LocalDestinationAPIKey);

                        //switch (Authenticator.GetAccessRights())
                        //{
                        //    case null:
                        //        webhook.AuthorizationError = "The destination is a local resource, but no valid API key was " +
                        //                                     "found in the 'Authorization' header of the webhook.";
                        //        break;
                        //    case var ar when
                        //}

                        // check access
                    }
                }

                // Check custom request auth
                if (webhook.CustomRequest != null) { }
                return webhook;
            })
            : entities;

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
            return Context.Webhook(client).CreateRequest(DestinationURL, POST, headers: Headers.ToTransient());
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
                        if (Headers is DbHeaders _headers)
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