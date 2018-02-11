using System;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    public class AbortedByCommitHook : Forbidden
    {
        public AbortedByCommitHook(Exception e) : base(ErrorCodes.AbortedByCommitHook, e.Message, e) { }
    }
}