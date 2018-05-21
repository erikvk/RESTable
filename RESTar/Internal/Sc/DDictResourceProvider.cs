using System;
using Dynamit;
using RESTar.Meta;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar.Internal.Sc
{
    internal class DDictResourceProvider : EntityResourceProvider<DDictionary>
    {
        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().IsSubclassOf(typeof(DDictionary)) && !type.HasResourceProviderAttribute();
            return type.IsSubclassOf(typeof(DDictionary)) && !type.HasResourceProviderAttribute();
        }

        internal override void Validate() { }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override Type AttributeType { get; }

        public override Selector<T> GetDefaultSelector<T>() => DDictionaryOperations<T>.Select;
        public override Inserter<T> GetDefaultInserter<T>() => DDictionaryOperations<T>.Insert;
        public override Updater<T> GetDefaultUpdater<T>() => DDictionaryOperations<T>.Update;
        public override Deleter<T> GetDefaultDeleter<T>() => DDictionaryOperations<T>.Delete;
        public override Counter<T> GetDefaultCounter<T>() => null;
        public override Profiler<T> GetProfiler<T>() => DDictionaryOperations<T>.Profile;

        public override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);
    }
}