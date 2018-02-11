using System;
using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    public class UnknownEntityResource : NotFound
    {
        public UnknownEntityResource(ErrorCodes code, string message, Exception ie) : base(code, message, ie) { }

        internal UnknownEntityResource(string searchString) : base(ErrorCodes.UnknownResource,
            $"RESTar could not locate any entity resource by '{searchString}'.") { }
    }
}