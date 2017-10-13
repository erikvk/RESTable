using Starcounter;

namespace RESTar.SQLite
{
    [Database]
    public class SQLiteSettings
    {
        public string DatabasePath { get; internal set; }
        private const string SQL = "SELECT t FROM RESTar.SQLite.SQLiteSettings t";
        internal static SQLiteSettings Instance => Db.SQL<SQLiteSettings>(SQL).First;
    }
}