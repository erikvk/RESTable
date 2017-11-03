﻿using System.Collections.Generic;
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
        new IResource<T> Resource { get; }

        /// <summary>
        /// The conditions of the request
        /// </summary>
        Condition<T>[] Conditions { get; }
    }

    /// <summary>
    /// A non-generic common interface for all request classes used in RESTar
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// The method of the request
        /// </summary>
        Methods Method { get; }

        /// <summary>
        /// The resource of the request
        /// </summary>
        IResource Resource { get; }

        /// <summary>
        /// The meta-conditions of the request
        /// </summary>
        MetaConditions MetaConditions { get; }

        /// <summary>
        /// The origin of the request
        /// </summary>
        Origin Origin { get; }

        /// <summary>
        /// The body of the request
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// The auth token assigned to this request
        /// </summary>
        string AuthToken { get; }

        /// <summary>
        /// To include additional HTTP headers in the response, add them to 
        /// this dictionary. Header names will be renamed to "X-[name]" where
        /// name is the key-value pair key.
        /// </summary>
        IDictionary<string, string> ResponseHeaders { get; }
    }
}