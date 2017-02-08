﻿using System;
using System.Linq;
using Starcounter;

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