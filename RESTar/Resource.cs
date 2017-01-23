using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Dynamit;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Resource :
        IInserter<Resource>,
        IUpdater<Resource>,
        IDeleter<Resource>
    {
        public string Name { get; private set; }
        public string AvailableMethods => Type.AvailableMethods()?.ToMethodsString();

        public string Alias
        {
            get { return DB.Get<ResourceAlias>("Resource", Name)?.Alias; }
            set
            {
                if (Name == null)
                {
                    AliasIn = value;
                    return;
                }
                var existingMapping = DB.Get<ResourceAlias>("Resource", Name);
                if (value == null)
                {
                    Db.Transact(() => { existingMapping?.Delete(); });
                    return;
                }
                var usedAliasMapping = DB.Get<ResourceAlias>("Alias", value);
                if (usedAliasMapping != null)
                {
                    if (usedAliasMapping.Resource == Name)
                        return;
                    throw new Exception($"Invalid alias: '{value}' is used to refer to another resource");
                }

                Db.Transact(() =>
                {
                    existingMapping = existingMapping ?? new ResourceAlias {Resource = Name};
                    existingMapping.Alias = value;
                });
            }
        }

        public bool Editable { get; private set; }

        [Transient, IgnoreDataMember] public string AliasIn;

        [IgnoreDataMember]
        public Type Type
        {
            get { return RESTarConfig.ResourcesDict[Name.ToLower()]; }
            set { Name = value.FullName; }
        }

        public void Insert(IEnumerable<Resource> resources, IRequest request)
        {
            var dynamicTables = resources.ToList();
            try
            {
                foreach (var entity in dynamicTables)
                {
                    var alias = string.IsNullOrEmpty(entity.AliasIn) ? entity.Name : entity.AliasIn;
                    if (DB.Exists<ResourceAlias>("Alias", alias))
                        throw new Exception($"Invalid alias: '{alias}' is used to refer to another resource");
                    Db.Transact(() =>
                    {
                        entity.Type = DynamitControl.AllocateNewTable(alias);
                        entity.Editable = true;
                    });
                }
            }
            catch (Exception e)
            {
                foreach (var resource in dynamicTables)
                    Db.Transact(() => resource.Delete());
                throw new AbortedInserterException($"Invalid resource: {e.Message}");
            }
        }

        public void Update(IEnumerable<Resource> entities, IRequest request)
        {
        }

        public void Delete(IEnumerable<Resource> entities, IRequest request)
        {
            foreach (var entity in entities.Where(e => e.Editable))
            {
                Db.Transact(() =>
                {
                    DynamitControl.ClearTable(entity.Name);
                    foreach (var mapping in DB.All<ResourceAlias>("Resource", entity.Name))
                        mapping.Delete();
                    entity.Delete();
                });
            }
        }
    }
}