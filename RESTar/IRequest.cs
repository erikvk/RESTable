using System.Collections.Generic;
using System.IO;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A RESTar request for a resource T. This is a common generic interface for all
    /// request types.
    /// </summary>
    public interface IRequest<T> : IRequest where T : class
    {
        /// <summary>
        /// The resource of the request
        /// </summary>
        new IEntityResource<T> Resource { get; }

        /// <summary>
        /// The conditions of the request
        /// </summary>
        Condition<T>[] Conditions { get; }

        /// <summary>
        /// The target to use when binding conditions and selecting entities for this request
        /// </summary>
        ITarget<T> Target { get; }
    }

    /// <summary>
    /// A non-generic common interface for all request classes used in RESTar
    /// </summary>
    public interface IRequest : ITraceable
    {
        /// <summary>
        /// The method of the request
        /// </summary>
        Methods Method { get; }

        /// <summary>
        /// The media type accepted by the client
        /// </summary>
        MimeType Accept { get; }

        /// <summary>
        /// The resource of the request
        /// </summary>
        IEntityResource Resource { get; }

        /// <summary>
        /// The meta-conditions of the request
        /// </summary>
        MetaConditions MetaConditions { get; }

        /// <summary>
        /// The body of the request
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// Gets the request body, deserialized to the given type
        /// </summary>
        T BodyObject<T>() where T : class;

        /// <summary>
        /// The auth token assigned to this request
        /// </summary>
        string AuthToken { get; }

        /// <summary>
        /// The headers included in the request. Headers reserved by RESTar,
        /// for example the Source header, will not be included here.
        /// </summary>
        Headers Headers { get; }

        /// <summary>
        /// To include additional HTTP headers in the response, add them to 
        /// this collection. Headers inserted here with names not already 
        /// beginning with "X-" will be renamed to "X-[name]" where name 
        /// is the key-value pair key. To add cookies, use the Cookies 
        /// string collection instead. 
        /// </summary>
        Headers ResponseHeaders { get; }

        /// <summary>
        /// Set cookies by adding strings to this collection. The strings in this
        /// collection will be used as values for Set-Cookie response headers.
        /// </summary>
        ICollection<string> Cookies { get; }

        /// <summary>
        /// The URI parameters that was used to construct this request
        /// </summary>
        IUriParameters UriParameters { get; }
    }
}