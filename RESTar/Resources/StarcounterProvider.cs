using System;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;
using Profiler = RESTar.Operations.Profiler;

namespace RESTar.Resources
{
    internal class StarcounterProvider : ResourceProvider<object>
    {
        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().HasAttribute<DatabaseAttribute>() && type.HasNoResourceProviderAttributes();
            return type.HasAttribute<DatabaseAttribute>() && type.HasNoResourceProviderAttributes();
        }

        internal override void Validate()
        {
        }

        internal StarcounterProvider() => DatabaseIndexer = new StarcounterIndexer();

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public override Type AttributeType { get; }

        public override Selector<T> GetDefaultSelector<T>() => StarcounterOperations<T>.Select;
        public override Inserter<T> GetDefaultInserter<T>() => StarcounterOperations<T>.Insert;
        public override Updater<T> GetDefaultUpdater<T>() => StarcounterOperations<T>.Update;
        public override Deleter<T> GetDefaultDeleter<T>() => StarcounterOperations<T>.Delete;
        public override Counter<T> GetDefaultCounter<T>() => StarcounterOperations<T>.Count;
        public override Profiler GetProfiler<T>() => StarcounterOperations<T>.Profile;

        public override bool IsValid(Type type, out string reason)
        {
            if (type.Implements(typeof(IProfiler)))
            {
                reason = $"Invalid IProfiler interface implementation for resource type '{type.FullName}'. " +
                         "DDictionary resources use their default profilers, and cannot implement IProfiler";
                return false;
            }
            reason = null;
            return true;
        }
    }
}