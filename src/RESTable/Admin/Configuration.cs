using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;
using static RESTable.Method;

namespace RESTable.Admin
{
    /// <summary>
    /// The configuration of the running RESTable application
    /// </summary>
    [RESTable(GET, Description = description)]
    public class Configuration : ISelector<Configuration>
    {
        private const string description = "The Configuration resource contains the current " +
                                           "configuration for the RESTable instance.";

        private RESTableConfiguration RESTableConfiguration { get; }
        private IConfiguration AppConfiguration { get; }

        /// <summary>
        /// The path and name of the currently running executable
        /// </summary>
        public string RunningExecutable => System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "Unknown";

        /// <summary>
        /// The root URI of the RESTable REST API
        /// </summary>
        public string RootUri => RESTableConfiguration.RootUri;

        /// <summary>
        /// The number of errors to store in the RESTable.Error resource
        /// </summary>
        public int NumberOfErrorsToKeep => AppConfiguration.GetValue<int?>("NrOfErrorsToKeep") ?? Error.DefaultNumberOfErrorsToKeep;

        /// <summary>
        /// The RESTable version of the current application
        /// </summary>
        public string RESTableVersion => RESTableConfiguration.Version;

        /// <summary>
        /// The maximum number of entitites that can be contained in change results, for example
        /// when inserting or updating entities 
        /// </summary>
        public int MaxNumberOfEntitiesInChangeResults => Change.MaxNumberOfEntitiesInChangeResults;

        /// <summary>
        /// The path where temporary files are created
        /// </summary>
        [RESTableMember(hide: true)]
        public string TempFilePath => Path.GetTempPath();

        public Configuration(RESTableConfiguration configuration, IConfiguration appConfiguration)
        {
            RESTableConfiguration = configuration;
            AppConfiguration = appConfiguration;
        }

        public IEnumerable<Configuration> Select(IRequest<Configuration> request)
        {
            var configuration = request.GetRequiredService<RESTableConfiguration>();
            var appConfiguration = request.GetRequiredService<IConfiguration>();
            yield return new Configuration(configuration, appConfiguration);
        }
    }
}