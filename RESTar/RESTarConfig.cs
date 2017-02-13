using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dynamit;
using Jil;
using Newtonsoft.Json;
using Starcounter;
using ScRequest = Starcounter.Request;

namespace RESTar
{
    public static class RESTarConfig
    {
        internal static List<Type> ResourcesList;
        internal static IDictionary<string, Type> ResourcesDict;
        internal static IDictionary<Type, Type> IEnumTypes;
        internal static Dictionary<Type, Dictionary<RESTarOperations, dynamic>> ResourceOperations;
        internal static Dictionary<RESTarMetaConditions, Type> MetaConditions;

        internal static readonly RESTarMethods[] Methods = {RESTarMethods.GET, RESTarMethods.POST, RESTarMethods.PATCH, RESTarMethods.PUT, RESTarMethods.DELETE};
        internal static readonly RESTarOperations[] Operations = {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update, RESTarOperations.Delete};

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
            bool prettyPrint = true,
            bool camelCase = false
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

            Settings.Init(uri, prettyPrint, camelCase, publicPort);
            Log.Init();
            uri += "{?}";
            Handle.GET(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.GET, RESTarMethods.GET));
            Handle.POST(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.POST, RESTarMethods.POST));
            Handle.PUT(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PUT, RESTarMethods.PUT));
            Handle.PATCH(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PATCH, RESTarMethods.PATCH));
            Handle.DELETE(publicPort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.DELETE, RESTarMethods.DELETE));
            if (privatePort == 0) return;
            Handle.GET(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.GET, RESTarMethods.Private_GET));
            Handle.POST(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.POST, RESTarMethods.Private_POST));
            Handle.PUT(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PUT, RESTarMethods.Private_PUT));
            Handle.PATCH(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.PATCH, RESTarMethods.Private_PATCH));
            Handle.DELETE(privatePort, uri, (ScRequest r, string q) => Evaluate(r, q, Evaluators.DELETE, RESTarMethods.Private_DELETE));
        }

        private static Response Evaluate(ScRequest scRequest, string query, Func<Request, Response> evaluator,
            RESTarMethods method)
        {
            Log.Info("==> RESTar request");
            Request request = null;
            try
            {
                request = new Request(scRequest, query, method, evaluator);
                request.ResolveMethod();
                var blockedMethod = MethodCheck(request);
                if (blockedMethod != null)
                    return Responses.BlockedMethod(blockedMethod.Value, request.Resource);
                request.ResolveDataSource();
                var response = request.Evaluator(request);
                request.SendResponse(response);
                return HandlerStatus.Handled;
            }
            catch (DeserializationException e)
            {
                if (e.InnerException != null)
                    return Responses.BadRequest(e.InnerException);
                return Responses.DeserializationError(scRequest.Body);
            }
            catch (JsonSerializationException e)
            {
                if (e.InnerException != null)
                    return Responses.BadRequest(e.InnerException);
                return Responses.DeserializationError(scRequest.Body);
            }
            catch (SqlException e)
            {
                return Responses.SemanticsError(e);
            }
            catch (SyntaxException e)
            {
                return Responses.BadRequest(e);
            }
            catch (UnknownColumnException e)
            {
                return Responses.NotFound(e);
            }
            catch (CustomEntityUnknownColumnException e)
            {
                return Responses.NotFound(e);
            }
            catch (AmbiguousColumnException e)
            {
                return Responses.AmbiguousColumn(e);
            }
            catch (ExternalSourceException e)
            {
                return Responses.BadRequest(e);
            }
            catch (UnknownResourceException e)
            {
                return Responses.NotFound(e);
            }
            catch (UnknownResourceForMappingException e)
            {
                return Responses.NotFound(e);
            }
            catch (AmbiguousResourceException e)
            {
                return Responses.AmbiguousResource(e);
            }
            catch (InvalidInputCountException e)
            {
                return Responses.BadRequest(e);
            }
            catch (AmbiguousMatchException e)
            {
                return Responses.AmbiguousMatch(e.Resource);
            }
            catch (ExcelInputException e)
            {
                return Responses.BadRequest(e);
            }
            catch (ExcelFormatException e)
            {
                return Responses.BadRequest(e);
            }
            catch (RESTarInternalException e)
            {
                return Responses.RESTarInternalError(e);
            }
            catch (NoContentException)
            {
                return Responses.NoContent();
            }
            catch (JsonReaderException)
            {
                return Responses.DeserializationError(scRequest.Body);
            }
            catch (DbException e)
            {
                return Responses.DatabaseError(e);
            }
            catch (AbortedSelectorException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource);
            }
            catch (AbortedInserterException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource);
            }
            catch (AbortedUpdaterException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource);
            }
            catch (AbortedDeleterException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource);
            }
            catch (Exception e)
            {
                return Responses.InternalError(e);
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
                        ResourceOperations[resource][operation] = operation == RESTarOperations.Select
                            ? method.CreateDelegate(typeof(Func<,>)
                                .MakeGenericType(typeof(IRequest), typeof(IEnumerable<DDictionary>)), null)
                            : method.CreateDelegate(typeof(Func<,,>)
                                .MakeGenericType(typeof(IEnumerable<DDictionary>), typeof(IRequest), typeof(int)), null);
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
                            ResourceOperations[resource][operation] = operation == RESTarOperations.Select
                                ? overrideMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                    typeof(IEnumerable<>).MakeGenericType(resource)), null)
                                : overrideMethod.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<>)
                                    .MakeGenericType(resource), typeof(IRequest), typeof(int)), null);
                        else if (LocalizedInterface(operation, typeof(object)).IsAssignableFrom(resource))
                            ResourceOperations[resource][operation] = operation == RESTarOperations.Select
                                ? overrideMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                    typeof(IEnumerable<object>)), null)
                                : overrideMethod.CreateDelegate(
                                    typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<object>),
                                        typeof(IRequest), typeof(int)), null);
                        else
                            ResourceOperations[resource][operation] = operation == RESTarOperations.Select
                                ? baseMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                    typeof(IEnumerable<object>)), null)
                                : baseMethod.CreateDelegate(
                                    typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<object>),
                                        typeof(IRequest), typeof(int)), null);
                    }
                }
            }
        }

        private static Type LocalizedInterface(RESTarOperations operation, Type type)
        {
            switch (operation)
            {
                case RESTarOperations.Select:
                    return typeof(ISelector<>).MakeGenericType(type);
                case RESTarOperations.Insert:
                    return typeof(IInserter<>).MakeGenericType(type);
                case RESTarOperations.Update:
                    return typeof(IUpdater<>).MakeGenericType(type);
                case RESTarOperations.Delete:
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
                    case RESTarMethods.GET:
                        return new[] {RESTarOperations.Select};
                    case RESTarMethods.POST:
                        return new[] {RESTarOperations.Insert};
                    case RESTarMethods.PUT:
                        return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                    case RESTarMethods.PATCH:
                        return new[] {RESTarOperations.Select, RESTarOperations.Update};
                    case RESTarMethods.DELETE:
                        return new[] {RESTarOperations.Select, RESTarOperations.Delete};
                    case RESTarMethods.Private_GET:
                        return new[] {RESTarOperations.Select};
                    case RESTarMethods.Private_POST:
                        return new[] {RESTarOperations.Insert};
                    case RESTarMethods.Private_PUT:
                        return new[] {RESTarOperations.Select, RESTarOperations.Insert, RESTarOperations.Update};
                    case RESTarMethods.Private_PATCH:
                        return new[] {RESTarOperations.Select, RESTarOperations.Update};
                    case RESTarMethods.Private_DELETE:
                        return new[] {RESTarOperations.Select, RESTarOperations.Delete};
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
                    ResourceOperations[resource][requiredMethod] = requiredMethod == RESTarOperations.Select
                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                            typeof(IEnumerable<>).MakeGenericType(resource)), null)
                        : method.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<>)
                            .MakeGenericType(resource), typeof(IRequest), typeof(int)), null);
                else if (LocalizedInterface(requiredMethod, typeof(object)).IsAssignableFrom(resource))
                    ResourceOperations[resource][requiredMethod] = requiredMethod == RESTarOperations.Select
                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                            typeof(IEnumerable<object>)), null)
                        : method.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<object>),
                            typeof(IRequest), typeof(int), null));
                else
                {
                    string missingInterface;
                    switch (requiredMethod)
                    {
                        case RESTarOperations.Select:
                            missingInterface = $"ISelector<{resource.FullName}> or ISelector<object>";
                            break;
                        case RESTarOperations.Insert:
                            missingInterface = $"IInserter<{resource.FullName}> or Inserter<object>";
                            break;
                        case RESTarOperations.Update:
                            missingInterface = $"IUpdater<{resource.FullName}> or IUpdater<object>";
                            break;
                        case RESTarOperations.Delete:
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