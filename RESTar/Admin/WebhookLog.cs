using System;
using System.Linq;
using System.Threading.Tasks;
using RESTar.Linq;
using RESTar.Resources;
using Starcounter;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <summary>
    /// Holds a log of webhook activity
    /// </summary>
    [RESTar(GET, DELETE), Database]
    public class WebhookLog
    {
        internal const string All = "SELECT t FROM RESTar.Admin.WebhookLog t";

        /// <summary>
        /// The webhook that did the request
        /// </summary>
        public Webhook Webhook { get; }

        /// <summary>
        /// The method of the request
        /// </summary>
        public Method Method { get; }

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

        private WebhookLog(Webhook webhook, long byteCount, string message, bool isError)
        {
            Webhook = webhook;
            Method = webhook.Method;
            Destination = webhook.Destination;
            ByteCount = byteCount;
            Message = message;
            IsError = isError;
            Time = DateTime.UtcNow;
        }

        internal static async Task Log(Webhook hook, bool isError, string message, long byteCount = 0)
        {
            await Scheduling.RunTask(() => Db.TransactAsync(() =>
            {
                var cutoff = DateTime.UtcNow.AddDays(0 - WebhookLogSettings.DaysToKeepLogItems);
                if (WebhookLogSettings.LastCleared < cutoff)
                {
                    Db.SQL<WebhookLog>(All).Where(wl => wl.Time < cutoff).ForEach(Db.Delete);
                    WebhookLogSettings.LastCleared = DateTime.UtcNow;
                }
                new WebhookLog(hook, byteCount, message, isError);
            }));
        }
    }
}