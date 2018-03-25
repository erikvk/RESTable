using System;
using System.Collections;
using System.Collections.Generic;
using RESTar.Requests;
using UriComponents = RESTar.Requests.UriComponents;

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

        private readonly IEnumerable<object> _content;

        /// <summary>
        /// The entities contained in the result
        /// </summary>
        private IEnumerable<object> Content => _content ?? new object[0];

        /// <summary>
        /// The number of entities in the result
        /// </summary>
        public ulong EntityCount { get; set; }

        string IEntitiesMetadata.ResourceFullName => Request.Resource.Name;
        internal string ExternalDestination { get; }

        /// <summary>
        /// Is the result paged?
        /// </summary>
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;

        private Entities(IRequest request, IEnumerable<object> content) : base(request)
        {
            _content = content;
            ExternalDestination = request.Headers.Destination;
            TimeElapsed = request.TimeElapsed;
        }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
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

        internal static Entities Create<TResource>(IRequestInternal<TResource> request, IEnumerable<object> content)
            where TResource : class => new Entities(request, content);
    }
}