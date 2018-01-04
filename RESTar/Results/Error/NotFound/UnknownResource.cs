using RESTar.Internal;

namespace RESTar.Results.Fail.NotFound
{
    internal class UnknownResource : NotFound
    {
        internal UnknownResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any resource by '{searchString}'.") { }
    }
}