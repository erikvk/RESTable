﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.Requests.Filters
{
    /// <summary>
    /// Orders the entities in an IEnumerable based on the values for some property
    /// </summary>
    public abstract class OrderBy : IFilter
    {
        internal Term Term { get; }
        internal string Key => Term.Key;
        internal IEntityResource Resource { get; }
        internal bool Skip { get; set; }

        protected async ValueTask<object?> Selector<T>(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            try
            {
                var termValue = await Term.GetValue(entity).ConfigureAwait(false);
                return termValue.Value;
            }
            catch
            {
                return default;
            }
        }

        internal OrderBy(IEntityResource resource, Term term)
        {
            Resource = resource;
            Term = term;
        }

        /// <summary>
        /// Applies the order by operation on an IEnumerable of entities
        /// </summary>
        public abstract IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : class;

        internal abstract OrderBy GetCopy();
    }
}