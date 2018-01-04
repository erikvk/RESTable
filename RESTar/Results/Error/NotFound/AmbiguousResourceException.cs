using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    internal class AmbiguousResourceException : NotFound
    {
        internal AmbiguousResourceException(string searchString) : base(ErrorCodes.AmbiguousResource,
            $"RESTar could not uniquely identify a resource by '{searchString}'. Try qualifying the name further. ") { }
    }
}