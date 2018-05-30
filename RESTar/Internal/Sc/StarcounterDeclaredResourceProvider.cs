using System;
using System.Collections.Generic;
using RESTar.Meta;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class StarcounterDeclaredResourceProvider : EntityResourceProvider<object>
    {
        protected override Type AttributeType => null;
        public override IDatabaseIndexer DatabaseIndexer => new ScIndexer();
        internal override void Validate() { }

        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
            return type.HasAttribute<DatabaseAttribute>() && !type.HasResourceProviderAttribute();
        }

        protected override Selector<T> GetDefaultSelector<T>() => StarcounterOperations<T>.Select;
        protected override Inserter<T> GetDefaultInserter<T>() => StarcounterOperations<T>.Insert;
        protected override Updater<T> GetDefaultUpdater<T>() => StarcounterOperations<T>.Update;
        protected override Deleter<T> GetDefaultDeleter<T>() => StarcounterOperations<T>.Delete;
        protected override Counter<T> GetDefaultCounter<T>() => null;
        protected override Profiler<T> GetProfiler<T>() => StarcounterOperations<T>.Profile;

        protected override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);

        protected override bool SupportsProceduralResources { get; } = false;

        protected override IEnumerable<IProceduralEntityResource> SelectProceduralResources() => throw new NotImplementedException();

        protected override IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods) =>
            throw new NotImplementedException();

        protected override void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods) =>
            throw new NotImplementedException();

        protected override void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription) =>
            throw new NotImplementedException();

        protected override bool DeleteProceduralResource(IProceduralEntityResource resource) => throw new NotImplementedException();
    }
}