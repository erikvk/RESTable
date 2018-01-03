using System.IO;
using System.Net;
using RESTar.Requests;

namespace RESTar.Operations
{
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    public abstract class Result : IFinalizedResult
    {
        /// <summary>
        /// The status code to use in HTTP responses based on this result
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The status description to use in HTTP responses based on this result
        /// </summary>
        public string StatusDescription { get; set; }

        /// <summary>
        /// The body to use in HTTP responses based on this result
        /// </summary>
        public MemoryStream Body { get; set; }

        Stream IFinalizedResult.Body => Body;

        /// <summary>
        /// The content type to use in HTTP responses based on this result
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The headers to use in HTTP responses based on this result
        /// </summary>
        public Headers Headers { get; }

        internal Result() => Headers = new Headers();
    }
}