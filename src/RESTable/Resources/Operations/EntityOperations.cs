using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;
using RESTable.Meta;

namespace RESTable.Resources.Operations
{
    internal class EntityOperations<T> where T : class
    {
        private static readonly IAsyncEnumerable<T> EmptyEnumeration = AsyncEnumerable.Empty<T>();

        private IOptionsMonitor<RESTableConfiguration> Configuration { get; }

        public EntityOperations(IOptionsMonitor<RESTableConfiguration> configuration)
        {
            Configuration = configuration;
        }

        private static IAsyncEnumerable<T> SelectFilter(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            IAsyncEnumerable<T> DefaultSelector() => request.Target.SelectAsync(request, cancellationToken);

            var selector = request.GetCustomSelector() ?? DefaultSelector;
            return selector()
                .Where(request.Conditions)
                .Filter(request.MetaConditions.Distinct)
                .Filter(request.MetaConditions.Search)
                .Filter(request.MetaConditions.OrderBy)
                .Filter(request.MetaConditions.Offset)
                .Filter(request.MetaConditions.Limit);
        }

        private static IAsyncEnumerable<object> SelectProcessFilter(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            IAsyncEnumerable<T> DefaultSelector() => request.Target.SelectAsync(request, cancellationToken);

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

        private static IAsyncEnumerable<T> TrySelectFilter(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => EmptyEnumeration;
                return SelectFilter(request, cancellationToken);
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

        private static async Task<long> TryCount(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            try
            {
                request.EntitiesProducer = () => EmptyEnumeration;
                if (request.EntityResource.CanCount &&
                    request.MetaConditions.CanUseExternalCounter)
                    return await request.EntityResource.CountAsync(request, cancellationToken).ConfigureAwait(false);
                var entities = request.MetaConditions.HasProcessors
                    ? SelectProcessFilter(request, cancellationToken)
                    : SelectFilter(request, cancellationToken);
                return await entities.LongCountAsync(cancellationToken).ConfigureAwait(false);
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

        private static IAsyncEnumerable<T> Insert(IEntityRequest<T> request, CancellationToken cancellationToken, bool limit = false)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer()
                {
                    var selector = request.GetCustomSelector() ?? (() => request.Body.DeserializeAsyncEnumerable<T>(cancellationToken));
                    var entities = selector();
                    return entities.InputLimit(limit).Validate(request.EntityResource, request.Context, cancellationToken);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return request.EntityResource.InsertAsync(request, cancellationToken);
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

        private static IAsyncEnumerable<T> Update(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer()
                {
                    IAsyncEnumerable<T> DefaultUpdater(IAsyncEnumerable<T> inputEntities) => request.Body.PopulateTo(inputEntities, cancellationToken);

                    var updater = request.GetCustomUpdater() ?? DefaultUpdater;
                    var entities = TrySelectFilter(request, cancellationToken).UnsafeLimit(!request.MetaConditions.Unsafe);
                    return updater(entities).Validate(request.EntityResource, request.Context, cancellationToken);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return request.EntityResource.UpdateAsync(request, cancellationToken);
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

        private static IAsyncEnumerable<T> SafePostUpdate(IEntityRequest<T> request, ICollection<(T source, JsonElement json)> items, CancellationToken cancellationToken)
        {
            try
            {
                var jsonProvider = request.GetRequiredService<IJsonProvider>();


                IAsyncEnumerable<T> RequestEntitiesProducer() => items
                    .ToAsyncEnumerable()
                    .SelectAwait(async item =>
                    {
                        var (source, json) = item;
                        var populator = jsonProvider.GetPopulator<T>(json);
                        await populator(source).ConfigureAwait(false);
                        return item.source;
                    })
                    .Validate(request.EntityResource, request.Context, cancellationToken);

                request.EntitiesProducer = RequestEntitiesProducer;
                return request.EntityResource.UpdateAsync(request, cancellationToken);
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

        private static async ValueTask<long> Delete(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            try
            {
                IAsyncEnumerable<T> RequestEntitiesProducer()
                {
                    return TrySelectFilter(request, cancellationToken).UnsafeLimit(!request.MetaConditions.Unsafe);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return await request.EntityResource.DeleteAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedDelete, e);
            }
        }

        #region Operation resolvers

        private static Entities<T1> MakeEntitiesDynamic<T1>(IRequest r, IAsyncEnumerable<T1> e) where T1 : class => new(r, e);
        private static InsertedEntities<T1> MakeInsertedEntitiesDynamic<T1>(IRequest r, int c, IReadOnlyCollection<T1> e) where T1 : class => new(r, c, e);
        private static UpdatedEntities<T1> MakeUpdatedEntitiesDynamic<T1>(IRequest r, int c, IReadOnlyCollection<T1> e) where T1 : class => new(r, c, e);
        private static SafePostedEntities<T1> MakeSafePostedEntitiesDynamic<T1>(IRequest r, int cu, int ci, IReadOnlyCollection<T1> e) where T1 : class => new(r, cu, ci, e);

        private static async Task<IReadOnlyCollection<object>> ProcessChangedEntities(IRequest request, IEnumerable<T> entities, CancellationToken cancellationToken)
        {
            var list = await entities
                .ToAsyncEnumerable()
                .Process(request.MetaConditions.Processors)
                .Filter(request.MetaConditions.OrderBy)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return list.AsReadOnly();
        }

        internal Func<IEntityRequest<T>, CancellationToken, Task<RequestSuccess>> GetMethodEvaluator(Method method)
        {
            Task<RequestSuccess> GetEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken)
            {
                var producer = request.EntitiesProducer;
                try
                {
                    request.EntitiesProducer = () => EmptyEnumeration;
                    if (request.MetaConditions.HasProcessors)
                    {
                        var entities = SelectProcessFilter(request, cancellationToken);
                        RequestSuccess result = MakeEntitiesDynamic(request, (dynamic) entities);
                        return Task.FromResult(result);
                    }
                    else
                    {
                        var entities = SelectFilter(request, cancellationToken);
                        RequestSuccess result = new Entities<T>(request, entities);
                        return Task.FromResult(result);
                    }
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

            async Task<RequestSuccess> PostEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken)
            {
                if (request.MetaConditions.SafePost is not null)
                    return await SafePost(request, cancellationToken).ConfigureAwait(false);
                var insertedEntities = Insert(request, cancellationToken);
                var (count, entities) = await ChangeCount(insertedEntities, Configuration.CurrentValue.MaxNumberOfEntitiesInChangeResults, cancellationToken).ConfigureAwait(false);
                if (request.MetaConditions.HasProcessors)
                {
                    var processedEntities = await ProcessChangedEntities(request, entities, cancellationToken).ConfigureAwait(false);
                    RequestSuccess requestSuccess = MakeInsertedEntitiesDynamic(request, count, (dynamic) processedEntities);
                    return requestSuccess;
                }
                return new InsertedEntities<T>(request, count, entities);
            }

            async Task<RequestSuccess> PutEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken)
            {
                var source = await TrySelectFilter(request, cancellationToken)
                    .InputLimit()
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                switch (source.Count)
                {
                    case 0:
                    {
                        var insertedEntities = Insert(request, cancellationToken);
                        var (count, entities) = await ChangeCount(insertedEntities, Configuration.CurrentValue.MaxNumberOfEntitiesInChangeResults, cancellationToken)
                            .ConfigureAwait(false);
                        if (request.MetaConditions.HasProcessors)
                        {
                            var processedEntities = await ProcessChangedEntities(request, entities, cancellationToken).ConfigureAwait(false);
                            RequestSuccess requestSuccess = MakeInsertedEntitiesDynamic(request, count, (dynamic) processedEntities);
                            return requestSuccess;
                        }
                        return new InsertedEntities<T>(request, count, entities);
                    }
                    case 1 when request.GetCustomUpdater() is null && !request.Body.CanRead:
                    {
                        return new UpdatedEntities<T>(request, 0, Array.Empty<T>());
                    }
                    default:
                    {
                        request.Selector = () => source.ToAsyncEnumerable();
                        var updatedEntities = Update(request, cancellationToken);
                        var (count, entities) = await ChangeCount(updatedEntities, Configuration.CurrentValue.MaxNumberOfEntitiesInChangeResults, cancellationToken)
                            .ConfigureAwait(false);
                        if (request.MetaConditions.HasProcessors)
                        {
                            var processedEntities = await ProcessChangedEntities(request, entities, cancellationToken).ConfigureAwait(false);
                            RequestSuccess requestSuccess = MakeUpdatedEntitiesDynamic(request, count, (dynamic) processedEntities);
                            return requestSuccess;
                        }
                        return new UpdatedEntities<T>(request, count, entities);
                    }
                }
            }

            async Task<RequestSuccess> HeadEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken)
            {
                var count = await TryCount(request, cancellationToken).ConfigureAwait(false);
                return new Head(request, count);
            }

            async Task<RequestSuccess> PatchEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken)
            {
                var updatedEntities = Update(request, cancellationToken);
                var (count, entities) = await ChangeCount(updatedEntities, Configuration.CurrentValue.MaxNumberOfEntitiesInChangeResults, cancellationToken).ConfigureAwait(false);
                if (request.MetaConditions.HasProcessors)
                {
                    var processedEntities = await ProcessChangedEntities(request, entities, cancellationToken).ConfigureAwait(false);
                    RequestSuccess requestSuccess = MakeUpdatedEntitiesDynamic(request, count, (dynamic) processedEntities);
                    return requestSuccess;
                }
                return new UpdatedEntities<T>(request, count, entities);
            }

            async Task<RequestSuccess> DeleteEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken) =>
                new DeletedEntities<T>(request, await Delete(request, cancellationToken).ConfigureAwait(false));

            async Task<RequestSuccess> ReportEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken) =>
                new Report(request, await TryCount(request, cancellationToken).ConfigureAwait(false));

            Task<RequestSuccess> ImATeapotEvaluator(IEntityRequest<T> request, CancellationToken cancellationToken) => Task.FromResult<RequestSuccess>(new ImATeapot(request));

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


        private static async Task<(int count, T[] changedEntities)> ChangeCount
        (
            IAsyncEnumerable<T> changedEntities,
            int maxNumberOfChangedEntities,
            CancellationToken cancellationToken
        )
        {
            var entityList = new List<T>();
            var count = 0;
            await foreach (var item in changedEntities.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (count < maxNumberOfChangedEntities)
                    entityList.Add(item);
                count += 1;
            }
            var changedEntitiesArray = count <= maxNumberOfChangedEntities ? entityList.ToArray() : Array.Empty<T>();
            return (count, changedEntitiesArray);
        }

        private static async Task<(int updatedCount, int insertedCount, T[] changedEntities)> SafePostCount
        (
            IAsyncEnumerable<T> updatedEntities,
            IAsyncEnumerable<T> insertedEntities,
            int maxNumberOfChangedEntities,
            CancellationToken cancellationToken
        )
        {
            var buffer = new List<T>();
            var (updatedCount, insertedCount, totalCount) = (0, 0, 0);
            await foreach (var item in updatedEntities.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (totalCount < maxNumberOfChangedEntities)
                    buffer.Add(item);
                updatedCount += 1;
                totalCount += 1;
            }
            await foreach (var item in insertedEntities.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (totalCount < maxNumberOfChangedEntities)
                    buffer.Add(item);
                insertedCount += 1;
                totalCount += 1;
            }
            var changedEntitiesArray = totalCount <= maxNumberOfChangedEntities ? buffer.ToArray() : Array.Empty<T>();
            return (updatedCount, insertedCount, changedEntitiesArray);
        }

        #endregion

        #region SafePost

        private async Task<RequestSuccess> SafePost(IEntityRequest<T> request, CancellationToken cancellationToken)
        {
            try
            {
                var jsonProvider = request.GetRequiredService<IJsonProvider>();
                var maxNumberOfItemsInSafePostResults = Configuration.CurrentValue.MaxNumberOfEntitiesInChangeResults / 2;
                var innerRequest = (IEntityRequest<T>) request.Context.CreateRequest<T>();
                var (toInsert, toUpdate) = await GetSafePostTasks(request, innerRequest).ConfigureAwait(false);
                var (insertedEntities, updatedEntities) = (EmptyEnumeration, EmptyEnumeration);
                if (toUpdate.Any())
                {
                    updatedEntities = SafePostUpdate(innerRequest, toUpdate, cancellationToken);
                }
                if (toInsert.Any())
                {
                    innerRequest.Selector = () => toInsert
                        .Select(item => jsonProvider.ToObject<T>(item)!)
                        .ToAsyncEnumerable();
                    insertedEntities = Insert(innerRequest, cancellationToken);
                }
                var (updatedCount, insertedCount, changedEntities) = await SafePostCount
                (
                    updatedEntities: updatedEntities,
                    insertedEntities: insertedEntities,
                    maxNumberOfChangedEntities: maxNumberOfItemsInSafePostResults,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (request.MetaConditions.HasProcessors)
                {
                    var processedEntities = await ProcessChangedEntities(request, changedEntities, cancellationToken).ConfigureAwait(false);
                    RequestSuccess requestSuccess = MakeSafePostedEntitiesDynamic(request, updatedCount, insertedCount, (dynamic) processedEntities);
                    return requestSuccess;
                }
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
                await foreach (var jsonElement in body.DeserializeAsyncEnumerable<JsonElement>().ConfigureAwait(false))
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