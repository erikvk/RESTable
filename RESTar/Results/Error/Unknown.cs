using System;
using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class Unknown : Internal
    {
        internal Unknown(Exception e) : base(ErrorCodes.Unknown, e.Message, e) { }
    }
}