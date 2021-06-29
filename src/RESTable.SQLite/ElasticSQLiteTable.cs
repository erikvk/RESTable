using System;
using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.SQLite
{
    /// <inheritdoc cref="SQLiteTable" />
    /// <inheritdoc cref="IDynamicMemberValueProvider" />
    /// <summary>
    /// Defines an elastic SQLite table
    /// </summary>
    public abstract class ElasticSQLiteTable : SQLiteTable, IDynamicMemberValueProvider
    {
        /// <summary>
        /// The dynamic members stored for this instance
        /// </summary>
        [
            SQLiteMember(ignore: true),
            RESTableMember(hide: true, mergeOntoOwner: true)
        ]
        public DynamicMemberCollection DynamicMembers { get; }

        /// <summary>
        /// Indexer used for access to dynamic members
        /// </summary>
        public object? this[string memberName]
        {
            get => DynamicMembers.SafeGet(memberName);
            set => DynamicMembers.TrySetValue(memberName, value);
        }

        /// <summary>
        /// Creates a new instance of this ElasticSQLiteTable type
        /// </summary>
        protected ElasticSQLiteTable()
        {
            var tableMapping = TableMapping.GetTableMapping(GetType());
            if (tableMapping is null)
                throw new InvalidOperationException($"No table mapping for type '{GetType().GetRESTableTypeName()}'");
            DynamicMembers = new DynamicMemberCollection(tableMapping);
        }

        /// <inheritdoc />
        public bool TryGetValue(string memberName, out object? value, out string? actualMemberName)
        {
            return DynamicMembers.TryGetValue(memberName, out value, out actualMemberName);
        }

        /// <inheritdoc />
        public bool TrySetValue(string memberName, object? value)
        {
            return DynamicMembers.TrySetValue(memberName, value);
        }
    }
}