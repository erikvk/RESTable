using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Requests;
using RESTar.View;
using Starcounter;
using static RESTar.Operations.Do;
using static RESTar.Serializer;

namespace RESTar.Operations
{
    internal static class Evaluators<T> where T : class
    {
        internal static IEnumerable<dynamic> SELECT(IRequest<T> request)
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

        internal class REST
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

            internal static Response GET(RESTRequest<T> request)
            {
                try
                {
                    var entities = SELECT(request);
                    if (entities?.Any() != true)
                        return Responses.NoContent;
                    var response = new Response();
                    request.SetResponseData(entities, response);
                    return response;
                }
                catch (Exception e)
                {
                    throw new AbortedSelectorException(e, request);
                }
            }

            internal static Response POST(IRequest<T> request)
            {
                IEnumerable<T> results = null;
                try
                {
                    var json = request.Body.First() == '[' ? request.Body : $"[{request.Body}]";
                    if (request.MetaConditions.SafePost != null)
                        return SafePOST(request, json);
                    var count = 0;

                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                    {
                        results = json.Deserialize<IEnumerable<T>>();
                        count = request.Resource.Insert(results, request);
                        return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                    }

                    #endregion

                    Db.TransactAsync(() =>
                    {
                        results = json.Deserialize<IEnumerable<T>>();
                        if (request.Resource.RequiresValidation)
                            results.OfType<IValidatable>().ForEach(item => item.RunValidation());
                        count = request.Resource.Insert(results, request);
                    });
                    return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    Db.TransactAsync(() => results?.Where(i => i != null).ForEach(item => Try(item.Delete)));
                    throw new AbortedInserterException(e, request);
                }
            }

            private static Response SafePOST(IRequest<T> request, string json)
            {
                var source = json.Deserialize<IEnumerable<JObject>>();
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
                return Responses.SafePostedEntities(request, insertedCount, updatedCount);
            }

            internal static Response PATCH(IRequest<T> request)
            {
                try
                {
                    var source = request.Resource.Select(request);
                    if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                        throw new AmbiguousMatchException(request.Resource);
                    var count = 0;

                    #region Index

                    if (typeof(T) == typeof(DatabaseIndex))
                    {
                        source.ForEach(entity => Populate(request.Body, entity));
                        count = request.Resource.Update(source, request);
                        return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
                    }

                    #endregion

                    Db.TransactAsync(() => source.ForEach(entity => Populate(request.Body, entity)));
                    Db.TransactAsync(() =>
                    {
                        if (request.Resource.RequiresValidation)
                            source.OfType<IValidatable>().ForEach(item => item.RunValidation());
                        count = request.Resource.Update(source, request);
                    });
                    return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    throw new AbortedUpdaterException(e, request);
                }
            }

            internal static Response PUT(IRequest<T> request)
            {
                IEnumerable<T> source;
                try
                {
                    request.MetaConditions.Unsafe = false;
                    source = request.Resource.Select(request);
                    if (source.MoreThanOne())
                        throw new AmbiguousMatchException(request.Resource);
                }
                catch (Exception e)
                {
                    throw new AbortedSelectorException(e, request);
                }

                T obj = null;
                var count = 0;
                if (!source.Any())
                {
                    try
                    {
                        #region Index

                        if (typeof(T) == typeof(DatabaseIndex))
                        {
                            obj = request.Body.Deserialize<T>();
                            count = request.Resource.Insert(new[] {obj}, request);
                            return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                        }

                        #endregion

                        Db.TransactAsync(() =>
                        {
                            obj = request.Body.Deserialize<T>();
                            if (obj is IValidatable i) i.RunValidation();
                            count = request.Resource.Insert(new[] {obj}, request);
                        });
                        return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                    }
                    catch (Exception e)
                    {
                        Db.TransactAsync(() => Try(() => obj?.Delete()));
                        throw new AbortedInserterException(e, request);
                    }
                }
                try
                {
                    obj = source.First();

                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                    {
                        Populate(request.Body, obj);
                        count = request.Resource.Update(new[] {obj}, request);
                        return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
                    }

                    #endregion

                    Db.TransactAsync(() =>
                    {
                        Populate(request.Body, obj);
                        if (obj is IValidatable i) i.RunValidation();
                        count = request.Resource.Update(obj.MakeList(request.Resource.TargetType), request);
                    });
                    return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    throw new AbortedUpdaterException(e, request);
                }
            }

