using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jil;
using Starcounter;
using static RESTar.Responses;
using static RESTar.RESTarMethods;
using Newtonsoft.Json;

namespace RESTar
{
    public static class RESTarConfig
    {
        internal static IList<Type> ResourcesList;
        internal static IDictionary<string, Type> ResourcesDict;
        internal static IDictionary<Type, Type> IEnumTypes;
        internal static readonly RESTarMethods[] Methods = {GET, POST, PATCH, PUT, DELETE};

        /// <summary>
        /// Initiates the RESTar interface
        /// </summary>
        /// <param name="publicPort">The main port that RESTar should listen on</param>
        /// <param name="privatePort">A private port that RESTar should accept private methods from</param>
        /// <param name="baseUri">The URI that RESTar should listen on. E.g. '/rest'</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted as default?
        ///  (can be changed in settings during runtime)</param>
        public static void Init
        (
            ushort publicPort = 8282,
            ushort privatePort = 8283,
            string baseUri = "/restar",
            bool prettyPrint = false
        )
        {
            if (baseUri.First() != '/')
                baseUri = $"/{baseUri}";

            foreach (var resource in DB.All<Resource>())
                Db.Transact(() => resource.Delete());

            ResourcesList = typeof(object)
                .GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .ToList();

            var illegalResource = ResourcesList.FirstOrDefault(type => !type.HasAttribute<DatabaseAttribute>() &&
                                                                       !type.HasAttribute<VirtualResourceAttribute>());
            if (illegalResource != null)
                throw new InvalidResourceDefinitionException(
                    $"Invalid resource definition '{illegalResource.FullName}'. " +
                    $"A RESTar resource must either be declared a Starcounter " +
                    $"database type (using the Database attribute) or a Virtual " +
                    $"resource (using the VirtualResource attribute). For more info " +
                    $"see help article with topic 'virtual resources'");

            ResourcesDict = ResourcesList.ToDictionary(
                type => type.FullName.ToLower(),
                type => type
            );

            IEnumTypes = ResourcesList.ToDictionary(
                type => type,
                type => typeof(IEnumerable<>).MakeGenericType(type)
            );

            var StarcounterResources = ResourcesList.Where(t => t.HasAttribute<DatabaseAttribute>()).ToList();
            var VirtualResources = ResourcesList.Except(StarcounterResources).ToList();

            CheckVirtualResources(VirtualResources);

            foreach (var resource in VirtualResources)
                Scheduling.ScheduleTask(() => Db.Transact(() => new VirtualResource(resource)));

            foreach (var resource in StarcounterResources)
                Scheduling.ScheduleTask(() => Db.Transact(() => new Table(resource)));

            foreach (var resource in ResourcesList)
            {
                Scheduling.ScheduleTask(() =>
                {
                    try
                    {
                        JSON.Deserialize("{}", resource, Options.ISO8601IncludeInherited);
                    }
                    catch
                    {
                    }
                });
                Scheduling.ScheduleTask(() =>
                {
                    try
                    {
                        JSON.Deserialize("{}", resource, Options.ISO8601PrettyPrintIncludeInherited);
                    }
                    catch
                    {
                    }
                });
            }

            Settings.Init(baseUri, prettyPrint, publicPort);
            Log.Init();

            baseUri += "{?}";

            Handle.GET(publicPort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.GET, GET));
            Handle.POST(publicPort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.POST, POST));
            Handle.PUT(publicPort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.PUT, PUT));
            Handle.PATCH(publicPort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.PATCH, PATCH));
            Handle.DELETE(publicPort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.DELETE, DELETE));

            if (privatePort == 0) return;

            Handle.GET(privatePort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.GET, Private_GET));
            Handle.POST(privatePort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.POST, Private_POST));
            Handle.PUT(privatePort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.PUT, Private_PUT));
            Handle.PATCH(privatePort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.PATCH, Private_PATCH));
            Handle.DELETE(privatePort, baseUri, (Request r, string q) => Evaluate(r, q, Eval.DELETE, Private_DELETE));
        }

        private static Response Evaluate(Request request, string query, Func<Command, Response> Evaluator,
            RESTarMethods method)
        {
            Log.Info("==> RESTar command");
            try
            {
                var command = new Command(request, query, method);
                var availableMethods = command.Resource.AvailableMethods();
                if (!availableMethods.Contains(method) &&
                    !availableMethods.Contains(method - 5))
                {
                    return BlockedMethod(PublicVersion(method), command.Resource);
                }
                command.ResolveDataSource();
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
                        case GET:
                            return new[] {"Get"};
                        case POST:
                            return new[] {"Insert"};
                        case PUT:
                            return new[] {"Get", "Insert"};
                        case PATCH:
                            return new[] {"Get"};
                        case DELETE:
                            return new[] {"Get", "Delete"};
                        case Private_GET:
                            return new[] {"Get"};
                        case Private_POST:
                            return new[] {"Insert"};
                        case Private_PUT:
                            return new[] {"Get", "Insert"};
                        case Private_PATCH:
                            return new[] {"Get"};
                        case Private_DELETE:
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
                            VirtualResourceMethodTemplates(type)[requiredMethod]
                        );
                    }
                    if (method.ReturnType != VirtualResourceMethodReturnTypes(type)[requiredMethod])
                    {
                        throw new VirtualResourceSignatureException(
                            $"Wrong return type for method '{requiredMethod}'. Expected " +
                            VirtualResourceMethodReturnTypes(type)[requiredMethod]);
                    }
                    if (method.GetParameters().FirstOrDefault()?.ParameterType !=
                        VirtualResourceMethodParameters(type)[requiredMethod])
                    {
                        throw new VirtualResourceSignatureException(
                            $"Wrong parameter type for method '{requiredMethod}'. Expected " +
                            VirtualResourceMethodParameters(type)[requiredMethod]);
                    }
                }

                if (type.GetFields().Any())
                    throw new VirtualResourceMemberException("A virtual resource cannot include fields, " +
                                                             "only properties.");
            }
        }

        private static RESTarMethods PublicVersion(RESTarMethods method)
        {
            if (method >= Private_GET)
                return method - 5;
            return method;
        }
    }
}