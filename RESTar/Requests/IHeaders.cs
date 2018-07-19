using System.Collections.Generic;

namespace RESTar.Requests
{
    internal interface IHeadersInternal : IHeaders
    {
        /// <summary>
        /// Returns the custom headers, optionally including whitelisted non-custom headers
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> GetCustom(HashSet<string> whitelist = null);
    }

    /// <inheritdoc cref="IDictionary{TKey,TValue}" />
    /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}" />
    /// <summary>
    /// A common interface for objects representing request headers
    /// </summary>
    public interface IHeaders : IDictionary<string, string>
    {
        /// <summary>
        /// The Accept header
        /// </summary>
        ContentTypes Accept { get; set; }

        /// <summary>
        /// The Content-Type header
        /// </summary>
        ContentType? ContentType { get; set; }

        /// <summary>
        /// The Source header
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// The Destination header
        /// </summary>
        string Destination { get; set; }

        /// <summary>
        /// The Authorization header
        /// </summary>
        string Authorization { get; set; }

        /// <summary>
        /// The Origin header
        /// </summary>
        string Origin { get; set; }
    }
}