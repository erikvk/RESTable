using System;
using System.Collections.Generic;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal class Entities : OK
    {
        internal IRequest Request { get; private set; }
        internal IEnumerable<dynamic> Content { get; set; }

        public long EntityCount { get; set; }
        internal string ExternalDestination { get; set; }
        public bool IsPaged => Content != null && EntityCount > 0 && EntityCount == Request.MetaConditions.Limit;

        private Entities() { }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        internal IUriParameters GetNextPageLink()
        {
            var existing = Request.UriParameters;
            existing.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            existing.MetaConditions.Add(new UriCondition("offset", Operators.EQUALS, (Request.MetaConditions.Offset + EntityCount).ToString()));
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