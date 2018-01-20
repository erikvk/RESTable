﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Requests;

namespace RESTar.Operations
{
    /// <summary>
    /// Describes a result that is ready to be sent back to the client, for example 
    /// using an HTTP response
    /// </summary>
    internal interface IFinalizedResult
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
        /// The body contained in the result
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// The content type of the result body
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// The headers to use in HTTP responses based on this result
        /// </summary>
        Headers Headers { get; }

        /// <summary>
        /// The cookies to set in the response
        /// </summary>
        ICollection<string> Cookies { get; }
    }
}