            internal static Response DELETE(IRequest<T> request)
            {
                try
                {
                    var count = 0;
                    var source = request.Resource.Select(request);
                    if (!request.MetaConditions.Unsafe && source.MoreThanOne())
                        throw new AmbiguousMatchException(request.Resource);

                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                    {
                        count = request.Resource.Delete(source, request);
                        return Responses.DeleteEntities(count, request.Resource.TargetType);
                    }

                    #endregion

                    Db.TransactAsync(() => count = request.Resource.Delete(source, request));
                    return Responses.DeleteEntities(count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    throw new AbortedDeleterException(e, request);
                }
            }
        }

        internal static class View
        {
            internal static Response VIEW(ViewRequest<T> request)
            {
                try
                {
                    if (request.MetaConditions.New)
                        return new Item().Populate(request, null);
                    var entities = SELECT(request);
                    if (request.IsSingular(entities))
                        return new Item().Populate(request, entities.First());
                    return new List().Populate(request, entities);
                }
                catch (NoHtmlException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new AbortedSelectorException(e, request);
                }
            }

            internal static int PATCH(object entity, string json, IRequest<T> request)
            {
                try
                {
                    var count = 0;

                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                    {
                        Populate(json, entity);
                        if (entity is IValidatable validatableResult)
                        {
                            if (!validatableResult.Validate(out string reason))
                                throw new ValidatableException(reason);
                        }
                        return request.Resource.Update(entity.MakeList(request.Resource.TargetType), request);
                    }

                    #endregion

                    Db.TransactAsync(() =>
                    {
                        Populate(json, entity);
                        if (entity is IValidatable validatableResult)
                        {
                            if (!validatableResult.Validate(out string reason))
                                throw new ValidatableException(reason);
                        }
                        count = request.Resource.Update(entity.MakeList(request.Resource.TargetType), request);
                    });
                    return count;
                }
                catch (Exception e)
                {
                    throw new AbortedUpdaterException(e, request);
                }
            }

            internal static int DELETE(object entity, IRequest<T> request)
            {
                try
                {
                    var count = 0;

                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                        return request.Resource.Delete(entity.MakeList(request.Resource.TargetType), request);

                    #endregion

                    Db.TransactAsync(() => count =
                        request.Resource.Delete(entity.MakeList(request.Resource.TargetType), request));
                    return count;
                }
                catch (Exception e)
                {
                    throw new AbortedDeleterException(e, request);
                }
            }

            internal static int POST(string json, IRequest<T> request)
            {
                object result = null;
                try
                {
                    var count = 0;

                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                    {
                        result = json.Deserialize(request.Resource.TargetType);
                        if (result is IValidatable validatableResult)
                        {
                            if (!validatableResult.Validate(out string reason))
                                throw new ValidatableException(reason);
                        }
                        return request.Resource.Insert(result.MakeList(request.Resource.TargetType), request);
                    }

                    #endregion

                    Db.TransactAsync(() =>
                    {
                        result = json.Deserialize(request.Resource.TargetType);
                        if (result is IValidatable validatableResult)
                        {
                            if (!validatableResult.Validate(out string reason))
                                throw new ValidatableException(reason);
                        }
                        count = request.Resource.Insert(result.MakeList(request.Resource.TargetType), request);
                    });
                    return count;
                }
                catch (Exception e)
                {
                    if (result != null)
                        Db.TransactAsync(() => Try(() => result.Delete()));
                    throw new AbortedInserterException(e, request);
                }
            }
        }
    }
}