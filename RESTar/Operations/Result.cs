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
        bool HasContent { get; }
    }

    internal class Result : IFinalizedResult
    {
        #region Unfinalized

        internal IRequest Request { get; }
        internal string ExternalDestination { get; set; }
        internal IEnumerable<dynamic> Entities { get; set; }

        #endregion

        #region Finalized

        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public MemoryStream Body { get; set; }
        Stream IFinalizedResult.Body => Body;
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public bool HasContent => Body?.Length > 0;
        public long EntityCount { get; set; }
        public bool HasEntities => Entities != null;
        public bool IsPaged => HasEntities && EntityCount > 0 && EntityCount == Request.MetaConditions.Limit;

        internal void SetContentDisposition(string extension) => Headers["Content-Disposition"] =
            $"attachment;filename={Request.Resource.Name}_{DateTime.Now:yyMMddHHmmssfff}{extension}";

        public IUriParameters GetNextPageLink()
        {
            var existing = Request.UriParameters;
            existing.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase("offset"));
            existing.MetaConditions.Add(new UriCondition("offset", EQUALS, (Request.MetaConditions.Offset + EntityCount).ToString()));
            return existing;
        }

        #endregion

        internal Result(IRequest request = null) => Request = request;
    }
}