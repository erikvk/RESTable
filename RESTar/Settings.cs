using Starcounter;

namespace RESTar
{
    [Database]
    [RESTar(RESTarPresets.ReadAndUpdate)]
    public class Settings
    {
        internal static bool _PrettyPrint => Instance.PrettyPrint;
        internal static string _Uri => Instance.Uri;
        internal static ushort _HttpPort => Instance.HttpPort;
        internal static string _ResourcesPath => Instance.ResourcesPath;
        internal static string _HelpResourcePath => Instance.HelpResourcePath;

        public bool PrettyPrint;
        public string Uri { get; private set; }
        public ushort HttpPort { get; private set; }
        public string ResourcesPath => $"http://[IP address]:{HttpPort}{Uri}";
        public string HelpResourcePath => ResourcesPath + "/RESTar.help";
        
        internal static void Init
        (
            string uri,
            bool prettyPrint,
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
                    HttpPort = httpPort
                };
            });
        }

        public static Settings Instance => _Instance ?? (_Instance = DB.First<Settings>());

        private static Settings _Instance;
    }
}