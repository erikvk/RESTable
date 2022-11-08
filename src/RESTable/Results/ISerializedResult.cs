using System;

namespace RESTable.Results;

public interface ISerializedResult : ILogable, ITraceable, IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     The result that was serialized
    /// </summary>
    IResult Result { get; }

    /// <summary>
    ///     The serialized body contained in the result. Can be seekable or non-seekable.
    /// </summary>
    Body Body { get; }

    /// <summary>
    ///     The time it took for RESTable to generate and serialize the result.
    /// </summary>
    TimeSpan TimeElapsed { get; }

    /// <summary>
    ///     The number of entities in the collection. Should be set by the serializer, since it is unknown
    ///     until the collection is iterated.
    /// </summary>
    long EntityCount { get; set; }

    /// <summary>
    ///     Is this a paged result with a next page?
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    ///     Is this a paged result with a previous page?
    /// </summary>
    bool HasPreviousPage { get; }
}
