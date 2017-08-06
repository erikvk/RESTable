using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.Auth
{
    internal class AccessRights : Dictionary<IResource, RESTarMethods[]>
    {
        internal static AccessRights Root { get; set; }

        internal AccessRights()
        {
        }

        internal AccessRights(IDictionary<IResource, RESTarMethods[]> other) : base(other)
        {
        }

        internal new RESTarMethods[] this[IResource resource]
        {
            get => ContainsKey(resource) ? base[resource] : null;
            set => base[resource] = value;
        }

        internal void AddOpenResources() => RESTarConfig.Resources.ForEach(r =>
        {
            if (!r.Type.HasAttribute<OpenResourceAttribute>(out var attribute)) return;
            if (!ContainsKey(r)) this[r] = attribute.AvailableMethods?.ToArray() ?? r.AvailableMethods.ToArray();
        });
    }
}