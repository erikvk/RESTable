using System;
using System.Collections;
using System.Collections.Generic;
using RESTar.Requests;
using UriComponents = RESTar.Requests.UriComponents;

namespace RESTar.Results
{
    /// <inheritdoc cref="OK" />
    /// <summary>
    /// A result that contains a set of entities
    /// </summary>
    internal sealed class Entities<T> : Content, IEntities<T> where T : class
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => Content.GetEnumerator();

        /// <summary>
        /// The entities contained in the result
        /// </summary>
        private IEnumerable<T> Content { get; }

        public Type EntityType => typeof(T);

        /// <inheritdoc />
        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; set; }

        /// <inheritdoc />
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;

        internal Entities(IRequest request, IEnumerable<T> content) : base(request)
        {
            Content = content ?? new T[0];
        }

        /// <inheritdoc />
        public void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        /// <inheritdoc />
        public IUriComponents GetNextPageLink() => GetNextPageLink(-1);

        /// <inheritdoc />
        public IUriComponents GetNextPageLink(int count)
        {
            var components = new UriComponents(Request.UriComponents);
            if (count > -1)
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("limit"));
                components.MetaConditions.Add(new UriCondition("limit", Operators.EQUALS, count.ToString()));
            }
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            components.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS,
                (Request.MetaConditions.Offset + (long) EntityCount).ToString()));
            return components;
        }
    }
}