using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;
using RESTable.Meta;

namespace RESTable.Resources.Operations
{
    internal static class EntityOperations<T> where T : class
    {
        private static IAsyncEnumerable<T> SelectFilter(IEntityRequest<T> request)
        {
            IAsyncEnumerable<T> DefaultSelector() => request.Target.SelectAsync(request) ?? AsyncEnumerable.Empty<T>();

            var selector = request.GetSelector() ?? DefaultSelector;
            return selector()
                .Where(request.Conditions)
                .Filter(request.MetaConditions.Distinct)
                .Filter(request.MetaConditions.Search)
                .Filter(request.MetaConditions.OrderBy)
                .Filter(request.MetaConditions.Offset)
                .Filter(request.MetaConditions.Limit);
        }

        private static IAsyncEnumerable<object> SelectProcessFilter(IEntityRequest<T> request)
        {
            IAsyncEnumerable<T> DefaultSelector() => request.Target.SelectAsync(request) ?? AsyncEnumerable.Empty<T>();

            var selector = request.GetSelector() ?? DefaultSelector;
            return selector()
                .Where(request.Conditions)
                .Process(request.MetaConditions.Processors)
                .Filter(request.MetaConditions.Distinct)
                .Filter(request.MetaConditions.Search)
                .Filter(request.MetaConditions.OrderBy)
                .Filter(request.MetaConditions.Offset)
                .Filter(request.MetaConditions.Limit);
        }

