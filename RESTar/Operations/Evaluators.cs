using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Requests;
using Starcounter;
using Request = RESTar.Requests.Request;

namespace RESTar.Operations
{
    internal static class Evaluators
    {
        internal static Response GET(Request request)
        {
            try
            {
                if (!request.MetaConditions.Unsafe && request.MetaConditions.Limit == -1)
                    request.MetaConditions.Limit = 1000;
                request.MetaConditions.Unsafe = true;
                var entities = request.Resource.Select(request)
                    .Process(request.MetaConditions.Add)
                    .Process(request.MetaConditions.Rename)
                    .Process(request.MetaConditions.Select)
                    .Filter(request.MetaConditions.OrderBy)
                    .Filter(request.MetaConditions.Limit);
                return entities?.Any() != true ? Responses.NoContent() : Responses.GetEntities(request, entities);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e);
            }
        }

        internal static Response POST(Request request)
        {
            try
            {
                var json = request.Body.First() == '[' ? request.Body : $"[{request.Body}]";
                if (request.MetaConditions.SafePost != null)
                    return SafePOST(request, json);
                dynamic results;
                var count = 0;
                request.Transaction.Scope(() =>
                {
                    results = json.Deserialize(RESTarConfig.IEnumTypes[request.Resource]);
                    foreach (var result in results)
                    {
                        var validatableResult = result as IValidatable;
                        if (validatableResult != null)
                        {
                            string reason;
                            if (!validatableResult.Validate(out reason))
                                throw new ValidatableException(reason);
                        }
                    }
                    count = request.Resource.Insert(results, request);
                });
                return Responses.InsertedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException(e);
            }
        }

        private static Response SafePOST(Request request, string json)
        {
            var inputEntities = json.DeserializeDyn();
            var insertedCount = 0;
            var updatedCount = 0;
            var keys = request.MetaConditions.SafePost.Split(',');
            foreach (IDictionary<string, dynamic> entity in inputEntities)
            {
                var jsonBytes = entity.SerializeDyn().ToBytes();
                Response response;
                try
                {
                    var valuePairs = keys.Select(k => $"{k}={((string) entity.GetNoCase(k).ToString()).UriEncode()}");
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

        internal static Response PATCH(Request request)
        {
            try
            {
                var entities = request.Resource.Select(request);
                if (!request.MetaConditions.Unsafe && entities.Count() > 1)
                    throw new AmbiguousMatchException(request.Resource);
                var count = 0;
                request.Transaction.Scope(() =>
                {
                    entities.ForEach(entity => JsonSerializer.PopulateObject(request.Body, entity));
                    foreach (var entity in entities)
                    {
                        var validatableResult = entity as IValidatable;
                        if (validatableResult != null)
                        {
                            string reason;
                            if (!validatableResult.Validate(out reason))
                                throw new ValidatableException(reason);
                        }
                    }
                    count = request.Resource.Update((dynamic) entities, request);
                });
                return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e);
            }
        }

        internal static Response PUT(Request request)
        {
            IEnumerable<dynamic> entities;

            try
            {
                request.MetaConditions.Unsafe = false;
                entities = request.Resource.Select(request);
                if (entities.Count() > 1)
                    throw new AmbiguousMatchException(request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedSelectorException(e);
            }

            object obj;
            var count = 0;
            if (!entities.Any())
            {
                try
                {
                    request.Transaction.Scope(() =>
                    {
                        obj = request.Body.Deserialize(request.Resource.TargetType);
                        var validatableResult = obj as IValidatable;
                        if (validatableResult != null)
                        {
                            string reason;
                            if (!validatableResult.Validate(out reason))
                                throw new ValidatableException(reason);
                        }
                        count = request.Resource.Insert(obj.MakeList(request.Resource.TargetType), request);
                    });
                    return Responses.InsertedEntities(request, count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    throw new AbortedInserterException(e);
                }
            }

            try
            {
                obj = entities.First();
                request.Transaction.Scope(() =>
                {
                    JsonSerializer.PopulateObject(request.Body, obj);
                    var validatableResult = obj as IValidatable;
                    if (validatableResult != null)
                    {
                        string reason;
                        if (!validatableResult.Validate(out reason))
                            throw new ValidatableException(reason);
                    }
                    count = request.Resource.Update(obj.MakeList(request.Resource.TargetType), request);
                });
                return Responses.UpdatedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e);
            }
        }

        internal static Response DELETE(Request request)
        {
            try
            {
                var count = 0;
                var entities = request.Resource.Select(request);
                if (!request.MetaConditions.Unsafe && entities.Count() > 1)
                    throw new AmbiguousMatchException(request.Resource);
                request.Transaction.Scope(() => count = request.Resource.Delete((dynamic) entities, request));
                return Responses.DeleteEntities(count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e);
            }
        }
    }
}