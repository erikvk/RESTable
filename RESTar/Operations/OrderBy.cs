﻿using System;
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

        public string SQL => IsStarcounterQueryable
            ? $"ORDER BY t.{PropertyChain.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}"
            : null;

        internal OrderBy()
        {
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (IsStarcounterQueryable) return entities;
            if (entities is IEnumerable<IDictionary<string, dynamic>> && !(entities is IEnumerable<DDictionary>))
                PropertyChain.MakeDynamic();
            else if (typeof(T) != Resource.TargetType)
                PropertyChain.Migrate(typeof(T));
            return Ascending
                ? entities.OrderBy(ToSelector<T>())
                : entities.OrderByDescending(ToSelector<T>());
        }
    }
}