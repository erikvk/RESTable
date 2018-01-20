using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal interface IEntitiesMetadata
    {
        ulong EntityCount { get; }
        string ResourceFullName { get; }
        IUriParameters GetNextPageLink();
    }

    internal class Entities : OK, IEntitiesMetadata
    {
        internal IRequest Request { get; private set; }
        internal IEnumerable<dynamic> Content { get; set; }
        public ulong EntityCount { get; set; }
        string IEntitiesMetadata.ResourceFullName => Request.Resource.Name;
        internal string ExternalDestination { get; set; }
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;

        private Entities(ITraceable trace) : base(trace) { }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        public IUriParameters GetNextPageLink()
        {
            var existing = Request.UriParameters;
            existing.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            existing.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS,
                (Request.MetaConditions.Offset + (long)EntityCount).ToString()));
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