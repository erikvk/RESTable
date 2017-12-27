using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Requests;
using static RESTar.Operators;

namespace RESTar.Operations
{
    internal interface IFinalizedResult
    {
        HttpStatusCode StatusCode { get; }
        string StatusDescription { get; }
        Stream Body { get; }
        string ContentType { get; }
        Dictionary<string, string> Headers { get; }
        long EntityCount { get; }
        bool HasContent { get; }
        bool IsPaged { get; }
    }

    internal class Result : IFinalizedResult
    {
        #region Unfinalized

        internal IRequest Request { get; }
        internal string ExternalDestination { get; set; }
        internal IEnumerable<dynamic> Entities { get; set; }

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment; filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        #endregion

        #region Finalized

        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public Stream Body { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public long EntityCount { get; set; }
        public bool HasContent => EntityCount > 0;
        public bool IsPaged => HasContent && EntityCount == Request.MetaConditions.Limit;

        public IUriParameters GetNextPageLink()
        {
            var existing = Request.UriParameters;
            existing.UriMetaConditions.RemoveAll(c => c.Key.EqualsNoCase("limit") || c.Key.EqualsNoCase("offset"));
            existing.UriMetaConditions.Add(new UriCondition("limit", EQUALS, Request.MetaConditions.Limit.ToString()));
            existing.UriMetaConditions.Add(new UriCondition("offset", EQUALS, (Request.MetaConditions.Offset + EntityCount).ToString()));
            return existing;
        }

        #endregion

        internal Result(IRequest request) => Request = request;
    }
}