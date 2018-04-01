using System;
using RESTar.Requests;

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

        /// <summary>
        /// The content type of this result
        /// </summary>
        public new ContentType ContentType { get; private set; }

        /// <inheritdoc />
        protected Content(IRequest request) : base(request) { }
    }
}