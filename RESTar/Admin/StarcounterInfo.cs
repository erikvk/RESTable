using System.Collections.Generic;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using Starcounter;
using Starcounter.Internal;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Contains information about the current Starcounter database
    /// </summary>
    [RESTar(Method.GET)]
    public class StarcounterInfo : ISelector<StarcounterInfo>
    {
        /// <summary>
        /// The name of the application
        /// </summary>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// The current Starcounter version
        /// </summary>
        public string StarcounterVersion { get; private set; }

        /// <summary>
        /// The channel of the Starcounter installation
        /// </summary>
        public string StarcounterChannel { get; private set; }

        /// <summary>
        /// The name of the current Starcounter database
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// The number of schedulers for the current database
        /// </summary>
        public int NrOfSchedulers { get; private set; }

        /// <summary>
        /// The URL to Starcounter's documentation site
        /// </summary>
        public string StarcounterDocumentationURL => "https://docs.starcounter.io";

        /// <inheritdoc />
        public IEnumerable<StarcounterInfo> Select(IRequest<StarcounterInfo> request) => new[] {Create()};

        /// <summary>
        /// Creates a StarcounterInfo instance
        /// </summary>
        /// <returns></returns>
        public static StarcounterInfo Create() => new StarcounterInfo
        {
            ApplicationName = Application.Current.Name,
            StarcounterVersion = CurrentVersion.Version,
            StarcounterChannel = CurrentVersion.ChannelName,
            DatabaseName = Db.Environment.DatabaseNameLower,
            NrOfSchedulers = Db.Environment.SchedulerCount
        };
    }
}