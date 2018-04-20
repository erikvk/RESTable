using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Results;
using RESTar.Serialization;

namespace RESTar.Operations
{
    internal static class EntityOperations<T> where T : class
    {
        #region Select

        private static IEnumerable<T> SelectFilter(IRequest<T> request) => request.Target
            .Select(request)?
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        private static IEnumerable<object> SelectFilterProcess(IRequest<T> request) => request.Target
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
                if (request.EntityResource.Count is Counter<T> counter &&
                    request.MetaConditions.CanUseExternalCounter)
                    return counter(request);
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
                var inserter = request.GetSelector() ?? (() => request.Body.Deserialize<T>());
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
                var updater = request.GetUpdater() ?? (_source => request.Body.PopulateTo(_source));
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
                        // INFO: This code was used to add a limit of 1000 to all requests that were not explicitly marked 
                        // as unsafe. During an evaluation period, this functionality is removed.
                        // if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                        //     request.MetaConditions.Limit = (Limit) 1000;
                        var entities = TrySelectFilterProcess(request);
                        if (entities == null)
                            return MakeEntities(request, default(IEnumerable<T>));
                        return MakeEntities(request, (dynamic) entities);
                    };

                case Method.POST:
                    return request =>
                    {
                        if (request.MetaConditions.SafePost != null)
                            return SafePOST(request);
                        return new InsertedEntities(request, Insert(request));
                    };

                case Method.PUT:
                    return request =>
                    {
                        var source = TrySelectFilter(request)?.ToList().InputLimit()?.ToList();
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new InsertedEntities(request, Insert(request));
                            case 1 when request.GetUpdater() == null && !request.Body.HasContent:
                                return new UpdatedEntities(request, 0);
                            default: return new UpdatedEntities(request, Update(request));
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

        private static RequestSuccess SafePOST(IRequest<T> request)
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

        private static (IEntityRequest<T> InnerRequest, JArray ToInsert, IList<(JObject json, T source)> ToUpdate)
            GetSafePostTasks(
                IRequest<T> request)
        {
            var innerRequest = (IEntityRequest<T>) request.Context.CreateRequest<T>(Method.GET);
            var toInsert = new JArray();
            var toUpdate = new List<(JObject json, T source)>();
            try
            {
                var conditions = request.MetaConditions.SafePost
                    .Split(',')
                    .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                    .ToList();
                foreach (var entity in request.Body.Deserialize<JObject>())
                {
                    conditions.ForEach(cond => cond.Value = cond.Term.Evaluate(entity));
                    request.Conditions = conditions;
                    var results = innerRequest.Result.ToEntities<T>().ToList();
                    switch (results.Count)
                    {
                        case 0:
                            toInsert.Add(entity);
                            break;
                        case 1:
                            toUpdate.Add((entity, results[0]));
                            break;
                        default: throw new AmbiguousMatch();
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
                    Serializers.Json.PopulateJToken(item.json, item.source);
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