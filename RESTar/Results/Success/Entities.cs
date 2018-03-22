using System;
using System.Collections;
using System.Collections.Generic;
using RESTar.Operations;
using RESTar.Requests;

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
        private IEnumerable<object> Content { get; set; }

        /// <summary>
        /// The number of entities in the result
        /// </summary>
        public ulong EntityCount { get; set; }

        string IEntitiesMetadata.ResourceFullName => Request.Resource.Name;
        internal string ExternalDestination { get; set; }

        /// <summary>
        /// Is the result paged?
        /// </summary>
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;

        private Entities(IRequest request) : base(request) { }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        /// <inheritdoc />
        public IUriComponents GetNextPageLink() => GetNextPageLink(-1);

        /// <inheritdoc />
        public IUriComponents GetNextPageLink(int count)
        {
            var existing = Request.UriComponents;
            if (count > -1)
            {
                existing.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("limit"));
                existing.MetaConditions.Add(new UriCondition("limit", Operators.EQUALS, count.ToString()));
            }
            existing.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            existing.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS,
                (Request.MetaConditions.Offset + (long) EntityCount).ToString()));
            return existing;
        }

        internal static Entities Create<TResource>(IRequestInternal<TResource> request, IEnumerable<object> content)
            where TResource : class => new Entities(request)
        {
            Content = content,
            ExternalDestination = request.Destination
        };
    }
}