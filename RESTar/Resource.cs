using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RESTar.Internal;
using static RESTar.Internal.DynamicResource;

namespace RESTar
{
    [RESTar(RESTarPresets.ReadAndWrite)]
    public class Resource : IOperationsProvider<Resource>
    {
        public string Name { get; set; }
        public bool Editable { get; private set; }
        public RESTarMethods[] AvailableMethods { get; set; }
        public string Alias { get; set; }

        [DataMember(Name = "TargetType")]
        public string TargetTypeString => TargetType?.FullName;

        [IgnoreDataMember]
        public Type TargetType { get; set; }

        public IEnumerable<Resource> Select(IRequest request)
        {
            return RESTarConfig.Resources
                .Filter(request.Conditions)
                .Select(m => new Resource
                {
                    Name = m.Name,
                    Alias = m.Alias,
                    AvailableMethods = m.AvailableMethods,
                    Editable = m.Editable,
                    TargetType = m.TargetType
                });
        }

        public int Insert(IEnumerable<Resource> resources, IRequest request)
        {
            var count = 0;
            var dynamicTables = resources.ToList();
            try
            {
                foreach (var entity in dynamicTables)
                {
                    if (string.IsNullOrEmpty(entity.Alias))
                        throw new Exception("No Alias for new resource");
                    if (DB.Exists<ResourceAlias>("Alias", entity.Alias))
                        throw new Exception($"Invalid Alias: '{entity.Alias}' is used to refer to another resource");
                    entity.AvailableMethods = RESTarConfig.Methods;
                    MakeTable(entity);
                    count += 1;
                }
            }
            catch (Exception e)
            {
                throw new AbortedInserterException(e, $"Invalid resource: {e.Message}");
            }
            return count;
        }

        public int Update(IEnumerable<Resource> entities, IRequest request)
        {
            var count = 0;
            foreach (var resource in entities)
            {
                DeleteTable(resource);
                MakeTable(resource);
                count += 1;
            }
            return count;
        }

        public int Delete(IEnumerable<Resource> entities, IRequest request)
        {
            var count = 0;
            foreach (var resource in entities)
            {
                DeleteTable(resource);
                count += 1;
            }
            return count;
        }
    }
}