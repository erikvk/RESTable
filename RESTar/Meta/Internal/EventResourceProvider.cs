using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class EventResourceProvider
    {
        internal void RegisterEventTypes(IEnumerable<Type> eventTypes) => eventTypes
            .OrderBy(t => t.RESTarTypeName())
            .ForEach(type =>
            {
                var payloadType = type.GetGenericTypeParameters(typeof(Event<>))[0];
                var resource = (IResource) BuildEventMethod.MakeGenericMethod(type, payloadType).Invoke(this, null);
                RESTarConfig.AddResource(resource);
            });

        internal EventResourceProvider() => BuildEventMethod = typeof(EventResourceProvider)
            .GetMethod(nameof(MakeEventResource), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly MethodInfo BuildEventMethod;

        private IResource MakeEventResource<TEvent, TPayload>() where TEvent : Event<TPayload> where TPayload : class
        {
            return new EventResource<TEvent, TPayload>();
        }
    }
}