using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.BadRequest.Aborted;
using RESTar.Results.Success;
using RESTar.Serialization;
using static RESTar.Serialization.Serializer;

namespace RESTar.Operations
{
    internal static class Operations<T> where T : class
    {
        #region Operations

        #region SELECT, COUNT and PROFILE

        internal static IEnumerable<T> SELECT(IRequest<T> request)
        {
            try
            {
                return request.Target.Select(request);
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, request);
            }
        }

        internal static IEnumerable<T> SELECT_VIEW(ViewRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = (Limit) 100;
                return request.Target.Select(request)?.Filter(request.MetaConditions.OrderBy);
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, request);
            }
        }


        internal static IEnumerable<T> SELECT_FILTER(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = (Limit) 1000;
                return request.Target.Select(request)?
                    .Filter(request.MetaConditions.Search)
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Offset)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, request);
            }
        }

        internal static IEnumerable<dynamic> SELECT_FILTER_PROCESS(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = (Limit) 1000;
                var results = request.Target.Select(request);
                if (results == null) return null;
                if (!request.MetaConditions.HasProcessors)
                    return results
                        .Filter(request.MetaConditions.Search)
                        .Filter(request.MetaConditions.OrderBy)
                        .Filter(request.MetaConditions.Offset)
                        .Filter(request.MetaConditions.Limit);
                return results
                    .Process(request.MetaConditions.Processors)
                    .Filter(request.MetaConditions.Search)
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Offset)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (InfiniteLoop)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, request);
            }
        }

        internal static long OP_COUNT(IRequest<T> request)
        {
            try
            {
                return request.Resource.Count?.Invoke(request) ?? request.Target.Select(request)?.LongCount() ?? 0L;
            }
            catch (Exception e)
            {
                throw new AbortedReport<T>(e, request);
            }
        }

        internal static ResourceProfile PROFILE(IRequest<T> request)
        {
            try
            {
                return request.Resource.Profile?.Invoke(request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedProfile<T>(e, request);
            }
        }

        #endregion

        #region INSERT

        private static int INSERT(IRequestInternal<T> request)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var entities = request.Body.DeserializeList<T>();
                    if (request.Resource.RequiresValidation)
                        entities.OfType<IValidatable>().ForEach(item => item.Validate());
                    return entities;
                };
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        private static int INSERT(IRequestInternal<T> request, Func<IEnumerable<T>> inserter)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var ienum = inserter.Invoke();
                    var results = ienum as ICollection<T> ?? ienum.ToList();
                    if (request.Resource.RequiresValidation)
                        results.OfType<IValidatable>().ForEach(item => item.Validate());
                    return results;
                };
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        private static int INSERT_ONE(IRequestInternal<T> request)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var result = request.Body.Deserialize<T>();
                    if (result is IValidatable i) i.Validate();
                    return new[] {result};
                };
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        private static int INSERT_ONE(IRequestInternal<T> request, Func<T> inserter)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var result = inserter.Invoke();
                    if (result is IValidatable i) i.Validate();
                    return new[] {result};
                };
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        private static int INSERT_ONE_JOBJECT(IRequestInternal<T> request, JObject json)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var result = json.ToObject<T>();
                    if (result is IValidatable i) i.Validate();
                    return new[] {result};
                };
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        private static int INSERT_JARRAY(IRequestInternal<T> request, JArray json)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var results = json.ToObject<List<T>>();
                    if (request.Resource.RequiresValidation)
                        results.OfType<IValidatable>().ForEach(item => item.Validate());
                    return results;
                };
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        #endregion

        #region UPDATE

        private static int UPDATE(IRequestInternal<T> request, ICollection<T> source)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var updatedSource = source.Populate(request.Body.GetJsonUpdateString());
                    if (request.Resource.RequiresValidation)
                        source.OfType<IValidatable>().ForEach(item => item.Validate());
                    return updatedSource;
                };
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        private static int UPDATE(IRequestInternal<T> request, Func<IEnumerable<T>, IEnumerable<T>> updater,
            ICollection<T> source)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var ienum = updater.Invoke(source);
                    var results = ienum as ICollection<T> ?? ienum.ToList();
                    if (request.Resource.RequiresValidation)
                        results.OfType<IValidatable>().ForEach(item => item.Validate());
                    return results;
                };
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        private static int UPDATE_ONE(IRequestInternal<T> request, T source)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    Populate(request.Body.GetJsonUpdateString(), source);
                    if (source is IValidatable i) i.Validate();
                    return new[] {source};
                };
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        private static int UPDATE_ONE(IRequestInternal<T> request, Func<T, T> updater, T source)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var result = updater.Invoke(source);
                    if (result is IValidatable i) i.Validate();
                    return new[] {result};
                };
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        private static int UPDATE_MANY(IRequestInternal<T> request, ICollection<(JObject json, T source)> items)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    var updated = items.Select(item =>
                    {
                        Populate(item.json, item.source);
                        return item.source;
                    }).ToList();
                    if (request.Resource.RequiresValidation)
                        updated.OfType<IValidatable>().ForEach(item => item.Validate());
                    return updated;
                };
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        #endregion

        #region DELETE

        private static int OP_DELETE(IRequestInternal<T> request, IEnumerable<T> source)
        {
            try
            {
                request.EntitiesGenerator = () => source;
                return request.Resource.Delete(request);
            }
            catch (Exception e)
            {
                throw new AbortedDelete<T>(e, request);
            }
        }

        private static int OP_DELETE_ONE(IRequestInternal<T> request, T source)
        {
            try
            {
                request.EntitiesGenerator = () => new[] {source};
                return request.Resource.Delete(request);
            }
            catch (Exception e)
            {
                throw new AbortedDelete<T>(e, request);
            }
        }

        #endregion

        #endregion

        internal static class REST
        {
            internal static Func<RESTRequest<T>, Result> GetEvaluator(Methods method)
            {
                switch (method)
                {
                    case Methods.GET: return GET;
                    case Methods.POST: return POST;
                    case Methods.PATCH: return PATCH;
                    case Methods.PUT: return PUT;
                    case Methods.DELETE: return DELETE;
                    case Methods.REPORT: return REPORT;
                    default: return null;
                }
            }

            private static Entities GET(RESTRequest<T> request)
            {
                return Entities.Create(request, SELECT_FILTER_PROCESS(request));
            }

            private static Report REPORT(RESTRequest<T> request)
            {
                return new Report(OP_COUNT(request), request);
            }

            private static Result POST(RESTRequest<T> request)
            {
                if (request.MetaConditions.SafePost != null) return SafePOST(request);
                return new InsertedEntities(INSERT(request), request);
            }

            private static Result PATCH(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                if (source?.Any() != true) return new UpdatedEntities(0, request);
                if (!request.MetaConditions.Unsafe && source.Count > 1)
                    throw new AmbiguousMatch(request.Resource);
                return new UpdatedEntities(UPDATE(request, source), request);
            }

            private static Result PUT(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0: return new InsertedEntities(INSERT_ONE(request), request);
                    case 1: return new UpdatedEntities(UPDATE_ONE(request, source[0]), request);
                    default: throw new AmbiguousMatch(request.Resource);
                }
            }

            private static Result DELETE(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request);
                if (source == null) return new DeletedEntities(0, request);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatch(request.Resource);
                    source = list;
                }
                return new DeletedEntities(OP_DELETE(request, source), request);
            }

            private static (Request<T> InnerRequest, JArray ToInsert, IList<(JObject json, T source)> ToUpdate)
                GetSafePostTasks(RESTRequest<T> request)
            {
                var innerRequest = new Request<T>();
                var toInsert = new JArray();
                var toUpdate = new List<(JObject json, T source)>();
                try
                {
                    var conditions = request.MetaConditions.SafePost
                        .Split(',')
                        .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                        .ToArray();
                    foreach (var entity in request.Body.DeserializeList<JObject>())
                    {
                        conditions.ForEach(cond => cond.Value = cond.Term.Evaluate(entity));
                        var results = innerRequest.WithConditions(conditions).GET().ToList();
                        switch (results.Count)
                        {
                            case 0:
                                toInsert.Add(entity);
                                break;
                            case 1:
                                toUpdate.Add((entity, results[0]));
                                break;
                            default: throw new AmbiguousMatch(request.Resource);
                        }
                    }
                    return (innerRequest, toInsert, toUpdate);
                }
                catch (Exception e)
                {
                    throw new AbortedInsert<T>(e, request, e.Message);
                }
            }

            private static Result SafePOST(RESTRequest<T> request)
            {
                var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(request);
                var (updatedCount, insertedCount) = (0, 0);
                if (toUpdate.Any()) updatedCount = UPDATE_MANY(innerRequest, toUpdate);
                if (toInsert.Any()) insertedCount = INSERT_JARRAY(innerRequest, toInsert);
                return new SafePostedEntities(updatedCount, insertedCount, request);
            }
        }

        internal static class View
        {
            internal static int POST(ViewRequest<T> request) => INSERT_ONE(request);
            internal static int PATCH(ViewRequest<T> request, T item) => UPDATE_ONE(request, item);
            internal static int DELETE(ViewRequest<T> request, T item) => OP_DELETE_ONE(request, item);
        }

        internal static class App
        {
            internal static int POST(Func<T> inserter, Request<T> request) => INSERT_ONE(request, inserter);
            internal static int POST(Func<IEnumerable<T>> inserter, Request<T> request) => INSERT(request, inserter);
            internal static int PATCH(Func<T, T> updater, T source, Request<T> request) => UPDATE_ONE(request, updater, source);

            internal static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater, ICollection<T> source, Request<T> request)
                => UPDATE(request, updater, source);

            internal static int PUT(Func<T> inserter, IEnumerable<T> source, Request<T> request)
            {
                var list = source?.ToList();
                switch (list?.Count)
                {
                    case null:
                    case 0: return INSERT_ONE(request, inserter);
                    case 1: return 0;
                    default: throw new AmbiguousMatch(request.Resource);
                }
            }

            internal static int PUT(Func<T> inserter, Func<T, T> updater, IEnumerable<T> source,
                Request<T> request)
            {
                var list = source?.ToList();
                switch (list?.Count)
                {
                    case null:
                    case 0: return INSERT_ONE(request, inserter);
                    case 1: return UPDATE_ONE(request, updater, list[0]);
                    default: throw new AmbiguousMatch(request.Resource);
                }
            }

            internal static int DELETE(T item, Request<T> request) => OP_DELETE_ONE(request, item);

            internal static int DELETE(IEnumerable<T> items, Request<T> request) => OP_DELETE(request, items);
        }
    }
}