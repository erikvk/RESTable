using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Filters;
using RESTar.Linq;
using RESTar.Results;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.BadRequest.Aborted;
using RESTar.Results.Success;
using RESTar.Serialization;

namespace RESTar.Operations
{
    internal static class Operations<T> where T : class
    {
        #region Select

        private static IEnumerable<T> SelectFilter(IQuery<T> query) => query.Target
            .Select(query)?
            .Filter(query.MetaConditions.Distinct)
            .Filter(query.MetaConditions.Search)
            .Filter(query.MetaConditions.OrderBy)
            .Filter(query.MetaConditions.Offset)
            .Filter(query.MetaConditions.Limit);

        private static IEnumerable<object> SelectFilterProcess(IQuery<T> query) => query.Target
            .Select(query)?
            .Process(query.MetaConditions.Processors)
            .Filter(query.MetaConditions.Distinct)
            .Filter(query.MetaConditions.Search)
            .Filter(query.MetaConditions.OrderBy)
            .Filter(query.MetaConditions.Offset)
            .Filter(query.MetaConditions.Limit);

        private static IEnumerable<T> TrySelectFilter(IQuery<T> query)
        {
            try
            {
                return SelectFilter(query);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, query);
            }
        }

        private static IEnumerable<object> TrySelectFilterProcess(IQuery<T> query)
        {
            try
            {
                if (!query.MetaConditions.HasProcessors)
                    return SelectFilter(query);
                return SelectFilterProcess(query);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, query);
            }
        }

        private static long TryCount(IQuery<T> query)
        {
            try
            {
                if (query.Resource.Count is Counter<T> counter &&
                    query.MetaConditions.CanUseExternalCounter)
                    return counter(query);
                if (!query.MetaConditions.HasProcessors)
                    return SelectFilter(query)?.Count() ?? 0L;
                return SelectFilterProcess(query)?.Count() ?? 0L;
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedReport<T>(e, query);
            }
        }

        #endregion

        private static int Insert(IQueryInternal<T> query, bool limit = false)
        {
            try
            {
                var inserter = query.GetSelector() ?? (() => query.Body.ToList<T>());
                if (limit)
                {
                    var _inserter = inserter;
                    inserter = () => _inserter().InputLimit();
                }
                query.EntitiesProducer = () => inserter()?.Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                }) ?? throw new MissingDataSource(query);
                return query.Resource.Insert(query);
            }
            catch (Exception e)
            {
                throw new AbortedInsert<T>(e, query);
            }
        }

        private static int Update(IQueryInternal<T> query)
        {
            try
            {
                var sourceSelector = query.GetSelector() ?? (() => TrySelectFilter(query)?.ToList() ?? new List<T>());
                if (!query.MetaConditions.Unsafe)
                {
                    var selector = sourceSelector;
                    sourceSelector = () => selector()?.UnsafeLimit();
                }
                var updater = query.GetUpdater() ?? (_source => query.Body.PopulateTo(_source));
                query.EntitiesProducer = () => updater(sourceSelector())?.Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                }) ?? throw new MissingDataSource(query);
                return query.Resource.Update(query);
            }
            catch (Exception e)
            {
                throw new AbortedUpdate<T>(e, query);
            }
        }

        private static int Delete(IQueryInternal<T> query)
        {
            try
            {
                var sourceSelector = query.GetSelector() ?? (() => TrySelectFilter(query)?.ToList() ?? new List<T>());
                if (!query.MetaConditions.Unsafe)
                {
                    var selector = sourceSelector;
                    sourceSelector = () => selector()?.UnsafeLimit();
                }
                query.EntitiesProducer = () => sourceSelector() ?? new T[0];
                return query.Resource.Delete(query);
            }
            catch (Exception e)
            {
                throw new AbortedDelete<T>(e, query);
            }
        }

        internal static Func<IQueryInternal<T>, Result> GetEvaluator(Method method)
        {
            switch (method)
            {
                case Method.GET:
                    return query =>
                    {
                        if (!query.MetaConditions.Unsafe && query.MetaConditions.Limit == -1)
                            query.MetaConditions.Limit = (Limit) 1000;
                        return new Entities(query, TrySelectFilterProcess(query));
                    };

                case Method.POST:
                    return query =>
                    {
                        if (query.MetaConditions.SafePost != null)
                            return SafePOST(query);
                        return new InsertedEntities(Insert(query), query);
                    };

                case Method.PUT:
                    return query =>
                    {
                        var sourceSelector = query.GetSelector() ?? (() => TrySelectFilter(query)?.ToList() ?? new List<T>());
                        var source = sourceSelector()?.InputLimit()?.ToList();
                        query.InputSelector = () => source;
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new InsertedEntities(Insert(query), query);
                            case 1 when query.GetUpdater() == null && !query.Body.HasContent:
                                return new UpdatedEntities(0, query);
                            default: return new UpdatedEntities(Update(query), query);
                        }
                    };

                case Method.HEAD:
                    return query =>
                    {
                        var count = TryCount(query);
                        if (count > 0) return new Head(query, count);
                        return new NoContent(query, query.TimeElapsed);
                    };

                case Method.PATCH: return query => new UpdatedEntities(Update(query), query);
                case Method.DELETE: return query => new DeletedEntities(Delete(query), query);
                case Method.REPORT: return query => new Report(query, TryCount(query));
                default: return query => new ImATeapot(query);
            }
        }

        #region SafePost

        private static Result SafePOST(IQuery<T> query)
        {
            var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(query);
            var (updatedCount, insertedCount) = (0, 0);
            if (toUpdate.Any())
                updatedCount = UpdateSafePost(innerRequest, toUpdate);
            if (toInsert.Any())
            {
                innerRequest.InputSelector = () => toInsert.Select(item => item.ToObject<T>());
                insertedCount = Insert(innerRequest);
            }
            return new SafePostedEntities(updatedCount, insertedCount, query);
        }

        private static (IQueryInternal<T> InnerRequest, JArray ToInsert, IList<(JObject json, T source)> ToUpdate) GetSafePostTasks(
            IQuery<T> query)
        {
            var innerRequest = (IQueryInternal<T>) Query<T>.Create(query, Method.GET);
            var toInsert = new JArray();
            var toUpdate = new List<(JObject json, T source)>();
            try
            {
                var conditions = query.MetaConditions.SafePost
                    .Split(',')
                    .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                    .ToList();
                foreach (var entity in query.Body.ToList<JObject>())
                {
                    conditions.ForEach(cond => cond.Value = cond.Term.Evaluate(entity));
                    query.Conditions = conditions;
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
                throw new AbortedInsert<T>(e, query, e.Message);
            }
        }

        private static int UpdateSafePost(IQueryInternal<T> query, ICollection<(JObject json, T source)> items)
        {
            try
            {
                query.EntitiesProducer = () => items.Select(item =>
                {
                    Serializers.Json.PopulateJToken(item.json, item.source);
                    (item.source as IValidatable)?.Validate();
                    return item.source;
                });
                return query.Resource.Update(query);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, query, jsonMessage);
            }
        }

        #endregion
    }
}