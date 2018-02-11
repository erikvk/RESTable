using System;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an operation was aborted by a commit hook. This is interpreted as a forbidden operation.
    /// search string.
    /// </summary>
    public class AbortedByCommitHook : Forbidden
    {
        /// <inheritdoc />
        public AbortedByCommitHook(Exception e) : base(ErrorCodes.AbortedByCommitHook, e.Message, e) { }
    }
}