using System;
using System.Linq;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using static RESTable.Method;

namespace RESTable.Admin
{
    /// <summary>
    /// The Error resource records instances where an error was encountered while
    /// handling a request. You can control how long entities remain in the resource
    /// by setting the daysToSaveErrors parameter in the call to RESTarConfig.Init().
    /// </summary>
    [InMemory, RESTable(GET, DELETE, Description = description)]
    public class Error
    {
        private const string description = "The Error resource records instances where an " +
                                           "error was encountered while handling a request.";

        private const int MaxStringLength = 10000;

        private static IRequest<Error> PostErrorRequest = RESTableContext.Root.CreateRequest<Error>(POST);
        private static Condition<Error> GetIdCondition = new Condition<Error>(nameof(Id), Operators.GREATER_THAN, 0);
        private static IRequest<Error> DeleteRequest = RESTableContext.Root.CreateRequest<Error>(DELETE).WithConditions(GetIdCondition);

        private static long Counter { get; set; }

        /// <summary>
        /// A unique ID for this error instance
        /// </summary>
        public long Id { get; }

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

        private Error()
        {
            Counter += 1;
            Id = Counter;
            if (Counter >= 10000 && Counter % 1000 == 0)
            {
                GetIdCondition.Value = Counter - 9000; 
                DeleteRequest.Evaluate();
            }
        }

        internal static Error Create(Results.Error errorResult, IRequest request)
        {
            var resource = request.SafeSelect(a => a.Resource);
            var uri = request.UriComponents.ToString();
            var stackTrace = $"{errorResult.StackTrace} §§§ INNER: {errorResult.InnerException?.StackTrace}";
            var totalMessage = errorResult.TotalMessage();
            var error = new Error
            {
                Time = DateTime.UtcNow,
                ResourceName = resource?.Name ?? "<unknown>",
                Method = request.Method,
                ErrorCode = errorResult.ErrorCode,
                Body = request.GetBody().ToString(),
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
            PostErrorRequest.WithEntities(error).Evaluate().ThrowIfError();
            return error;
        }
    }
}