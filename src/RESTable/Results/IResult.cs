﻿using System;
using System.Net;
using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc cref="ILogable" />
/// <inheritdoc cref="IDisposable" />
/// <summary>
///     A RESTable result
/// </summary>
public interface IResult : ILogable, IHeaderHolder, ITraceable, IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     The protocol holder of this result
    /// </summary>
    IProtocolHolder ProtocolHolder { get; }

    /// <summary>
    ///     The request that generated this result
    /// </summary>
    IRequest Request { get; }

    /// <summary>
    ///     The status code of the result
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    ///     The description of the status
    /// </summary>
    string StatusDescription { get; }

    /// <summary>
    ///     The cookies to set in the response
    /// </summary>
    Cookies Cookies { get; }

    /// <summary>
    ///     Is this a successful result?
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    ///     Is this an error result?
    /// </summary>
    bool IsError { get; }

    /// <summary>
    ///     The time it took for RESTable to generate the response.
    /// </summary>
    TimeSpan TimeElapsed { get; }

    /// <summary>
    ///     The metadata for this result, for use in the RESTable-metadata header in remote requests.
    /// </summary>
    string Metadata { get; }

    /// <summary>
    ///     If the result is non-successful, invoking this method will throw an
    ///     exception containing the erroneous result.
    /// </summary>
    void ThrowIfError();

    /// <summary>
    ///     Tries to convert the result to an IEntities instance, or throws an
    ///     Exception if the result is non-successful or cannot be cast to the given type.
    /// </summary>
    IEntities<T> ToEntities<T>() where T : class;
}
