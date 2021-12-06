using System;
using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Results;

public abstract class Change : OK, IChange
{
    protected Change(IRequest request, long count) : base(request)
    {
        MaxNumberOfEntitiesInChangeResults = request.Context.Configuration.MaxNumberOfEntitiesInChangeResults;
        Count = count;
    }

    private int MaxNumberOfEntitiesInChangeResults { get; }

    /// <summary>
    ///     True if the number of entities changed exceeded the maximum number of entities
    ///     that can be included in the Entities array of this result.
    /// </summary>
    public bool TooManyEntities => Count > MaxNumberOfEntitiesInChangeResults;

    /// <summary>
    ///     The number of changed entities
    /// </summary>
    public long Count { get; }

    /// <summary>
    ///     Gets the changed non-deleted entities in case of TooManyEntities being false. Else this enumeratoin is empty.
    /// </summary>
    public abstract IEnumerable<object> GetEntities();

    public abstract Type EntityType { get; }
}

/// <summary>
///     A result that encodes a change in a resource, for example an update or insert
/// </summary>
public abstract class Change<T> : Change, IChange<T> where T : class
{
    /// <inheritdoc />
    protected Change(IRequest request, long count, IReadOnlyCollection<T> entities) : base(request, count)
    {
        Entities = entities;
    }

    /// <summary>
    ///     The changed non-deleted entities in case of TooManyEntities being false. Else this array is empty.
    /// </summary>
    public IReadOnlyCollection<T> Entities { get; }

    public override IEnumerable<object> GetEntities()
    {
        return Entities;
    }

    public override Type EntityType => typeof(T);
}