using System;
using System.Collections.Generic;
using System.Reflection;
using Dynamit;
using RESTar.Internal;
using RESTar.Operations;
using static System.Reflection.BindingFlags;

namespace RESTar.Resources
{
    internal class DynamicResourceProvider : ResourceProvider<object>
    {
        #region Skipped

        internal override bool Include(Type type)
        {
            return false;
        }

        internal override void MakeClaimRegular(IEnumerable<Type> types)
        {
        }

        internal override void MakeClaimWrapped(IEnumerable<Type> types)
        {
        }

        internal override void Validate()
        {
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override Type AttributeType { get; }

        public override Selector<T> GetDefaultSelector<T>()
        {
            throw new NotImplementedException();
        }

        public override Inserter<T> GetDefaultInserter<T>()
        {
            throw new NotImplementedException();
        }

        public override Updater<T> GetDefaultUpdater<T>()
        {
            throw new NotImplementedException();
        }

        public override Deleter<T> GetDefaultDeleter<T>()
        {
            throw new NotImplementedException();
        }

        public override Counter<T> GetDefaultCounter<T>()
        {
            throw new NotImplementedException();
        }

        public override Profiler<T> GetProfiler<T>()
        {
            throw new NotImplementedException();
        }

        #endregion

        private readonly MethodInfo DynamicBuilderMethod;

        internal DynamicResourceProvider()
        {
            DynamicBuilderMethod = typeof(DynamicResourceProvider).GetMethod(nameof(_BuildDynamicResource), NonPublic | Instance);
        }

        internal void BuildDynamicResource(DynamicResource resource) => DynamicBuilderMethod
            .MakeGenericMethod(resource.Table)
            .Invoke(this, new object[] {resource});

        private void _BuildDynamicResource<T>(DynamicResource resource) where T : DDictionary => new Internal.Resource<T>
        (
            name: resource.Name,
            attribute: resource.Attribute,
            selector: DDictionaryOperations<T>.Select,
            inserter: DDictionaryOperations<T>.Insert,
            updater: DDictionaryOperations<T>.Update,
            deleter: DDictionaryOperations<T>.Delete,
            counter: DDictionaryOperations<T>.Count,
            profiler: DDictionaryOperations<T>.Profile,
            provider: this
        );
    }
}