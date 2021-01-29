using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Resources;
using RESTable.Linq;

namespace RESTable.Meta.Internal
{
    internal class TerminalResourceProvider
    {
        internal void RegisterTerminalTypes(List<Type> terminalTypes)
        {
            terminalTypes.OrderBy(t => t.GetRESTableTypeName()).ForEach(type =>
            {
                var resource = (IResource) BuildTerminalMethod.MakeGenericMethod(type).Invoke(this, null);
                RESTableConfig.AddResource(resource);
            });
            Shell.TerminalResource = Meta.TerminalResource<Shell>.Get;
        }

        internal TerminalResourceProvider()
        {
            BuildTerminalMethod = typeof(TerminalResourceProvider).GetMethod
            (
                name: nameof(MakeTerminalResource),
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic
            );
        }

        private readonly MethodInfo BuildTerminalMethod;
        private IResource MakeTerminalResource<T>() where T : class, ITerminal => new TerminalResource<T>();
    }
}