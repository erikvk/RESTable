using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Requests.Filters;
using RESTable.Results;

namespace RESTable.Requests;

/// <summary>
///     Extension methods for IRequest
/// </summary>
public static class ExtensionMethods
{
    public static TResult Expecting<TResult, TResource>(this IRequest<TResource> request, Func<IRequest<TResource>, TResult> selector, string errorMessage)
        where TResource : class
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

    public static async Task<TResult> Expecting<TResult, TResource>(this IRequest<TResource> request, Func<IRequest<TResource>, Task<TResult>> selector, string errorMessage)
        where TResource : class
    {
        try
        {
            return await selector(request).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            errorMessage = $"Error in request to resource '{typeof(TResource).GetRESTableTypeName()}': {errorMessage}";
            throw new BadRequest(ErrorCodes.Unknown, errorMessage, e);
        }
    }

    /// <summary>
    ///     Sets the given method to the request, and returns the request
    /// </summary>
    public static IRequest WithMethod(this IRequest request, Method method)
    {
        request.Method = method;
        return request;
    }

    /// <summary>
    ///     Sets the given method to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithMethod<T>(this IRequest<T> request, Method method) where T : class
    {
        request.Method = method;
        return request;
    }

    /// <summary>
    ///     Sets the given body to the request, and returns the request
    /// </summary>
    public static IRequest WithBody(this IRequest request, object? bodyObject)
    {
        request.Body = new Body(request, bodyObject);
        return request;
    }

    /// <summary>
    ///     Sets the given body to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithBody<T>(this IRequest<T> request, object? bodyObject) where T : class
    {
        request.Body = new Body(request, bodyObject);
        return request;
    }

    /// <summary>
    ///     Sets the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithLimit<T>(this IRequest<T> request, Limit limit) where T : class
    {
        request.MetaConditions.Limit = limit;
        return request;
    }

    /// <summary>
    ///     Sets the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithOffset<T>(this IRequest<T> request, Offset offset) where T : class
    {
        request.MetaConditions.Offset = offset;
        return request;
    }

#if !NETSTANDARD2_0
    /// <summary>
    ///     Sets the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithOffsetAndLimit<T>(this IRequest<T> request, Range range) where T : class
    {
        var (offset, limit) = range.ToSlicedOffsetAndLimit(0, -1);
        request.MetaConditions.Offset = offset;
        request.MetaConditions.Limit = limit;
        return request;
    }
#endif

    /// <summary>
    ///     Sets the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithOffsetAndLimit<T>(this IRequest<T> request, Offset offset, Limit limit) where T : class
    {
        request.MetaConditions.Offset = offset;
        request.MetaConditions.Limit = limit;
        return request;
    }

    /// <summary>
    ///     Removes all conditions from the request by setting Conditions to a new List of Condition of T
    /// </summary>
    public static IRequest<T> WithNoConditions<T>(this IRequest<T> request) where T : class
    {
        request.Conditions = new List<Condition<T>>();
        return request;
    }

    /// <summary>
    ///     Sets a set of parameters from a given object to the request as conditions and returns the request
    /// </summary>
    public static IRequest<T> WithAddedParameters<T>(this IRequest<T> request, object? parameters) where T : class
    {
        if (parameters is null) return request;
        var jsonProvider = request.GetRequiredService<IJsonProvider>();
        var jsonElement = jsonProvider.ToJsonElement(parameters);
        if (jsonElement.ValueKind != JsonValueKind.Object) throw new ArgumentException($"Invalid parameters object. Expected object, found '{jsonElement.ValueKind}'");
        foreach (var property in jsonElement.EnumerateObject()) request.WithAddedCondition(property.Name, Operators.EQUALS, jsonProvider.ToObject<object>(property.Value));
        return request;
    }

    /// <summary>
    ///     Adds the given parameter as a condition to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithAddedParameter<T>(this IRequest<T> request, string key, object? value) where T : class
    {
        return request.WithAddedCondition(key, Operators.EQUALS, value);
    }

    /// <summary>
    ///     Adds the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithAddedConditions<T>(this IRequest<T> request, IEnumerable<Condition<T>> conditions) where T : class
    {
        request.Conditions.AddRange(conditions);
        return request;
    }

    /// <summary>
    ///     Adds the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithAddedConditions<T>(this IRequest<T> request, params Condition<T>[] conditionsArray) where T : class
    {
        return WithAddedConditions(request, conditions: conditionsArray);
    }

    /// <summary>
    ///     Adds the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithAddedCondition<T>(this IRequest<T> request, string key, Operators op, object? value) where T : class
    {
        return WithAddedConditions(request, (key, op, value));
    }

