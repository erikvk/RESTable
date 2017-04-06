using System;
using System.Collections.Generic;
using System.Linq;
using Starcounter;
using static System.UriKind;
using static RESTar.Responses;
using static RESTar.Settings;

namespace RESTar
{
    internal static class Evaluators
    {
        internal static Response GET(Request request)
        {
            if (!request.Unsafe && request.Limit == -1)
                request.Limit = 1000;
            var entities = request.GetExtension(true);
            return !entities.Any() ? NoContent() : GetEntities(request, entities);
        }

        internal static Response POST(Request request)
        {
            try
            {
                var json = request.Json.First() == '[' ? request.Json : $"[{request.Json}]";
                if (request.SafePost != null)
                    return SafePOST(request, json);
                dynamic results = null;
                Db.TransactAsync(() => results = json.Deserialize(RESTarConfig.IEnumTypes[request.Resource]));
                int count = request.Resource.Insert(results, request);
                return InsertedEntities(request, count, request.Resource.TargetType);
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
            var keys = request.SafePost.Split(',');
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
                        relativeUri: new Uri(uriString, Relative),
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

        internal static Response PATCH(Request request)
        {
            var entities = request.GetExtension();
            try
            {
                foreach (var entity in entities)
                    Db.TransactAsync(() => Serializer.PopulateObject(request.Json, entity));
                int count = request.Resource.Update((dynamic) entities, request);
                return UpdatedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e);
            }
        }

        internal static Response PUT(Request request)
        {
            var entities = request.GetExtension(false);
            object obj = null;
            int count;
            if (!entities.Any())
            {
                try
                {
                    Db.TransactAsync(() => obj = request.Json.Deserialize(request.Resource.TargetType));
                    count = request.Resource.Insert(obj.MakeList(request.Resource.TargetType), request);
                    return InsertedEntities(request, count, request.Resource.TargetType);
                }
                catch (Exception e)
                {
                    throw new AbortedInserterException(e);
                }
            }
            try
            {
                obj = entities.First();
                Db.TransactAsync(() => Serializer.PopulateObject(request.Json, obj));
                count = request.Resource.Update(obj.MakeList(request.Resource.TargetType), request);
                return UpdatedEntities(request, count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e);
            }
        }

        internal static Response DELETE(Request request)
        {
            var entities = request.GetExtension(true);
            try
            {
                int count = request.Resource.Delete((dynamic) entities, request);
                return DeleteEntities(count, request.Resource.TargetType);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e);
            }
        }

        //        internal static Response MIGRATE(Request request)
        //        {
        //            if (request.Destination == null)
        //                throw new SyntaxException("Missing destination header in MIGRATE request");
        //            var method_uri = request.Destination.Split(new[] {' '}, 2);
        //            if (method_uri.Length == 1 || method_uri[0].ToUpper() != "IMPORT")
        //                throw new SyntaxException("MIGRATE destination must be of form 'IMPORT [URI]'");
        //
        //            var entities = request.GetExtension(true);
        //            if (!entities.Any())
        //                return NoContent();
        //            var outDict = entities.ToDictionary(
        //                e => DbHelper.GetObjectNo(e),
        //                e => e
        //            );
        //
        //            string jsonString;
        //            if (request.Select == null && request.Rename == null)
        //                jsonString = outDict.Serialize(typeof(IDictionary<,>).MakeGenericType(typeof(ulong),
        //                    RESTarConfig.IEnumTypes[request.Resource]));
        //            else jsonString = outDict.Serialize(typeof(IDictionary<ulong, dynamic>));
        //            return HTTP.Request("IMPORT", method_uri[1], jsonString);
        //        }

        //        internal static Response IMPORT(Request request)
        //        {
        //            return null;
        //        }
    }
}