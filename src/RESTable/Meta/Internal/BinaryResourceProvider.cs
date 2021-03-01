using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Resources.Operations;
using RESTable.Linq;

namespace RESTable.Meta.Internal
{
    public class BinaryResourceProvider
    {
        private MethodInfo BuildBinaryMethod { get; }
        private TypeCache TypeCache { get; }
        private ResourceCollection ResourceCollection { get; }

        public BinaryResourceProvider(TypeCache typeCache, ResourceCollection resourceCollection)
        {
            BuildBinaryMethod = typeof(BinaryResourceProvider).GetMethod(nameof(MakeBinaryResource), BindingFlags.Instance | BindingFlags.NonPublic);
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
        }

        internal void RegisterBinaryTypes(IEnumerable<Type> binaryTypes) => binaryTypes
            .OrderBy(t => t.GetRESTableTypeName())
            .ForEach(type =>
            {
                var resource = (IResource) BuildBinaryMethod.MakeGenericMethod(type).Invoke(this, null);
                ResourceCollection.AddResource(resource);
            });


        private IResource MakeBinaryResource<T>() where T : class, Resources.IBinary<T>
        {
            var binarySelector = DelegateMaker.GetDelegate<BinarySelector<T>>(typeof(T));
            return new BinaryResource<T>(binarySelector, TypeCache);
        }
    }
}