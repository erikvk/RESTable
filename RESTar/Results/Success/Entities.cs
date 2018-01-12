using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal interface IEntitiesMetaData
    {
        ulong EntityCount { get; }
        string ResourceFullName { get; }
    }

    internal class Entities : OK, IEntitiesMetaData
    {
        internal IRequest Request { get; private set; }
        internal IEnumerable<dynamic> Content { get; set; }
        public ulong EntityCount { get; set; }
        string IEntitiesMetaData.ResourceFullName => Request.Resource.FullName;
        internal string ExternalDestination { get; set; }
        public bool IsPaged => Content != null && EntityCount > 0 && (long) EntityCount == Request.MetaConditions.Limit;

        private Entities() { }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.FullName}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        internal IUriParameters GetNextPageLink()
        {
            var existing = Request.UriParameters;
            existing.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            existing.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS, (Request.MetaConditions.Offset + (long) EntityCount).ToString()));
            return existing;
        }

        internal static Entities Create<T>(RESTRequest<T> request, IEnumerable<dynamic> content) where T : class => new Entities
        {
            Content = content,
            Request = request,
            ExternalDestination = request.Destination
        };
    }
}