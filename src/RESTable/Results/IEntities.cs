using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RESTable.Results;

/// <inheritdoc cref="IEnumerable" />
/// <inheritdoc cref="IResult" />
/// <inheritdoc cref="ISerializedResult" />
/// <summary>
///     A non-generic interface for a collection of result entities from a RESTable request
/// </summary>
public interface IEntities : IResult
{
    /// <summary>
    ///     The type of entities in the entity collection
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    ///     Counts the number of entities in this result
    /// </summary>
    ValueTask<long> CountAsync();
}

/// <inheritdoc cref="IEntities" />
/// <inheritdoc cref="IEnumerable{T}" />
/// <summary>
///     A generic interface for a collection of result entities from a RESTable request
/// </summary>
/// <typeparam name="T">The entity type contained in the entity collection</typeparam>
public interface IEntities<out T> : IEntities, IAsyncDisposable, IAsyncEnumerable<T> where T : class
{
    public IEntities<T> Result { get; }

    /// <summary>
    ///     Marks this result as 204 NoContent
    /// </summary>
    void MakeNoContent();
}
