using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Admin;
using RESTar.Dynamic;
using RESTar.Meta;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;
using Starcounter;

namespace RESTar.Internal.Sc
{
    internal class DynamitResourceProvider : EntityResourceProvider<DDictionary>, IProceduralEntityResourceProvider
    {
        internal override bool Include(Type type)
        {
            if (type.IsWrapper())
                return type.GetWrappedType().IsSubclassOf(typeof(DDictionary)) && !type.HasResourceProviderAttribute();
            return type.IsSubclassOf(typeof(DDictionary)) && !type.HasResourceProviderAttribute();
        }

        internal override void Validate() { }

        // ReSharper disable once UnassignedGetOnlyAutoProperty
        protected override Type AttributeType { get; }

        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => DDictionaryOperations<T>.Select(request);
        protected override int DefaultInsert<T>(IRequest<T> request) => DDictionaryOperations<T>.Insert(request);
        protected override int DefaultUpdate<T>(IRequest<T> request) => DDictionaryOperations<T>.Update(request);
        protected override int DefaultDelete<T>(IRequest<T> request) => DDictionaryOperations<T>.Delete(request);
        protected override ResourceProfile DefaultProfile<T>(IEntityResource<T> resource) => DDictionaryOperations<T>.Profile(resource);

        public override IDatabaseIndexer DatabaseIndexer { get; }
        internal DynamitResourceProvider(IDatabaseIndexer databaseIndexer) => DatabaseIndexer = databaseIndexer;

        protected override bool IsValid(IEntityResource resource, out string reason) =>
            StarcounterOperations<object>.IsValid(resource, out reason);

        private static bool Exists(Type type) => Db.SQL<DynamicResource>(DynamicResource.ByTableName, type.RESTarTypeName()).FirstOrDefault() != null;

        protected override IEnumerable<IProceduralEntityResource> SelectProceduralResources() => Db
            .SQL<DynamicResource>(DynamicResource.All)
            .Where(resource =>
            {
                var resourceObjectLost = resource.Type == null;
                if (resourceObjectLost)
                {
                    Db.TransactAsync(resource.Delete);
                    return false;
                }
                return true;
            })
            .ToList();

        protected override IProceduralEntityResource InsertProceduralResource(string name, string description, Method[] methods, dynamic data)
        {
            DynamicResource proceduralResource = null;
            Db.TransactAsync(() =>
            {
                var newTable = DynamitControl.DynamitTypes.FirstOrDefault(type => !Exists(type)) ?? throw new NoAvailableDynamicTable();
                proceduralResource = new DynamicResource(name, newTable, methods, description);
            });
            return proceduralResource;
        }

        protected override void SetProceduralResourceMethods(IProceduralEntityResource resource, Method[] methods) =>
            Db.TransactAsync(() =>
            {
                var _resource = (DynamicResource) resource;
                _resource.Methods = methods;
            });

        protected override void SetProceduralResourceDescription(IProceduralEntityResource resource, string newDescription) =>
            Db.TransactAsync(() =>
            {
                var _resource = (DynamicResource) resource;
                _resource.Description = newDescription;
            });

        protected override bool DeleteProceduralResource(IProceduralEntityResource resource)
        {
            var _resource = (DynamicResource) resource;
            DynamitControl.ClearTable(_resource.TableName);
            Db.TransactAsync(_resource.Delete);
            return true;
        }
    }
}