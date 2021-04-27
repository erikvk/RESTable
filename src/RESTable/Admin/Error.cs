using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        private const int DeleteBatch = 100;
        public const int DefaultNumberOfErrorsToKeep = 2000;

        private static long Counter { get; set; }

        /// <summary>
        /// A unique ID for this error instance
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// The URI of the request that generated the error
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// The method used when the error was created
        /// </summary>
        public Method Method { get; }

        /// <summary>
        /// The headers of the request that generated the error (API keys are not saved here)
        /// </summary>
        public string Headers { get; }

        /// <summary>
        /// The body of the request that generated the error
        /// </summary>
        public string Body { get; }

        /// <summary>
        /// The name of the resource that the request was aimed at
        /// </summary>
        public string ResourceName { get; }

        /// <summary>
        /// The date and time when this error was created
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// The error code of the error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <summary>
        /// The runtime stack trace for the thrown exception
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// A message describing the error
        /// </summary>
        public string Message { get; }

        private Error
        (
            string uri,
            Method method,
            string headers,
            string body,
            string resourceName,
            DateTime time,
            ErrorCodes errorCode,
            string stackTrace,
            string message
        )
        {
            Counter += 1;
            Id = Counter;
            Uri = uri;
            Method = method;
            Headers = headers;
            Body = body;
            ResourceName = resourceName;
            Time = time;
            ErrorCode = errorCode;
            StackTrace = stackTrace;
            Message = message;
        }

        internal static Error Create(Results.Error errorResult, IRequest request)
        {
            var resource = request.SafeSelect(a => a.Resource);
            var uri = request.UriComponents.ToString();
            var errorStackTrace = errorResult.StackTrace;
            var innerStackTrace = errorResult.InnerException?.StackTrace;
            var nl = Environment.NewLine;
            var stackTrace = string.Join($"{nl}§§§ INNER: §§§{nl}", errorStackTrace, innerStackTrace);
            var totalMessage = errorResult.ToString();
            var errorsToKeep = request
                .GetService<IConfiguration>()?
                .GetValue<int>("NrOfErrorsToKeep") ?? DefaultNumberOfErrorsToKeep;
            if (Counter > errorsToKeep && Counter % DeleteBatch == 0)
            {
                var cutoffId = Counter - errorsToKeep;
                var entitiesToDelete = InMemoryOperations<Error>
                    .Select()
                    .Where(existingError => existingError.Id <= cutoffId)
                    .ToList();
                InMemoryOperations<Error>.Delete(entitiesToDelete);
            }
            var error = new Error
            (
                uri: uri,
                method: request.Method,
                headers: resource is IEntityResource {RequiresAuthentication: true}
                    ? null
                    : request.Headers.StringJoin(" | ", dict => dict.Select(header => header.Key.ToLower() switch
                    {
                        "authorization" => "Authorization: apikey *******",
                        "x-original-url" when header.Value.Contains("key=") => "*******",
                        _ => $"{header.Key}: {header.Value}"
                    })),
                resourceName: resource?.Name ?? "<unknown>",
                body: request.Body.ToString(),
                time: DateTime.UtcNow,
                errorCode: errorResult.ErrorCode,
                stackTrace: stackTrace.Length > MaxStringLength ? stackTrace.Substring(0, MaxStringLength) : stackTrace,
                message: totalMessage.Length > MaxStringLength ? totalMessage.Substring(0, MaxStringLength) : totalMessage
            );
            InMemoryOperations<Error>.Insert(error);
            return error;
        }
    }
}