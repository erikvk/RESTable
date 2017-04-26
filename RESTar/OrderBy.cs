using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Internal;
using static RESTar.Do;

namespace RESTar
{
    public class OrderBy
    {
        public bool Descending;
        public bool Ascending => !Descending;
        public string Key => PropertyChain.Key;
        internal PropertyChain PropertyChain;

        internal OrderBy()
        {
        }

        public string SQL => $"ORDER BY t.{PropertyChain.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}";

        internal Func<T1, dynamic> ToSelector<T1>()
        {
            return item => Try(() => PropertyChain.GetValue(item), null);
        }

        internal ICollection<T> Evaluate<T>(IEnumerable<T> entities)
        {
            return Ascending
                ? entities.OrderBy(ToSelector<T>()).ToList()
                : entities.OrderByDescending(ToSelector<T>()).ToList();
        }

        internal void Migrate(Type type)
        {
            PropertyChain.Migrate(type);
        }
    }
}