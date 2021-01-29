using System.Collections.Generic;
using System.IO;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable.Admin
{
    /// <summary>
    /// The settings resource contains the current settings for the RESTable instance.
    /// </summary>
    [RESTable(GET, Description = description)]
    public class Settings : ISelector<Settings>
    {
        private const string description = "The Settings resource contains the current " +
                                           "settings for the RESTable instance.";
        
        public static ushort _Port => Instance.Port;
        public static string _Uri => Instance.Uri;
        public static bool _PrettyPrint => Instance.PrettyPrint;
        public static int _DaysToSaveErrors => Instance.DaysToSaveErrors;
        public static string _HelpResourcePath => Instance.DocumentationURL;
        public static LineEndings _LineEndings => Instance.LineEndings;

        /// <summary>
        /// The port of the RESTable REST API
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// The URI of the RESTable REST API
        /// </summary>
        public string Uri { get; private set; }

        /// <summary>
        /// Will JSON be serialized with pretty print? (indented JSON)
        /// </summary>
        public bool PrettyPrint { get; set; }

        /// <summary>
        /// The line endings to use when writing JSON
        /// </summary>
        public LineEndings LineEndings { get; private set; }

        /// <summary>
        /// The path where help resources are available
        /// </summary>
        public string DocumentationURL => "https://develop.mopedo.com";

        /// <summary>
        /// The number of days to store errors in the RESTable.Error resource
        /// </summary>
        public int DaysToSaveErrors { get; private set; }

        /// <summary>
        /// The RESTable version of the current application
        /// </summary>
        public string RESTableVersion { get; private set; }

        /// <summary>
        /// The path where temporary files are created
        /// </summary>
        [RESTableMember(hide: true)] public string TempFilePath { get; private set; }

        public IEnumerable<Settings> Select(IRequest<Settings> request)
        {
            yield return Instance;
        }

        private static Settings Instance { get; set; }

        internal static void Init
        (
            ushort port,
            string uri,
            bool prettyPrint,
            int daysToSaveErrors,
            LineEndings lineEndings
        )
        {
            Instance = new Settings
            {
                Port = port,
                Uri = uri,
                PrettyPrint = prettyPrint,
                DaysToSaveErrors = daysToSaveErrors,
                LineEndings = lineEndings,
                TempFilePath = Path.GetTempPath(),
                RESTableVersion = RESTableConfig.Version
            };
        }
    }
}