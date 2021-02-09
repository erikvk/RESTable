using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.Resources.Operations
{
    internal static class EntityOperationExtensions
    {
        internal static IEnumerable<T> Validate<T>(this IEnumerable<T> entities, IEntityResource<T> resource) where T : class
        {
            return resource.Validate(entities);
        }
    }

    internal static class EntityOperations<T> where T : class
    {
        private static async Task<IEnumerable<T>> SelectFilter(IRequest<T> request)
        {
            var entities = await request.Target.SelectAsync(request);
            return entities?
                .Where(request.Conditions)
                .Filter(request.MetaConditions.Distinct)
                .Filter(request.MetaConditions.Search)
                .Filter(request.MetaConditions.OrderBy)
                .Filter(request.MetaConditions.Offset)
                .Filter(request.MetaConditions.Limit);
        }

        private static async Task<IEnumerable<object>> SelectProcessFilter(IRequest<T> request)
        {
            var entities = await request.Target.SelectAsync(request);
            return entities?
                .Where(request.Conditions)
                .Process(request.MetaConditions.Processors)
                .Filter(request.MetaConditions.Distinct)
                .Filter(request.MetaConditions.Search)
                .Filter(request.MetaConditions.OrderBy)
                .Filter(request.MetaConditions.Offset)
                .Filter(request.MetaConditions.Limit);
        }

        private static async Task<IEnumerable<T>> TrySelectFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => Task.FromResult<IEnumerable<T>>(new T[0]);
                return await SelectFilter(request);
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

        private static async Task<IEnumerable<object>> TrySelectProcessFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => Task.FromResult<IEnumerable<T>>(new T[0]);
                if (!request.MetaConditions.HasProcessors)
                    return await SelectFilter(request);
                return await SelectProcessFilter(request);
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
                request.EntitiesProducer = () => Task.FromResult<IEnumerable<T>>(new T[0]);
                if (request.EntityResource.CanCount &&
                    request.MetaConditions.CanUseExternalCounter)
                    return (ulong) await request.EntityResource.CountAsync(request);
                var entities = request.MetaConditions.HasProcessors
                    ? await SelectProcessFilter(request)
                    : await SelectFilter(request);
                return (ulong) (entities?.LongCount() ?? 0L);
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

        private static async Task<int> Insert(IEntityRequest<T> request, bool limit = false)
        {
            try
            {
                async Task<IEnumerable<T>> RequestEntitiesProducer()
                {
                    var selector = request.GetSelector() ?? (() => Task.FromResult(request.Body.Deserialize<T>()));
                    var entities = await selector();
                    return entities?.InputLimit(limit)?.Validate(request.EntityResource) ?? throw new MissingDataSource(request);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return await request.EntityResource.InsertAsync(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e);
            }
        }

        private static async Task<int> Update(IEntityRequest<T> request)
        {
            try
            {
                async Task<IEnumerable<T>> RequestEntitiesProducer()
                {
                    async Task<IEnumerable<T>> DefaultSelector() => await TrySelectFilter(request) ?? new T[0];
                    Task<IEnumerable<T>> DefaultUpdater(IEnumerable<T> inputEntities) => Task.FromResult(request.Body.PopulateTo(inputEntities));

                    var selector = request.GetSelector() ?? DefaultSelector;
                    var updater = request.GetUpdater() ?? DefaultUpdater;
                    var entities = (await selector())?.UnsafeLimit(!request.MetaConditions.Unsafe);
                    return (await updater(entities))?.Validate(request.EntityResource) ?? throw new MissingDataSource(request);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return await request.EntityResource.UpdateAsync(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        private static async Task<int> SafePostUpdate(IEntityRequest<T> request, ICollection<(JObject json, T source)> items)
        {
            try
            {
                Task<IEnumerable<T>> RequestEntitiesProducer()
                {
                    var updatedEntities = items.Select(item =>
                    {
                        var (json, source) = item;
                        if (json == null || source == null) return source;
                        using var sr = json.CreateReader();
                        JsonProvider.Serializer.Populate(sr, source);
                        return source;
                    }).Validate(request.EntityResource);
                    return Task.FromResult(updatedEntities);
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return await request.EntityResource.UpdateAsync(request);
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
                async Task<IEnumerable<T>> RequestEntitiesProducer()
                {
                    async Task<IEnumerable<T>> DefaultSelector() => await TrySelectFilter(request) ?? new T[0];
                    var selector = request.GetSelector() ?? DefaultSelector;
                    var entities = await selector();
                    return entities?.UnsafeLimit(!request.MetaConditions.Unsafe) ?? new T[0];
                }

                request.EntitiesProducer = RequestEntitiesProducer;
                return await request.EntityResource.DeleteAsync(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedDelete, e);
            }
        }

        #region Operation resolvers

        internal static Func<IEntityRequest<T>, Task<RequestSuccess>> GetMethodEvaluator(Method method)
        {
            async Task<RequestSuccess> GetEvaluator(IEntityRequest<T> request)
            {
                var entities = await TrySelectProcessFilter(request);
                if (entities == null) return MakeEntities(request, default(IEnumerable<T>));
                return MakeEntities(request, (dynamic) entities);
            }

            async Task<RequestSuccess> PostEvaluator(IEntityRequest<T> request)
            {
                if (request.MetaConditions.SafePost != null) return await SafePost(request);
                return new InsertedEntities(request, await Insert(request));
            }

            async Task<RequestSuccess> PutEvaluator(IEntityRequest<T> request)
            {
                var source = (await TrySelectFilter(request))?.InputLimit()?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0:
                        return new InsertedEntities(request, await Insert(request));
                    case 1 when request.GetUpdater() == null && !request.Body.HasContent:
                        return new UpdatedEntities(request, 0);
                    default:
                        request.Selector = () => Task.FromResult<IEnumerable<T>>(source);
                        return new UpdatedEntities(request, await Update(request));
                }
            }

            async Task<RequestSuccess> HeadEvaluator(IEntityRequest<T> request)
            {
                var count = await TryCount(request);
                return new Head(request, count);
            }

            async Task<RequestSuccess> PatchEvaluator(IEntityRequest<T> request) => new UpdatedEntities(request, await Update(request));
            async Task<RequestSuccess> DeleteEvaluator(IEntityRequest<T> request) => new DeletedEntities(request, await Delete(request));
            async Task<RequestSuccess> ReportEvaluator(IEntityRequest<T> request) => new Report(request, await TryCount(request));
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
        private static Entities<T1> MakeEntities<T1>(IRequest r, IEnumerable<T1> e) where T1 : class => new(r, e);

        #endregion

        #region SafePost

        private static async Task<RequestSuccess> SafePost(IRequest request)
        {
            try
            {
                await using var innerRequest = (IEntityRequest<T>) request.Context.CreateRequest<T>();
                var (toInsert, toUpdate) = await GetSafePostTasks(request, innerRequest);
                var (updatedCount, insertedCount) = (0, 0);
                if (toUpdate.Any())
                    updatedCount = await SafePostUpdate(innerRequest, toUpdate);
                if (toInsert.Any())
                {
                    Task<IEnumerable<T>> InnerRequestSelector()
                    {
                        var entities = toInsert.Select(item => item.ToObject<T>(JsonProvider.Serializer));
                        return Task.FromResult(entities);
                    }

                    innerRequest.Selector = InnerRequestSelector;
                    insertedCount = await Insert(innerRequest);
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
            try
            {
                var body = request.Body;
                if (!body.HasContent) return (toInsert, toUpdate);
                var conditions = request.MetaConditions.SafePost
                    .Split(',')
                    .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                    .ToList();
                foreach (var entity in body.Deserialize<JObject>())
                {
                    conditions.ForEach(cond => cond.Value = entity.SafeSelect(cond.Term.Evaluate));
                    innerRequest.Conditions = conditions;
                    var result = await innerRequest.Evaluate();
                    var entities = result.ToEntities<T>().ToList();
                    switch (entities.Count)
                    {
                        case 0:
                            toInsert.Add(entity);
                            break;
                        case 1:
                            toUpdate.Add((entity, entities[0]));
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