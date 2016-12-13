using System.Collections.Generic;
using System.Linq;
using System.Net;
using Jil;
using Newtonsoft.Json;
using Starcounter;
using static RESTar.Responses;

namespace RESTar
{
    internal static class Eval
    {
        internal static Response DELETE(Command command)
        {
            var entities = command.GetExtension();
            foreach (var entity in entities)
                Db.Transact(() => { Db.Delete(entity); });
            return DeleteEntities(entities.Count(), command.Resource);
        }

        internal static Response GET(Command command)
        {
            if (!command.Unsafe && command.Limit == -1)
                command.Limit = 1000;
            var entities = command.GetExtension(true);
            return !entities.Any() ? NoContent() : GetEntities(command, entities);
        }

        internal static Response PATCH(Command command)
        {
            var entities = command.GetExtension();
            foreach (var entity in entities)
                Db.Transact(() => { JsonConvert.PopulateObject(command.Json, entity); });
            return UpdatedEntities(entities.Count(), command.Resource);
        }

        internal static Response POST(Command command)
        {
            var jsonTarget = command.Json.First() == '['
                ? RESTarConfig.IEnumTypes[command.Resource]
                : command.Resource;
            var results = Db.Transact(() => JSON.Deserialize(command.Json, jsonTarget));
            if (results is IEnumerable<object>)
                return InsertedEntities(((IEnumerable<object>) results).Count(), command.Resource);
            if (results != null)
                return InsertedEntities(1, command.Resource);
            return InsertedEntities(0, command.Resource);
        }

        internal static Response PUT(Command command)
        {
            var entities = command.GetExtension(false);
            object obj;
            if (!entities.Any())
            {
                obj = Db.Transact(() => JSON.Deserialize(command.Json, command.Resource));
                return new Response
                {
                    StatusCode = (ushort) HttpStatusCode.Created,
                    Body = obj.Serialize(command.Resource)
                };
            }
            obj = entities.First();
            Db.Transact(() => JsonConvert.PopulateObject(command.Json, obj));
            return new Response
            {
                StatusCode = (ushort) HttpStatusCode.OK,
                Body = obj.Serialize(command.Resource)
            };
        }
    }
}