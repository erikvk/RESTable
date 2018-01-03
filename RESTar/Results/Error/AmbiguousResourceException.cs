﻿using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar expected a unique match for a resource, but found more than one.
    /// </summary>
    public class AmbiguousResourceException : NotFound
    {
        internal AmbiguousResourceException(string searchString) : base(ErrorCodes.AmbiguousResource,
            $"RESTar could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ") { }
    }
}