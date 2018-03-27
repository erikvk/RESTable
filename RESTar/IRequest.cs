using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Requests;
using RESTar.Resources;

namespace RESTar
{
    internal interface IRequestInternal : IRequest
    {
        bool IsWebSocketUpgrade { get; }
        CachedProtocolProvider CachedProtocolProvider { get; }
    }

    internal interface IRequestInternal<T> : IRequestInternal, IRequest<T> where T : class
    {
        Func<IEnumerable<T>> EntitiesProducer { set; }
        Func<IEnumerable<T>> GetSelector();
        Func<IEnumerable<T>, IEnumerable<T>> GetUpdater();
    }

    /// <inheritdoc />
    /// <summary>
    /// An interface defining the operations for a RESTar request for a resource T.
    /// </summary>
    public interface IRequest<T> : IRequest where T : class
    {
        /// <summary>
        /// The resource of the request
        /// </summary>
        new IEntityResource<T> Resource { get; }

        /// <summary>
        /// Does this request have conditions?
        /// </summary>
        bool HasConditions { get; }

        /// <summary>
        /// The conditions of the request. Cannot be changed while the request is being evaluated
        /// </summary>
        List<Condition<T>> Conditions { get; set; }

        /// <summary>
        /// The target to use when binding conditions and selecting entities for this request
        /// </summary>
        ITarget<T> Target { get; }

        /// <summary>
        /// Returns the entities affected by this request. Use this in Inserters and Deleters to receive
        /// the entities to insert or delete, and in Updaters to receive and update the entities selected 
        /// by the request.
        /// </summary>
        IEnumerable<T> GetEntities();

        /// <summary>
        /// The method used when selecting entities for request input. Set this property to override the default behavior.
        /// This delegate is used in GetEntitites(). By default RESTar will generate entities by deserializing the request 
        /// body to an <see cref="IEnumerable{T}"/> using the content type provided in the Content-Type header.
        /// </summary>
        Func<IEnumerable<T>> Selector { set; }

        /// <summary>
        /// The method used when updating existing entities. Set this property to override the default behavior.
        /// By default RESTar will populate the existing entities with content from the request body, using the 
        /// content type provided in the Content-Type header.
        /// </summary>
        Func<IEnumerable<T>, IEnumerable<T>> Updater { set; }
    }

    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="ILogable" />
    /// <summary>
    /// A non-generic common interface for all request classes used in RESTar
    /// </summary>
    public interface IRequest : ITraceable, ILogable
    {
        /// <summary>
        /// The method of the request
        /// </summary>
        Method Method { get; set; }

        /// <summary>
        /// The resource of the request
        /// </summary>
        IEntityResource Resource { get; }

        /// <summary>
        /// The meta-conditions of the request
        /// </summary>
        MetaConditions MetaConditions { get; }

        /// <summary>
        /// Gets the request body. Cannot be changed while the request is being evaluated
        /// </summary>
        Body Body { get; set; }

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
        IUriComponents UriComponents { get; }

        /// <summary>
        /// Evaluates the request and returns the result
        /// </summary>
        IResult Result { get; }

        /// <summary>
        /// Is this request valid?
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// The time elapsed since request evaluation began
        /// </summary>
        TimeSpan TimeElapsed { get; }
    }
}