using System;
using System.Linq;
using System.Threading.Tasks;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Results;
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
        /// The ID of the webhook that made the request
        /// </summary>
        public string WebhookId { get; }

        /// <summary>
        /// The method of the request
        /// </summary>
        public Method Method { get; }

        /// <summary>
        /// The destination URL of the request
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// The date and time when the request was sent
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// Does this log entry encode a success?
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// The log message
        /// </summary>
        public string ResponseStatus { get; }

        /// <summary>
        /// The number of bytes contained in the request body
        /// </summary>
        public long BodyByteCount { get; }

        /// <summary>
        /// The webhook that did the request
        /// </summary>
        [RESTarMember(hide: true)] public Webhook Webhook { get; }

        private WebhookLog(Webhook webhook, long bodyByteCount, string responseStatus, bool isError)
        {
            WebhookId = webhook.Id;
            Method = webhook.Method;
            Destination = webhook.Destination;
            BodyByteCount = bodyByteCount;
            ResponseStatus = responseStatus;
            IsSuccess = !isError;
            Time = DateTime.UtcNow;
            Webhook = webhook;
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
                Feed.Publish(new WebhookLog(hook, byteCount, message, isError));
            }));
        }

        /// <inheritdoc />
        [RESTar(Description = "A feed of all created webhook log messages")]
        public class Feed : Resources.Templates.FeedTerminal
        {
            private static TerminalSet<Feed> Terminals { get; }
            static Feed() => Terminals = new TerminalSet<Feed>();

            internal static void Publish(WebhookLog log)
            {
                var r = new UnknownResource("Foobo");
                Terminals
                    .Where(feed => feed.IsOpen)
                    .ForEach(feed =>
                    {
                        feed.WebSocket.SendJson(log);
                        feed.WebSocket.SendResult(r);
                    });
            }

            /// <inheritdoc />
            public override void Open()
            {
                base.Open();
                Terminals.Add(this);
            }

            /// <inheritdoc />
            public override void Dispose()
            {
                Terminals.Remove(this);
            }
        }
    }
}