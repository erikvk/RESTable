using System;
using RESTar.Operations;

namespace RESTar.Resources
{
    internal class VirtualResourceProvider : ResourceProvider<object>
    {
        internal override bool Include(Type type) => !type.HasResourceProviderAttribute();

        internal override void Validate()
        {
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override Type AttributeType { get; }

        public override Selector<T> GetDefaultSelector<T>() => null;
        public override Inserter<T> GetDefaultInserter<T>() => null;
        public override Updater<T> GetDefaultUpdater<T>() => null;
        public override Deleter<T> GetDefaultDeleter<T>() => null;
        public override Counter<T> GetDefaultCounter<T>() => null;
        public override Profiler<T> GetProfiler<T>() => DelegateMaker.GetDelegate<Profiler<T>, T>();
    }
}