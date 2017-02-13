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
        internal static ushort _HttpPort => Instance.HttpPort;
        internal static string _ResourcesPath => Instance.ResourcesPath;
        internal static string _HelpResourcePath => Instance.HelpResourcePath;

        private bool _camelCase;
        private bool _prettyPrint;

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
                    prettyPrint: PrettyPrint,
                    serializationNameFormat: value
                        ? SerializationNameFormat.CamelCase
                        : SerializationNameFormat.Verbatim
                );

                Serializer.JsonNetSettings = new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTime,
                    ContractResolver = value
                        ? new CamelCasePropertyNamesContractResolver()
                        : new DefaultContractResolver(),
                    NullValueHandling = NullValueHandling.Include,
                    FloatParseHandling = FloatParseHandling.Decimal,

                };
            }
        }

        public string Uri { get; private set; }
        public ushort HttpPort { get; private set; }
        public string ResourcesPath => $"http://[IP address]:{HttpPort}{Uri}";
        public string HelpResourcePath => ResourcesPath + "/RESTar.help";

        internal static void Init
        (
            string uri,
            bool prettyPrint,
            bool camelCase,
            ushort httpPort
        )
        {
            Db.Transact(() =>
            {
                foreach (var obj in DB.All<Settings>())
                    obj.Delete();
                new Settings
                {
                    Uri = uri,
                    PrettyPrint = prettyPrint,
                    CamelCase = camelCase,
                    HttpPort = httpPort
                };
            });
        }

        public static Settings Instance => DB.First<Settings>();
    }
}