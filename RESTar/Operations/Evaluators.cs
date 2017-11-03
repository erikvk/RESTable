﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Serialization;
using Starcounter;
using static RESTar.Operations.Do;
using static RESTar.Requests.Responses;
using static RESTar.Serialization.Serializer;
using static RESTar.Admin.Settings;

namespace RESTar.Operations
{
    internal static class Evaluators<T> where T : class
    {
        #region Operations

        #region SELECT, COUNT and PROFILE

        internal static IEnumerable<T> SELECT(IRequest<T> request)
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

        internal static IEnumerable<T> SELECT_FILTER(IRequest<T> request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = (Limit) 1000;
                return request.Resource.Select(request)?
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
                var results = request.Resource.Select(request);
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
                       ?? request.Resource.Select(request)?.LongCount()
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
                var updatedSource = source.Populate(request.Body.GetString());
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
                Populate(request.Body.GetString(), source);
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
            internal static Func<RESTRequest<T>, Response> GetEvaluator(Methods method)
            {
                #region Long running transactions test

                if (!_DontUseLRT)
                {
                    switch (method)
                    {
                        case Methods.GET: return GET;
                        case Methods.POST: return LrPOST;
                        case Methods.PATCH: return LrPATCH;
                        case Methods.PUT: return LrPUT;
                        case Methods.DELETE: return LrDELETE;
                        case Methods.COUNT: return COUNT;
                        default: return null;
                    }
                }

                #endregion

                switch (method)
                {
                    case Methods.GET: return GET;
                    case Methods.POST: return POST;
                    case Methods.PATCH: return PATCH;
                    case Methods.PUT: return PUT;
                    case Methods.DELETE: return DELETE;
                    case Methods.COUNT: return COUNT;
                    default: return null;
                }
            }

            private static Response GET(RESTRequest<T> request)
            {
                var results = SELECT_FILTER_PROCESS(request);
                if (results == null) return NoContent;
                try
                {
                    var (stream, hasContent, mimeType, extension) = default((MemoryStream, bool, string, string));
                    switch (request.Accept)
                    {
                        case MimeType.Json:
                            hasContent = results.GetJsonStream(out stream);
                            (mimeType, extension) = (MimeTypes.JSON, ".json");
                            break;
                        case MimeType.Excel:
                            hasContent = results.GetExcelStream(request.Resource, out stream);
                            (mimeType, extension) = (MimeTypes.Excel, ".xlsx");
                            break;
                        case MimeType.XML:
                            hasContent = results.GetXmlStream(out stream);
                            (mimeType, extension) = (MimeTypes.XML, ".xml");
                            break;
                    }
                    if (!hasContent) return NoContent;
                    var fileName = $"{request.Resource.AliasOrName}_{DateTime.Now:yyMMddHHmmssfff}{extension}";
                    stream.Seek(0, SeekOrigin.Begin);
                    return new Response
                    {
                        StreamedBody = stream,
                        ContentType = mimeType,
                        ContentLength = (int) stream.Length,
                        Headers = {["Content-Disposition"] = $"attachment; filename={fileName}"}
                    };
                }
                catch (Exception e)
                {
                    throw new AbortedSelectorException<T>(e, request);
                }
            }

            private static Response COUNT(RESTRequest<T> request)
            {
                var count = OP_COUNT(request);
                return request.EntityCount(count);
            }

            #region Using long running transactions

            private static Response LrPOST(RESTRequest<T> request)
            {
                //request.Body = request.Body[0] == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null) return LrSafePOST(request);
                return request.InsertedEntities(Transaction<T>.Transact(() => INSERT(request)));
            }

            private static Response LrPATCH(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                if (source?.Any() != true) return request.UpdatedEntities(0);
                if (!request.MetaConditions.Unsafe && source.Count > 1)
                    throw new AmbiguousMatchException(request.Resource);
                return request.UpdatedEntities(Transaction<T>.Transact(() => UPDATE(request, source)));
            }

            private static Response LrPUT(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0: return request.InsertedEntities(Transaction<T>.Transact(() => INSERT_ONE(request)));
                    case 1: return request.UpdatedEntities(Transaction<T>.Transact(() => UPDATE_ONE(request, source[0])));
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            private static Response LrDELETE(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request);
                if (source == null) return request.DeletedEntities(0);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return request.DeletedEntities(Transaction<T>.Transact(() => OP_DELETE(request, source)));
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
                        .Select(s => new Condition<T>(s, Operator.EQUALS, null))
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
                    return request.SafePostedEntities(updatedCount, insertedCount);
                }
                catch
                {
                    outerTrans.Rollback();
                    throw;
                }
            }

            #endregion

            #region Using Transactions.Trans()

            private static Response POST(RESTRequest<T> request)
            {
                //request.Body = request.Body[0] == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null) return SafePOST(request);
                return request.InsertedEntities(Transaction<T>.ShTransact(() => INSERTorTryDelete(request)));
            }

            private static Response PATCH(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                if (source?.Any() != true) return request.UpdatedEntities(0);
                if (!request.MetaConditions.Unsafe && source.Count > 1)
                    throw new AmbiguousMatchException(request.Resource);
                return request.UpdatedEntities(Transaction<T>.ShTransact(() => UPDATE(request, source)));
            }

            private static Response PUT(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request)?.ToList();
                switch (source?.Count)
                {
                    case null:
                    case 0: return request.InsertedEntities(Transaction<T>.ShTransact(() => INSERT_ONEorTryDelete(request)));
                    case 1: return request.UpdatedEntities(Transaction<T>.ShTransact(() => UPDATE_ONE(request, source[0])));
                    default: throw new AmbiguousMatchException(request.Resource);
                }
            }

            private static Response DELETE(RESTRequest<T> request)
            {
                var source = SELECT_FILTER(request);
                if (source == null) return request.DeletedEntities(0);
                if (!request.MetaConditions.Unsafe)
                {
                    var list = source.ToList();
                    if (list.Count > 1)
                        throw new AmbiguousMatchException(request.Resource);
                    source = list;
                }
                return request.DeletedEntities(Transaction<T>.ShTransact(() => OP_DELETE(request, source)));
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
                    return request.SafePostedEntities(updatedCount, insertedCount);
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