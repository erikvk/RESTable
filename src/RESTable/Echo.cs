﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable;

/// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
/// <summary>
///     The Echo resource is a test and utility resource that returns the
///     request conditions as an object.
/// </summary>
[RESTable(GET, POST, PATCH, PUT, REPORT, HEAD, AllowDynamicConditions = true, Description = description)]
public class Echo : Dictionary<string, object?>, IAsyncSelector<Echo>, IAsyncInserter<Echo>, IAsyncUpdater<Echo>
{
    private const string description = "The Echo resource is a test and utility entity resource that " +
                                       "returns the request conditions as an entity.";

    public IAsyncEnumerable<Echo> InsertAsync(IRequest<Echo> request, CancellationToken cancellationToken)
    {
        return request.GetInputEntitiesAsync();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Echo> SelectAsync(IRequest<Echo> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.Conditions.Any())
        {
            var conditionEcho = new Echo();
            foreach (var (key, value) in request.Conditions)
                conditionEcho[key] = value;
            request.Conditions.Clear();
            yield return conditionEcho;
        }

        await foreach (var bodyObject in request.Body.DeserializeAsyncEnumerable<Echo>(cancellationToken).ConfigureAwait(false))
        {
            var bodyEcho = new Echo();
            foreach (var (key, value) in bodyObject)
                bodyEcho[key] = value;
            yield return bodyEcho;
        }
    }

    public IAsyncEnumerable<Echo> UpdateAsync(IRequest<Echo> request, CancellationToken cancellationToken)
    {
        return request.GetInputEntitiesAsync();
    }
}
