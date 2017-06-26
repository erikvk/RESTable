using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Requests;
using RESTar.View;
using Starcounter;

namespace RESTar.Operations
{
    internal static class Evaluators
    {
        internal static IEnumerable<T> StaticSELECT<T>(IRequest request)
        {
            if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                request.MetaConditions.Limit = 1000;
            request.MetaConditions.Unsafe = true;
            return (IEnumerable<T>) request.Resource
                .Select(request)?
                .Filter(request.MetaConditions.OrderBy)
                .Filter(request.MetaConditions.Limit);
        }

        internal static IEnumerable<dynamic> SELECT(IRequest request)
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

        #region REST

        internal static Response GET(Requests.HttpRequest request)
        {
            try
            {
                var entities = SELECT(request);
                if (entities?.Any() != true)
                    return Responses.NoContent();
                var response = new Response();
                request.SetResponseData(entities, response);
                return response;
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, request);
            }
        }

        internal static Response POST(Requests.HttpRequest request)
        {
            dynamic results = null;
            try
            {
                var json = request.Body.First() == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null)
                    return SafePOST(request, json);
                var count = 0;

                #region Index

                if (request.Resource.TargetType == typeof(DatabaseIndex))
                {
                    results = json.Deserialize(RESTarConfig.IEnumTypes[request.Resource]);
                    count = request.Resource.Insert(results, request);
                    return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                }

                #endregion

                Db.TransactAsync(() =>
                {
                    results = json.Deserialize(RESTarConfig.IEnumTypes[request.Resource]);
                    if (request.Resource.RequiresValidation)
                    {
                        foreach (var result in results)
                        {
                            var validatableResult = result as IValidatable;
                            if (validatableResult != null)
                            {
                                if (!validatableResult.Validate(out string reason))
                                    throw new ValidatableException(reason);
                            }
                        }
                    }
                    count = request.Resource.Insert(results, request);
                });
                return Responses.InsertedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                if (results != null)
                    Db.TransactAsync(() =>
                    {
                        foreach (var item in results)
                            if (item != null)
                                Do.Try(() => Db.Delete(item));
                    });
                throw new AbortedInserterException(e, request);
            }
        }

        private static Response SafePOST(Requests.HttpRequest request, string json)
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

        internal static Response PATCH(Requests.HttpRequest request)
        {
            try
            {
                var source = request.Resource.Select(request);
                if (!request.MetaConditions.Unsafe && source.Count() > 1)
                    throw new AmbiguousMatchException(request.Resource);
                var count = 0;

                #region Index

                if (request.Resource.TargetType == typeof(DatabaseIndex))
                {
                    foreach (var entity in source)
                        Serializer.Populate(request.Body, entity);
                    count = request.Resource.Update((dynamic) source, request);
                    return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
                }

                #endregion

                Db.TransactAsync(() =>
                {
                    foreach (var entity in source)
                        Serializer.Populate(request.Body, entity);
                });
                Db.TransactAsync(() =>
                {
                    if (request.Resource.RequiresValidation)
                    {
                        foreach (var entity in source)
                        {
                            var validatableResult = entity as IValidatable;
                            if (validatableResult != null)
                            {
                                if (!validatableResult.Validate(out string reason))
                                    throw new ValidatableException(reason);
                            }
                        }
                    }
                    count = request.Resource.Update((dynamic) source, request);
                });
                return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e, request);
            }
        }

        internal static Response PUT(Requests.HttpRequest request)
        {
            IEnumerable<dynamic> source;
            try
            {
                request.MetaConditions.Unsafe = false;
                source = request.Resource.Select(request);
                if (source.Count() > 1)
                    throw new AmbiguousMatchException(request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e, request);
            }

            object obj = null;
            var count = 0;
            if (!source.Any())
            {
                try
                {
                    #region Index

                    if (request.Resource.TargetType == typeof(DatabaseIndex))
                    {
                        obj = request.Body.Deserialize(request.Resource.TargetType);
                        count = request.Resource.Insert(obj.MakeList(request.Resource.TargetType), request);
                        return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                    }

                    #endregion


                    Db.TransactAsync(() =>
                    {
                        obj = request.Body.Deserialize(request.Resource.TargetType);
                        if (obj is IValidatable validatableResult)
                        {
                            if (!validatableResult.Validate(out string reason))
                                throw new ValidatableException(reason);
                        }
                        count = request.Resource.Insert(obj.MakeList(request.Resource.TargetType), request);
                    });
                    return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    Db.TransactAsync(() => Do.Try(() => obj?.Delete()));
                    throw new AbortedInserterException(e, request);
                }
            }
            try
            {
                obj = source.First();

                #region Index

                if (request.Resource.TargetType == typeof(DatabaseIndex))
                {
                    Serializer.Populate(request.Body, obj);
                    count = request.Resource.Update(obj.MakeList(request.Resource.TargetType), request);
                    return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
                }

                #endregion

                Db.TransactAsync(() =>
                {
                    Serializer.Populate(request.Body, obj);
                    if (obj is IValidatable validatableResult)
                    {
                        if (!validatableResult.Validate(out string reason))
                            throw new ValidatableException(reason);
                    }
                    count = request.Resource.Update(obj.MakeList(request.Resource.TargetType), request);
                });
                return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e, request);
            }
        }

        internal static Response DELETE(Requests.HttpRequest request)
        {
            try
            {
                var count = 0;
                var source = request.Resource.Select(request);
                if (!request.MetaConditions.Unsafe && source.Count() > 1)
                    throw new AmbiguousMatchException(request.Resource);

                #region Index

                if (request.Resource.TargetType == typeof(DatabaseIndex))
                {
                    count = request.Resource.Delete((dynamic) source, request);
                    return Responses.DeleteEntities(count, request.Resource.TargetType);
                }

                #endregion

                Db.TransactAsync(() => count = request.Resource.Delete((dynamic) source, request));
                return Responses.DeleteEntities(count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e, request);
            }
        }

        #endregion

        #region VIEW

        internal static Response VIEW(Requests.HttpRequest request)
        {
            try
            {
                if (request.MetaConditions.New)
                    return new Item().Populate(request, null);
                var entities = SELECT(request);
                if (entities?.IsSingular(request) == true)
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

        internal static int PATCH(object entity, string json, Requests.HttpRequest request)
        {
            try
            {
                var count = 0;

                #region Index

                if (request.Resource.TargetType == typeof(DatabaseIndex))
                {
                    Serializer.Populate(json, entity);
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
                    Serializer.Populate(json, entity);
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

        internal static int DELETE(object entity, Requests.HttpRequest request)
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

        internal static int POST(string json, Requests.HttpRequest request)
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
                    Db.TransactAsync(() => Do.Try(() => result.Delete()));
                throw new AbortedInserterException(e, request);
            }
        }

        #endregion
    }
}