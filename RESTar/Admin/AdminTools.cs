using System.Collections.Generic;
using System.Runtime.Serialization;
using static RESTar.Methods;

namespace RESTar.Admin
{
    /// <summary>
    /// Provides access to commmon admin tasks
    /// </summary>
    [RESTar(GET, PATCH, Description = description)]
    public class AdminTools : ISelector<AdminTools>, IUpdater<AdminTools>
    {
        private const string description = "The AdminTools resource gives access to commonly used " +
                                           "tools for administrating a RESTar instance.";

        /// <summary>
        /// Should the config file be reloaded?
        /// </summary>
        [DataMember(Name = "ReloadConfigFile")]
        public bool _ReloadConfigFile { get; set; }

        /// <summary>
        /// Reloads the config file and updates the access rights for API keys
        /// </summary>
        public static void ReloadConfigFile() => RESTarConfig.UpdateAuthInfo();

        /// <inheritdoc />
        public IEnumerable<AdminTools> Select(IRequest<AdminTools> request) => new[] {new AdminTools()};

        /// <inheritdoc />
        public int Update(IEnumerable<AdminTools> entities, IRequest<AdminTools> request)
        {
            var updated = false;
            var count = 0;
            foreach (var at in entities)
            {
                if (at._ReloadConfigFile)
                {
                    ReloadConfigFile();
                    updated = true;
                }

                if (updated) count += 1;
            }
            return count;
        }
    }
}