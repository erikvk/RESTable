using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jil;
using Starcounter;
using Dynamit;
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
        internal static Dictionary<Type, Dictionary<RESTarOperations, dynamic>> ResourceOperations;
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
            string uri = "/rest",
            bool prettyPrint = true
        )
        {
            if (uri.First() != '/')
                uri = $"/{uri}";
            ResourcesList = typeof(object)
                .GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .Union(DynamitControl.DynamitTypes)
                .ToList();
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
            ResourceOperations = ResourcesList.ToDictionary(type => type,
                type => Operations.ToDictionary(o => o, o => default(dynamic)));
            DynamitConfig.Init();
            foreach (var resource in DB.All<Resource>().Where(r => !r.Editable))
                Db.Transact(() => { resource.Delete(); });
            foreach (var resource in ResourcesList.Where(t => !t.HasAttribute<DynamicTableAttribute>()))
                Db.Transact(() => { new Resource {Type = resource}; });
            CheckOperations(ResourcesList);
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

        private static Response Evaluate(ScRequest scRequest, string query, Func<Request, Response> evaluator,
            RESTarMethods method)
        {
            Log.Info("==> RESTar request");
            try
            {
                var request = new Request(scRequest, query, method, evaluator);
                var blockedMethod = MethodCheck(request);
                if (blockedMethod != null)
                    return BlockedMethod(blockedMethod.Value, request.Resource);
                request.ResolveDataSource();
                var response = request.Evaluator(request);
                request.SendResponse(response);
                return HandlerStatus.Handled;
            }
            catch (DeserializationException e)
            {
                if (e.InnerException != null)
                    return BadRequest(e.InnerException);
                return DeserializationError(scRequest.Body);
            }
            catch (JsonSerializationException e)
            {
                if (e.InnerException != null)
                    return BadRequest(e.InnerException);
                return DeserializationError(scRequest.Body);
            }
            catch (SqlException e)
            {
                return SemanticsError(e);
            }
            catch (SyntaxException e)
            {
                return BadRequest(e);
            }
            catch (UnknownColumnException e)
            {
                return NotFound(e);
            }
            catch (CustomEntityUnknownColumnException e)
            {
                return NotFound(e);
            }
            catch (AmbiguousColumnException e)
            {
                return AmbiguousColumn(e);
            }
            catch (ExternalSourceException e)
            {
                return BadRequest(e);
            }
            catch (UnknownResourceException e)
            {
                return NotFound(e);
            }
            catch (UnknownResourceForMappingException e)
            {
                return NotFound(e);
            }
            catch (AmbiguousResourceException e)
            {
                return AmbiguousResource(e);
            }
            catch (InvalidInputCountException e)
            {
                return BadRequest(e);
            }
            catch (AmbiguousMatchException e)
            {
                return AmbiguousMatch(e.Resource);
            }
            catch (ExcelInputException e)
            {
                return BadRequest(e);
            }
            catch (ExcelFormatException e)
            {
                return BadRequest(e);
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
            catch (DbException e)
            {
                return DatabaseError(e);
            }
            catch (AbortedSelectorException e)
            {
                return BadRequest(e);
            }
            catch (AbortedInserterException e)
            {
                return BadRequest(e);
            }
            catch (AbortedUpdaterException e)
            {
                return BadRequest(e);
            }
            catch (AbortedDeleterException e)
            {
                return BadRequest(e);
            }
            catch (Exception e)
            {
                return InternalError(e);
            }
        }

        private static void CheckOperations(IEnumerable<Type> resources)
        {
            foreach (var resource in resources)
            {
                if (resource.IsSubclassOf(typeof(DDictionary)))
                {
                    foreach (var operation in Operations)
                    {
                        var method = typeof(DDictionaryOperations).GetMethod(operation.ToString(),
                            BindingFlags.Public | BindingFlags.Instance);
                        ResourceOperations[resource][operation] = operation == Select
                            ? method.CreateDelegate(typeof(Func<,>)
                                .MakeGenericType(typeof(IRequest), typeof(IEnumerable<DDictionary>)), null)
                            : method.CreateDelegate(typeof(Action<,>)
                                .MakeGenericType(typeof(IEnumerable<DDictionary>), typeof(IRequest)), null);
                    }
                }
                else if (!resource.HasAttribute<DatabaseAttribute>())
                    CheckVirtualResource(resource);
                else
                {
                    var operationsProvider = typeof(StarcounterOperations);
                    foreach (var operation in Operations)
                    {
                        var overrideMethod = resource.GetMethod(operation.ToString(),
                            BindingFlags.Public | BindingFlags.Instance);
                        var baseMethod = operationsProvider.GetMethod(operation.ToString(),
                            BindingFlags.Public | BindingFlags.Instance);
                        if (LocalizedInterface(operation, resource).IsAssignableFrom(resource))
                            ResourceOperations[resource][operation] = operation == Select
                                ? overrideMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                    typeof(IEnumerable<>).MakeGenericType(resource)), null)
                                : overrideMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<>)
                                    .MakeGenericType(resource), typeof(IRequest)), null);
                        else if (LocalizedInterface(operation, typeof(object)).IsAssignableFrom(resource))
                            ResourceOperations[resource][operation] = operation == Select
                                ? overrideMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                    typeof(IEnumerable<object>)), null)
                                : overrideMethod.CreateDelegate(
                                    typeof(Action<,>).MakeGenericType(typeof(IEnumerable<object>),
                                        typeof(IRequest)), null);
                        else
                            ResourceOperations[resource][operation] = operation == Select
                                ? baseMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                    typeof(IEnumerable<object>)), null)
                                : baseMethod.CreateDelegate(
                                    typeof(Action<,>).MakeGenericType(typeof(IEnumerable<object>),
                                        typeof(IRequest)), null);
                    }
                }
            }
        }

        private static Type LocalizedInterface(RESTarOperations operation, Type type)
        {
            switch (operation)
            {
                case Select:
                    return typeof(ISelector<>).MakeGenericType(type);
                case Insert:
                    return typeof(IInserter<>).MakeGenericType(type);
                case Update:
                    return typeof(IUpdater<>).MakeGenericType(type);
                case Delete:
                    return typeof(IDeleter<>).MakeGenericType(type);
            }
            return null;
        }

        private static IEnumerable<RESTarOperations> NecessaryOpDefs(IEnumerable<RESTarMethods> restMethods)
        {
            return restMethods.SelectMany(method =>
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
        }

        private static void CheckVirtualResource(Type resource)
        {
            foreach (var requiredMethod in NecessaryOpDefs(resource.AvailableMethods()))
            {
                var method = resource.GetMethod(requiredMethod.ToString(), BindingFlags.Public | BindingFlags.Instance);
                if (LocalizedInterface(requiredMethod, resource).IsAssignableFrom(resource))
                    ResourceOperations[resource][requiredMethod] = requiredMethod == Select
                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                            typeof(IEnumerable<>).MakeGenericType(resource)), null)
                        : method.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<>)
                            .MakeGenericType(resource), typeof(IRequest)), null);
                else if (LocalizedInterface(requiredMethod, typeof(object)).IsAssignableFrom(resource))
                    ResourceOperations[resource][requiredMethod] = requiredMethod == Select
                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                            typeof(IEnumerable<object>)), null)
                        : method.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<object>),
                            typeof(IRequest)), null);
                else
                {
                    string missingInterface;
                    switch (requiredMethod)
                    {
                        case Select:
                            missingInterface = $"ISelector<{resource.FullName}> or ISelector<object>";
                            break;
                        case Insert:
                            missingInterface = $"IInserter<{resource.FullName}> or Inserter<object>";
                            break;
                        case Update:
                            missingInterface = $"IUpdater<{resource.FullName}> or IUpdater<object>";
                            break;
                        case Delete:
                            missingInterface = $"IDeleter<{resource.FullName}> or IDeleter<object>";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    throw new VirtualResourceMissingInterfaceImplementation(resource, missingInterface);
                }
            }
            var fields = resource.GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Any())
                throw new VirtualResourceMemberException(
                    $"A virtual resource cannot include public instance fields, " +
                    $"only properties. Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{resource.FullName}'"
                );
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