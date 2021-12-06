﻿using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <inheritdoc />
/// <summary>
///     Interface used to register an authenticator for a given resource type.
///     Authenticators are executed once for each REST request to this resource.
/// </summary>
public interface IAsyncAuthenticatable<T> : IOperationsInterface where T : class
{
    /// <summary>
    ///     The delete method for this IDeleter instance. Defines the Delete
    ///     operation for a given resource.
    /// </summary>
    ValueTask<AuthResults> AuthenticateAsync(IRequest<T> request, CancellationToken cancellationToken);
}