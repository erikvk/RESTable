using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class Unknown : Internal
    {
        internal Unknown(Exception e) : base(ErrorCodes.Unknown, e.Message, e) { }
    }
}