using Jil;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Starcounter;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadAndUpdate)]
    public class Settings
    {
        internal static bool _PrettyPrint => Instance.PrettyPrint;
        internal static bool _CamelCase => Instance.CamelCase;
        internal static string _Uri => Instance.Uri;
        internal static ushort _Port => Instance.Port;
        internal static string _ResourcesPath => Instance.ResourcesPath;
        internal static string _HelpResourcePath => Instance.HelpResourcePath;
        internal static bool _LocalTimes => Instance.LocalTimes;
        internal static int _DaysToSaveErrors => Instance.DaysToSaveErrors;

        private bool _prettyPrint;
        private bool _camelCase;
        private bool _localTimes;
        
        public bool PrettyPrint
        {
            get { return _prettyPrint; }
            set
            {
                _prettyPrint = value;
                Serializer.SerializerOptions = new Options
                (
                    includeInherited: true,
                    dateFormat: DateTimeFormat.ISO8601,
                    unspecifiedDateTimeKindBehavior: LocalTimes
                        ? UnspecifiedDateTimeKindBehavior.IsLocal
                        : UnspecifiedDateTimeKindBehavior.IsUTC,
                    prettyPrint: value,
                    serializationNameFormat: CamelCase
                        ? SerializationNameFormat.CamelCase
                        : SerializationNameFormat.Verbatim
                );
            }
        }

        public bool CamelCase
        {
            get { return _camelCase; }
            set
            {
                _camelCase = value;
                Serializer.SerializerOptions = new Options
                (
                    includeInherited: true,
                    dateFormat: DateTimeFormat.ISO8601,
                    unspecifiedDateTimeKindBehavior: LocalTimes
                        ? UnspecifiedDateTimeKindBehavior.IsLocal
                        : UnspecifiedDateTimeKindBehavior.IsUTC,
                    prettyPrint: PrettyPrint,
                    serializationNameFormat: value
                        ? SerializationNameFormat.CamelCase
                        : SerializationNameFormat.Verbatim
                );
                Serializer.JsonNetSettings.ContractResolver = value
                    ? new CamelCasePropertyNamesContractResolver()
                    : new DefaultContractResolver();
            }
        }

        public bool LocalTimes
        {
            get { return _localTimes; }
            set
            {
                _localTimes = value;
                Serializer.SerializerOptions = new Options
                (
                    includeInherited: true,
                    dateFormat: DateTimeFormat.ISO8601,
                    unspecifiedDateTimeKindBehavior: value
                        ? UnspecifiedDateTimeKindBehavior.IsLocal
                        : UnspecifiedDateTimeKindBehavior.IsUTC,
                    prettyPrint: PrettyPrint,
                    serializationNameFormat: CamelCase
                        ? SerializationNameFormat.CamelCase
                        : SerializationNameFormat.Verbatim
                );
                Serializer.JsonNetSettings.DateTimeZoneHandling = value
                    ? DateTimeZoneHandling.Local
                    : DateTimeZoneHandling.Utc;
            }
        }

        public string Uri { get; private set; }
        public ushort Port { get; private set; }
        public string ResourcesPath => $"http://[IP address]:{Port}{Uri}";
        public string HelpResourcePath => ResourcesPath + "/RESTar.help";
        public int DaysToSaveErrors { get; private set; }

        internal static void Init
        (
            string uri,
            ushort port,
            bool prettyPrint,
            bool camelCase,
            bool localTimes,
            int daysToSaveErrors
        )
        {
            Db.TransactAsync(() =>
            {
                foreach (var obj in DB.All<Settings>())
                    obj.Delete();

                Serializer.SerializerOptions = new Options
                (
                    includeInherited: true,
                    dateFormat: DateTimeFormat.ISO8601,
                    unspecifiedDateTimeKindBehavior: localTimes
                        ? UnspecifiedDateTimeKindBehavior.IsLocal
                        : UnspecifiedDateTimeKindBehavior.IsUTC,
                    prettyPrint: prettyPrint,
                    serializationNameFormat: camelCase
                        ? SerializationNameFormat.CamelCase
                        : SerializationNameFormat.Verbatim
                );

                Serializer.JsonNetSettings = new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTime,
                    DateTimeZoneHandling = localTimes
                        ? DateTimeZoneHandling.Local
                        : DateTimeZoneHandling.Utc,
                    ContractResolver = camelCase
                        ? new CamelCasePropertyNamesContractResolver()
                        : new DefaultContractResolver(),
                    NullValueHandling = NullValueHandling.Include,
                    FloatParseHandling = FloatParseHandling.Decimal
                };

                new Settings
                {
                    Uri = uri,
                    Port = port,
                    _prettyPrint = prettyPrint,
                    _camelCase = camelCase,
                    _localTimes = localTimes,
                    DaysToSaveErrors = daysToSaveErrors
                };
            });
        }

        public static Settings Instance => DB.First<Settings>();
    }
}