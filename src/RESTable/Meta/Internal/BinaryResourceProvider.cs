using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Resources.Operations;
using RESTable.Linq;

namespace RESTable.Meta.Internal
{
    internal class BinaryResourceProvider
    {
        internal void RegisterBinaryTypes(IEnumerable<Type> binaryTypes) => binaryTypes
            .OrderBy(t => t.GetRESTableTypeName())
            .ForEach(type =>
            {
                var resource = (IResource) BuildBinaryMethod.MakeGenericMethod(type).Invoke(this, null);
                RESTableConfig.AddResource(resource);
            });

        internal BinaryResourceProvider() => BuildBinaryMethod = typeof(BinaryResourceProvider)
            .GetMethod(nameof(MakeBinaryResource), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly MethodInfo BuildBinaryMethod;

        private IResource MakeBinaryResource<T>() where T : class, Resources.IBinary<T>
        {
            var binarySelector = DelegateMaker.GetDelegate<AsyncBinarySelector<T>>(typeof(T));
            return new BinaryResource<T>(binarySelector);
        }
    }
}