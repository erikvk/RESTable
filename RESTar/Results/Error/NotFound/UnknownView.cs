using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    internal class UnknownView : NotFound
    {
        internal UnknownView(string searchString, ITarget resource) : base(ErrorCodes.UnknownResourceView,
            $"RESTar could not locate any resource view in '{resource.Name}' by '{searchString}'.") { }
    }
}