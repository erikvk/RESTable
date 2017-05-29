﻿using System.Runtime.Serialization;
using Jil;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTar.Internal;
using Starcounter;
using DateTimeFormat = Jil.DateTimeFormat;

namespace RESTar
{
    [Database, RESTar(RESTarPresets.ReadAndUpdate, Singleton = true, Viewable = true)]
    public class Settings
    {
        internal static ushort _Port => Instance.Port;
        internal static string _Uri => Instance.Uri;
        internal static bool _ViewEnabled => Instance.ViewEnabled;
        internal static bool _PrettyPrint => Instance.PrettyPrint;
        internal static bool _CamelCase => Instance.CamelCase;
        internal static bool _LocalTimes => Instance.LocalTimes;
        internal static int _DaysToSaveErrors => Instance.DaysToSaveErrors;
        internal static string _ResourcesPath => Instance.ResourcesPath;
        internal static string _HelpResourcePath => Instance.HelpResourcePath;

        public ushort Port { get; private set; }
        public string Uri { get; private set; }
        public bool ViewEnabled { get; private set; }
     
        private bool _prettyPrint;
        private bool _camelCase;
        private bool _localTimes;

        public bool PrettyPrint
        {
            get => _prettyPrint;
            set
            {
                _prettyPrint = value;
                JsonSerializer.SerializerOptions = new Options
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
            get => _camelCase;
            set
            {
                _camelCase = value;
                JsonSerializer.SerializerOptions = new Options
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
                JsonSerializer.JsonNetSettings.ContractResolver = value
                    ? new CamelCasePropertyNamesContractResolver()
                    : new DefaultContractResolver();
            }
        }

        public bool LocalTimes
        {
            get => _localTimes;
            set
            {
                _localTimes = value;
                JsonSerializer.SerializerOptions = new Options
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
                JsonSerializer.JsonNetSettings.DateTimeZoneHandling = value
                    ? DateTimeZoneHandling.Local
                    : DateTimeZoneHandling.Utc;
            }
        }

        public string ResourcesPath => $"http://[IP address]:{Port}{Uri}";
        public string HelpResourcePath => ResourcesPath + "/RESTar.help";
        public int DaysToSaveErrors { get; private set; }


        internal static void Init
        (
            ushort port,
            string uri,
            bool viewEnabled,
            bool prettyPrint,
            bool camelCase,
            bool localTimes,
            int daysToSaveErrors
        )
        {
            Db.TransactAsync(() =>
            {
                DB.All<Settings>().ForEach(Db.Delete);

                JsonSerializer.SerializerOptions = new Options
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

                JsonSerializer.JsonNetSettings = new JsonSerializerSettings
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
                    Port = port,
                    Uri = uri,
                    ViewEnabled = viewEnabled,
                    _prettyPrint = prettyPrint,
                    _camelCase = camelCase,
                    _localTimes = localTimes,
                    DaysToSaveErrors = daysToSaveErrors
                };
            });
        }

        [IgnoreDataMember]
        public static Settings Instance => DB.First<Settings>();
    }
}