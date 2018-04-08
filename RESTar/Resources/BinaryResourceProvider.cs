using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Operations;
using static System.Reflection.BindingFlags;

namespace RESTar.Resources
{
    internal class BinaryResourceProvider
    {
        internal void RegisterBinaryTypes(IEnumerable<Type> binaryTypes) => binaryTypes
            .OrderBy(t => t.RESTarTypeName())
            .ForEach(type =>
            {
                var resource = (IResource) BuildBinaryMethod.MakeGenericMethod(type).Invoke(this, null);
                RESTarConfig.AddResource(resource);
            });

        internal BinaryResourceProvider() => BuildBinaryMethod = typeof(BinaryResourceProvider)
            .GetMethod(nameof(MakeBinaryResource), Instance | NonPublic);

        private readonly MethodInfo BuildBinaryMethod;

        private IResource MakeBinaryResource<T>() where T : class, RESTar.IBinaryResource<T>
        {
            var binarySelector = DelegateMaker.GetDelegate<BinarySelector<T>>(typeof(T));
            return new BinaryResource<T>(binarySelector);
        }
    }
}