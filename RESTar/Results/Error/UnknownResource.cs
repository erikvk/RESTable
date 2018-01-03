﻿using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar cannot locate a resource using a given search string
    /// </summary>
    public class UnknownResource : NotFound
    {
        internal UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'.") { }
    }
}