using System;
using System.Linq;
using System.Net;
using Starcounter;

namespace RESTar
{
    internal static class Responses
    {
        #region Not found

        internal static Response NoResource(UnknownResourceException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NotFound,
            StatusDescription = $"Could not locate a unique resource by '{e.SearchString}'. Candidates were: " +
                                $"{string.Join(", ", e.Candidates.Select(s => $"'{s}'"))}. Try qualifying the " +
                                $"resource locator further, e.g. from '{e.SearchString}' to '{e.Candidates.First()}'."
        };

        #endregion

        #region Bad request

        internal static Response SyntaxError(SyntaxException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = e.Message
        };

        internal static Response SemanticsError(SqlException e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"{e.Message}To enumerate available sub-resources (e.g. columns in a table) " +
                                $"for a RESTar resource R: GET {Settings._ResourcesPath}/RESTar.resource/name=R"
        };

        internal static Response DeserializationError(string json) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.BadRequest,
            StatusDescription = $"Error while deserializing JSON. Check JSON syntax. JSON: {json}"
        };

        internal static Response BlockedMethod(RESTarMethods method, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Forbidden,
            StatusDescription = $"{method} is blocked for resource '{resource.FullName}'. Available " +
                                $"methods: {resource.AvailableMethods().ToMethodsString()}"
        };

        #endregion

        #region Internal

        internal static Response UnknownError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = $"Unknown error: {e.Message}"
        };

        internal static Response RESTarInternalError(Exception e) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.InternalServerError,
            StatusDescription = $"Internal RESTar error: {e.Message}"
        };

        #endregion

        #region Ambiguous

        internal static Response AmbiguousMatch(Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Conflict,
            StatusDescription = $"Expected a uniquely matched entity in resource '{resource.FullName}' for this command, " +
                                "but matched multiple entities satisfying the given conditions. To enable manipulation of " +
                                "multiple matched entities (for commands that support this), add 'unsafe=true' to the " +
                                $"command's meta-conditions. GET: {Settings._Uri}/help/topic=unsafe for more info."
        };

        internal static Response AmbiguousPutMatch() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Conflict,
            StatusDescription = "Found multiple entities matching the given conditions in a PUT command."
        };

        #endregion

        #region Success responses

        internal static Response NoContent() => new Response
        {
            StatusCode = (ushort) HttpStatusCode.NoContent,
            StatusDescription = "No results found matching query"
        };

        internal static Response InsertedEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.Created,
            StatusDescription = $"Inserted {count} entities into the resource '{resource.FullName}'"
        };

        internal static Response UpdatedEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = $"Updated {count} entities in resource '{resource.FullName}'"
        };

        internal static Response DeleteEntities(int count, Type resource) => new Response
        {
            StatusCode = (ushort) HttpStatusCode.OK,
            StatusDescription = $"Deleted {count} entities from resource '{resource.FullName}'"
        };

        #endregion
    }
}