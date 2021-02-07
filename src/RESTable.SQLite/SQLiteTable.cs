using System.Threading.Tasks;
using RESTable.Resources;

namespace RESTable.SQLite
{
    /// <summary>
    /// The base class for all SQLite table resource types
    /// </summary>
    public abstract class SQLiteTable
    {
        /// <summary>
        /// The unique SQLite row ID for this row
        /// </summary>
        [RESTableMember(order: int.MaxValue), Key]
        public long RowId { get; internal set; }

        internal Task _OnSelect() => OnSelect();
        internal Task _OnInsert() => OnInsert();
        internal Task _OnUpdate() => OnUpdate();
        internal Task _OnDelete() => OnDelete();

        /// <summary>
        /// Called for this entity after it has been created and populated with data from
        /// the SQLite table.
        /// </summary>
        protected virtual Task OnSelect() => Task.CompletedTask;

        /// <summary>
        /// Called for this entity before it is converted to a row in the SQLite table. No
        /// new dynamic members can be added here, since the INSERT statement is already
        /// compiled. Values can be changed.
        /// </summary>
        protected virtual Task OnInsert() => Task.CompletedTask;

        /// <summary>
        /// Called for this entity before it is used to push updates to a given row in
        /// the SQLite table.
        /// </summary>
        protected virtual Task OnUpdate() => Task.CompletedTask;

        /// <summary>
        /// Called for this entity before it is deleted from the SQLite table.
        /// </summary>
        protected virtual Task OnDelete() => Task.CompletedTask;
    }
}