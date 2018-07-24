using System;
using System.Linq;
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

        internal static ushort DaysToKeepLogItems
        {
            get => Instance._DaysToKeepLogItems;
            set => Instance._DaysToKeepLogItems = value;
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
        [RESTarMember(name: nameof(DaysToKeepLogItems))]
        public ushort _DaysToKeepLogItems { get; set; }

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
}