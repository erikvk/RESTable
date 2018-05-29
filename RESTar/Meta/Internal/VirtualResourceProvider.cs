using System;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar.Meta.Internal
{
    internal class VirtualResourceProvider : EntityResourceProvider<object>
    {
        internal override bool Include(Type type) => !type.HasResourceProviderAttribute();
        internal override void Validate() { }
        
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        protected override Type AttributeType { get; }

        protected override Selector<T> GetDefaultSelector<T>() => null;
        protected override Inserter<T> GetDefaultInserter<T>() => null;
        protected override Updater<T> GetDefaultUpdater<T>() => null;
        protected override Deleter<T> GetDefaultDeleter<T>() => null;
        protected override Counter<T> GetDefaultCounter<T>() => null;
        protected override Profiler<T> GetProfiler<T>() => DelegateMaker.GetDelegate<Profiler<T>>(typeof(T));
    }
}