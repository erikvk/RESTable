using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Jil;
using Newtonsoft.Json;
using RESTar.Internal;
using Starcounter;
using static RESTar.RESTarMethods;
using static RESTar.RESTarOperations;
using IResource = RESTar.Internal.IResource;
using ScRequest = Starcounter.Request;

namespace RESTar
{
    public static class RESTarConfig
    {
        internal static ICollection<IResource> Resources => NameResources.Values;
        internal static readonly IDictionary<string, IResource> NameResources = new Dictionary<string, IResource>();
        internal static readonly IDictionary<Type, IResource> TypeResources = new Dictionary<Type, IResource>();
        internal static readonly IDictionary<IResource, Type> IEnumTypes = new Dictionary<IResource, Type>();
        internal static Dictionary<RESTarMetaConditions, Type> MetaConditions;
        internal static readonly RESTarMethods[] Methods = {GET, POST, PATCH, PUT, DELETE};
        internal static readonly RESTarOperations[] Operations = {Select, Insert, Update, Delete};

        internal static void AddResource(IResource toAdd)
        {
            NameResources[toAdd.Name.ToLower()] = toAdd;
            TypeResources[toAdd.TargetType] = toAdd;
            IEnumTypes[toAdd] = typeof(IEnumerable<>).MakeGenericType(toAdd.TargetType);
        }

        internal static void RemoveResource(IResource toRemove)
        {
            NameResources.Remove(toRemove.Name.ToLower());
            TypeResources.Remove(toRemove.TargetType);
            IEnumTypes.Remove(toRemove);
        }

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
            if (uri.Trim().First() != '/')
                uri = $"/{uri}";

            foreach (var type in typeof(object).GetSubclasses().Where(t => t.HasAttribute<RESTarAttribute>()))
                ResourceHelper.AutoMakeResource(type);

            foreach (var dynamicResource in DB.All<DynamicResource>())
                AddResource(dynamicResource);

            MetaConditions = Enum.GetNames(typeof(RESTarMetaConditions)).ToDictionary(
                name => (RESTarMetaConditions) Enum.Parse(typeof(RESTarMetaConditions), name),
                name => typeof(RESTarMetaConditions).GetField(name).GetAttribute<TypeAttribute>().Type
            );

            DynamitConfig.Init();

