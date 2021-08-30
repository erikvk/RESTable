using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests
{
    internal interface IEntityRequest<T> : IEntityRequest, IRequest, IRequest<T> where T : class
    {
        IEntityResource<T> EntityResource { get; }
        Func<IAsyncEnumerable<T>>? EntitiesProducer { get; set; }
        Func<IAsyncEnumerable<T>>? GetCustomSelector();
        Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>? GetCustomUpdater();
    }

    /// <inheritdoc />
    /// <summary>
    /// An interface defining the operations for a RESTable request for a resource T.
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
        List<Condition<T>> Conditions { get; set; }

        /// <summary>
        /// The target to use when binding conditions and selecting entities for this request
        /// </summary>
        new ITarget<T> Target { get; }

        /// <summary>
        /// Selects, processes and returns the input entities for this request. Use this in Inserters and
        /// Deleters to receive the entities to insert or delete, and in Updaters to receive and update the
        /// entities selected by the request. The entities may be generated in various different ways, depending
        /// on the request, for example by deserializing input JSON data to this request type. This will run the
        /// entire select query for all entities selected by the request, so it should only be called once.
        /// </summary>
        IEnumerable<T> GetInputEntities();

        /// <summary>
        /// Selects, processes and returns the input entities for this request. Use this in Inserters and
        /// Deleters to receive the entities to insert or delete, and in Updaters to receive and update the
        /// entities selected by the request. The entities may be generated in various different ways, depending
        /// on the request, for example by deserializing input JSON data to this request type. This will run the
        /// entire select query for all entities selected by the request, so it should only be called once.
        /// </summary>
        IAsyncEnumerable<T> GetInputEntitiesAsync();

        /// <summary>
        /// The method used when selecting entities for request input. Set this property to override the default behavior.
        /// This delegate is used in GetInputEntities(). By default RESTable will generate entities by deserializing the request 
        /// body to an <see cref="IAsyncEnumerable{T}"/> using the content type provided in the Content-Type header.
        /// </summary>
        Func<IAsyncEnumerable<T>>? Selector { set; }

        /// <summary>
        /// The method used when updating existing entities. Set this property to override the default behavior.
        /// By default RESTable will populate the existing entities with content from the request body, using the 
        /// content type provided in the Content-Type header.
        /// </summary>
        Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>? Updater { set; }
        
        /// <summary>
        /// Gets a client data point for the current resouce. Data points assigned to the client of the request, for use with RESTable
        /// internally to pass custom data between operations, for example account or session information. Each resource can have its
        /// separate client data set, that is not available from other resuorces.
        /// </summary>
        TData? GetClientData<TData>(string key);

        /// <summary>
        /// Sets a client data point for the current resouce. Data points assigned to the client of the request, for use with RESTable
        /// internally to pass custom data between operations, for example account or session information. Each resource can have its
        /// separate client data set, that is not available from other resuorces.
        /// </summary>
        void SetClientData<TData>(string key, TData value);
    }

    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IDisposable" />
    /// <inheritdoc cref="ILogable" />
    /// <summary>
    /// A non-generic common interface for all request classes used in RESTable
    /// </summary>
    public interface IRequest : IServiceProvider, IProtocolHolder, IHeaderHolder, ITraceable, ILogable, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The method of the request
        /// </summary>
        Method Method { get; set; }

        /// <summary>
        /// The resource of the request
        /// </summary>
        IResource Resource { get; }

        /// <summary>
        /// The target of the request
        /// </summary>
        ITarget Target { get; }

        /// <summary>
        /// Does this request have conditions?
        /// </summary>
        bool HasConditions { get; }

        /// <summary>
        /// The meta-conditions of the request
        /// </summary>
        MetaConditions MetaConditions { get; }

        /// <summary>
        /// The body of the request
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
        Cookies Cookies { get; }

        /// <summary>
        /// The URI parameters that was used to construct this request
        /// </summary>
        IUriComponents UriComponents { get; }

        /// <summary>
        /// Evaluates the request asynchronously and returns the result
        /// </summary>
        ValueTask<IResult> GetResult(CancellationToken cancellationToken = new());

        /// <summary>
        /// Is this request valid?
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// The time elapsed since request evaluation began
        /// </summary>
        TimeSpan TimeElapsed { get; }

        /// <summary>
        /// Gets a deep exact copy of this request
        /// </summary>
        /// <returns></returns>
        ValueTask<IRequest> GetCopy(string? newProtocol = null);
    }
}