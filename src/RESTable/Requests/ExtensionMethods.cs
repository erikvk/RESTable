using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Results;

namespace RESTable.Requests
{
    /// <summary>
    /// Extension methods for IRequest
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
        /// Sets the given method to the request, and returns the request
        /// </summary>
        public static IRequest WithMethod(this IRequest request, Method method)
        {
            request.Method = method;
            return request;
        }

        /// <summary>
        /// Sets the given method to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithMethod<T>(this IRequest<T> request, Method method) where T : class
        {
            request.Method = method;
            return request;
        }

        /// <summary>
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest WithBody(this IRequest request, object? bodyObject)
        {
            if (request.Body.IsClosed)
                request.Body = new Body(request, bodyObject);
            else request.Body.UninitializedBodyObject = bodyObject;
            return request;
        }

        /// <summary>
        /// Sets the given body to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithBody<T>(this IRequest<T> request, object? bodyObject) where T : class
        {
            if (request.Body.IsClosed)
                request.Body = new Body(request, bodyObject);
            else request.Body.UninitializedBodyObject = bodyObject;
            return request;
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithConditions<T>(this IRequest<T> request, IEnumerable<Condition<T>> conditions) where T : class
        {
            request.Conditions.AddRange(conditions);
            return request;
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithConditions<T>(this IRequest<T> request, params Condition<T>[] conditionsArray) where T : class
        {
            return WithConditions(request, conditions: conditionsArray);
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithCondition<T>(this IRequest<T> request, string key, Operators op, object value) where T : class
        {
            return WithConditions(request, (key, op, value));
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithCondition<T>(this IRequest<T> request, string key, Operators op, object value, out Condition<T> condition) where T : class
        {
            var termFactory = request.GetRequiredService<TermFactory>();
            var target = request.Target;
            condition = new Condition<T>
            (
                term: termFactory.MakeConditionTerm(target, key),
                op: op,
                value: value
            );
            return WithConditions(request, condition);
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithConditions<T>(this IRequest<T> request, params (string key, Operators op, object value)[] conditions) where T : class
        {
            var termFactory = request.GetRequiredService<TermFactory>();
            var target = request.Target;

            IEnumerable<Condition<T>> Converter()
            {
                foreach (var (key, op, value) in conditions)
                {
                    yield return new Condition<T>
                    (
                        term: termFactory.MakeConditionTerm(target, key),
                        op: op,
                        value: value
                    );
                }
            }

            return WithConditions(request, conditions: Converter());
        }


        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithSelector<T>(this IRequest<T> request, Func<IAsyncEnumerable<T>> selector) where T : class
        {
            request.Selector = selector;
            return request;
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithSelectorEntities<T>(this IRequest<T> request, IEnumerable<T> entities) where T : class
        {
            request.Selector = entities.ToAsyncEnumerable;
            return request;
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithSelectorEntities<T>(this IRequest<T> request, params T[] entities) where T : class
        {
            return request.WithSelectorEntities((IEnumerable<T>) entities);
        }

        /// <summary>
        /// Sets the given selector to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithUpdater<T>(this IRequest<T> request, Func<IAsyncEnumerable<T>, IAsyncEnumerable<T>> updater) where T : class
        {
            request.Updater = updater;
            return request;
        }

        /// <summary>
        /// Sets the given conditions to the request, and returns the request
        /// </summary>
        public static IRequest<T> WithMetaConditions<T>(this IRequest<T> request, Action<MetaConditions> editMetaconditions) where T : class
        {
            editMetaconditions(request.MetaConditions);
            return request;
        }
    }
}