        private static IAsyncEnumerable<T> TrySelectFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = AsyncEnumerable.Empty<T>;
                return SelectFilter(request);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedSelect, e);
            }
            finally
            {
                request.EntitiesProducer = producer;
            }
        }

        private static IAsyncEnumerable<object> TrySelectProcessFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = AsyncEnumerable.Empty<T>;
                if (!request.MetaConditions.HasProcessors)
                    return SelectFilter(request);
                return SelectProcessFilter(request);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedSelect, e);
            }
            finally
            {
                request.EntitiesProducer = producer;
            }
        }

        private static async Task<ulong> TryCount(IEntityRequest<T> request)
        {
            try
            {
                request.EntitiesProducer = AsyncEnumerable.Empty<T>;
                if (request.EntityResource.CanCount &&
                    request.MetaConditions.CanUseExternalCounter)
                    return (ulong) await request.EntityResource.CountAsync(request).ConfigureAwait(false);
                var entities = request.MetaConditions.HasProcessors
                    ? SelectProcessFilter(request)
                    : SelectFilter(request);
                if (entities == null)
                    return 0UL;
                return (ulong) await entities.LongCountAsync().ConfigureAwait(false);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedReport, e);
            }
        }

        private static IAsyncEnumerable<T> Insert(IEntityRequest<T> request, bool limit = false)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer()
                {
                    var selector = request.GetSelector() ?? (() => request.Body.Deserialize<T>());
                    var entities = selector();
                    return entities?.InputLimit(limit)?.Validate(request.EntityResource, request.Context) ?? throw new MissingDataSource(request);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return request.EntityResource.InsertAsync(request);
            }
            catch (InvalidInputEntity)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e);
            }
        }

        private static IAsyncEnumerable<T> Update(IEntityRequest<T> request)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer()
                {
                    IAsyncEnumerable<T> DefaultUpdater(IAsyncEnumerable<T> inputEntities) => request.Body.PopulateTo(inputEntities);

                    var updater = request.GetUpdater() ?? DefaultUpdater;
                    var entities = TrySelectFilter(request).UnsafeLimit(!request.MetaConditions.Unsafe);
                    return updater(entities)?.Validate(request.EntityResource, request.Context) ?? throw new MissingDataSource(request);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return request.EntityResource.UpdateAsync(request);
            }
            catch (InvalidInputEntity)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        private static IAsyncEnumerable<T> SafePostUpdate(IEntityRequest<T> request, IJsonProvider jsonProvider, ICollection<(JObject json, T source)> items)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer() => items
                    .Select(item =>
                    {
                        var (json, source) = item;
                        if (json == null || source == null) return source;
                        using var sr = json.CreateReader();
                        jsonProvider.GetSerializer().Populate(sr, source);
                        return source;
                    })
                    .ToAsyncEnumerable()
                    .Validate(request.EntityResource, request.Context);

                request.EntitiesProducer = RequestEntitiesProducer;
                return request.EntityResource.UpdateAsync(request);
            }
            catch (InvalidInputEntity)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        private static async Task<int> Delete(IEntityRequest<T> request)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer()
                {
                    return TrySelectFilter(request)?.UnsafeLimit(!request.MetaConditions.Unsafe) ?? AsyncEnumerable.Empty<T>();
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return await request.EntityResource.DeleteAsync(request).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedDelete, e);
            }
        }

        #region Operation resolvers

        internal static Func<IEntityRequest<T>, Task<RequestSuccess>> GetMethodEvaluator(Method method)
        {
            Task<RequestSuccess> GetEvaluator(IEntityRequest<T> request)
            {
                var entities = TrySelectProcessFilter(request);
                RequestSuccess result;
                if (entities == null)
                    result = MakeEntities(request, default(IAsyncEnumerable<T>));
                else result = MakeEntities(request, (dynamic) entities);
                return Task.FromResult(result);
            }

            async Task<RequestSuccess> PostEvaluator(IEntityRequest<T> request)
            {
                if (request.MetaConditions.SafePost != null)
                    return await SafePost(request).ConfigureAwait(false);
                return new InsertedEntities(request, await Insert(request).ConfigureAwait(false));
            }

            async Task<RequestSuccess> PutEvaluator(IEntityRequest<T> request)
            {
                var task = TrySelectFilter(request)?.InputLimit()?.ToListAsync();
                var source = !task.HasValue ? null : await task.Value.ConfigureAwait(false);
                switch (source?.Count)
                {
                    case null:
                    case 0:
                        return new InsertedEntities(request, await Insert(request).ConfigureAwait(false));
                    case 1 when request.GetUpdater() == null && !request.Body.CanRead:
                        return new UpdatedEntities(request, 0);
                    default:
                        request.Selector = () => source.ToAsyncEnumerable();
                        return new UpdatedEntities(request, await Update(request).ConfigureAwait(false));
                }
            }

            async Task<RequestSuccess> HeadEvaluator(IEntityRequest<T> request)
            {
                var count = await TryCount(request).ConfigureAwait(false);
                return new Head(request, count);
            }

            async Task<RequestSuccess> PatchEvaluator(IEntityRequest<T> request) => new UpdatedEntities(request, await Update(request).ConfigureAwait(false));
            async Task<RequestSuccess> DeleteEvaluator(IEntityRequest<T> request) => new DeletedEntities(request, await Delete(request).ConfigureAwait(false));
            async Task<RequestSuccess> ReportEvaluator(IEntityRequest<T> request) => new Report(request, await TryCount(request).ConfigureAwait(false));
            Task<RequestSuccess> ImATeapotEvaluator(IEntityRequest<T> request) => Task.FromResult<RequestSuccess>(new ImATeapot(request));

            return method switch
            {
                Method.GET => GetEvaluator,
                Method.POST => PostEvaluator,
                Method.PUT => PutEvaluator,
                Method.HEAD => HeadEvaluator,
                Method.PATCH => PatchEvaluator,
                Method.DELETE => DeleteEvaluator,
                Method.REPORT => ReportEvaluator,
                _ => ImATeapotEvaluator
            };
        }

        /// <summary>
        /// Needed since some <see cref="Entities{T}"/> instances are created using dynamic binding, which requires
        /// a separate static method in a non-generic class.
        /// </summary>
        private static Entities<T1> MakeEntities<T1>(IRequest r, IAsyncEnumerable<T1> e) where T1 : class => new(r, e);

        #endregion

        #region SafePost

        private static async Task<RequestSuccess> SafePost(IRequest request)
        {
            try
            {
                var jsonProvider = request.GetRequiredService<IJsonProvider>();
                var innerRequest = (IEntityRequest<T>) request.Context.CreateRequest<T>();
                var (toInsert, toUpdate) = await GetSafePostTasks(request, innerRequest).ConfigureAwait(false);
                var (updatedCount, insertedCount) = (0, 0);
                if (toUpdate.Any())
                    updatedCount = await SafePostUpdate(innerRequest, jsonProvider, toUpdate).ConfigureAwait(false);
                if (toInsert.Any())
                {
                    innerRequest.Selector = () => toInsert
                        .Select(item => item.ToObject<T>(jsonProvider.GetSerializer()))
                        .ToAsyncEnumerable();
                    insertedCount = await Insert(innerRequest).ConfigureAwait(false);
                }
                return new SafePostedEntities(request, updatedCount, insertedCount);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e, e.Message);
            }
        }

        private static async Task<(List<JObject> ToInsert, IList<(JObject json, T source)> ToUpdate)> GetSafePostTasks
        (
            IRequest request,
            IRequest<T> innerRequest
        )
        {
            var toInsert = new List<JObject>();
            var toUpdate = new List<(JObject json, T source)>();
            var termFactory = request.GetRequiredService<TermFactory>();
            try
            {
                var body = request.Body;
                var conditions = request.MetaConditions.SafePost
                    .Split(',')
                    .Select(key =>
                    {
                        var term = termFactory.MakeConditionTerm(request.Target, key);
                        return new Condition<T>(term, Operators.EQUALS, null);
                    })
                    .ToList();
                await foreach (var entity in body.Deserialize<JObject>().ConfigureAwait(false))
                {
                    foreach (var cond in conditions)
                        cond.Value = entity.SafeSelect(cond.Term.GetValue);
                    innerRequest.Conditions = conditions;
                    var result = await innerRequest.GetResultEntities().ConfigureAwait(false);
                    var resultList = await result.ToListAsync().ConfigureAwait(false);
                    switch (resultList.Count)
                    {
                        case 0:
                            toInsert.Add(entity);
                            break;
                        case 1:
                            toUpdate.Add((entity, resultList[0]));
                            break;
                        case var multiple:
                            throw new SafePostAmbiguousMatch
                            (
                                count: multiple,
                                uri: innerRequest.UriComponents.ToUriString()
                            );
                    }
                }
                return (toInsert, toUpdate);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e, e.Message);
            }
        }

        #endregion
    }
}