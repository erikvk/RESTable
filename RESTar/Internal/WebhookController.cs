using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTar.Admin;
using RESTar.Meta.Internal;
using Starcounter;

namespace RESTar.Internal
{
    internal static class WebhookController
    {
        private static IEnumerable<Webhook> GetHooks<T>(IEventInternal<T> @event) where T : class
        {
            return Db.SQL<Webhook>(Webhook.ByEventName, @event.Name);
        }

        internal static async Task Post<T>(IEventInternal<T> @event) where T : class
        {
            await Task.WhenAll(GetHooks(@event).Select(hook => hook.Post(@event)));
        }
    }
}