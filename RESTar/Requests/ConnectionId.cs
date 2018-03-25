using System.Linq;
using Starcounter;

namespace RESTar.Queries {
    /// <summary>
    /// An ID, generated when a new connection is set up
    /// </summary>
    [Database]
    public class ConnectionId
    {
        private const string All = "SELECT t FROM RESTar.Requests.ConnectionId t";

        /// <summary>
        /// The number stored in the database
        /// </summary>
        public ulong _number { get; private set; }

        private ConnectionId() { }
        internal static string Next => DbHelper.Base64EncodeObjectNo(Db.Transact(() => Get._number += 1));
        private static ConnectionId Get => Db.SQL<ConnectionId>(All).FirstOrDefault() ?? Db.Transact(() => new ConnectionId());
    }
}