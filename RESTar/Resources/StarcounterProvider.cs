using System;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;

namespace RESTar.Resources
{
    internal class StarcounterProvider : ResourceProvider<object>
    {
        public override Type AttributeType => null;
        internal StarcounterProvider() => DatabaseIndexer = new StarcounterIndexer();
        internal override void Validate() { }
        
        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
            return type.HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
        }

        public override Selector<T> GetDefaultSelector<T>() => StarcounterOperations<T>.Select;
        public override Inserter<T> GetDefaultInserter<T>() => StarcounterOperations<T>.Insert;
        public override Updater<T> GetDefaultUpdater<T>() => StarcounterOperations<T>.Update;
        public override Deleter<T> GetDefaultDeleter<T>() => StarcounterOperations<T>.Delete;
        public override Counter<T> GetDefaultCounter<T>() => StarcounterOperations<T>.Count;
        public override Profiler<T> GetProfiler<T>() => StarcounterOperations<T>.Profile;

        public override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);
    }
}