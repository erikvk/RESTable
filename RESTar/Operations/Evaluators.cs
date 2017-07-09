using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static class Operations
        {
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

            internal static int INSERT(IRequest<T> request)
            {
                IEnumerable<T> results = null;
                try
                {
                    results = request.Body.Deserialize<IEnumerable<T>>();
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

            internal static int DELETE(IRequest<T> request, IEnumerable<T> source)
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
                    result = request.Body.Deserialize<T>();
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

            internal static int DELETE_ONE(IRequest<T> request, T source)
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
        }

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
                var entities = Operations.DynSELECT(request);
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
                    ? Operations.INSERT(request)
                    : Trans(() => Operations.INSERT(request));
                return InsertedEntities(request, count, request.Resource.TargetType);
            }

            private static Response PATCH(RESTRequest<T> request)
            {
                var source = Operations.StatSELECT(request);
                if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                var count = typeof(T) == typeof(DatabaseIndex)
                    ? Operations.UPDATE(request, source)
                    : Trans(() => Operations.UPDATE(request, source));
                return UpdatedEntities(request, count, request.Resource.TargetType);
            }

            private static Response PUT(RESTRequest<T> request)
            {
                request.MetaConditions.Unsafe = false;
                var source = Operations.StatSELECT(request);
                if (source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                var count = source.Any()
                    ? (typeof(T) == typeof(DatabaseIndex)
                        ? Operations.UPDATE_ONE(request, source.First())
                        : Trans(() => Operations.UPDATE_ONE(request, source.First())))
                    : (typeof(T) == typeof(DatabaseIndex)
                        ? Operations.INSERT_ONE(request)
                        : Trans(() => Operations.INSERT_ONE(request)));
                return InsertedEntities(request, count, request.Resource.TargetType);
            }

            private static Response DELETE(RESTRequest<T> request)
            {
                var source = Operations.StatSELECT(request);
                if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                    throw new AmbiguousMatchException(request.Resource);
                var count = typeof(T) == typeof(DatabaseIndex)
                    ? Operations.DELETE(request, source)
                    : Trans(() => Operations.DELETE(request, source));
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
            internal static int PATCH(ViewRequest<T> request, T item)
            {
                return typeof(T) == typeof(DatabaseIndex)
                    ? Operations.UPDATE_ONE(request, item)
                    : Trans(() => Operations.UPDATE_ONE(request, item));
            }

            internal static int DELETE(ViewRequest<T> request, T item) => typeof(T) == typeof(DatabaseIndex)
                ? Operations.DELETE_ONE(request, item)
                : Trans(() => Operations.DELETE_ONE(request, item));

            internal static int POST(ViewRequest<T> request) => typeof(T) == typeof(DatabaseIndex)
                ? Operations.INSERT_ONE(request)
                : Trans(() => Operations.INSERT_ONE(request));
        }
    }
}