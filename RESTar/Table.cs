using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadOnly)]
    public class Table : 
        Resource, 
        ISelector<object>,
        IInserter<object>, 
        IUpdater<object>, 
        IDeleter<object>
    {
        public int NrOfColumns => Schema.Count;
        public long NrOfRows => DB.RowCount(Locator);

        public IEnumerable<dynamic> Select(IRequest request)
        {
            return DB.Select(request);
        }

        public void Insert(IEnumerable<dynamic> entities, IRequest request)
        {
        }

        public void Update(IEnumerable<dynamic> entities, IRequest request)
        {
        }

        public void Delete(IEnumerable<dynamic> entities, IRequest request)
        {
            foreach (var entity in entities)
                Db.Transact(() => { Db.Delete(entity); });
        }

        public IDictionary<string, string> Schema
        {
            get
            {
                var properties = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                var dict = new Dictionary<string, string>();
                foreach (var property in properties)
                {
                    if (!property.HasAttribute<IgnoreDataMemberAttribute>())
                    {
                        var alias = property.GetAttribute<DataMemberAttribute>()?.Name;
                        dict[alias ?? property.Name] = property.PropertyType.FullName;
                    }
                }
                return dict;
            }
        }

        public Table(Type type)
        {
            Type = type;
        }
    }
}