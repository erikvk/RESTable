using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using Starcounter;
using static System.Text.RegularExpressions.RegexOptions;
using static RESTar.Methods;
using IResource = RESTar.Internal.IResource;

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
        public HandlerActions HandlerAction;

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

        internal static Error Create(ErrorCodes errorCode, Exception e, IResource resource, Args args,
            HandlerActions action)
        {
            if (args.HasMetaConditions && args.MetaConditions.ToLower().Contains("key="))
                args.MetaConditions = Regex.Replace(args.MetaConditions, RegEx.KeyMetaCondition, "key=*******", IgnoreCase);
            var uri = args.UriString;
            var stackTrace = $"{e.StackTrace} §§§ INNER: {e.InnerException?.StackTrace}";
            var totalMessage = e.TotalMessage();
            return new Error
            {
                Time = DateTime.Now,
                ResourceName = (resource?.Name ?? "<unknown>") +
                               (resource?.Alias != null ? $" ({resource.Alias})" : ""),
                HandlerAction = action,
                ErrorCode = errorCode,
                Body = args.BodyBytes != null ? Encoding.UTF8.GetString(args.BodyBytes.Take(5000).ToArray()) : null,
                StackTrace = stackTrace.Length > MaxStringLength ? stackTrace.Substring(0, MaxStringLength) : stackTrace,
                Message = totalMessage.Length > MaxStringLength ? totalMessage.Substring(0, MaxStringLength) : totalMessage,
                Uri = uri,
                Headers = resource?.RequiresAuthentication == false
                    ? args.Headers.StringJoin(" | ", dict => dict.Select(header =>
                    {
                        if (header.Key?.ToLower() == "authorization")
                            return "Authorization: apikey *******";
                        return $"{header.Key}: {header.Value}";
                    }))
                    : null
            };
        }

        private static DateTime Checked;

        internal static void ClearOld()
        {
            if (Checked >= DateTime.Now.Date) return;
            const string SQL = "SELECT t FROM RESTar.Admin.Error t WHERE t.\"Time\" <?";
            var matches = Db.SQL<Error>(SQL, DateTime.Now.AddDays(0 - Settings._DaysToSaveErrors));
            matches.ForEach(match => Transact.TransAsync(match.Delete));
            Checked = DateTime.Now.Date;
        }
    }
}