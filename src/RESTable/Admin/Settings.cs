using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
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

//        public static string _Uri => Instance.Uri;
//        public static bool _PrettyPrint => Instance.PrettyPrint;
//        public static int _NumberOfErrorsToKeep => Instance.NumberOfErrorsToKeep;
//        public static LineEndings _LineEndings => Instance.LineEndings;

        /// <summary>
        /// The root URI of the RESTable REST API
        /// </summary>
        public string RootUri => Configuration.RootUri;

        /// <summary>
        /// The number of errors to store in the RESTable.Error resource
        /// </summary>
        public int NumberOfErrorsToKeep => Configuration.NrOfErrorsToKeep;

        /// <summary>
        /// The RESTable version of the current application
        /// </summary>
        public string RESTableVersion => Configuration.Version;

        private RESTableConfiguration Configuration { get; }

        public Settings(RESTableConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// The path where temporary files are created
        /// </summary>
        [RESTableMember(hide: true)]
        public string TempFilePath => Path.GetTempPath();

        public IEnumerable<Settings> Select(IRequest<Settings> request)
        {
            var configuration = request.GetRequiredService<RESTableConfiguration>();
            yield return new Settings(configuration);
        }
    }
}