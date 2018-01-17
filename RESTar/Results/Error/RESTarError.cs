using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Fail.BadRequest;
using RESTar.Results.Fail.Forbidden;

namespace RESTar.Results.Error
{
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    internal abstract class RESTarError : Exception, IFinalizedResult
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <summary>
        /// The status code to use in HTTP responses
        /// </summary>
        public HttpStatusCode StatusCode { get; protected set; }

        /// <summary>
        /// The status description to use in HTTP responses
        /// </summary>
        public string StatusDescription { get; protected set; }

        /// <summary>
        /// The headers to use in HTTP responses
        /// </summary>
        public Headers Headers { get; } = new Headers();

        /// <summary>
        /// Does this result contain content?
        /// </summary>
        public bool HasContent { get; } = false;

        Stream IFinalizedResult.Body { get; } = null;
        string IFinalizedResult.ContentType { get; } = null;

        internal RESTarError(ErrorCodes code, string message) : base(message)
        {
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal RESTarError(ErrorCodes code, string message, Exception ie) : base(message, ie)
        {
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal static RESTarError GetError(Exception exception)
        {
            switch (exception)
            {
                case RESTarError re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException _: return new FailedJsonDeserialization(exception);
                case Starcounter.DbException _ when exception.Message.Contains("SCERR4034"): return new AbortedByCommitHook(exception);
                case Starcounter.DbException _: return new DatabaseError(exception);
                default: return new Unknown(exception);
            }
        }
    }
}