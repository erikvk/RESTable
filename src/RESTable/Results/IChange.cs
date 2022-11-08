using System;
using System.Collections.Generic;

namespace RESTable.Results;

public interface IChange : IResult
{
    /// <summary>
    ///     The type of entities in the entity collection
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    ///     The number of changed entities
    /// </summary>
    long Count { get; }

    /// <summary>
    ///     The changed entities
    /// </summary>
    public IEnumerable<object> GetEntities();
}

public interface IChange<out T> : IChange, IResult where T : class
{
    /// <summary>
    ///     The changed entities
    /// </summary>
    public IReadOnlyCollection<T> Entities { get; }
}
