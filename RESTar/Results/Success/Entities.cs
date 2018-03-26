using System;
using System.Collections;
using System.Collections.Generic;
using RESTar.Queries;
using UriComponents = RESTar.Queries.UriComponents;

namespace RESTar.Results.Success
{
    /// <inheritdoc cref="OK" />
    /// <inheritdoc cref="IEntitiesMetadata" />
    /// <summary>
    /// A result that contains a set of entities
    /// </summary>
    public sealed class Entities : Content, IEntitiesMetadata, IEnumerable<object>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<object> GetEnumerator() => Content.GetEnumerator();

        /// <summary>
        /// The entities contained in the result
        /// </summary>
        private IEnumerable<object> Content { get; }

        /// <summary>
        /// The number of entities in the result
        /// </summary>
        public ulong EntityCount { get; set; }

        string IEntitiesMetadata.ResourceFullName => Query.Resource.Name;
        internal string ExternalDestination { get; }

        /// <summary>
        /// Is the result paged?
        /// </summary>
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Query.MetaConditions.Limit;

        internal Entities(IQuery query, IEnumerable<object> content) : base(query)
        {
            Content = content ?? new object[0];
            ExternalDestination = query.Headers.Destination;
            TimeElapsed = query.TimeElapsed;
        }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Query.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        /// <inheritdoc />
        public IUriComponents GetNextPageLink() => GetNextPageLink(-1);

        /// <inheritdoc />
        public IUriComponents GetNextPageLink(int count)
        {
            var components = new UriComponents(Query.UriComponents);
            if (count > -1)
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("limit"));
                components.MetaConditions.Add(new UriCondition("limit", Operators.EQUALS, count.ToString()));
            }
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            components.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS,
                (Query.MetaConditions.Offset + (long) EntityCount).ToString()));
            return components;
        }
    }
}