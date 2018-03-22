using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Results.Error;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.BadRequest.Aborted;
using RESTar.Results.Success;
using RESTar.Serialization;

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

        private static IEnumerable<T> SELECT_FILTER(IRequest<T> request) => request.Target
            .Select(request)?
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        private static IEnumerable<object> SELECT_FILTER_PROCESS(IRequest<T> request) => request.Target
            .Select(request)?
            .Process(request.MetaConditions.Processors)
            .Filter(request.MetaConditions.Distinct)
            .Filter(request.MetaConditions.Search)
            .Filter(request.MetaConditions.OrderBy)
            .Filter(request.MetaConditions.Offset)
            .Filter(request.MetaConditions.Limit);

        private static IEnumerable<T> TRY_SELECT_FILTER(IRequest<T> request)
        {
            try
            {
                return SELECT_FILTER(request);
            }
            catch (Exception e)
            {
                throw new AbortedSelect<T>(e, request);
            }
        }

        private static IEnumerable<object> TRY_SELECT_FILTER_PROCESS(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.HasProcessors)
                    return SELECT_FILTER(request);
                return SELECT_FILTER_PROCESS(request);
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

        internal static long TRY_COUNT(IRequest<T> request)
        {
            try
            {
                if (request.Resource.Count is Counter<T> counter &&
                    request.MetaConditions.CanUseExternalCounter)
                    return counter(request);
                if (!request.MetaConditions.HasProcessors)
                    return SELECT_FILTER(request)?.Count() ?? 0L;
                return SELECT_FILTER_PROCESS(request)?.Count() ?? 0L;
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
                request.EntitiesGenerator = () => request.GetInserter().Invoke().Select(entity =>
                {
                    (entity as IValidatable).Validate();
                    return entity;
                });
                return request.Resource.Insert(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedInsert<T>(e, request, jsonMessage);
            }
        }

        [Obsolete]
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

        [Obsolete]
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

        [Obsolete]
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

        [Obsolete]
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

        private static int UPDATE(IRequestInternal<T> request, IEnumerable<T> source)
        {
            try
            {
                request.EntitiesGenerator = () => request.GetUpdater().Invoke(source).Select(entity =>
                {
                    (entity as IValidatable)?.Validate();
                    return entity;
                });
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        [Obsolete]
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

        [Obsolete]
        private static int UPDATE_ONE(IRequestInternal<T> request, List<T> source)
        {
            try
            {
                request.EntitiesGenerator = () =>
                {
                    request.Body.PopulateTo(source);
                    var item = source[0];
                    if (item is IValidatable i) i.Validate();
                    return source;
                };
                return request.Resource.Update(request);
            }
            catch (Exception e)
            {
                var jsonMessage = e is JsonSerializationException jse ? jse.TotalMessage() : null;
                throw new AbortedUpdate<T>(e, request, jsonMessage);
            }
        }

        [Obsolete]
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

        private static int UPDATE_SAFEPOST(IRequestInternal<T> request, ICollection<(JObject json, T source)> items)
        {
            try
            {
                request.EntitiesGenerator = () => items.Select(item =>
                {
                    Serializers.Json.PopulateJToken(item.json, item.source);
                    (item.source as IValidatable)?.Validate();
                    return item.source;
                });
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

        [Obsolete]
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
            internal static Func<IRequestInternal<T>, Result> GetEvaluator(Methods method)
            {
                switch (method)
                {
                    case Methods.GET: return GET;
                    case Methods.POST: return POST;
                    case Methods.PATCH: return PATCH;
                    case Methods.PUT: return PUT;
                    case Methods.DELETE: return DELETE;
                    case Methods.REPORT: return REPORT;
                    case Methods.HEAD: return HEAD;
                    default: return null;
                }
            }

            private static Entities GET(IRequestInternal<T> request)
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = (Limit) 1000;
                return Entities.Create(request, TRY_SELECT_FILTER_PROCESS(request));
            }

            private static Report REPORT(IRequestInternal<T> request)
            {
                return new Report(request, TRY_COUNT(request));
            }

            private static Result HEAD(IRequestInternal<T> request)
            {
                var count = TRY_COUNT(request);
                if (count > 0)
                    return new Head(request, count);
                return new NoContent(request);
            }

            private static Result POST(IRequestInternal<T> request)
            {
                if (request.MetaConditions.SafePost != null) return SafePOST(request);
                return new InsertedEntities(INSERT(request), request);
            }

            private static Result PATCH(IRequestInternal<T> request)
            {
                var source = TRY_SELECT_FILTER(request)?.ToList();
                if (source?.Any() != true) return new UpdatedEntities(0, request);
                if (!request.MetaConditions.Unsafe && source.Count > 1)
                    throw new AmbiguousMatch(request.Resource);
                return new UpdatedEntities(UPDATE(request, source), request);
            }

            private static Result PUT(IRequestInternal<T> request)
            {
                var source = TRY_SELECT_FILTER(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0:
                        request.Inserter = () =>
                        {
                            var results = request.Body.ToList<T>();
                            if (results.Count > 1) throw new InvalidInputCount();
                            return results;
                        };
                        return new InsertedEntities(INSERT(request), request);
                    case 1:
                        return new UpdatedEntities(UPDATE(request, source), request);
                    default: throw new AmbiguousMatch(request.Resource);
                }
            }

            private static Result DELETE(IRequestInternal<T> request)
            {
                var source = TRY_SELECT_FILTER(request);
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

            private static (IRequestInternal<T> InnerRequest, JArray ToInsert, IList<(JObject json, T source)> ToUpdate) GetSafePostTasks(
                IRequest<T> request)
            {
                var innerRequest = (IRequestInternal<T>) Request<T>.Create(request, Methods.GET);
                var toInsert = new JArray();
                var toUpdate = new List<(JObject json, T source)>();
                try
                {
                    var conditions = request.MetaConditions.SafePost
                        .Split(',')
                        .Select(s => new Condition<T>(s, Operators.EQUALS, null))
                        .ToList();
                    foreach (var entity in request.Body.ToList<JObject>())
                    {
                        conditions.ForEach(cond => cond.Value = cond.Term.Evaluate(entity));
                        request.Conditions = conditions;
                        var results = innerRequest.GetResult().ToEntities<T>().ToList();
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

            private static Result SafePOST(IRequest<T> request)
            {
                var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(request);
                var (updatedCount, insertedCount) = (0, 0);
                if (toUpdate.Any())
                    updatedCount = UPDATE_SAFEPOST(innerRequest, toUpdate);
                if (toInsert.Any())
                {
                    innerRequest.Inserter = () => toInsert.Select(item => item.ToObject<T>());
                    insertedCount = INSERT(innerRequest);
                }
                return new SafePostedEntities(updatedCount, insertedCount, request);
            }
        }


        //        internal static class App
        //        {
        //            internal static int POST(Func<T> inserter, InternalRequest<T> internalRequest) => INSERT_ONE(internalRequest, inserter);
        //            internal static int POST(Func<IEnumerable<T>> inserter, InternalRequest<T> internalRequest) => INSERT(internalRequest, inserter);
        //            internal static int PATCH(Func<T, T> updater, T source, InternalRequest<T> internalRequest) => UPDATE_ONE(internalRequest, updater, source);
        //
        //            internal static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater, ICollection<T> source, InternalRequest<T> internalRequest)
        //                => UPDATE(internalRequest, updater, source);
        //
        //            internal static int PUT(Func<T> inserter, IEnumerable<T> source, InternalRequest<T> internalRequest)
        //            {
        //                var list = source?.ToList();
        //                switch (list?.Count)
        //                {
        //                    case null:
        //                    case 0: return INSERT_ONE(internalRequest, inserter);
        //                    case 1: return 0;
        //                    default: throw new AmbiguousMatch(internalRequest.Resource);
        //                }
        //            }
        //
        //            internal static int PUT(Func<T> inserter, Func<T, T> updater, IEnumerable<T> source,
        //                InternalRequest<T> internalRequest)
        //            {
        //                var list = source?.ToList();
        //                switch (list?.Count)
        //                {
        //                    case null:
        //                    case 0: return INSERT_ONE(internalRequest, inserter);
        //                    case 1: return UPDATE_ONE(internalRequest, updater, list[0]);
        //                    default: throw new AmbiguousMatch(internalRequest.Resource);
        //                }
        //            }
        //
        //            internal static int DELETE(T item, InternalRequest<T> internalRequest) => OP_DELETE_ONE(internalRequest, item);
        //
        //            internal static int DELETE(IEnumerable<T> items, InternalRequest<T> internalRequest) => OP_DELETE(internalRequest, items);
        //        }
    }
}