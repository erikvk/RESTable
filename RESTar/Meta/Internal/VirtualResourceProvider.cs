using System;
using System.Collections.Generic;
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