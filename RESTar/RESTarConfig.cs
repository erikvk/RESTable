using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        internal static IList<Type> ResourcesList;
        internal static IDictionary<string, Type> ResourcesDict;
        internal static IDictionary<Type, Type> IEnumTypes;

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

            foreach (var resource in DB.All<ScTable>())
                Db.Transact(() => resource.Delete());

            ResourcesList = typeof(object)
                .GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .ToList();

            var ScTableResources = ResourcesList.Where(t => t.HasAttribute<DatabaseAttribute>()).ToList();
            var VirtualResources = ResourcesList.Except(ScTableResources);

            CheckVirtualResources(VirtualResources);

            ResourcesDict = ResourcesList.ToDictionary(
                type => type.FullName.ToLower(),
                type => type
            );

            IEnumTypes = ResourcesList.ToDictionary(
                type => type,
                type => typeof(IEnumerable<>).MakeGenericType(type)
            );

            foreach (var type in ScTableResources)
            {
                Scheduling.ScheduleTask(() => Db.Transact(() => new ScTable(type)));

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
            Log.Init();

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
            Log.Info("==> RESTar command");
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

        private static void CheckVirtualResources(IEnumerable<Type> virtualResources)
        {
            var VirtualResourceMethodTemplates = new Func<Type, Dictionary<string, string>>
            (type => new Dictionary<string, string>
            {
                ["Get"] = $"public static IEnumerable<{type.FullName}> Get(IEnumerable<Condition> conditions) {{ }}",
                ["Insert"] = $"public static void Insert(IEnumerable<{type.FullName}> entities) {{ }}",
                ["Delete"] = $"public static void Delete(IEnumerable<{type.FullName}> entities) {{ }}"
            });

            var VirtualResourceMethodParameters = new Func<Type, Dictionary<string, Type>>
            (type => new Dictionary<string, Type>
            {
                ["Get"] = typeof(IEnumerable<Condition>),
                ["Insert"] = typeof(IEnumerable<>).MakeGenericType(type),
                ["Delete"] = typeof(IEnumerable<>).MakeGenericType(type)
            });

            var VirtualResourceMethodReturnTypes = new Func<Type, Dictionary<string, Type>>
            (type => new Dictionary<string, Type>
            {
                ["Get"] = typeof(IEnumerable<>).MakeGenericType(type),
                ["Insert"] = typeof(void),
                ["Delete"] = typeof(void)
            });

            Func<IEnumerable<RESTarMethods>, IEnumerable<string>> necessarytMethodDefs =
                restMethods => restMethods.SelectMany(method =>
                {
                    switch (method)
                    {
                        case RESTarMethods.GET:
                            return new[] {"Get"};
                        case RESTarMethods.POST:
                            return new[] {"Insert"};
                        case RESTarMethods.PUT:
                            return new[] {"Get", "Insert"};
                        case RESTarMethods.PATCH:
                            return new[] {"Get"};
                        case RESTarMethods.DELETE:
                            return new[] {"Get", "Delete"};
                    }
                    return null;
                }).Distinct();

            foreach (var type in virtualResources)
            {
                foreach (var requiredMethod in necessarytMethodDefs(type.AvailableMethods()))
                {
                    var method = type.GetMethod(requiredMethod, BindingFlags.Public | BindingFlags.Static);
                    if (method == null)
                    {
                        throw new VirtualResourceMissingMethodException
                        (
                            type,
                            $"Missing definition for '{requiredMethod}' according to template: " +
                            $"{VirtualResourceMethodTemplates(type)[requiredMethod]}"
                        );
                    }
                    if (method.ReturnType != VirtualResourceMethodReturnTypes(type)[requiredMethod])
                    {
                        throw new VirtualResourceSignatureException(
                            $"Wrong return type for method '{requiredMethod}'. Expected " +
                            $"" + VirtualResourceMethodReturnTypes(type)[requiredMethod]);
                    }
                    if (method.GetParameters().FirstOrDefault()?.ParameterType !=
                        VirtualResourceMethodParameters(type)[requiredMethod])
                    {
                        throw new VirtualResourceSignatureException(
                            $"Wrong parameter type for method '{requiredMethod}'. Expected " +
                            $"" + VirtualResourceMethodParameters(type)[requiredMethod]);
                    }
                }

                if (type.GetFields().Any())
                    throw new VirtualResourceMemberException("A virtual resource cannot include fields, " +
                                                             "only properties.");
            }
        }
    }
}