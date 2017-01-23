using System.Collections.Generic;
using System.Linq;
using System.Net;
using Jil;
using Newtonsoft.Json;
using Starcounter;
using static RESTar.Responses;

namespace RESTar
{
    internal static class Evaluators
    {
        internal static Response DELETE(Request request)
        {
            var entities = request.GetExtension();
            var count = entities.Count();
            request.Deleter((dynamic) entities, request);
            return DeleteEntities(count, request.Resource);
        }

        internal static Response GET(Request request)
        {
            if (!request.Unsafe && request.Limit == -1)
                request.Limit = 1000;
            var entities = request.GetExtension(true);
            return !entities.Any() ? NoContent() : GetEntities(request, entities);
        }

        internal static Response PATCH(Request request)
        {
            var entities = request.GetExtension();
            foreach (var entity in entities)
                Db.Transact(() => { JsonConvert.PopulateObject(request.Json, entity, new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTime,
                   
                }); });
            request.Updater((dynamic) entities, request);
            return UpdatedEntities(entities.Count(), request.Resource);
        }

        internal static Response POST(Request request)
        {
            var json = request.Json.First() == '[' ? request.Json : $"[{request.Json}]";
            dynamic results = Db.Transact(() =>
                JSON.Deserialize(json, RESTarConfig.IEnumTypes[request.Resource], Options.ISO8601IncludeInherited));
            var count = Enumerable.Count(results);
            request.Inserter(results, request);
            return InsertedEntities(count, request.Resource);
        }

        internal static Response PUT(Request request)
        {
            var entities = request.GetExtension(false);
            object obj;
            if (!entities.Any())
            {
                obj = Db.Transact(() => JSON.Deserialize(
                    request.Json, request.Resource, Options.ISO8601IncludeInherited)
                );
                request.Inserter(obj.MakeList(request.Resource), request);
                return new Response
                {
                    StatusCode = (ushort) HttpStatusCode.Created,
                    Body = obj.SerializeDyn()
                };
            }
            obj = entities.First();
            Db.Transact(() => { JsonConvert.PopulateObject(request.Json, obj); });
            request.Updater(obj.MakeList(request.Resource), request);
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.OK,
                Body = obj.SerializeDyn()
            };
        }

        internal static Response MIGRATE(Request request)
        {
            if (request.Destination == null)
                throw new SyntaxException("Missing destination header in MIGRATE request");
            var method_uri = request.Destination.Split(new[] {' '}, 2);
            if (method_uri.Length == 1 || method_uri[0].ToUpper() != "IMPORT")
                throw new SyntaxException("MIGRATE destination must be of form 'IMPORT [URI]'");

            var entities = request.GetExtension(true);
            if (!entities.Any())
                return NoContent();
            var outDict = entities.ToDictionary(
                e => DbHelper.GetObjectNo(e),
                e => e
            );

            string jsonString;
            if (request.Select == null && request.Rename == null)
                jsonString = outDict.Serialize(typeof(IDictionary<,>).MakeGenericType(typeof(ulong),
                    RESTarConfig.IEnumTypes[request.Resource]));
            else jsonString = outDict.Serialize(typeof(IDictionary<ulong, dynamic>));
            return HTTP.Request("IMPORT", method_uri[1], jsonString);
        }

        internal static Response IMPORT(Request request)
        {
            return null;
        }
    }
}