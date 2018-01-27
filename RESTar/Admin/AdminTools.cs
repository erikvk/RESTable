using System;
using System.Collections.Generic;
using static RESTar.Methods;

namespace RESTar.Admin
{
    /// <summary>
    /// Provides access to commmon admin tasks
    /// </summary>
    [RESTar(GET, Description = description)]
    public class Utilities : ISelector<Utilities>
    {
        private const string description = "The AdminTools resource gives access to commonly used " +
                                           "tools for administrating a RESTar instance, in form of " +
                                           "views that can be used as static methods.";

        /// <summary>
        /// The message used when returning results from operations
        /// </summary>
        public string Message { get; set; }

        /// <inheritdoc />
        [RESTarView(Description = "Reloads the configuration file")]
        public class ReloadConfiguration : ISelector<Utilities>
        {
            /// <inheritdoc />
            public IEnumerable<Utilities> Select(IRequest<Utilities> request)
            {
                _ReloadConfigFile();
                return new[] {new Utilities {Message = "Configuration file reloaded"}};
            }
        }

        /// <summary>
        /// Reloads the config file and updates the access rights for API keys
        /// </summary>
        public static void _ReloadConfigFile() => RESTarConfig.UpdateConfiguration();

        /// <inheritdoc />
        public IEnumerable<Utilities> Select(IRequest<Utilities> request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            return new[] {new Utilities {Message = description}};
        }
    }
}