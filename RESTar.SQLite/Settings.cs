using System.Collections.Generic;
using System.Runtime.Serialization;
using Starcounter;

namespace RESTar.SQLite
{
    [Database]
    public class Settings
    {
        public string DatabasePath { get; internal set; }
        public string DatabaseDirectory { get; internal set; }
        public string DatabaseName { get; internal set; }

        [IgnoreDataMember] public string DatabaseConnectionString { get; internal set; }

        private const string SQL = "SELECT t FROM RESTar.SQLite.Settings t";
        internal static IEnumerable<Settings> All => Db.SQL<Settings>(SQL);
        internal static Settings Instance => Db.SQL<Settings>(SQL).First;
    }
}