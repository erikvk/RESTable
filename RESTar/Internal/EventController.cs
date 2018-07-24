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

        internal static async Task Raise<TEvent, TPayload>(TEvent @event)
            where TEvent : class, IEventInternal<TPayload> where TPayload : class
        {
            if (!RESTarConfig.Initialized) return;
            if (Db.SQL<Event>(Event.ByName, @event.Name).FirstOrDefault() == null)
                throw new UnknownEventTypeException(@event);
            var hookTask = WebhookController.Post(@event);
            Events.Custom<TEvent>.OnRaise(@event);
            await hookTask;
        }
    }
}