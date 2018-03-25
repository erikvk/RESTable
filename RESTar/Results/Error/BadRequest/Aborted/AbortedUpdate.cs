﻿using System;
using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest.Aborted
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error updating entities in a given resource.
    /// </summary>
    internal class AbortedUpdate<T> : AbortedOperation<T> where T : class
    {
        internal AbortedUpdate(Exception ie, IQuery<T> query, string message = null)
            : base(ErrorCodes.AbortedUpdate, ie, query, message) { }
    }
}