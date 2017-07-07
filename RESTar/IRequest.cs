using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Requests;

namespace RESTar
{
    /// <summary>
    /// A non-generic common interface for all request classes used in RESTar
    /// </summary>
    public interface IRequestView : IDisposable
    {
        /// <summary>
        /// The method of the request
        /// </summary>
        RESTarMethods Method { get; }

        /// <summary>
        /// The resource of the request
        /// </summary>
        IResourceView Resource { get; }

        /// <summary>
        /// The conditions of the request
        /// </summary>
        Conditions Conditions { get; }

        /// <summary>
        /// The meta-conditions of the request
        /// </summary>
        MetaConditions MetaConditions { get; }

        /// <summary>
        /// The body of the request
        /// </summary>
        string Body { get; }

        /// <summary>
        /// The auth token assigned to this request
        /// </summary>
        string AuthToken { get; }

        /// <summary>
        /// Is this request internal?
        /// </summary>
        bool Internal { get; }

        /// <summary>
        /// To include additional HTTP headers in the response, add them to 
        /// this dictionary. Header names will be renamed to "X-[name]" where
        /// name is the key-value pair key.
        /// </summary>
        IDictionary<string, string> ResponseHeaders { get; }
    }

    /// <summary>
    /// A common interface for RESTar requests
    /// </summary>
    public interface IRequest<T> : IRequestView where T : class
    {
        /// <summary>
        /// The resource of the request
        /// </summary>
        new IResource<T> Resource { get; }
    }
}