using System;
using RESTar.Internal;

namespace RESTar.Results.Fail.Forbidden
{
    internal class AbortedByCommitHook : Forbidden
    {
        public AbortedByCommitHook(Exception e) : base(ErrorCodes.AbortedByCommitHook, e.Message, e) { }
    }
}