using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.ContentTypeProviders;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Results;

namespace RESTar.Resources.Operations
{
    internal static class EntityOperations<T> where T : class
    {
        #region Select

        internal static IEnumerable<T> SelectFilter(IRequest<T> request) => request.Target
            .Select(request)?
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        internal static IEnumerable<object> SelectFilterProcess(IRequest<T> request) => request.Target
            .Select(request)?
            .Process(request.MetaConditions.Processors)
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        private static IEnumerable<T> TrySelectFilter(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => new T[0];
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

        private static IEnumerable<object> TrySelectFilterProcess(IEntityRequest<T> request)
        {
            var producer = request.EntitiesProducer;
            try
            {
                request.EntitiesProducer = () => new T[0];
                if (!request.MetaConditions.HasProcessors)
                    return SelectFilter(request);
                return SelectFilterProcess(request);
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

        private static long TryCount(IEntityRequest<T> request)
        {
            try
            {
                request.EntitiesProducer = () => new T[0];
                if (request.EntityResource.CanCount &&
                    request.MetaConditions.CanUseExternalCounter)
                    return request.EntityResource.Count(request);
                if (!request.MetaConditions.HasProcessors)
                    return SelectFilter(request)?.Count() ?? 0L;
                return SelectFilterProcess(request)?.Count() ?? 0L;
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

        #endregion

        private static int Insert(IEntityRequest<T> request, bool limit = false)
        {
            try
            {
                var inserter = request.GetSelector() ?? (() => request.GetBody().Deserialize<T>());
                if (limit)
                {
                    var _inserter = inserter;
                    inserter = () => _inserter().InputLimit();
                }
                request.EntitiesProducer = () => inserter()?.Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                }) ?? throw new MissingDataSource(request);
                return request.EntityResource.Insert(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e);
            }
        }

        private static int Update(IEntityRequest<T> request)
        {
            try
            {
                var sourceSelector = request.GetSelector() ?? (() => TrySelectFilter(request) ?? new List<T>());
                if (!request.MetaConditions.Unsafe)
                {
                    var selector = sourceSelector;
                    sourceSelector = () => selector()?.UnsafeLimit();
                }
                var updater = request.GetUpdater() ?? (_source => request.GetBody().PopulateTo(_source));
                request.EntitiesProducer = () => updater(sourceSelector())?.Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                }) ?? throw new MissingDataSource(request);
                return request.EntityResource.Update(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        private static int Delete(IEntityRequest<T> request)
        {
            try
            {
                var sourceSelector = request.GetSelector() ?? (() => TrySelectFilter(request) ?? new List<T>());
                if (!request.MetaConditions.Unsafe)
                {
                    var selector = sourceSelector;
                    sourceSelector = () => selector()?.UnsafeLimit();
                }
                request.EntitiesProducer = () => sourceSelector() ?? new T[0];
                return request.EntityResource.Delete(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedDelete, e);
            }
        }

        private static Entities<TEntityType> MakeEntities<TEntityType>(IRequest request, IEnumerable<TEntityType> content) where TEntityType : class
        {
            return new Entities<TEntityType>(request, content);
        }

        internal static Func<IEntityRequest<T>, RequestSuccess> GetEvaluator(Method method)
        {
            switch (method)
            {
                case Method.GET:
                    return request =>
                    {
                        var entities = TrySelectFilterProcess(request);
                        if (entities == null)
                            return MakeEntities(request, default(IEnumerable<T>));
                        return MakeEntities(request, (dynamic) entities);
                    };
                case Method.POST:
                    return request => request.MetaConditions.SafePost == null
                        ? new InsertedEntities(request, Insert(request))
                        : SafePOST(request);
                case Method.PUT:
                    return request =>
                    {
                        var source = TrySelectFilter(request)?.InputLimit()?.ToList();
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new InsertedEntities(request, Insert(request));
                            case 1 when request.GetUpdater() == null && !request.GetBody().HasContent:
                                return new UpdatedEntities(request, 0);
                            default:
                                request.Selector = () => source;
                                return new UpdatedEntities(request, Update(request));
                        }
                    };
                case Method.HEAD:
                    return request =>
                    {
                        var count = TryCount(request);
                        if (count > 0) return new Head(request, count);
                        return new NoContent(request);
                    };
                case Method.PATCH: return request => new UpdatedEntities(request, Update(request));
                case Method.DELETE: return request => new DeletedEntities(request, Delete(request));
                case Method.REPORT: return request => new Report(request, TryCount(request));
                default: return request => new ImATeapot(request);
            }
        }

        #region SafePost

        private static RequestSuccess SafePOST(IEntityRequest<T> request)
        {
            var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(request);
            var (updatedCount, insertedCount) = (0, 0);
            if (toUpdate.Any())
                updatedCount = UpdateSafePost(innerRequest, toUpdate);
            if (toInsert.Any())
            {
                innerRequest.Selector = () => toInsert.Select(item => item.ToObject<T>());
                insertedCount = Insert(innerRequest);
            }
            return new SafePostedEntities(request, updatedCount, insertedCount);
        }

        private static (IEntityRequest<T> InnerRequest, JArray ToInsert, IList<(JObject json, T source)> ToUpdate) GetSafePostTasks(
            IEntityRequest<T> request)
        {
            var innerRequest = (IEntityRequest<T>) request.Context.CreateRequest<T>();
            var toInsert = new JArray();
            var toUpdate = new List<(JObject json, T source)>();
            try
            {
                var conditions = request.MetaConditions.SafePost
                    .Split(',')
                    .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                    .ToList();
                foreach (var entity in request.GetBody().Deserialize<JObject>())
                {
                    conditions.ForEach(cond => cond.Value = entity.SafeGet(cond.Term.Evaluate));
                    innerRequest.Conditions = conditions;
                    var results = innerRequest.Evaluate().ToEntities<T>().ToList();
                    switch (results.Count)
                    {
                        case 0:
                            toInsert.Add(entity);
                            break;
                        case 1:
                            toUpdate.Add((entity, results[0]));
                            break;
                        case var multiple:
                            throw new SafePostAmbiguousMatch(multiple,
                                request.CachedProtocolProvider.ProtocolProvider.MakeRelativeUri(innerRequest.UriComponents));
                    }
                }
                return (innerRequest, toInsert, toUpdate);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedInsert, e, e.Message);
            }
        }

        private static int UpdateSafePost(IEntityRequest<T> request, ICollection<(JObject json, T source)> items)
        {
            try
            {
                request.EntitiesProducer = () => items.Select(item =>
                {
                    if (item.json != null && item.source != null)
                        using (var sr = item.json.CreateReader())
                            JsonProvider.Serializer.Populate(sr, item.source);
                    (item.source as IValidatable)?.Validate();
                    return item.source;
                });
                return request.EntityResource.Update(request);
            }
            catch (Exception e)
            {
                throw new AbortedOperation(request, ErrorCodes.AbortedUpdate, e);
            }
        }

        #endregion
    }
}