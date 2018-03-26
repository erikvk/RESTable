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

        internal static long TryCount(IQuery<T> query)
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

        private static int INSERT(IQueryInternal<T> query, int hardLimit = -1)
        {
            try
            {
                var inserter = query.GetSelector() ?? (() => query.Body.ToList<T>());
                query.EntitiesProducer = () => inserter()?.HardLimit(hardLimit)?.Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                }) ?? throw new MissingDataSource(query);
                return query.Resource.Insert(query);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, query, jsonMessage);
            }
        }

        private static int UPDATE(IQueryInternal<T> query, IEnumerable<T> source)
        {
            try
            {
                var updater = query.GetUpdater() ?? (_source => query.Body.PopulateTo(_source));
                query.EntitiesProducer = () => updater(source)?.Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                }) ?? throw new MissingDataSource(query);
                return query.Resource.Update(query);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, query, jsonMessage);
            }
        }

        private static int UPDATE_SAFEPOST(IQueryInternal<T> query, ICollection<(JObject json, T source)> items)
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

        private static int OP_DELETE(IQueryInternal<T> query, IEnumerable<T> source)
        {
            try
            {
                query.EntitiesProducer = () => source;
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
                        return Entities.Create(query, TrySelectFilterProcess(query));
                    };

                case Method.POST:
                    return query => query.MetaConditions.SafePost == null
                        ? new InsertedEntities(INSERT(query), query)
                        : SafePOST(query);

                case Method.PATCH:
                    return query =>
                    {
                        var source = TrySelectFilter(query)?.ToList();
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new UpdatedEntities(0, query);
                            case var _ when query.MetaConditions.Unsafe:
                            case 1: return new UpdatedEntities(UPDATE(query, source), query);
                            default: throw new AmbiguousMatch(query.Resource);
                        }
                    };

                case Method.PUT:
                    return query =>
                    {
                        var source = TrySelectFilter(query)?.ToList();
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new InsertedEntities(INSERT(query, 1), query);
                            case 1 when query.GetUpdater() == null && !query.Body.HasContent:
                                return new UpdatedEntities(0, query);
                            case 1: return new UpdatedEntities(UPDATE(query, source), query);
                            default: throw new AmbiguousMatch(query.Resource);
                        }
                    };

                case Method.DELETE:
                    return query =>
                    {
                        var source = TrySelectFilter(query)?.ToList();
                        switch (source?.Count)
                        {
                            case null:
                            case 0: return new DeletedEntities(0, query);
                            case var _ when query.MetaConditions.Unsafe:
                            case 1: return new DeletedEntities(OP_DELETE(query, source), query);
                            default: throw new AmbiguousMatch(query.Resource);
                        }
                    };

                case Method.REPORT: return query => new Report(query, TryCount(query));

                case Method.HEAD:
                    return query =>
                    {
                        var count = TryCount(query);
                        if (count > 0) return new Head(query, count);
                        return new NoContent(query, query.TimeElapsed);
                    };

                default: return query => new ImATeapot(query);
            }
        }

        private static Result SafePOST(IQuery<T> query)
        {
            var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(query);
            var (updatedCount, insertedCount) = (0, 0);
            if (toUpdate.Any())
                updatedCount = UPDATE_SAFEPOST(innerRequest, toUpdate);
            if (toInsert.Any())
            {
                innerRequest.Selector = () => toInsert.Select(item => item.ToObject<T>());
                insertedCount = INSERT(innerRequest);
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
                        default: throw new AmbiguousMatch(query.Resource);
                    }
                }
                return (innerRequest, toInsert, toUpdate);
            }
            catch (Exception e)
            {
                throw new AbortedInsert<T>(e, query, e.Message);
            }
        }
    }
}