using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Requests;
using Starcounter;
using static RESTar.Internal.RESTarResourceType;
using static RESTar.Internal.Transactions;
using static RESTar.Operations.Do;
using static RESTar.Requests.Responses;
using static RESTar.Serializer;
using static RESTar.Settings;

namespace RESTar.Operations
{
    internal static class Evaluators<T> where T : class
    {
        #region Operations

        internal static IEnumerable<T> StatSELECT(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = 1000;
                return request.Resource
                    .Select(request)?
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, request);
            }
        }

        internal static IEnumerable<dynamic> DynSELECT(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = 1000;
                return request.Resource
                    .Select(request)?
                    .Process(request.MetaConditions.Add)
                    .Process(request.MetaConditions.Rename)
                    .Process(request.MetaConditions.Select)
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Limit);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, request);
            }
        }

        internal static IEnumerable<T> AppSELECT(IRequest<T> request)
        {
            try
            {
                return request.Resource.Select(request);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, request);
            }
        }

        internal static int INSERT_JArray(IRequest<T> request, JArray json)
        {
            IEnumerable<T> results = null;
            try
            {
                if (request.Resource.ResourceType == RESTarDynamicResource)
                    results = (T[]) json.ToObject(request.Resource.TargetType.MakeArrayType());
                else results = json.ToObject<IEnumerable<T>>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                var _results = results;
                Trans(() => _results?.Where(i => i != null).ForEach(item => Try(item.Delete)));
                throw new AbortedInserterException(e, request);
            }
        }

        internal static int INSERT(IRequest<T> request)
        {
            IEnumerable<T> results = null;
            try
            {
                if (request.Resource.ResourceType == RESTarDynamicResource)
                {
                    var type = request.Resource.TargetType.MakeArrayType();
                    results = request.Body.DeserializeExplicit<T[]>(type);
                }
                else results = request.Body.Deserialize<IEnumerable<T>>();
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                var _results = results;
                Trans(() => _results?.Where(i => i != null).ForEach(item => Try(item.Delete)));
                throw new AbortedInserterException(e, request);
            }
        }

        internal static int UPDATE(IRequest<T> request, IEnumerable<T> source)
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
                throw new AbortedUpdaterException(e, request);
            }
        }

        internal static int UPDATE_MANY(IRequest<T> request, IEnumerable<(JObject json, T source)> items)
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
                throw new AbortedUpdaterException(e, request);
            }
        }

        internal static int DELETEop(IRequest<T> request, IEnumerable<T> source)
        {
            try
            {
                return request.Resource.Delete(source, request);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e, request);
            }
        }

        internal static int INSERT_ONE(IRequest<T> request)
        {
            T result = null;
            try
            {
                if (request.Resource.ResourceType == RESTarDynamicResource)
                    result = request.Body.DeserializeExplicit<T>(request.Resource.TargetType);
                else result = request.Body.Deserialize<T>();
                if (result is IValidatable i) i.RunValidation();
                return request.Resource.Insert(new[] {result}, request);
            }
            catch (Exception e)
            {
                Trans(() => Try(() => result?.Delete()));
                throw new AbortedInserterException(e, request);
            }
        }

        internal static int UPDATE_ONE(IRequest<T> request, T source)
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
                throw new AbortedUpdaterException(e, request);
            }
        }

        internal static int DELETEop_ONE(IRequest<T> request, T source)
        {
            try
            {
                return request.Resource.Delete(new[] {source}, request);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e, request);
            }
        }

        internal static int INSERT(IRequest<T> request, Func<IEnumerable<T>> inserter)
        {
            IEnumerable<T> results = null;
            try
            {
                results = inserter?.Invoke();
                if (results == null) return 0;
                if (request.Resource.RequiresValidation)
                    results.OfType<IValidatable>().ForEach(item => item.RunValidation());
                return request.Resource.Insert(results, request);
            }
            catch (Exception e)
            {
                var _results = results;
                Trans(() => _results?.Where(i => i != null).ForEach(item => Try(item.Delete)));
                throw new AbortedInserterException(e, request);
            }
        }

        internal static int INSERT_ONE(IRequest<T> request, Func<T> inserter)
        {
            T result = null;
            try
            {
                result = inserter?.Invoke();
                if (result == null) return 0;
                if (result is IValidatable i)
                    i.RunValidation();
                return request.Resource.Insert(new[] {result}, request);
            }
            catch (Exception e)
            {
                Trans(() => Try(() => result?.Delete()));
                throw new AbortedInserterException(e, request);
            }
        }

        internal static int UPDATE(IRequest<T> request, Func<IEnumerable<T>, IEnumerable<T>> updater,
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
                throw new AbortedUpdaterException(e, request);
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
                throw new AbortedUpdaterException(e, request);
            }
        }

        #endregion

        internal static class REST
        {
            internal static RESTEvaluator<T> GetEvaluator(RESTarMethods method)
            {
                #region Long running transactions test

                if (_UseLRT)
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
                var entities = DynSELECT(request);
                if (entities?.Any() != true)
                    return NoContent;
                var response = new Response();
                request.SetResponseData(entities, response);
                return response;
            }

            #region Using long running transactions

            private static Response LrPOST(RESTRequest<T> request)
            {
                request.Body = request.Body[0] == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null)
                    return LrSafePOST(request);
                int count;
                if (typeof(T) == typeof(DatabaseIndex))
                    count = INSERT(request);
                else
                {
                    var t = new Transaction();
                    try
                    {
                        count = t.Scope(() => INSERT(request));
                        t.Commit();
                    }
                    catch
                    {
                        t.Rollback();
                        throw;
                    }
                }
                return InsertedEntities(request, count, request.Resource.TargetType);
            }

            private static Response LrPATCH(RESTRequest<T> request)
            {
                var source = StatSELECT(request);
                if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                int count;
                if (typeof(T) == typeof(DatabaseIndex))
                    count = UPDATE(request, source);
                else
                {
                    var t = new Transaction();
                    try
                    {
                        count = t.Scope(() => UPDATE(request, source));
                        t.Commit();
                    }
                    catch
                    {
                        t.Rollback();
                        throw;
                    }
                }
                return UpdatedEntities(request, count, request.Resource.TargetType);
            }

            private static Response LrPUT(RESTRequest<T> request)
            {
                request.MetaConditions.Unsafe = false;
                var source = StatSELECT(request);
                if (source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                int count;
                if (source.Any())
                {
                    if (typeof(T) == typeof(DatabaseIndex))
                        count = UPDATE_ONE(request, source.First());
                    else
                    {
                        var t = new Transaction();
                        try
                        {
                            count = t.Scope(() => UPDATE_ONE(request, source.First()));
                            t.Commit();
                        }
                        catch
                        {
                            t.Rollback();
                            throw;
                        }
                    }
                    return UpdatedEntities(request, count, request.Resource.TargetType);
                }
                if (typeof(T) == typeof(DatabaseIndex))
                    count = INSERT_ONE(request);
                else
                {
                    var t = new Transaction();
                    try
                    {
                        count = t.Scope(() => INSERT_ONE(request));
                        t.Commit();
                    }
                    catch
                    {
                        t.Rollback();
                        throw;
                    }
                }
                return InsertedEntities(request, count, request.Resource.TargetType);
            }

            private static Response LrDELETE(RESTRequest<T> request)
            {
                var source = StatSELECT(request);
                if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                int count;
                if (typeof(T) == typeof(DatabaseIndex))
                {
                    count = DELETEop(request, source);
                }
                else
                {
                    var t = new Transaction();
                    try
                    {
                        count = t.Scope(() => DELETEop(request, source));
                        t.Commit();
                    }
                    catch
                    {
                        t.Rollback();
                        throw;
                    }
                }
                return DeleteEntities(count, request.Resource.TargetType);
            }

            private static Response LrSafePOST(RESTRequest<T> request)
            {
                var transaction = new Transaction();
                try
                {
                    var chains = request.MetaConditions.SafePost.Split(',')
                        .Select(k => request.Resource.MakePropertyChain(k, request.Resource.DynamicConditionsAllowed));
                    var conditions = chains.Select(chain => new Condition(chain, Operator.EQUALS, null)).ToList();
                    var innerRequest = new Request<T>();
                    var toInsert = new JArray();
                    var toUpdate = new List<(JObject json, T source)>();
                    foreach (var entity in request.Body.Deserialize<IEnumerable<JObject>>())
                    {
                        conditions.ForEach(cond => cond.SetValue(cond.PropertyChain.Evaluate(entity)));
                        innerRequest.Conditions.Clear();
                        innerRequest.Conditions.AddRange(conditions);
                        var results = innerRequest.GET();
                        if (results.MoreThanOne()) throw new AmbiguousMatchException(request.Resource);
                        if (results.Any()) toUpdate.Add((entity, results.First()));
                        else toInsert.Add(entity);
                    }
                    var insertedCount = 0;
                    var updatedCount = 0;
                    if (toInsert.Any())
                        insertedCount = typeof(T) == typeof(DatabaseIndex)
                            ? INSERT_JArray(innerRequest, toInsert)
                            : transaction.Scope(() => INSERT_JArray(innerRequest, toInsert));
                    if (toUpdate.Any())
                        updatedCount = typeof(T) == typeof(DatabaseIndex)
                            ? UPDATE_MANY(innerRequest, toUpdate)
                            : transaction.Scope(() => UPDATE_MANY(innerRequest, toUpdate));
                    transaction.Commit();
                    return SafePostedEntities(request, insertedCount, updatedCount);
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw new AbortedInserterException(e, request, e.Message);
                }
            }

            #endregion

            #region Using Transactions.Trans()

            private static Response POST(RESTRequest<T> request)
            {
                request.Body = request.Body[0] == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null)
                    return SafePOST(request);
                var count = typeof(T) == typeof(DatabaseIndex)
                    ? INSERT(request)
                    : Trans(() => INSERT(request));
                return InsertedEntities(request, count, request.Resource.TargetType);
            }

            private static Response PATCH(RESTRequest<T> request)
            {
                var source = StatSELECT(request);
                if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                var count = typeof(T) == typeof(DatabaseIndex)
                    ? UPDATE(request, source)
                    : Trans(() => UPDATE(request, source));
                return UpdatedEntities(request, count, request.Resource.TargetType);
            }

            private static Response PUT(RESTRequest<T> request)
            {
                request.MetaConditions.Unsafe = false;
                var source = StatSELECT(request);
                if (source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                int count;
                if (source.Any())
                {
                    count = typeof(T) == typeof(DatabaseIndex)
                        ? UPDATE_ONE(request, source.First())
                        : Trans(() => UPDATE_ONE(request, source.First()));
                    return UpdatedEntities(request, count, request.Resource.TargetType);
                }
                count = typeof(T) == typeof(DatabaseIndex)
                    ? INSERT_ONE(request)
                    : Trans(() => INSERT_ONE(request));
                return InsertedEntities(request, count, request.Resource.TargetType);
            }

            private static Response DELETE(RESTRequest<T> request)
            {
                var source = StatSELECT(request);
                if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                var count = typeof(T) == typeof(DatabaseIndex)
                    ? DELETEop(request, source)
                    : Trans(() => DELETEop(request, source));
                return DeleteEntities(count, request.Resource.TargetType);
            }

            private static Response SafePOST(RESTRequest<T> request)
            {
                var insertedCount = 0;
                var updatedCount = 0;
                try
                {
                    request.MetaConditions.Unsafe = false;
                    var chains = request.MetaConditions.SafePost
                        .Split(',')
                        .Select(k => request.Resource.MakePropertyChain(k, request.Resource.DynamicConditionsAllowed));
                    var conditions = chains.Select(chain => new Condition(chain, Operator.EQUALS, null)).ToList();
                    var innerRequest = new Request<T>();
                    foreach (var entity in request.Body.Deserialize<IEnumerable<JObject>>())
                    {
                        conditions.ForEach(cond => cond.SetValue(cond.PropertyChain.Evaluate(entity)));
                        innerRequest.Conditions.Clear();
                        innerRequest.Conditions.AddRange(conditions);
                        var results = innerRequest.GET();
                        if (!results.Any())
                        {
                            innerRequest.Body = entity.Serialize();
                            insertedCount += typeof(T) == typeof(DatabaseIndex)
                                ? INSERT_ONE(innerRequest)
                                : Trans(() => INSERT_ONE(innerRequest));
                        }
                        else if (results.MoreThanOne())
                            throw new AmbiguousMatchException(request.Resource);
                        else
                        {
                            var match = results.First();
                            innerRequest.Body = entity.Serialize();
                            updatedCount += typeof(T) == typeof(DatabaseIndex)
                                ? UPDATE_ONE(innerRequest, match)
                                : Trans(() => UPDATE_ONE(innerRequest, match));
                        }
                    }
                    return SafePostedEntities(request, insertedCount, updatedCount);
                }
                catch (Exception e)
                {
                    var message = $"Inserted {insertedCount} and updated {updatedCount} in resource " +
                                  $"'{request.Resource.Name}' using SafePOST before encountering the error. " +
                                  "These changes remain in the resource";
                    throw new AbortedInserterException(e, request, $"{e.Message} : {message}");
                }
            }

            #endregion
        }

        internal static class View
        {
            internal static int POST(ViewRequest<T> request) => typeof(T) == typeof(DatabaseIndex)
                ? INSERT_ONE(request)
                : Trans(() => INSERT_ONE(request));

            internal static int PATCH(ViewRequest<T> request, T item)
            {
                return typeof(T) == typeof(DatabaseIndex)
                    ? UPDATE_ONE(request, item)
                    : Trans(() => UPDATE_ONE(request, item));
            }

            internal static int DELETE(ViewRequest<T> request, T item) => typeof(T) == typeof(DatabaseIndex)
                ? DELETEop_ONE(request, item)
                : Trans(() => DELETEop_ONE(request, item));
        }

        internal static class App
        {
            internal static int POST(Func<T> inserter, Request<T> request)
            {
                return typeof(T) == typeof(DatabaseIndex)
                    ? INSERT_ONE(request, inserter)
                    : Trans(() => INSERT_ONE(request, inserter));
            }

            internal static int POST(Func<IEnumerable<T>> inserter, Request<T> request)
            {
                return typeof(T) == typeof(DatabaseIndex)
                    ? INSERT(request, inserter)
                    : Trans(() => INSERT(request, inserter));
            }

            internal static int PATCH(Func<T, T> updater, T source, Request<T> request)
            {
                return typeof(T) == typeof(DatabaseIndex)
                    ? UPDATE_ONE(request, updater, source)
                    : Trans(() => UPDATE_ONE(request, updater, source));
            }

            internal static int PATCH(Func<IEnumerable<T>, IEnumerable<T>> updater, IEnumerable<T> source,
                Request<T> request)
            {
                return typeof(T) == typeof(DatabaseIndex)
                    ? UPDATE(request, updater, source)
                    : Trans(() => UPDATE(request, updater, source));
            }

            internal static int PUT(Func<T> inserter, Func<T, T> updater, IEnumerable<T> source,
                Request<T> request)
            {
                if (!source.Any())
                    return typeof(T) == typeof(DatabaseIndex)
                        ? INSERT_ONE(request, inserter)
                        : Trans(() => INSERT_ONE(request, inserter));
                if (source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                return typeof(T) == typeof(DatabaseIndex)
                    ? UPDATE_ONE(request, updater, source.First())
                    : Trans(() => UPDATE_ONE(request, updater, source.First()));
            }

            internal static int DELETE(T item, Request<T> request) => typeof(T) == typeof(DatabaseIndex)
                ? DELETEop_ONE(request, item)
                : Trans(() => DELETEop_ONE(request, item));

            internal static int DELETE(IEnumerable<T> items, Request<T> request) =>
                typeof(T) == typeof(DatabaseIndex)
                    ? DELETEop(request, items)
                    : Trans(() => DELETEop(request, items));
        }
    }
}