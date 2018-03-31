namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A result that contains only status code, description and headers
    /// </summary>
    public class Head : OK
    {
        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; }

        internal Head(IRequest request, ulong count) : base(request) => EntityCount = count;
    }
}