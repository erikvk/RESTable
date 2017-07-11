using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar.Requests;
using Starcounter;
using static RESTar.Internal.Transactions;
using static RESTar.Operations.Do;
using static RESTar.Requests.Responses;
using static RESTar.Serializer;

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
                request.MetaConditions.Unsafe = true;
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
                request.MetaConditions.Unsafe = true;
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

        internal static int INSERT(IRequest<T> request)
        {
            IEnumerable<T> results = null;
            try
            {
                if (typeof(T) == typeof(DDictionary))
                {
                    var type = request.Resource.TargetType.MakeArrayType();
                    results = request.Body.DeserializeExplicit<IEnumerable<T>>(type);
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
                if (typeof(T) == typeof(DDictionary))
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
                switch (method)
                {
                    case RESTarMethods.GET: return GET;
                    case RESTarMethods.POST: return POST;
                    case RESTarMethods.PATCH: return PATCH;
                    case RESTarMethods.PUT: return PUT;
                    case RESTarMethods.DELETE: return DELETE;
                    default: throw new ArgumentOutOfRangeException(nameof(method), method, null);
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

            private static Response POST(RESTRequest<T> request)
            {
                request.Body = request.Body.First() == '[' ? request.Body : $"[{request.Body}]";
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
                var count = source.Any()
                    ? (typeof(T) == typeof(DatabaseIndex)
                        ? UPDATE_ONE(request, source.First())
                        : Trans(() => UPDATE_ONE(request, source.First())))
                    : (typeof(T) == typeof(DatabaseIndex)
                        ? INSERT_ONE(request)
                        : Trans(() => INSERT_ONE(request)));
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
                var source = request.Body.Deserialize<IEnumerable<JObject>>();
                var insertedCount = 0;
                var updatedCount = 0;
                var keys = request.MetaConditions.SafePost.Split(',');
                foreach (var entity in source)
                {
                    var jsonBytes = entity.Serialize().ToBytes();
                    Response response;
                    try
                    {
                        var valuePairs = keys.Select(k => $"{k}={entity.GetNoCase(k).ToString().UriEncode()}");
                        var uriString = $"/{request.Resource.Name}/" + $"{string.Join("&", valuePairs)}";
                        response = HTTP.InternalRequest
                        (
                            method: RESTarMethods.PUT,
                            relativeUri: new Uri(uriString, UriKind.Relative),
                            authToken: request.AuthToken,
                            bodyBytes: jsonBytes,
                            headers: new Dictionary<string, string> {["Authorization"] = request.AuthToken}
                        );
                    }
                    catch (InvalidOperationException)
                    {
                        throw new Exception("Error during safe post: Check Safepost parameter syntax");
                    }

                    if (response?.IsSuccessStatusCode != true)
                        throw new Exception("Error during safe post: " + (response?.StatusDescription ??
                                                                          "unknown error"));
                    if (response.StatusCode == 200)
                        updatedCount += 1;
                    else if (response.StatusCode == 201)
                        insertedCount += 1;
                }
                return SafePostedEntities(request, insertedCount, updatedCount);
            }
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