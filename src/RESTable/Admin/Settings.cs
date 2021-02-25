﻿using System.Collections.Generic;
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

        public static string _Uri => Instance.Uri;
        public static bool _PrettyPrint => Instance.PrettyPrint;
        public static int _NumberOfErrorsToKeep => Instance.NumberOfErrorsToKeep;
        public static LineEndings _LineEndings => Instance.LineEndings;

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
        /// The number of errors to store in the RESTable.Error resource
        /// </summary>
        public int NumberOfErrorsToKeep { get; private set; }

        /// <summary>
        /// The RESTable version of the current application
        /// </summary>
        public string RESTableVersion { get; private set; }

        /// <summary>
        /// The path where temporary files are created
        /// </summary>
        [RESTableMember(hide: true)]
        public string TempFilePath { get; private set; }

        public IEnumerable<Settings> Select(IRequest<Settings> request)
        {
            yield return Instance;
        }

        private static Settings Instance { get; set; }

        internal static void Init
        (
            string uri,
            bool prettyPrint,
            int numberOfErrorsToKeep,
            LineEndings lineEndings
        )
        {
            Instance = new Settings
            {
                Uri = uri,
                PrettyPrint = prettyPrint,
                NumberOfErrorsToKeep = numberOfErrorsToKeep,
                LineEndings = lineEndings,
                TempFilePath = Path.GetTempPath(),
                RESTableVersion = RESTableConfig.Version
            };
        }
    }
}