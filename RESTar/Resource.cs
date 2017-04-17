using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RESTar.Internal;
using Starcounter;

namespace RESTar
{
    [RESTar(RESTarPresets.ReadAndWrite)]
    public class Resource : IOperationsProvider<Resource>
    {
        public string Name { get; set; }
        public bool Editable { get; private set; }
        public ICollection<RESTarMethods> AvailableMethods { get; set; }
        public string Alias { get; set; }

        [DataMember(Name = "TargetType")]
        public string TargetTypeString => TargetType?.FullName;

        [IgnoreDataMember]
        public Type TargetType { get; set; }

        public IEnumerable<Resource> Select(IRequest request)
        {
            var all = RESTarConfig.Resources;
            var matches = request.EvaluateEntitites(all);
            return matches.Select(m => new Resource
            {
                Name = m.Name,
                Alias = m.Alias,
                AvailableMethods = m.AvailableMethods,
                Editable = m.Editable,
                TargetType = m.TargetType
            }).ToList();
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
                    DynamicResource.Make(entity);
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
                DynamicResource.Delete(resource);
                DynamicResource.Make(resource);
                count += 1;
            }
            return count;
        }

        public int Delete(IEnumerable<Resource> entities, IRequest request)
        {
            var count = 0;
            foreach (var resource in entities)
            {
                DynamicResource.Delete(resource);
                count += 1;
            }
            return count;
        }
    }
}