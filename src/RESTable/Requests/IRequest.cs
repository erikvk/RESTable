using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests
{
    internal interface IRequestInternal : IRequest
    {
        bool IsWebSocketUpgrade { get; }
    }

    internal interface IEntityRequest : IRequestInternal
    {
        IMacro Macro { get; }
    }

    internal interface IEntityRequest<T> : IEntityRequest, IRequestInternal, IRequest<T> where T : class
    {
        IEntityResource<T> EntityResource { get; }
        Func<IAsyncEnumerable<T>> EntitiesProducer { get; set; }
        Func<IAsyncEnumerable<T>> GetSelector();
        Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> GetUpdater();
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
        ITarget<T> Target { get; }

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
        /// body to an <see cref="IEnumerable{T}"/> using the content type provided in the Content-Type header.
        /// </summary>
        Func<IAsyncEnumerable<T>> Selector { set; }

        /// <summary>
        /// The method used when updating existing entities. Set this property to override the default behavior.
        /// By default RESTable will populate the existing entities with content from the request body, using the 
        /// content type provided in the Content-Type header.
        /// </summary>
        Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> Updater { set; }

        /// <summary>
        /// Evaluates the request synchronously and returns the result as an entity collection. Only valid for GET requests.
        /// If an error is encountered while evaluating the request, an exception is thrown. Equivalent to Evaluate().ToEntities&lt;T&gt;()
        /// but shorter and with one less generic type parameter.
        /// </summary>
        Task<IEntities<T>> EvaluateToEntities();

        /// <summary>
        /// Gets a client data point for the current resouce. Data points assigned to the client of the request, for use with RESTable
        /// internally to pass custom data between operations, for example account or session information. Each resource can have its
        /// separate client data set, that is not available from other resuorces.
        /// </summary>
        TData GetClientData<TData>(string key);

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
        /// The type of the request target
        /// </summary>
        Type TargetType { get; }

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
        [NotNull]
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
        /// Evaluates the request synchronously and returns the result
        /// </summary>
        Task<IResult> Evaluate();

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
        Task<IRequest> GetCopy(string newProtocol = null);

        /// <summary>
        /// Adds a service object to this request, that is disposed when the
        /// request is disposed.
        /// </summary>
        void EnsureServiceAttached<T>(T service) where T : class;

        /// <summary>
        /// Adds a service object to this request, that is disposed when the
        /// request is disposed.
        /// </summary>
        void EnsureServiceAttached<TService, TImplementation>(TImplementation service) where TImplementation : class, TService where TService : class;
    }

    /// <summary>
    /// Extension methods for IRequest
    /// </summary>
    public static class ExtensionMethods
    {
        public static TResult Expecting<TResult, TResource>(this IRequest<TResource> request, Func<IRequest<TResource>, TResult> selector, string errorMessage) where TResource : class
        {
            try
            {
                return selector(request);
            }
            catch (Exception e)
            {
                errorMessage = $"Error in request to resource '{typeof(TResource).GetRESTableTypeName()}': {errorMessage}";
                throw new BadRequest(ErrorCodes.Unknown, errorMessage, e);
            }
        }
        
        public static async Task<TResult> Expecting<TResult, TResource>(this IRequest<TResource> request, Func<IRequest<TResource>, Task<TResult>> selector, string errorMessage) where TResource : class
        {
            try
            {
                return await selector(request);
            }
            catch (Exception e)
            {
                errorMessage = $"Error in request to resource '{typeof(TResource).GetRESTableTypeName()}': {errorMessage}";
                throw new BadRequest(ErrorCodes.Unknown, errorMessage, e);
            }
        }

        /// <summary>
        /// Sets the given method to the request, and returns the request
        /// </summary>
        public static IRequest WithMethod(this IRequest request, Method method)
        {
            if (request == null) return null;
            request.Method = method;
            return request;
        }

        /// <summary>
        /// Sets the given method to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithMethod<T>(this IRequest<T> request, Method method) where T : class
        {
            if (request == null) return null;
            request.Method = method;
            return request;
        }

        /// <summary>
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest WithBody(this IRequest request, object bodyObject)
        {
            return WithBody(request, new Body(request, bodyObject));
        }

        /// <summary>
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithBody<T>(this IRequest<T> request, object bodyObject) where T : class
        {
            return WithBody(request, new Body(request, bodyObject));
        }

        /// <summary>
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest WithBody(this IRequest request, Body body)
        {
            if (request == null) return null;
            request.Body = body;
            return request;
        }

        /// <summary>
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithBody<T>(this IRequest<T> request, Body body) where T : class
        {
            if (request == null) return null;
            request.Body = body;
            return request;
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithConditions<T>(this IRequest<T> request, IEnumerable<Condition<T>> conditions) where T : class
        {
            if (request == null) return null;
            request.Conditions = conditions?.ToList();
            return request;
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithConditions<T>(this IRequest<T> request, params Condition<T>[] conditionsArray) where T : class
        {
            if (request == null) return null;
            return WithConditions(request, conditions: conditionsArray);
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithSelector<T>(this IRequest<T> request, Func<IAsyncEnumerable<T>> selector) where T : class
        {
            if (request == null) return null;
            request.Selector = selector;
            return request;
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithEntities<T>(this IRequest<T> request, IEnumerable<T> entities) where T : class
        {
            if (request == null) return null;
            request.Selector = entities.ToAsyncEnumerable;
            return request;
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithEntities<T>(this IRequest<T> request, params T[] entities) where T : class
        {
            return request.WithEntities((IEnumerable<T>) entities);
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithUpdater<T>(this IRequest<T> request, Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> updater) where T : class
        {
            if (request == null) return null;
            request.Updater = updater;
            return request;
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithMetaConditions<T>(this IRequest<T> request, Action<MetaConditions> editMetaconditions) where T : class
        {
            if (request == null) return null;
            editMetaconditions(request.MetaConditions);
            return request;
        }
    }
}