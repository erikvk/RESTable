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

        public override Selector<T> GetDefaultSelector<T>() => null;
        public override Inserter<T> GetDefaultInserter<T>() => null;
        public override Updater<T> GetDefaultUpdater<T>() => null;
        public override Deleter<T> GetDefaultDeleter<T>() => null;
        public override Counter<T> GetDefaultCounter<T>() => null;
        public override Profiler<T> GetProfiler<T>() => DelegateMaker.GetDelegate<Profiler<T>>(typeof(T));
    }
}