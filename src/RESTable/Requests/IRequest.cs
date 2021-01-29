﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests
{
    internal interface IRequestInternal : IRequest
    {
        bool IsWebSocketUpgrade { get; }
        CachedProtocolProvider CachedProtocolProvider { get; }
    }

    internal interface IEntityRequest : IRequestInternal
    {
        IMacro Macro { get; }
    }

    internal interface IEntityRequest<T> : IEntityRequest, IRequestInternal, IRequest<T> where T : class
    {
        IEntityResource<T> EntityResource { get; }
        Func<IEnumerable<T>> EntitiesProducer { get; set; }
        Func<IEnumerable<T>> GetSelector();
        Func<IEnumerable<T>, IEnumerable<T>> GetUpdater();
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
        /// The method used when selecting entities for request input. Set this property to override the default behavior.
        /// This delegate is used in GetInputEntities(). By default RESTable will generate entities by deserializing the request 
        /// body to an <see cref="IEnumerable{T}"/> using the content type provided in the Content-Type header.
        /// </summary>
        Func<IEnumerable<T>> Selector { set; }

        /// <summary>
        /// The method used when updating existing entities. Set this property to override the default behavior.
        /// By default RESTable will populate the existing entities with content from the request body, using the 
        /// content type provided in the Content-Type header.
        /// </summary>
        Func<IEnumerable<T>, IEnumerable<T>> Updater { set; }

        /// <summary>
        /// Evaluates the request synchronously and returns the result as an entity collection. Only valid for GET requests.
        /// If an error is encountered while evaluating the request, an exception is thrown. Equivalent to Evaluate().ToEntities&lt;T&gt;()
        /// but shorter and with one less generic type parameter.
        /// </summary>
        IEntities<T> EvaluateToEntities();

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
    public interface IRequest : IServiceProvider, ITraceable, ILogable, IDisposable
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
        /// Gets the request body
        /// </summary>
        Body GetBody();

        /// <summary>
        /// Assigns a new Body instance from a .NET object and, optionally, a content type.
        /// If string, Stream or byte array, the content is used directly - with the content type
        /// given in Headers. Otherwise it is serialized using the given content type, or the
        /// protocol default if contentType is null.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="contentType"></param>
        void SetBody(object content, ContentType? contentType = null);

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
        IResult Evaluate();

        /// <summary>
        /// Is this request valid?
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// The time elapsed since request evaluation began
        /// </summary>
        TimeSpan TimeElapsed { get; }

        /// <summary>
        /// Gets an exacy copy of this request
        /// </summary>
        /// <returns></returns>
        IRequest GetCopy(string newProtocol = null);

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
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest WithBody(this IRequest request, object content, ContentType? contentType = null)
        {
            if (request == null) return null;
            request.SetBody(content, contentType);
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
        public static IRequest<T> WithBody<T>(this IRequest<T> request, object content, ContentType? contentType = null) where T : class
        {
            if (request == null) return null;
            request.SetBody(content, contentType);
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
        public static IRequest<T> WithSelector<T>(this IRequest<T> request, Func<IEnumerable<T>> selector) where T : class
        {
            if (request == null) return null;
            request.Selector = selector;
            return request;
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithUpdater<T>(this IRequest<T> request, Func<IEnumerable<T>, IEnumerable<T>> updater) where T : class
        {
            if (request == null) return null;
            request.Updater = updater;
            return request;
        }
    }
}