using System;
using System.Linq;
using System.Threading.Tasks;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;
using static RESTar.Method;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Settings for WebhookLog
    /// </summary>
    [Database, RESTar(GET, PATCH, DELETE)]
    public class WebhookLogSettings : IDeleter<WebhookLogSettings>
    {
        internal const string All = "SELECT t FROM RESTar.Admin.WebhookLogSettings t";

        private static WebhookLogSettings Instance => Db.SQL<WebhookLogSettings>(All).FirstOrDefault()
                                                      ?? Db.Transact(() => new WebhookLogSettings());

        internal static DateTime LastCleared
        {
            get => Instance._LastCleared;
            set => Instance._LastCleared = value;
        }

        internal static void Init()
        {
            if (Db.SQL<WebhookLogSettings>(All).FirstOrDefault() == null)
                Db.Transact(() => new WebhookLogSettings());
        }

        /// <summary>
        /// The date and time when the WebhookLog log was last cleared
        /// </summary>
        [RESTarMember(name: nameof(LastCleared))]
        public DateTime _LastCleared { get; internal set; }

        /// <summary>
        /// The number of days to keep log items
        /// </summary>
        public ushort DaysToKeepLogItems { get; set; }

        /// <inheritdoc />
        public int Delete(IRequest<WebhookLogSettings> request)
        {
            var count = 0;
            foreach (var entity in request.GetInputEntities())
            {
                var lc = entity._LastCleared;
                Instance.Delete();
                new WebhookLogSettings {_LastCleared = lc};
                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        private WebhookLogSettings()
        {
            _LastCleared = DateTime.UtcNow;
            DaysToKeepLogItems = 14;
        }
    }

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
            var cutoff = DateTime.UtcNow.AddDays(-14);
            if (WebhookLogSettings.LastCleared < cutoff)
                await Db.TransactAsync(() =>
                {
                    Db.SQL<WebhookLog>(All).Where(wl => wl.Time < cutoff).ForEach(Db.Delete);
                    WebhookLogSettings.LastCleared = DateTime.UtcNow;
                });
            await Scheduling.RunTask(() => Db.TransactAsync(() => new WebhookLog(hook, byteCount, message, isError)));
        }
    }
}