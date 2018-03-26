using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A RESTar result
    /// </summary>
    public interface IResult : ILogable
    {
        /// <summary>
        /// The status code of the result
        /// </summary>
        HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The description of the status
        /// </summary>
        string StatusDescription { get; }

        /// <summary>
        /// The cookies to set in the response
        /// </summary>
        ICollection<string> Cookies { get; }

        /// <summary>
        /// Serializes the result and prepares output streams and content types.
        /// Optionally, provide a content type to serialize the result with.
        /// If null, the content type specified in the request will be used.
        /// If no content type is specified in the request, the default content 
        /// type for the protocol is used.
        /// </summary>
        ISerializedResult Serialize(ContentType? contentType = null);

        /// <summary>
        /// If the result is non-successful, invoking this method will throw an 
        /// exception containing the erroneous result.
        /// </summary>
        void ThrowIfError();

        /// <summary>
        /// Tries to convert the result to an Entities instance, or throws an 
        /// Exception if the result is non-successful or cannot be cast to the given type.
        /// </summary>
        IEnumerable<T> ToEntities<T>() where T : class;
    }

    /// <inheritdoc />
    /// <summary>
    /// Describes a result that is ready to be sent back to the client, for example 
    /// in an HTTP response.
    /// </summary>
    public interface ISerializedResult : IResult
    {
        /// <summary>
        /// The serialized body contained in the result
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// The content type of the result body, if any
        /// </summary>
        ContentType? ContentType { get; }

        /// <summary>
        /// The time it took for RESTar to generate the response. Calculated as the request 
        /// evaluation time plus time for serialization.
        /// </summary>
        TimeSpan TimeElapsed { get; }
    }
}