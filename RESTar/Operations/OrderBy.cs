using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Internal;

namespace RESTar.Operations
{
    public class OrderBy : IFilter
    {
        internal IResource Resource;
        public bool Descending;
        public bool Ascending => !Descending;
        public string Key => PropertyChain.Key;
        internal PropertyChain PropertyChain;
        internal bool IsStarcounterQueryable = true;

        private Func<T1, dynamic> ToSelector<T1>() => item => Do.Try(() => PropertyChain.GetValue(item), null);
        private void Migrate(Type type) => PropertyChain.Migrate(type);

        public string SQL => IsStarcounterQueryable
            ? $"ORDER BY t.{PropertyChain.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}"
            : null;

        internal OrderBy()
        {
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (IsStarcounterQueryable) return entities;
            if (typeof(T) != Resource.TargetType) Migrate(typeof(T));
            if (entities is IEnumerable<IDictionary<string, dynamic>> && !(entities is IEnumerable<DDictionary>))
                PropertyChain.MakeDynamic();
            return Ascending
                ? entities.OrderBy(ToSelector<T>())
                : entities.OrderByDescending(ToSelector<T>());
        }

        //        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        //        {
        //            if (IsStarcounterQueryable) return entities;
        //            if (typeof(T) != Resource.TargetType) Migrate(typeof(T));
        //            if (entities is IEnumerable<DDictionary>)
        //                return Ascending
        //                    ? entities.OrderBy(dict => (dict as DDictionary)?.SafeGetNoCase(Key)?.ToString() ?? "",
        //                        new NumericComparer())
        //                    : entities.OrderByDescending(dict => (dict as DDictionary)?.SafeGetNoCase(Key)?.ToString() ?? "",
        //                        new NumericComparer());
        //            if (entities is IEnumerable<IDictionary<string, dynamic>>)
        //            {
        //                PropertyChain.MakeDynamic();
        //                return Ascending
        //                    ? entities.OrderBy<T, string>(
        //                        dict => (dict as Dictionary<string, dynamic>)?.SafeGetNoCase(Key)?.ToString() ?? "",
        //                        new NumericComparer())
        //                    : entities.OrderByDescending<T, string>(
        //                        dict => (dict as Dictionary<string, dynamic>)?.SafeGetNoCase(Key)?.ToString() ?? "",
        //                        new NumericComparer());
        //            }
        //            return Ascending
        //                ? entities.OrderBy(ToSelector<T>())
        //                : entities.OrderByDescending(ToSelector<T>());
        //        }
    }
}