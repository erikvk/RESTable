using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;
using RESTable.Meta;

namespace RESTable.Resources.Operations
{
    internal static class EntityOperations<T> where T : class
    {
        private static readonly IAsyncEnumerable<T> EmptyEnumeration;

        static EntityOperations()
        {
            EmptyEnumeration = AsyncEnumerable.Empty<T>();
        }

        private static IAsyncEnumerable<T> SelectFilter(IEntityRequest<T> request)
        {
            IAsyncEnumerable<T> DefaultSelector() => request.Target.SelectAsync(request);

            var selector = request.GetCustomSelector() ?? DefaultSelector;
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
            IAsyncEnumerable<T> DefaultSelector() => request.Target.SelectAsync(request);

            var selector = request.GetCustomSelector() ?? DefaultSelector;
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
                request.EntitiesProducer = () => EmptyEnumeration;
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
                request.EntitiesProducer = () => EmptyEnumeration;
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

        private static async Task<long> TryCount(IEntityRequest<T> request)
        {
            try
            {
                request.EntitiesProducer = () => EmptyEnumeration;
                if (request.EntityResource.CanCount &&
                    request.MetaConditions.CanUseExternalCounter)
                    return await request.EntityResource.CountAsync(request).ConfigureAwait(false);
                var entities = request.MetaConditions.HasProcessors
                    ? SelectProcessFilter(request)
                    : SelectFilter(request);
                return await entities.LongCountAsync().ConfigureAwait(false);
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
                    var selector = request.GetCustomSelector() ?? (() => request.Body.Deserialize<T>());
                    var entities = selector();
                    return entities.InputLimit(limit).Validate(request.EntityResource, request.Context);
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

                    var updater = request.GetCustomUpdater() ?? DefaultUpdater;
                    var entities = TrySelectFilter(request).UnsafeLimit(!request.MetaConditions.Unsafe);
                    return updater(entities).Validate(request.EntityResource, request.Context);
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

        private static IAsyncEnumerable<T> SafePostUpdate(IEntityRequest<T> request, ICollection<(T source, JsonElement json)> items)
        {
            try
            {
                var jsonProvider = request.GetRequiredService<IJsonProvider>();

                IAsyncEnumerable<T> RequestEntitiesProducer() => items
                    .Select(item =>
                    {
                        var (source, json) = item;
                        jsonProvider.Populate(source, json);
                        return item.source;
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
                    return TrySelectFilter(request).UnsafeLimit(!request.MetaConditions.Unsafe);
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
                RequestSuccess result = MakeEntities(request, (dynamic) entities);
                return Task.FromResult(result);
            }

            async Task<RequestSuccess> PostEvaluator(IEntityRequest<T> request)
            {
                if (request.MetaConditions.SafePost is not null)
                    return await SafePost(request).ConfigureAwait(false);
                var insertedEntities = Insert(request);
                var (count, entities) = await ChangeCount(insertedEntities).ConfigureAwait(false);
                return new InsertedEntities<T>(request, count, entities);
            }

            async Task<RequestSuccess> PutEvaluator(IEntityRequest<T> request)
            {
                var source = await TrySelectFilter(request)
                    .InputLimit()
                    .ToListAsync()
                    .ConfigureAwait(false);
                switch (source.Count)
                {
                    case 0:
                    {
                        var insertedEntities = Insert(request);
                        var (count, entities) = await ChangeCount(insertedEntities).ConfigureAwait(false);
                        return new InsertedEntities<T>(request, count, entities);
                    }
                    case 1 when request.GetCustomUpdater() is null && !request.Body.CanRead:
                    {
                        return new UpdatedEntities<T>(request, 0, Array.Empty<T>());
                    }
                    default:
                    {
                        request.Selector = () => source.ToAsyncEnumerable();
                        var updatedEntities = Update(request);
                        var (count, entities) = await ChangeCount(updatedEntities).ConfigureAwait(false);
                        return new UpdatedEntities<T>(request, count, entities);
                    }
                }
            }

            async Task<RequestSuccess> HeadEvaluator(IEntityRequest<T> request)
            {
                var count = await TryCount(request).ConfigureAwait(false);
                return new Head(request, count);
            }

            async Task<RequestSuccess> PatchEvaluator(IEntityRequest<T> request)
            {
                var updatedEntities = Update(request);
                var (count, entities) = await ChangeCount(updatedEntities).ConfigureAwait(false);
                return new UpdatedEntities<T>(request, count, entities);
            }

            async Task<RequestSuccess> DeleteEvaluator(IEntityRequest<T> request) => new DeletedEntities<T>(request, await Delete(request).ConfigureAwait(false));
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


        private static async Task<(int count, IReadOnlyCollection<T> changedEntities)> ChangeCount
        (
            IAsyncEnumerable<T> changedEntities,
            int maxNumberOfChangedEntities = Change.MaxNumberOfEntitiesInChangeResults
        )
        {
            var entityList = new List<T>();
            var count = 0;
            await foreach (var item in changedEntities.ConfigureAwait(false))
            {
                if (count < maxNumberOfChangedEntities)
                    entityList.Add(item);
                count += 1;
            }
            IReadOnlyCollection<T> changedEntitiesArray = count <= maxNumberOfChangedEntities ? entityList.AsReadOnly() : Array.Empty<T>();
            return (count, changedEntitiesArray);
        }

        private const int MaxNumberOfEntitiesInSafePostResults = Change.MaxNumberOfEntitiesInChangeResults / 2;

        private static async Task<(int updatedCount, int insertedCount, T[] changedEntities)> SafePostCount
        (
            IAsyncEnumerable<T> updatedEntities,
            IAsyncEnumerable<T> insertedEntities,
            int maxNumberOfChangedEntities = MaxNumberOfEntitiesInSafePostResults
        )
        {
            var buffer = new List<T>();
            var (updatedCount, insertedCount, totalCount) = (0, 0, 0);
            await foreach (var item in updatedEntities.ConfigureAwait(false))
            {
                if (totalCount < maxNumberOfChangedEntities)
                    buffer.Add(item);
                updatedCount += 1;
                totalCount += 1;
            }
            await foreach (var item in insertedEntities.ConfigureAwait(false))
            {
                if (totalCount < maxNumberOfChangedEntities)
                    buffer.Add(item);
                insertedCount += 1;
                totalCount += 1;
            }
            var changedEntitiesArray = totalCount <= maxNumberOfChangedEntities ? buffer.ToArray() : Array.Empty<T>();
            return (updatedCount, insertedCount, changedEntitiesArray);
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
                var (insertedEntities, updatedEntities) = (EmptyEnumeration, EmptyEnumeration);
                if (toUpdate.Any())
                {
                    updatedEntities = SafePostUpdate(innerRequest, toUpdate);
                }
                if (toInsert.Any())
                {
                    innerRequest.Selector = () => toInsert
                        .Select(item => jsonProvider.ToObject<T>(item)!)
                        .ToAsyncEnumerable();
                    insertedEntities = Insert(innerRequest);
                }
                var (updatedCount, insertedCount, changedEntities) = await SafePostCount(updatedEntities, insertedEntities).ConfigureAwait(false);
                return new SafePostedEntities<T>(request, updatedCount, insertedCount, changedEntities);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e, e.Message);
            }
        }

        private static async Task<(List<JsonElement> ToInsert, IList<(T source, JsonElement json)> ToUpdate)> GetSafePostTasks
        (
            IRequest request,
            IRequest<T> innerRequest
        )
        {
            var toInsert = new List<JsonElement>();
            var toUpdate = new List<(T source, JsonElement json)>();
            var termFactory = request.GetRequiredService<TermFactory>();
            try
            {
                var body = request.Body;
                var conditions = request.MetaConditions.SafePost!
                    .Split(',')
                    .Select(key =>
                    {
                        var term = termFactory.MakeConditionTerm(request.Target, key);
                        return new Condition<T>(term, Operators.EQUALS, null);
                    })
                    .ToList();
                await foreach (var jsonElement in body.Deserialize<JsonElement>().ConfigureAwait(false))
                {
                    foreach (var cond in conditions)
                    {
                        try
                        {
                            var termValue = await cond.Term.GetValue(jsonElement).ConfigureAwait(false);
                            cond.Value = termValue.Value;
                        }
                        catch
                        {
                            cond.Value = null;
                        }
                    }
                    innerRequest.Conditions = conditions;
                    var resultEntities = innerRequest.GetResultEntities();
                    var resultList = await resultEntities.ToListAsync().ConfigureAwait(false);
                    switch (resultList.Count)
                    {
                        case 0:
                            toInsert.Add(jsonElement);
                            break;
                        case 1:
                            toUpdate.Add((resultList[0], jsonElement));
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