﻿using System.Runtime.Serialization;
using RESTar.Internal;
using Starcounter;

namespace RESTar
{
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

        public ushort Port { get; private set; }
        public string Uri { get; private set; }
        public bool ViewEnabled { get; private set; }
        public bool PrettyPrint { get; set; }

        public string ResourcesPath => $"http://[IP address]:{Port}{Uri}";
        public string HelpResourcePath => ResourcesPath + "/RESTar.help";
        public int DaysToSaveErrors { get; private set; }

        internal static void Init(ushort port, string uri, bool viewEnabled, bool prettyPrint, bool camelCase,
            int daysToSaveErrors)
        {
            Db.TransactAsync(() =>
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

        [IgnoreDataMember]
        public static Settings Instance => DB.First<Settings>();
    }
}