using System;
using System.Linq;
using System.Text;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Results.Error;
using Starcounter;
using static RESTar.Admin.Settings;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <summary>
    /// The Error resource records instances where an error was encountered while
    /// handling a request. You can control how long entities remain in the resource
    /// by setting the daysToSaveErrors parameter in the call to RESTarConfig.Init().
    /// </summary>
    [Database, RESTar(GET, DELETE, Description = description)]
    public class Error
    {
        private const string description = "The Error resource records instances where an " +
                                           "error was encountered while handling a request.";

        internal const string All = "SELECT t FROM RESTar.Admin.Error t";
        internal const string ByTimeLessThan = All + " WHERE t.\"Time\" <?";

        /// <summary>
        /// A unique ID for this error instance
        /// </summary>
        public string Id => this.GetObjectID();

        /// <summary>
        /// The date and time when this error was created
        /// </summary>
        public DateTime Time;

        /// <summary>
        /// The name of the resource that the request was aimed at
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// The method used when the error was created
        /// </summary>
        public Method Method;

        /// <summary>
        /// The error code of the error
        /// </summary>
        public ErrorCodes ErrorCode;

        /// <summary>
        /// The runtime stack trace for the thrown exception
        /// </summary>
        public string StackTrace;

        /// <summary>
        /// A message describing the error
        /// </summary>
        public string Message;

        /// <summary>
        /// The URI of the request that generated the error
        /// </summary>
        public string Uri;

        /// <summary>
        /// The headers of the request that generated the error (API keys are not saved here)
        /// </summary>
        public string Headers;

        /// <summary>
        /// The body of the request that generated the error
        /// </summary>
        public string Body;

        private Error() { }

        private const int MaxStringLength = 10000;

        internal static Error Create(RESTarError error, IRequest request)
        {
            var resource = request.SafeGet(a => a.Resource);
            var uri = request.UriComponents.ToString();
            var stackTrace = $"{error.StackTrace} §§§ INNER: {error.InnerException?.StackTrace}";
            var totalMessage = error.TotalMessage();
            return new Error
            {
                Time = DateTime.Now,
                ResourceName = (resource?.Name ?? "<unknown>") +
                               (resource?.Alias != null ? $" ({resource.Alias})" : ""),
                Method = request.Method,
                ErrorCode = error.ErrorCode,
                Body = request.Body.HasContent
                    ? Encoding.UTF8.GetString(request.Body.Bytes.Take(5000).ToArray())
                    : null,
                StackTrace = stackTrace.Length > MaxStringLength ? stackTrace.Substring(0, MaxStringLength) : stackTrace,
                Message = totalMessage.Length > MaxStringLength ? totalMessage.Substring(0, MaxStringLength) : totalMessage,
                Uri = uri,
                Headers = resource is IEntityResource e && e.RequiresAuthentication
                    ? null
                    : request.Headers.StringJoin(" | ", dict => dict.Select(header =>
                    {
                        switch (header.Key.ToLower())
                        {
                            case "authorization": return "Authorization: apikey *******";
                            case "x-original-url" when header.Value.Contains("key="): return "*******";
                            default: return $"{header.Key}: {header.Value}";
                        }
                    }))
            };
        }

        private static DateTime Checked;

        internal static void ClearOld()
        {
            if (Checked >= DateTime.Now.Date) return;
            var matches = Db.SQL<Error>(ByTimeLessThan, DateTime.Now.AddDays(0 - _DaysToSaveErrors));
            matches.ForEach(match => Transact.TransAsync(match.Delete));
            Checked = DateTime.Now.Date;
        }
    }
}