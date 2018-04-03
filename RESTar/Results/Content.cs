using System;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A 200 result that encodes a content
    /// </summary>
    public abstract class Content : OK
    {
        /// <summary>
        /// The number of entities contained in this result
        /// </summary>
        public ulong EntityCount { get; set; }

        /// <summary>
        /// The type of entities contained in this result
        /// </summary>
        public abstract Type EntityType { get; }

        /// <inheritdoc />
        protected Content(IRequest request) : base(request) { }
    }
}