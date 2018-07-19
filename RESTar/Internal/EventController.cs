using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RESTar.Linq;
using RESTar.Meta.Internal;
using RESTar.Resources;
using Starcounter;
using Event = RESTar.Admin.Event;

namespace RESTar.Internal
{
    internal static class EventController
    {
        internal static void Add(ICollection<Type> eventTypes) => Db.TransactAsync(() =>
        {
            Db.SQL<Event>(Event.All).ForEach(Db.Delete);
            eventTypes.ForEach(eventType => new Event(
                name: eventType.RESTarTypeName(),
                description: eventType.GetCustomAttribute<RESTarEventAttribute>().Description
            ));
        });

        internal static async Task Raise<T>(IEventInternal<T> @event) where T : class
        {
            if (!RESTarConfig.Initialized) return;
            if (!(Db.SQL<Event>(Event.ByName, @event.Name).FirstOrDefault() is Event eventType))
                throw new UnknownEventTypeException(@event);
            var hookTask = WebhookController.Post(@event);
            RaiseEventHandlers(eventType, (dynamic) @event);
            await hookTask;
        }

        private static void RaiseEventHandlers<T>(object sender, T @event) where T : EventArgs, IEvent
        {
            Event<T>.InvokeRaise(sender, @event);
        }
    }
}