using System;
using RESTar.Internal.Auth;
using RESTar.Requests;
using RESTar.Results;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    internal class WebhookContext : Context
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        private string ApiKey { get; }

        private WebhookContext(string apiKey) : base(Client.Webhook)
        {
            ApiKey = apiKey;
        }

        internal static bool TryCreate(string apiKey, out Context context, out Error error)
        {
            var _context = new WebhookContext(apiKey);
            var accessRights = Authenticator.GetAccessRights(apiKey);
            if (!RESTarConfig.RequireApiKey)
                accessRights = AccessRights.Root;
            if (accessRights == null)
            {
                context = null;
                error = new NotAuthorized();
                return false;
            }
            context = _context;
            context.Client.AccessRights = accessRights;
            error = null;
            return true;
        }
    }
}