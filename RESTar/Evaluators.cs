﻿using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Starcounter;
using static RESTar.Settings;

namespace RESTar
{
    internal static class Evaluators
    {
        internal static Response DELETE(Request request)
        {
            var entities = request.GetExtension();
            try
            {
                int count = request.Deleter((dynamic) entities, request);
                return Responses.DeleteEntities(count, request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedDeleterException(e.Message);
            }
        }

        internal static Response GET(Request request)
        {
            if (!request.Unsafe && request.Limit == -1)
                request.Limit = 1000;
            var entities = request.GetExtension(true);
            return !entities.Any() ? Responses.NoContent() : Responses.GetEntities(request, entities);
        }

        internal static Response PATCH(Request request)
        {
            var entities = request.GetExtension();
            try
            {
                foreach (var entity in entities)
                    Db.Transact(() => { Serializer.PopulateObject(request.Json, entity); });
                int count = request.Updater((dynamic) entities, request);
                return Responses.UpdatedEntities(request, count, request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e.Message);
            }
        }

        internal static Response POST(Request request)
        {
            try
            {
                var json = request.Json.First() == '[' ? request.Json : $"[{request.Json}]";
                if (request.SafePost != null)
                    return SafePOST(request, json);
                dynamic results = Db.Transact(() => json.Deserialize(RESTarConfig.IEnumTypes[request.Resource]));
                int count = request.Inserter(results, request);
                return Responses.InsertedEntities(request, count, request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedInserterException(e.Message);
            }
        }

        internal static Response PUT(Request request)
        {
            var entities = request.GetExtension(false);
            object obj;
            int count;
            if (!entities.Any())
            {
                try
                {
                    obj = Db.Transact(() => request.Json.Deserialize(request.Resource));
                    count = request.Inserter(obj.MakeList(request.Resource), request);
                    return Responses.InsertedEntities(request, count, request.Resource);
                }
                catch (Exception e)
                {
                    throw new AbortedInserterException(e.Message);
                }
            }
            try
            {
                obj = entities.First();
                Db.Transact(() => { Serializer.PopulateObject(request.Json, obj); });
                count = request.Updater(obj.MakeList(request.Resource), request);
                return Responses.UpdatedEntities(request, count, request.Resource);
            }
            catch (Exception e)
            {
                throw new AbortedUpdaterException(e.Message);
            }
        }

        internal static Response SafePOST(Request request, string json)
        {
            var inputEntities = json.DeserializeDyn();
            var insertedCount = 0;
            var updatedCount = 0;
            var keys = request.SafePost.Split(',');
            foreach (var entity in inputEntities)
            {
                var entityJson = Serializer.SerializeDyn(entity);
                Response response;
                try
                {
                    response = Self.PUT
                    (
                        port: _HttpPort,
                        uri: $"{_Uri}/{request.Resource.FullName}/" +
                             $"{string.Join("&", keys.Select(k => $"{k}={((IDictionary<string, dynamic>) entity).GetNoCase(k)}"))}",
                        body: entityJson
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
            return new Response
            {
                StatusCode = 200,
                StatusDescription = $"Inserted {insertedCount} and updated {updatedCount} entities " +
                                    $"in resource {request.Resource.FullName}"
            };
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