using System.Threading.Tasks;
using RESTable.Resources;

namespace RESTable.Sqlite;

/// <summary>
///     The base class for all SQLite table resource types
/// </summary>
public abstract class SqliteTable
{
    /// <summary>
    ///     The unique SQLite row ID for this row
    /// </summary>
    [RESTableMember(order: int.MaxValue)]
    [Key]
    public long RowId { get; internal set; }

    internal Task _OnSelect()
    {
        return OnSelect();
    }

    internal Task _OnInsert()
    {
        return OnInsert();
    }

    internal Task _OnUpdate()
    {
        return OnUpdate();
    }

    internal Task _OnDelete()
    {
        return OnDelete();
    }

    /// <summary>
    ///     Called for this entity after it has been created and populated with data from
    ///     the SQLite table.
    /// </summary>
    protected virtual Task OnSelect()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called for this entity before it is converted to a row in the SQLite table. No
    ///     new dynamic members can be added here, since the INSERT statement is already
    ///     compiled. Values can be changed.
    /// </summary>
    protected virtual Task OnInsert()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called for this entity before it is used to push updates to a given row in
    ///     the SQLite table.
    /// </summary>
    protected virtual Task OnUpdate()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called for this entity before it is deleted from the SQLite table.
    /// </summary>
    protected virtual Task OnDelete()
    {
        return Task.CompletedTask;
    }
}