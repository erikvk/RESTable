using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Resources;
using RESTable.Linq;

namespace RESTable.Meta.Internal
{
    public class TerminalResourceProvider
    {
        private MethodInfo BuildTerminalMethod { get; }
        private TypeCache TypeCache { get; }
        private ResourceCollection ResourceCollection { get; }

        public TerminalResourceProvider(TypeCache typeCache, ResourceCollection resourceCollection)
        {
            BuildTerminalMethod = typeof(TerminalResourceProvider).GetMethod
            (
                name: nameof(MakeTerminalResource),
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic
            );
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
        }

        public void RegisterTerminalTypes(List<Type> terminalTypes)
        {
            terminalTypes.OrderBy(t => t.GetRESTableTypeName()).ForEach(type =>
            {
                var resource = (IResource) BuildTerminalMethod.MakeGenericMethod(type).Invoke(this, null);
                ResourceCollection.AddResource(resource);
            });
            Shell.TerminalResource = ResourceCollection.GetTerminalResource<Shell>();
        }

        private IResource MakeTerminalResource<T>() where T : Terminal => new TerminalResource<T>(TypeCache);
    }
}