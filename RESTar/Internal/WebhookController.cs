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
        private static IEnumerable<Webhook> GetHooks(IEventInternal @event) => Db.SQL<Webhook>(Webhook.ByEventName, @event.Name);
        internal static async Task Post(IEventInternal @event) => await Task.WhenAll(GetHooks(@event).Select(hook => hook.Post(@event)));
    }
}