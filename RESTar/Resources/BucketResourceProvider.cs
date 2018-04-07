using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Operations;
using static System.Reflection.BindingFlags;

namespace RESTar.Resources
{
    internal class BucketResourceProvider
    {
        internal void RegisterBucketTypes(List<Type> bucketTypes) => bucketTypes
            .OrderBy(t => t.RESTarTypeName())
            .ForEach(type =>
            {
                var resource = (IResource) BuildBucketMethod.MakeGenericMethod(type).Invoke(this, null);
                RESTarConfig.AddResource(resource);
            });

        internal BucketResourceProvider() => BuildBucketMethod = typeof(BucketResourceProvider)
            .GetMethod(nameof(MakeBucketResource), Instance | NonPublic);

        private readonly MethodInfo BuildBucketMethod;
        private IResource MakeBucketResource<T>() where T : class, IBucket<T>
        {
            var binarySelector = DelegateMaker.GetDelegate<BinarySelector<T>>(typeof(T));
            return new BucketResource<T>(binarySelector);
        }
    }
}