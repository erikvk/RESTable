using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Requests;
using Starcounter;
using static RESTar.Operations.Transact;
using static RESTar.RESTarMethods;
using static RESTar.RESTarPresets;
using static RESTar.Settings;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// Gets all error codes used by RESTar
    /// </summary>
    [RESTar(ReadOnly)]
    public class ErrorCode : ISelector<ErrorCode>
    {
        /// <summary>
        /// The name of the error
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The numeric code of the error
        /// </summary>
        public int Code { get; private set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<ErrorCode> Select(IRequest<ErrorCode> request) => EnumMember<ErrorCodes>
            .GetMembers()
            .Select(m => new ErrorCode {Name = m.Name, Code = m.Value})
            .Where(request.Conditions);
    }

    /// <summary>
    /// The error resource contains instances where an error was encountered while
    /// handling a request. You can control how long entities remain in the resource
    /// by setting the daysToSaveErrors parameter in the call to RESTarConfig.Init().
    /// </summary>
    [Database, RESTar(GET, DELETE)]
    public class Error
    {
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

        private Error()
        {
        }

        internal static Error Create(ErrorCodes errorCode, Exception e, IResource resource, Request scRequest, HandlerActions action) => new Error
        {
            Time = DateTime.Now,
            ResourceName = (resource?.Name ?? "<unknown>") +
                           (resource?.Alias != null ? $" ({resource.Alias})" : ""),
            HandlerAction = action,
            ErrorCode = errorCode,
            StackTrace = e.StackTrace + e.InnerException?.StackTrace,
            Message = e.TotalMessage(),
            Body = scRequest.Body,
            Uri = scRequest.Uri,
            Headers = scRequest.HeadersDictionary?.StringJoin(" | ", dict => dict.Select(header =>
            {
                if (header.Key?.ToLower() == "authorization")
                    return "Authorization: apikey *******";
                return $"{header.Key}: {header.Value}";
            }))
        };

        private static DateTime Checked;

        internal static void ClearOld()
        {
            if (Checked >= DateTime.Now.Date) return;
            var matches = Db.SQL<Error>($"SELECT t FROM {typeof(Error).FullName} t WHERE t.\"Time\" <?",
                DateTime.Now.AddDays(0 - _DaysToSaveErrors));
            matches.ForEach(match => TransAsync(match.Delete));
            Checked = DateTime.Now.Date;
        }
    }
}