using System;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class Unknown : Internal
    {
        public Unknown(Exception e) : base(ErrorCodes.Unknown, e.Message, e) { }
    }
}