    /// <summary>
    ///     Adds the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithAddedCondition<T>(this IRequest<T> request, string key, Operators op, object? value, out Condition<T> condition) where T : class
    {
        var termFactory = request.GetRequiredService<TermFactory>();
        var target = request.Target;
        condition = new Condition<T>
        (
            termFactory.MakeConditionTerm(target, key),
            op,
            value
        );
        return WithAddedConditions(request, condition);
    }

    /// <summary>
    ///     Adds the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithAddedConditions<T>(this IRequest<T> request, params (string key, Operators op, object? value)[] conditions) where T : class
    {
        var termFactory = request.GetRequiredService<TermFactory>();
        var target = request.Target;

        IEnumerable<Condition<T>> Converter()
        {
            foreach (var (key, op, value) in conditions)
                yield return new Condition<T>
                (
                    termFactory.MakeConditionTerm(target, key),
                    op,
                    value
                );
        }

        return WithAddedConditions(request, Converter());
    }

    /// <summary>
    ///     Sets the given selector to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithSelector<T>(this IRequest<T> request, Func<IAsyncEnumerable<T>>? selector) where T : class
    {
        request.Selector = selector;
        return request;
    }

    /// <summary>
    ///     Sets the given selector to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithSelectorEntities<T>(this IRequest<T> request, IEnumerable<T> entities) where T : class
    {
        request.Selector = entities.ToAsyncEnumerable;
        return request;
    }

    /// <summary>
    ///     Sets the given selector to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithSelectorEntities<T>(this IRequest<T> request, params T[] entities) where T : class
    {
        return request.WithSelectorEntities((IEnumerable<T>) entities);
    }

    /// <summary>
    ///     Sets the given selector to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithUpdater<T>(this IRequest<T> request, Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>>? updater) where T : class
    {
        request.Updater = updater;
        return request;
    }

    /// <summary>
    ///     Sets the given conditions to the request, and returns the request
    /// </summary>
    public static IRequest<T> WithMetaConditions<T>(this IRequest<T> request, Action<MetaConditions> editMetaconditions) where T : class
    {
        editMetaconditions(request.MetaConditions);
        return request;
    }

    /// <summary>
    ///     Evaluates the request asynchronously and returns the result, or
    ///     disposes the result and throws an exception if the result is an error.
    /// </summary>
    public static async ValueTask<IResult> GetResultOrThrow(this IRequest request, CancellationToken cancellationToken = new())
    {
        var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
        if (result is Error error)
        {
            await request.DisposeAsync().ConfigureAwait(false);
            throw error;
        }
        return result;
    }

    /// <summary>
    ///     Evaluates the request asynchronously and returns the result, or
    ///     disposes the result and throws an exception if the result is an error
    ///     or not of the given result type.
    /// </summary>
    public static async ValueTask<TResult> GetResultOrThrow<TResult>(this IRequest request, CancellationToken cancellationToken = new()) where TResult : IResult
    {
        var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
        switch (result)
        {
            case Error error:
            {
                await request.DisposeAsync().ConfigureAwait(false);
                throw error;
            }
            case TResult tResult:
            {
                return tResult;
            }
            case var other:
            {
                await request.DisposeAsync().ConfigureAwait(false);
                throw new InvalidCastException($"Result was of type '{other.GetType()}', " +
                                               $"and cannot be converted to '{typeof(TResult)}'");
            }
        }
    }

    /// <summary>
    ///     Evaluates the request asynchronously and gets its result, then
    ///     serializes the result, optionally to the given output stream.
    /// </summary>
    public static async Task<ISerializedResult> GetAndSerializeResult(this IRequest request, Stream? customOutputStream = null, CancellationToken cancellationToken = new())
    {
        var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
        result.ThrowIfError();
        return await result.Serialize(customOutputStream, cancellationToken).ConfigureAwait(false);
    }

    public static async IAsyncEnumerable<T> GetResultEntities<T>(this IRequest<T> request, [EnumeratorCancellation] CancellationToken cancellationToken = new()) where T : class
    {
        await using var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
        switch (result)
        {
            case Error error: throw error;
            case Change<T> change:
            {
                foreach (var entity in change.Entities)
                    yield return entity;
                yield break;
            }
            case IEntities<T> entities:
            {
                await foreach (var entity in entities)
                    yield return entity;
                yield break;
            }
            case var other: throw new InvalidOperationException($"Cannot convert result of type '{other.GetType()}' to an enumeration of entities");
        }
    }
}
