using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc cref="OK" />
    /// <inheritdoc cref="IEntitiesMetadata" />
    /// <summary>
    /// A result that contains a set of entities
    /// </summary>
    public sealed class Entities : OK, IEntitiesMetadata
    {
        /// <summary>
        /// The request that this result was generated for
        /// </summary>
        public IRequest Request { get; private set; }

        /// <summary>
        /// The entities contained in the result
        /// </summary>
        public IEnumerable<dynamic> Content { get; set; }

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

        private Entities(ITraceable trace) : base(trace) { }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        /// <inheritdoc />
        public IUriParameters GetNextPageLink() => GetNextPageLink(-1);

        /// <inheritdoc />
        public IUriParameters GetNextPageLink(int count)
        {
            var existing = Request.UriParameters;
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

        internal static Entities Create<T>(RESTRequest<T> request, IEnumerable<dynamic> content) where T : class => new Entities(request)
        {
            Content = content,
            Request = request,
            ExternalDestination = request.Destination
        };
    }
}