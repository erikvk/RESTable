using System;
using System.Collections.Generic;
using System.Linq;
using Jil;
using Starcounter;
using static RESTar.Responses;
using static RESTar.Evaluators;
using Newtonsoft.Json;
using RESTar.TestDb;

namespace RESTar
{
    public static class RESTarConfig
    {
        internal static IList<Type> DbDomainList;
        internal static IDictionary<string, Type> DbDomainDict;
        internal static IDictionary<Type, Type> IEnumType;

        internal static RESTarMethods[] Methods =
        {
            RESTarMethods.GET,
            RESTarMethods.POST,
            RESTarMethods.PATCH,
            RESTarMethods.PUT,
            RESTarMethods.DELETE
        };

        /// <summary>
        /// Initiates the RESTar interface
        /// </summary>
        /// <param name="httpPort">The port that RESTar should listen on</param>
        /// <param name="baseUri">The URI that RESTar should listen on. E.g. '/rest'</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted</param>
        public static void Init
        (
            ushort httpPort,
            string baseUri = "/restar",
            bool prettyPrint = false
        )
        {
            if (baseUri.First() != '/')
                baseUri = $"/{baseUri}";

            foreach (var resource in DB.All<Resource>())
                Db.Transact(() => resource.Delete());

            var stopDelete = new Action(() =>
            {
                throw new InvalidOperationException("RESTar resources cannot be deleted during " +
                                                    "runtime");
            });

            Hook<Table>.BeforeDelete += (s, resource) => stopDelete();
            Hook<Settings>.BeforeDelete += (s, resource) => stopDelete();
            Hook<Help>.BeforeDelete += (s, resource) => stopDelete();

            DbDomainList = typeof(object)
                .GetSubclasses()
                .Where(t => t.HasAttribute<DatabaseAttribute>())
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .ToList();

            DbDomainDict = DbDomainList.ToDictionary(
                type => type.FullName.ToLower(),
                type => type
            );

            IEnumType = DbDomainList.ToDictionary(
                type => type,
                type => typeof(IEnumerable<>).MakeGenericType(type)
            );

            foreach (var type in DbDomainList)
            {
                Scheduling.ScheduleTask(() => Db.Transact(() => new Table(type)));

                Scheduling.ScheduleTask(() =>
                {
                    try
                    {
                        JSON.Deserialize("{}", type, Options.ISO8601IncludeInherited);
                    }
                    catch
                    {
                    }
                });

                Scheduling.ScheduleTask(() =>
                {
                    try
                    {
                        JSON.Deserialize("{}", type, Options.ISO8601PrettyPrintIncludeInherited);
                    }
                    catch
                    {
                    }
                });
            }

            Settings.Init(baseUri, prettyPrint, httpPort);
            TestDatabase.Init();

            baseUri += "{?}";

            Handle.GET(httpPort, baseUri, (Request request, string query) =>
                    Evaluate(request, query, GET, RESTarMethods.GET));

            Handle.POST(httpPort, baseUri, (Request request, string query) =>
                    Evaluate(request, query, POST, RESTarMethods.POST));

            Handle.PUT(httpPort, baseUri, (Request request, string query) =>
                    Evaluate(request, query, PUT, RESTarMethods.PUT));

            Handle.PATCH(httpPort, baseUri, (Request request, string query) =>
                    Evaluate(request, query, PATCH, RESTarMethods.PATCH));

            Handle.DELETE(httpPort, baseUri, (Request request, string query) =>
                    Evaluate(request, query, DELETE, RESTarMethods.DELETE));
        }

        private static Response Evaluate(Request request, string query, Func<Command, Response> Evaluator,
            RESTarMethods method)
        {
            try
            {
                var command = new Command(request, query, method);
                command.ResolveDataSource();
                if (command.Resource.BlockedMethods().Contains(method))
                    return BlockedMethod(method, command.Resource);
                return Evaluator(command);
            }
            catch (SqlException e)
            {
                return SemanticsError(e);
            }
            catch (SyntaxException e)
            {
                return SyntaxError(e);
            }
            catch (UnknownColumnException e)
            {
                return UnknownColumn(e);
            }
            catch (AmbiguousColumnException e)
            {
                return AmbiguousColumn(e);
            }
            catch (ExternalSourceException e)
            {
                return ExternalSourceError(e);
            }
            catch (UnknownResourceException e)
            {
                return UnknownResource(e);
            }
            catch (AmbiguousResourceException e)
            {
                return AmbiguousResource(e);
            }
            catch (InvalidInputCountException e)
            {
                return InvalidInputCount(e);
            }
            catch (AmbiguousMatchException e)
            {
                return AmbiguousMatch(e.Resource);
            }
            catch (ExcelInputException e)
            {
                return ExcelFormatError(e);
            }
            catch (ExcelFormatException e)
            {
                return ExcelFormatError(e);
            }
            catch (RESTarInternalException e)
            {
                return RESTarInternalError(e);
            }
            catch (JsonReaderException)
            {
                return DeserializationError(request.Body);
            }
            catch (DeserializationException)
            {
                return DeserializationError(request.Body);
            }
            catch (JsonSerializationException)
            {
                return DeserializationError(request.Body);
            }
            catch (Exception e)
            {
                if (e.InnerException is SqlException)
                    return SemanticsError((SqlException) e.InnerException);
                if (e.InnerException is DeserializationException)
                    return DeserializationError(e.Message);

                return UnknownError(e);
            }
        }
    }
}