using System;
using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Queries;

namespace RESTar
{
    internal interface IQueryInternal : IQuery
    {
        bool IsWebSocketUpgrade { get; }
        CachedProtocolProvider CachedProtocolProvider { get; }
    }

    internal interface IQueryInternal<T> : IQueryInternal, IQuery<T> where T : class
    {
        EntitiesSelector<T> EntitiesProducer { set; }
        EntitiesSelector<T> GetSelector();
        EntitiesUpdater<T> GetUpdater();
    }

    /// <inheritdoc />
    /// <summary>
    /// A RESTar request for a resource T. This is a common generic interface for all
    /// request types.
    /// </summary>
    public interface IQuery<T> : IQuery where T : class
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
        /// Returns the processed entities belonging to this request. If the request is an update request,
        /// for example, this IEnumerable contains all the updated entities. For insert requests, all the 
        /// requests to insert, and so on. For update and insert requests, if the resource type is a
        /// Starcounter database class, make sure to call GetEntities() from inside a transaction scope.
        /// The returned value from GetEntities() is never null, but may contain zero entities.
        /// </summary>
        IEnumerable<T> GetEntities();

        /// <summary>
        /// The method used when inserting new entities. Set this property to override the default behavior.
        /// By default RESTar will deserialize the request body to an <see cref="IEnumerable{T}"/> using the 
        /// content type provided in the Content-Type header.
        /// </summary>
        EntitiesSelector<T> Selector { set; }

        /// <summary>
        /// The method used when updating existing entities. Set this property to override the default behavior.
        /// By default RESTar will populate the existing entities with content from the request body, using the 
        /// content type provided in the Content-Type header.
        /// </summary>
        EntitiesUpdater<T> Updater { set; }
    }

    /// <summary>
    /// Defines a function that generates a collection of resource entities.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public delegate IEnumerable<T> EntitiesSelector<out T>() where T : class;

    /// <summary>
    /// Defines a function that updates a collection of resource entities.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public delegate IEnumerable<T> EntitiesUpdater<T>(IEnumerable<T> source);

    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="ILogable" />
    /// <summary>
    /// A non-generic common interface for all request classes used in RESTar
    /// </summary>
    public interface IQuery : ITraceable, ILogable
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