using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Linq;
using RESTar.Requests;
using Starcounter;
using static RESTar.Operations.Do;
using static RESTar.Requests.Responses;
using static RESTar.Serializer;
using static RESTar.Settings;

namespace RESTar.Operations
{
    internal static class Evaluators<T> where T : class
    {
        #region Operations

        #region SELECT

        internal static IEnumerable<T> RAW_SELECT(IRequest<T> request)
        {
            try
            {
                return request.Resource.Select(request);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException<T>(e, request);
            }
        }

        internal static IEnumerable<T> STATIC_SELECT(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = 1000;
                return request.Resource.Select(request)?
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException<T>(e, request);
            }
        }

        internal static IEnumerable<object> DYNAMIC_SELECT(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = 1000;
                return request.Resource.Select(request)?
                    .Process(request.MetaConditions.Add)
                    .Process(request.MetaConditions.Rename)
                    .Process(request.MetaConditions.Select)
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException<T>(e, request);
            }
        }

        #endregion

        #region INSERT

        private static int INSERT(IRequest<T> request)
        {
            try
            {
                var results = request.Body.Deserialize<List<T>>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException<T>(e, request);
            }
        }

        private static int INSERT(IRequest<T> request, Func<ICollection<T>> inserter)
        {
            try
            {
                var results = inserter?.Invoke();
                if (results == null) return 0;
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
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
                if (result is IValidatable i) i.RunValidation();
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
                    i.RunValidation();
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
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
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
                results = request.Body.Deserialize<List<T>>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
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
                if (result is IValidatable i) i.RunValidation();
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
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
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

        private static int UPDATE(IRequest<T> request, IEnumerable<T> source)
        {
            try
            {
                source.ForEach(entity => Populate(request.Body, entity));
                if (request.Resource.RequiresValidation)
                    source.OfType<IValidatable>().ForEach(item => item.RunValidation());
                return request.Resource.Update(source, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        private static int UPDATE(IRequest<T> request, Func<IEnumerable<T>, IEnumerable<T>> updater,
            IEnumerable<T> source)
        {
            try
            {
                var results = updater?.Invoke(source);
                if (results == null) return 0;
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
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
                Populate(request.Body, source);
                if (source is IValidatable i)
                    i.RunValidation();
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
                    i.RunValidation();
                return request.Resource.Update(new[] {result}, request);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException<T>(e, request);
            }
        }

        private static int UPDATE_MANY(IRequest<T> request, IEnumerable<(JObject json, T source)> items)
        {
            try
            {
                var updated = new List<T>();
                items.ForEach(item =>
                {
                    Populate(item.json, item.source);
                    updated.Add(item.source);
                });
                if (request.Resource.RequiresValidation)
                    updated.OfType<IValidatable>().ForEach(item => item.RunValidation());
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
            internal static RESTEvaluator<T> GetEvaluator(RESTarMethods method)
            {
                #region Long running transactions test

                if (!_DontUseLRT)
                {
                    switch (method)
                    {
                        case RESTarMethods.GET: return GET;
                        case RESTarMethods.POST: return LrPOST;
                        case RESTarMethods.PATCH: return LrPATCH;
                        case RESTarMethods.PUT: return LrPUT;
                        case RESTarMethods.DELETE: return LrDELETE;
                        default: return null;
                    }
                }

                #endregion

                switch (method)
                {
                    case RESTarMethods.GET: return GET;
                    case RESTarMethods.POST: return POST;
                    case RESTarMethods.PATCH: return PATCH;
                    case RESTarMethods.PUT: return PUT;
                    case RESTarMethods.DELETE: return DELETE;
                    default: return null;
                }
            }

            private static Response GET(RESTRequest<T> request)
            {
                var results = DYNAMIC_SELECT(request);
                if (results == null) return NoContent;
                return request.MakeResponse(results) ?? NoContent;
            }

            #region Using long running transactions

            private static Response LrPOST(RESTRequest<T> request)
            {
                request.Body = request.Body[0] == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null) return LrSafePOST(request);
                return InsertedEntities<T>(Transaction<T>.Transact(() => INSERT(request)));
            }

            private static Response LrPATCH(RESTRequest<T> request)
            {
                var source = STATIC_SELECT(request);
                if (source == null)
                    return UpdatedEntities<T>(0);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return UpdatedEntities<T>(Transaction<T>.Transact(() => UPDATE(request, source)));
            }

            private static Response LrPUT(RESTRequest<T> request)
            {
                var source = STATIC_SELECT(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0: return InsertedEntities<T>(Transaction<T>.Transact(() => INSERT_ONE(request)));
                    case 1: return UpdatedEntities<T>(Transaction<T>.Transact(() => UPDATE_ONE(request, source[0])));
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            private static Response LrDELETE(RESTRequest<T> request)
            {
                var source = STATIC_SELECT(request);
                if (source == null)
                    return DeletedEntities<T>(0);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return DeletedEntities<T>(Transaction<T>.Transact(() => OP_DELETE(request, source)));
            }

            private static (Request<T> InnerRequest, JArray ToInsert, IList<(JObject json, T source)> ToUpdate)
                GetSafePostTasks(RESTRequest<T> request)
            {
                var innerRequest = new Request<T>();
                var toInsert = new JArray();
                var toUpdate = new List<(JObject json, T source)>();
                try
                {
                    var terms = request.MetaConditions.SafePost.Split(',').Select(k =>
                        request.Resource.MakeTerm(k, request.Resource.DynamicConditionsAllowed));
                    var conditions = terms.Select(term => new Condition<T>(term, Operator.EQUALS, null)).ToList();
                    foreach (var entity in request.Body.Deserialize<IEnumerable<JObject>>())
                    {
                        conditions.ForEach(cond => cond.SetValue(cond.Term.Evaluate(entity)));
                        var results = innerRequest.WithConditions(conditions).GET().ToList();
                        switch (results.Count)
                        {
                            case 0:
                                toInsert.Add(entity);
                                break;
                            case 1:
                                toUpdate.Add((entity, results.First()));
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

            private static Response LrSafePOST(RESTRequest<T> request)
            {
                var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(request);
                var trans = new Transaction<T>();
                try
                {
                    var insertedCount = toInsert.Any() ? trans.Scope(() => INSERT_JARRAY(innerRequest, toInsert)) : 0;
                    var updatedCount = toUpdate.Any() ? trans.Scope(() => UPDATE_MANY(innerRequest, toUpdate)) : 0;
                    trans.Commit();
                    return SafePostedEntities<T>(insertedCount, updatedCount);
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }

            #endregion

            #region Using Transactions.Trans()

            private static Response POST(RESTRequest<T> request)
            {
                request.Body = request.Body[0] == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null) return SafePOST(request);
                return InsertedEntities<T>(Transaction<T>.ShTransact(() => INSERTorTryDelete(request)));
            }

            private static Response PATCH(RESTRequest<T> request)
            {
                var source = STATIC_SELECT(request);
                if (source == null)
                    return UpdatedEntities<T>(0);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return UpdatedEntities<T>(Transaction<T>.ShTransact(() => UPDATE(request, source)));
            }

            private static Response PUT(RESTRequest<T> request)
            {
                var source = STATIC_SELECT(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0: return InsertedEntities<T>(Transaction<T>.ShTransact(() => INSERT_ONEorTryDelete(request)));
                    case 1: return UpdatedEntities<T>(Transaction<T>.ShTransact(() => UPDATE_ONE(request, source[0])));
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            private static Response DELETE(RESTRequest<T> request)
            {
                var source = STATIC_SELECT(request);
                if (source == null)
                    return DeletedEntities<T>(0);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return DeletedEntities<T>(Transaction<T>.ShTransact(() => OP_DELETE(request, source)));
            }

            private static Response SafePOST(RESTRequest<T> request)
            {
                var (insertedCount, updatedCount) = (0, 0);
                var (innerRequest, toInsert, toUpdate) = GetSafePostTasks(request);
                try
                {
                    insertedCount = toInsert.Any()
                        ? Transaction<T>.ShTransact(() => INSERT_JARRAYorTryDelete(innerRequest, toInsert))
                        : 0;
                    updatedCount = toUpdate.Any()
                        ? Transaction<T>.ShTransact(() => UPDATE_MANY(innerRequest, toUpdate))
                        : 0;
                    return SafePostedEntities<T>(insertedCount, updatedCount);
                }
                catch (Exception e)
                {
                    var message = $"Inserted {insertedCount} and updated {updatedCount} in resource " +
                                  $"'{request.Resource.Name}' using SafePOST before encountering the error. " +
                                  "These changes remain in the resource";
                    throw new AbortedInserterException<T>(e, request, $"{e.Message} : {message}");
                }
            }

            #endregion
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

            internal static int POST(Func<ICollection<T>> inserter, Request<T> request)
            {
                return Transaction<T>.Transact(() => INSERT(request, inserter));
            }

            internal static int PATCH(Func<T, T> updater, T source, Request<T> request)
            {
                return Transaction<T>.Transact(() => UPDATE_ONE(request, updater, source));
            }

            internal static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater, IEnumerable<T> source,
                Request<T> request)
            {
                return Transaction<T>.Transact(() => UPDATE(request, updater, source));
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