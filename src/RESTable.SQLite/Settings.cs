using System;
using RESTable.Resources;

namespace RESTable.SQLite
{
    /// <summary>
    /// Settings for the SQLite instance
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// The path to the database
        /// </summary>
        public string? DatabasePath { get; internal set; }

        /// <summary>
        /// The directory of the database
        /// </summary>
        public string? DatabaseDirectory { get; internal set; }

        /// <summary>
        /// The database name
        /// </summary>
        public string? DatabaseName { get; internal set; }

        /// <summary>
        /// The connection string to use when accessing the database
        /// </summary>
        [RESTableMember(ignore: true)]
        public string? DatabaseConnectionString { get; internal set; }

        public static Settings? Instance { get; set; }

        /// <summary>
        /// The SQLite database connection string to use for manual access to the SQLite
        /// database
        /// </summary>
        public static string ConnectionString => Instance?.DatabaseConnectionString ?? throw new InvalidOperationException
        (
            "Cannot access the connection string, are RESTable.SQLite services not added or RESTable not configured?"
        );
    }
}