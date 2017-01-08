using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jil;
using Starcounter;
using static RESTar.Responses;
using static RESTar.RESTarMethods;
using static RESTar.RESTarOperations;
using ScRequest = Starcounter.Request;
using Newtonsoft.Json;

namespace RESTar
{
    public static class RESTarConfig
    {
        internal static List<Type> ResourcesList;
        internal static IDictionary<string, Type> ResourcesDict;
        internal static IDictionary<Type, Type> IEnumTypes;
        internal static Dictionary<Type, Dictionary<RESTarOperations, dynamic>> VrOperations;
        internal static Dictionary<RESTarMetaConditions, Type> MetaConditions;

        internal static readonly RESTarMethods[] Methods = {GET, POST, PATCH, PUT, DELETE};
        internal static readonly RESTarOperations[] Operations = {Select, Insert, Update, Delete};


        /// <summary>
        /// Initiates the RESTar interface
        /// </summary>
        /// <param name="publicPort">The main port that RESTar should listen on</param>
        /// <param name="privatePort">A private port that RESTar should accept private methods from</param>
        /// <param name="uri">The URI that RESTar should listen on. E.g. '/rest'</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted as default?
        ///  (can be changed in settings during runtime)</param>
        public static void Init
        (
            ushort publicPort = 8282,
            ushort privatePort = 8283,
            string uri = "/restar",
            bool prettyPrint = false
        )
        {
            if (uri.First() != '/')
                uri = $"/{uri}";

            foreach (var resource in DB.All<Resource>())
                Db.Transact(() => { resource.Delete(); });

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

            MetaConditions = Enum.GetNames(typeof(RESTarMetaConditions)).ToDictionary(
                name => (RESTarMetaConditions) Enum.Parse(typeof(RESTarMetaConditions), name),
                name => typeof(RESTarMetaConditions).GetField(name).GetAttribute<TypeAttribute>().Type
            );

            var StarcounterResources = ResourcesList.Where(t => t.HasAttribute<DatabaseAttribute>()).ToList();
            var VirtualResources = ResourcesList.Except(StarcounterResources).ToList();
            CheckVirtualResources(VirtualResources);
            global::Dynamit.DynamitConfig.Init();

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
                    catch (Exception)
                    {
                    }
                });
                Scheduling.ScheduleTask(() =>
                {
                    try
                    {
                        JSON.Deserialize("{}", resource, Options.ISO8601PrettyPrintIncludeInherited);
                    }
                    catch (Exception)
                    {
                    }
                });
            }

            Settings.Init(uri, prettyPrint, publicPort);
            Log.Init();
            uri += "{?}";
            Handle.GET(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.GET, GET));
            Handle.POST(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.POST, POST));
            Handle.PUT(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PUT, PUT));
            Handle.PATCH(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PATCH, PATCH));
            Handle.DELETE(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.DELETE, DELETE));
            if (privatePort == 0) return;
            Handle.GET(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.GET, Private_GET));
            Handle.POST(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.POST, Private_POST));
            Handle.PUT(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PUT, Private_PUT));
            Handle.PATCH(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PATCH, Private_PATCH));
            Handle.DELETE(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.DELETE, Private_DELETE));
        }

        private static Response Evaluate(ScRequest scRequest, string query, Func<Request, Response> Evaluator,
            RESTarMethods method)
        {
            Log.Info("==> RESTar command");
            try
            {
                var request = new Request(scRequest, query, method);
                var blockedMethod = MethodCheck(request);
                if (blockedMethod != null)
                    return BlockedMethod(blockedMethod.Value, request.Resource);
                request.ResolveDataSource();
                var response = Evaluator(request);
                request.SendResponse(response);
                return HandlerStatus.Handled;
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
            catch (CustomEntityUnknownColumnException e)
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
            catch (NoContentException)
            {
                return NoContent();
            }
            catch (JsonReaderException)
            {
                return DeserializationError(scRequest.Body);
            }
            catch (DeserializationException)
            {
                return DeserializationError(scRequest.Body);
            }
            catch (JsonSerializationException)
            {
                return DeserializationError(scRequest.Body);
            }
            catch (DbException e)
            {
                return DatabaseError(e);
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

        private static void CheckVirtualResources(ICollection<Type> virtualResources)
        {
            VrOperations = virtualResources.ToDictionary(type => type,
                type => Operations.ToDictionary(o => o, o => default(dynamic)));

            var VirtualResourceMethodTemplates = new Func<Type, Dictionary<RESTarOperations, string>>
            (type => new Dictionary<RESTarOperations, string>
            {
                [Select] = $"public static IEnumerable<{type.FullName}> Select(IRequest request) {{ }}",
                [Insert] = $"public static void Insert(IEnumerable<{type.FullName}> entities, IRequest request) {{ }}",
                [Update] = $"public static void Update(IEnumerable<{type.FullName}> entities, IRequest request) {{ }}",
                [Delete] = $"public static void Delete(IEnumerable<{type.FullName}> entities, IRequest request) {{ }}"
            });

            var VirtualResourceMethodParameters = new Func<Type, Dictionary<RESTarOperations, Type[]>>
            (type => new Dictionary<RESTarOperations, Type[]>
            {
                [Select] = new[] {typeof(IRequest)},
                [Insert] = new[] {typeof(IEnumerable<>).MakeGenericType(type), typeof(IRequest)},
                [Update] = new[] {typeof(IEnumerable<>).MakeGenericType(type), typeof(IRequest)},
                [Delete] = new[] {typeof(IEnumerable<>).MakeGenericType(type), typeof(IRequest)}
            });

            var VirtualResourceMethodReturnTypes = new Func<Type, Dictionary<RESTarOperations, Type>>
            (type => new Dictionary<RESTarOperations, Type>
            {
                [Select] = typeof(IEnumerable<>).MakeGenericType(type),
                [Insert] = typeof(void),
                [Update] = typeof(void),
                [Delete] = typeof(void)
            });

            Func<IEnumerable<RESTarMethods>, IEnumerable<RESTarOperations>> necessarytMethodDefs =
                restMethods => restMethods.SelectMany(method =>
                {
                    switch (method)
                    {
                        case GET:
                            return new[] {Select};
                        case POST:
                            return new[] {Insert};
                        case PUT:
                            return new[] {Select, Insert, Update};
                        case PATCH:
                            return new[] {Select, Update};
                        case DELETE:
                            return new[] {Select, Delete};
                        case Private_GET:
                            return new[] {Select};
                        case Private_POST:
                            return new[] {Insert};
                        case Private_PUT:
                            return new[] {Select, Insert, Update};
                        case Private_PATCH:
                            return new[] {Select, Update};
                        case Private_DELETE:
                            return new[] {Select, Delete};
                    }
                    return null;
                }).Distinct();

            foreach (var type in virtualResources)
            {
                foreach (var requiredMethod in necessarytMethodDefs(type.AvailableMethods()))
                {
                    var method = type.GetMethod(requiredMethod.ToString(), BindingFlags.Public | BindingFlags.Static);
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

                    if (!method.GetParameters().Select(i => i.ParameterType).SequenceEqual(
                        VirtualResourceMethodParameters(type)[requiredMethod]))
                    {
                        throw new VirtualResourceSignatureException(
                            $"Wrong parameter type(s) for method '{requiredMethod}'. Expected " +
                            string.Join(", ", VirtualResourceMethodParameters(type)[requiredMethod]
                                .Select(i => i.ToString())));
                    }

                    VrOperations[type][requiredMethod] = requiredMethod == Select
                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                            typeof(IEnumerable<>).MakeGenericType(type)))
                        : method.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<>)
                            .MakeGenericType(type), typeof(IRequest)));
                }

                if (type.GetFields(BindingFlags.Public | BindingFlags.Instance).Any())
                    throw new VirtualResourceMemberException(
                        "A virtual resource cannot include public instance fields, " +
                        "only properties.");
            }
        }

        private static RESTarMethods? MethodCheck(IRequest request)
        {
            var availableMethods = request.Resource.AvailableMethods();
            var method = request.Method;
            var publicParallel = PublicParallel(request.Method);
            if (!availableMethods.Contains(method) &&
                (publicParallel == null || !availableMethods.Contains(publicParallel.Value)))
                return method;
            return null;
        }

        internal static RESTarMethods? PublicParallel(RESTarMethods method)
        {
            var methodString = method.ToString();
            if (methodString.Contains("Private"))
            {
                RESTarMethods outMethod;
                Enum.TryParse(methodString.Split('_')[1], out outMethod);
                return outMethod;
            }
            return null;
        }
    }
}