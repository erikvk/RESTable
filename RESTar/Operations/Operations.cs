using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Results.Error;
using RESTar.Results.Success;
using RESTar.Serialization;
using Starcounter;
using static RESTar.Operations.Do;
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
                throw new AbortedSelectorException<T>(e, request);
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
                throw new AbortedSelectorException<T>(e, request);
            }
        }


        internal static IEnumerable<T> SELECT_FILTER(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = (Limit) 1000;
                return request.Target.Select(request)?
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Offset)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException<T>(e, request);
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
                        .Filter(request.MetaConditions.OrderBy)
                        .Filter(request.MetaConditions.Offset)
                        .Filter(request.MetaConditions.Limit);
                return results
                    .Process(request.MetaConditions.Processors)
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
                throw new AbortedSelectorException<T>(e, request);
            }
        }

        internal static long OP_COUNT(IRequest<T> request)
        {
            try
            {
                return request.Resource.Count?.Invoke(request)
                       ?? request.Target.Select(request)?.LongCount()
                       ?? 0L;
            }
            catch (Exception e)
            {
                throw new AbortedCounterException<T>(e, request);
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
                throw new AbortedProfilerException<T>(e, request);
            }
        }

        #endregion

        #region INSERT

        private static int INSERT(IRequest<T> request)
        {
            try
            {
                var results = request.Body.DeserializeList<T>();
                if (results.Count == 0) return 0;
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT(IRequest<T> request, Func<IEnumerable<T>> inserter)
        {
            try
            {
                var ienum = inserter?.Invoke();
                var results = ienum as ICollection<T> ?? ienum?.ToList();
                if (results?.Any() != true) return 0;
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT_ONE(IRequest<T> request)
        {
            try
            {
                var result = request.Body.Deserialize<T>();
                if (result is IValidatable i) i.Validate();
                return request.Resource.Insert(new[] {result}, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT_ONE(IRequest<T> request, Func<T> inserter)
        {
            try
            {
                var result = inserter?.Invoke();
                if (result == null) return 0;
                if (result is IValidatable i)
                    i.Validate();
                return request.Resource.Insert(new[] {result}, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT_ONE_JOBJECT(IRequest<T> request, JObject json)
        {
            try
            {
                var result = json.ToObject<T>();
                if (result is IValidatable i) i.Validate();
                return request.Resource.Insert(new[] {result}, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT_JARRAY(IRequest<T> request, JArray json)
        {
            try
            {
                var results = json.ToObject<List<T>>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERTorTryDelete(IRequest<T> request)
        {
            List<T> results = null;
            try
            {
                results = request.Body.DeserializeList<T>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                var _results = results;
                Db.TransactAsync(() => _results?.Where(i => i != null).ForEach(item => Try(item.Delete)));
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT_ONEorTryDelete(IRequest<T> request)
        {
            var result = default(T);
            try
            {
                result = request.Body.Deserialize<T>();
                if (result is IValidatable i) i.Validate();
                return request.Resource.Insert(new[] {result}, request);
            }
            catch (Exception e)
            {
                Try(() => result?.Delete());
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT_JARRAYorTryDelete(IRequest<T> request, JArray json)
        {
            List<T> results = null;
            try
            {
                results = json.ToObject<List<T>>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                var _results = results;
                Db.TransactAsync(() => _results?.Where(i => i != null).ForEach(item => Try(item.Delete)));
                throw new AbortedInserterException<T>(e, request);
            }
        }

        #endregion

        #region UPDATE

        private static int UPDATE(IRequest<T> request, ICollection<T> source)
        {
            try
            {
                var updatedSource = source.Populate(request.Body.GetJsonUpdateString());
                if (request.Resource.RequiresValidation)
                    source.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Update(updatedSource, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        private static int UPDATE(IRequest<T> request, Func<IEnumerable<T>, IEnumerable<T>> updater,
            ICollection<T> source)
        {
            try
            {
                var ienum = updater?.Invoke(source);
                var results = ienum as ICollection<T> ?? ienum?.ToList();
                if (results?.Any() != true) return 0;
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Update(results, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        private static int UPDATE_ONE(IRequest<T> request, T source)
        {
            try
            {
                Populate(request.Body.GetJsonUpdateString(), source);
                if (source is IValidatable i)
                    i.Validate();
                return request.Resource.Update(new[] {source}, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        private static int UPDATE_ONE(IRequest<T> request, Func<T, T> updater, T source)
        {
            try
            {
                var result = updater?.Invoke(source);
                if (result == null) return 0;
                if (result is IValidatable i)
                    i.Validate();
                return request.Resource.Update(new[] {result}, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        private static int UPDATE_MANY(IRequest<T> request, ICollection<(JObject json, T source)> items)
        {
            try
            {
                var updated = items.Select(item =>
                {
                    Populate(item.json, item.source);
                    return item.source;
                }).ToList();
                if (request.Resource.RequiresValidation)
                    updated.OfType<IValidatable>().ForEach(item => item.Validate());
                return request.Resource.Update(updated, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        #endregion

        #region DELETE

        private static int OP_DELETE(IRequest<T> request, IEnumerable<T> source)
        {
            try
            {
                return request.Resource.Delete(source, request);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException<T>(e, request);
            }
        }

        private static int OP_DELETE_ONE(IRequest<T> request, T source)
        {
            try
            {
                return request.Resource.Delete(new[] {source}, request);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException<T>(e, request);
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

            private static Result GET(RESTRequest<T> request)
            {
                return Entities.Create(request, SELECT_FILTER_PROCESS(request));
            }

            private static Result REPORT(RESTRequest<T> request)
            {
                return new Report(OP_COUNT(request));
            }

            private static Result POST(RESTRequest<T> request)
            {
                if (request.MetaConditions.SafePost != null) return SafePOST(request);
                return new InsertedEntities(Transaction<T>.Transact(() => INSERT(request)), request.Resource);
            }

            private static Result PATCH(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                if (source?.Any() != true) return new UpdatedEntities(0, request.Resource);
                if (!request.MetaConditions.Unsafe && source.Count > 1)
                    throw new AmbiguousMatchException(request.Resource);
                return new UpdatedEntities(Transaction<T>.Transact(() => UPDATE(request, source)), request.Resource);
            }

            private static Result PUT(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0: return new InsertedEntities(Transaction<T>.Transact(() => INSERT_ONE(request)), request.Resource);
                    case 1: return new UpdatedEntities(Transaction<T>.Transact(() => UPDATE_ONE(request, source[0])), request.Resource);
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            private static Result DELETE(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request);
                if (source == null) return new DeletedEntities(0, request.Resource);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return new DeletedEntities(Transaction<T>.Transact(() => OP_DELETE(request, source)), request.Resource);
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
                            default: throw new AmbiguousMatchException(request.Resource);
                        }
                    }
                    return (innerRequest, toInsert, toUpdate);
                }
                catch (Exception e)
                {
                    throw new AbortedInserterException<T>(e, request, e.Message);
                }
            }

            private static Result SafePOST(RESTRequest<T> request)
            {
                var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(request);
                var outerTrans = new Transaction<T>();
                try
                {
                    int updatedCount = 0, insertedCount = 0;
                    outerTrans.Scope(() =>
                    {
                        if (toUpdate.Any())
                        {
                            var updTrans = new Transaction<T>();
                            try
                            {
                                updTrans.Scope(() => updatedCount = UPDATE_MANY(innerRequest, toUpdate));
                                updTrans.Commit();
                            }
                            catch
                            {
                                updTrans.Rollback();
                                throw;
                            }
                        }

                        if (toInsert.Any())
                        {
                            var insTrans = new Transaction<T>();
                            try
                            {
                                {
                                    insTrans.Scope(() => insertedCount = INSERT_JARRAY(innerRequest, toInsert));
                                    insTrans.Commit();
                                }
                            }
                            catch
                            {
                                insTrans.Rollback();
                                throw;
                            }
                        }
                    });
                    outerTrans.Commit();
                    return new SafePostedEntities(updatedCount, insertedCount, request.Resource);
                }
                catch
                {
                    outerTrans.Rollback();
                    throw;
                }
            }
        }

        internal static class View
        {
            internal static int POST(ViewRequest<T> request)
            {
                return Transaction<T>.Transact(() => INSERT_ONE(request));
            }

            internal static int PATCH(ViewRequest<T> request, T item)
            {
                return Transaction<T>.Transact(() => UPDATE_ONE(request, item));
            }

            internal static int DELETE(ViewRequest<T> request, T item)
            {
                return Transaction<T>.Transact(() => OP_DELETE_ONE(request, item));
            }
        }

        internal static class App
        {
            internal static int POST(Func<T> inserter, Request<T> request)
            {
                return Transaction<T>.Transact(() => INSERT_ONE(request, inserter));
            }

            internal static int POST(Func<IEnumerable<T>> inserter, Request<T> request)
            {
                return Transaction<T>.Transact(() => INSERT(request, inserter));
            }

            internal static int PATCH(Func<T, T> updater, T source, Request<T> request)
            {
                return Transaction<T>.Transact(() => UPDATE_ONE(request, updater, source));
            }

            internal static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater, ICollection<T> source,
                Request<T> request)
            {
                return Transaction<T>.Transact(() => UPDATE(request, updater, source));
            }

            internal static int PUT(Func<T> inserter, IEnumerable<T> source, Request<T> request)
            {
                var list = source?.ToList();
                switch (list?.Count)
                {
                    case null:
                    case 0: return Transaction<T>.Transact(() => INSERT_ONE(request, inserter));
                    case 1: return 0;
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            internal static int PUT(Func<T> inserter, Func<T, T> updater, IEnumerable<T> source,
                Request<T> request)
            {
                var list = source?.ToList();
                switch (list?.Count)
                {
                    case null:
                    case 0: return Transaction<T>.Transact(() => INSERT_ONE(request, inserter));
                    case 1: return Transaction<T>.Transact(() => UPDATE_ONE(request, updater, list[0]));
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            internal static int DELETE(T item, Request<T> request)
            {
                return Transaction<T>.Transact(() => OP_DELETE_ONE(request, item));
            }

            internal static int DELETE(IEnumerable<T> items, Request<T> request)
            {
                return Transaction<T>.Transact(() => OP_DELETE(request, items));
            }
        }
    }
}