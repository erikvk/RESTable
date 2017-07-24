using System.Runtime.Serialization;
using RESTar.Internal;
using RESTar.Linq;
using Starcounter;
using static RESTar.Internal.Transactions;

namespace RESTar
{
    /// <summary>
    /// The settings resource for the RESTar instance, stores its settings.
    /// </summary>
    [Database, RESTar(RESTarPresets.ReadAndUpdate, Singleton = true)]
    public class Settings
    {
        internal static ushort _Port => Instance.Port;
        internal static string _Uri => Instance.Uri;
        internal static bool _ViewEnabled => Instance.ViewEnabled;
        internal static bool _PrettyPrint => Instance.PrettyPrint;
        internal static int _DaysToSaveErrors => Instance.DaysToSaveErrors;
        internal static string _ResourcesPath => Instance.ResourcesPath;
        internal static string _HelpResourcePath => Instance.HelpResourcePath;
        internal static bool _DontUseLRT => Instance.DontUseLRT;

        /// <summary>
        /// The port of the RESTar REST API
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// The URI of the RESTar REST API
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// Is the view enabled?
        /// </summary>
        public bool ViewEnabled { get; private set; }

        /// <summary>
        /// Will JSON be serialized with pretty print? (indented JSON)
        /// </summary>
        public bool PrettyPrint { get; set; }

        /// <summary>
        /// Use long running transactions instead of regular transact calls
        /// </summary>
        public bool DontUseLRT { get; set; }

        /// <summary>
        /// The path where resources are available
        /// </summary>
        public string ResourcesPath => $"http://[IP address]:{Port}{Uri}";

        /// <summary>
        /// The path where help resources are available
        /// </summary>
        public string HelpResourcePath => ResourcesPath + "/RESTar.help";

        /// <summary>
        /// The number of days to store errors in the RESTar.Error resource
        /// </summary>
        public int DaysToSaveErrors { get; private set; }

        internal static void Init(ushort port, string uri, bool viewEnabled, bool prettyPrint, bool camelCase,
            int daysToSaveErrors)
        {
            Trans(() =>
            {
                DB.All<Settings>().ForEach(Db.Delete);
                new Settings
                {
                    Port = port,
                    Uri = uri,
                    ViewEnabled = viewEnabled,
                    PrettyPrint = prettyPrint,
                    DaysToSaveErrors = daysToSaveErrors
                };
            });
        }

        /// <summary>
        /// Gets the only instance of the Settings resource
        /// </summary>
        [IgnoreDataMember]
        public static Settings Instance => DB.First<Settings>();
    }
}