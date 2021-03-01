namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Contains the results of a call to IsAuthenticated()
    /// </summary>
    public struct AuthResults
    {
        internal readonly bool Success;
        internal readonly string FailedReason;

        /// <summary>
        /// Creates a new AuthenticationResults with the given success status and failedReason.
        /// failedReason is only used when success is false.
        /// </summary>
        public AuthResults(bool success = true, string failedReason = null)
        {
            Success = success;
            FailedReason = failedReason;
        }

        public static implicit operator AuthResults(string failedReason) => new(false, failedReason);
        public static implicit operator AuthResults(bool success) => new(success);
    }
}