            Settings.Init(uri, prettyPrint, camelCase, publicPort);
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
            Request request = null;
            try
            {
                request = new Request(scRequest, query, method, evaluator);
                request.ResolveMethod();
                var blockedMethod = MethodCheck(request);
                if (blockedMethod != null)
                    return Responses.BlockedMethod(request, blockedMethod.Value, request.Resource.TargetType);
                request.ResolveDataSource();
                var response = request.Evaluator(request);
                return request.GetResponse(response);
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
                return Responses.AmbiguousMatch(e.Resource.TargetType);
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
                return Responses.AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (AbortedInserterException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (AbortedUpdaterException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (AbortedDeleterException e)
            {
                return Responses.AbortedOperation(e, method, request?.Resource.TargetType);
            }
            catch (Exception e)
            {
                return Responses.InternalError(e);
            }
        }

//        private static void CheckOperations(IEnumerable<Type> resources)
//        {
//            foreach (var resource in resources)
//            {
//                if (resource.IsSubclassOf(typeof(DDictionary)))
//                {
//                    foreach (var operation in Operations)
//                    {
//                        var method = typeof(DDictionaryOperations).GetMethod(operation.ToString(),
//                            BindingFlags.Public | BindingFlags.Instance);
//                        ResourceOperations[resource][operation] = operation == RESTarOperations.Select
//                            ? method.CreateDelegate(typeof(Func<,>)
//                                .MakeGenericType(typeof(IRequest), typeof(IEnumerable<DDictionary>)), null)
//                            : method.CreateDelegate(typeof(Func<,,>)
//                                .MakeGenericType(typeof(IEnumerable<DDictionary>), typeof(IRequest), typeof(int)), null);
//                    }
//                }
//                else if (!resource.HasAttribute<DatabaseAttribute>())
//                    CheckVirtualResource(resource);
//                else
//                {
//                    var operationsProvider = typeof(StarcounterOperations);
//                    foreach (var operation in Operations)
//                    {
//                        var overrideMethod = resource.GetMethod(operation.ToString(),
//                            BindingFlags.Public | BindingFlags.Instance);
//                        var baseMethod = operationsProvider.GetMethod(operation.ToString(),
//                            BindingFlags.Public | BindingFlags.Instance);
//                        if (LocalizedInterface(operation, resource).IsAssignableFrom(resource))
//                            ResourceOperations[resource][operation] = operation == RESTarOperations.Select
//                                ? overrideMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
//                                    typeof(IEnumerable<>).MakeGenericType(resource)), null)
//                                : overrideMethod.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<>)
//                                    .MakeGenericType(resource), typeof(IRequest), typeof(int)), null);
//                        else if (LocalizedInterface(operation, typeof(object)).IsAssignableFrom(resource))
//                            ResourceOperations[resource][operation] = operation == RESTarOperations.Select
//                                ? overrideMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
//                                    typeof(IEnumerable<object>)), null)
//                                : overrideMethod.CreateDelegate(
//                                    typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<object>),
//                                        typeof(IRequest), typeof(int)), null);
//                        else
//                            ResourceOperations[resource][operation] = operation == RESTarOperations.Select
//                                ? baseMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
//                                    typeof(IEnumerable<object>)), null)
//                                : baseMethod.CreateDelegate(
//                                    typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<object>),
//                                        typeof(IRequest), typeof(int)), null);
//                    }
//                }
//            }
//        }

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

//        private static void CheckVirtualResource(Type resource)
//        {
//            foreach (var requiredMethod in NecessaryOpDefs(resource.GetAttribute<RESTarAttribute>()?.AvailableMethods))
//            {
//                var method = resource.GetMethod(requiredMethod.ToString(), BindingFlags.Public | BindingFlags.Instance);
//                if (LocalizedInterface(requiredMethod, resource).IsAssignableFrom(resource))
//                    ResourceOperations[resource][requiredMethod] = requiredMethod == Select
//                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
//                            typeof(IEnumerable<>).MakeGenericType(resource)), null)
//                        : method.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<>)
//                            .MakeGenericType(resource), typeof(IRequest), typeof(int)), null);
//                else if (LocalizedInterface(requiredMethod, typeof(object)).IsAssignableFrom(resource))
//                    ResourceOperations[resource][requiredMethod] = requiredMethod == Select
//                        ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
//                            typeof(IEnumerable<object>)), null)
//                        : method.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IEnumerable<object>),
//                            typeof(IRequest), typeof(int), null));
//                else
//                {
//                    string missingInterface;
//                    switch (requiredMethod)
//                    {
//                        case Select:
//                            missingInterface = $"ISelector<{resource.FullName}> or ISelector<object>";
//                            break;
//                        case Insert:
//                            missingInterface = $"IInserter<{resource.FullName}> or Inserter<object>";
//                            break;
//                        case Update:
//                            missingInterface = $"IUpdater<{resource.FullName}> or IUpdater<object>";
//                            break;
//                        case Delete:
//                            missingInterface = $"IDeleter<{resource.FullName}> or IDeleter<object>";
//                            break;
//                        default:
//                            throw new ArgumentOutOfRangeException();
//                    }
//                    throw new VirtualResourceMissingInterfaceImplementation(resource, missingInterface);
//                }
//            }
//            var fields = resource.GetFields(BindingFlags.Public | BindingFlags.Instance);
//            if (fields.Any())
//                throw new VirtualResourceMemberException(
//                    $"A virtual resource cannot include public instance fields, " +
//                    $"only properties. Fields: {string.Join(", ", fields.Select(f => $"'{f.Name}'"))} in resource '{resource.FullName}'"
//                );
//        }

        private static RESTarMethods? MethodCheck(IRequest request)
        {
            var availableMethods = request.Resource.AvailableMethodsString.ToMethodsList();
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