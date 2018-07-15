using System;
using System.Threading.Tasks;
using RESTar.Resources;
using Starcounter;

namespace RESTar.Admin {
    /// <summary>
    /// Holds a log of webhook activity
    /// </summary>
    [RESTar(Method.GET, Method.DELETE), Database]
    public class WebhookLog
    {
        /// <summary>
        /// The webhook that did the request
        /// </summary>
        public Webhook Webhook { get; }

        /// <summary>
        /// The destination URL of the request
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// The number of bytes contained in the request body
        /// </summary>
        public long ByteCount { get; }

        /// <summary>
        /// The log message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Does this log entry encode an error?
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// The date and time when the request was sent
        /// </summary>
        public DateTime Time { get; }

        internal WebhookLog(Webhook webhook, long byteCount, string message, bool isError)
        {
            Webhook = webhook;
            Destination = webhook.Destination;
            ByteCount = byteCount;
            Message = message;
            IsError = isError;
            Time = DateTime.UtcNow;
        }

        internal static async Task Log(Webhook hook, bool isError, string message, long byteCount = 0)
        {
            await Scheduling.RunTask(() => Db.TransactAsync(() => new WebhookLog(hook, byteCount, message, isError)));
        }
    }
}