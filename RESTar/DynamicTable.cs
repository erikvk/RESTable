using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using Dynamit;
using RESTar.Dynamit;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class DynamicTable :
        Resource,
        IInserter<DynamicTable>,
        IDeleter<DynamicTable>
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                var mapping = DB.Get<ResourceMapping>("Alias", _name);
                if (mapping != null)
                    mapping.Alias = value;
                _name = value;
            }
        }

        public string TableId;
        public string KvpTableId => Table?.GetAttribute<DDictionaryAttribute>().KeyValuePairTable.FullName;

        [IgnoreDataMember]
        public Type Table
        {
            get { return DynamitControl.GetByTableId(TableId); }
            set
            {
                TableId = value.FullName;
                Type = value;
            }
        }

        public void Insert(IEnumerable<DynamicTable> entities, IRequest request)
        {
            var dynamicTables = entities.ToList();
            foreach (var entity in dynamicTables)
            {
                var name = entity.Name ?? $"DynamicTable_{entity.GetObjectID()}";
                if (ResourceMapping.FindByAlias(name) != null)
                {
                    foreach (var _entity in dynamicTables)
                        Db.Transact(() => _entity.Delete());
                    throw new AbortedInserterException($"Alias '{name}' is used to refer to another resource");
                }
                Db.Transact(() =>
                {
                    entity.Table = DynamitControl.AllocateNewTable(name);
                    entity.Name = name;
                });
            }
        }

        public void Delete(IEnumerable<DynamicTable> entities, IRequest request)
        {
            foreach (var entity in entities)
            {
                Db.Transact(() =>
                {
                    DynamitControl.ClearTable(entity.TableId);
                    foreach (var mapping in DB.All<ResourceMapping>("Resource", entity.TableId))
                        mapping.Delete();
                    Db.Delete(entity);
                });
            }
        }
    }
}