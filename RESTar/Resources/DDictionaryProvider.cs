using System;
using Dynamit;
using RESTar.Internal;
using RESTar.Operations;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.Resources
{
    internal class DDictionaryProvider : ResourceProvider<DDictionary>
    {
        internal override bool Include(Type type) => type.IsSubclassOf(typeof(DDictionary))
                                                     && type.HasNoResourceProviderAttributes();

        internal override void Validate()
        {
        }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override Type AttributeType { get; }

        public override Selector<T> GetDefaultSelector<T>() => DDictionaryOperations<T>.Select;
        public override Inserter<T> GetDefaultInserter<T>() => DDictionaryOperations<T>.Insert;
        public override Updater<T> GetDefaultUpdater<T>() => DDictionaryOperations<T>.Update;
        public override Deleter<T> GetDefaultDeleter<T>() => DDictionaryOperations<T>.Delete;
        public override Counter<T> GetDefaultCounter<T>() => DDictionaryOperations<T>.Count;
        public override Profiler GetProfiler<T>() => DDictionaryOperations<T>.Profile;

        public override bool IsValid(Type type, out string reason)
        {
            if (type.Implements(typeof(IProfiler)))
            {
                reason = $"Invalid IProfiler interface implementation for resource type '{type.FullName}'. " +
                         "Starcounter resources use their default profilers, and cannot implement IProfiler";
                return false;
            }
            reason = null;
            return true;
        }
    }
}