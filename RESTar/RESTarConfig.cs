﻿using System;
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
using RESTar.Dynamit;

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
            string uri = "/restar",
            bool prettyPrint = false
        )
        {
            if (uri.First() != '/')
                uri = $"/{uri}";

            foreach (var resource in DB.All<VirtualResource>())
                Db.Transact(() => { resource.Delete(); });

            foreach (var resource in DB.All<Table>())
                Db.Transact(() => { resource.Delete(); });

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
            var StarcounterResources = ResourcesList
                .Where(t => t.HasAttribute<DatabaseAttribute>() &&
                            !t.HasAttribute<DDictAttribute>())
                .ToList();
            foreach (var resource in StarcounterResources)
                Db.Transact(() => new Table(resource));
            CheckOperations(StarcounterResources);
            var VirtualResources = ResourcesList
                .Where(t => !t.HasAttribute<DatabaseAttribute>())
                .ToList();
            CheckVirtualResources(VirtualResources);
            foreach (var resource in VirtualResources)
                Scheduling.ScheduleTask(() => Db.Transact(() => new VirtualResource(resource)));

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
            Log.Info("==> RESTar command");
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
                if (e.InnerException is SqlException)
                    return SemanticsError(e.InnerException);
                if (e.InnerException is DeserializationException)
                    return DeserializationError(e.Message);
                return UnknownError(e);
            }
        }

        private static void CheckOperations(ICollection<Type> starcounterResources)
        {
            foreach (var operation in Operations)
            {
                var method = typeof(Table).GetMethod(operation.ToString(),
                    BindingFlags.Public | BindingFlags.Instance);
                ResourceOperations[typeof(Table)][operation] = operation == Select
                    ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                        typeof(IEnumerable<object>)), null)
                    : method.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<object>),
                        typeof(IRequest)), null);
            }

            foreach (var resource in starcounterResources.Except(new[] {typeof(Table)}))
            {
                var metaResource = DB.Get<Table>("Locator", resource.FullName);
                foreach (var operation in Operations)
                {
                    var overrideMethod = resource.GetMethod(operation.ToString(),
                        BindingFlags.Public | BindingFlags.Instance);
                    var baseMethod = metaResource.GetType()
                        .GetMethod(operation.ToString(), BindingFlags.Public | BindingFlags.Instance);
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
                            : baseMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<object>),
                                typeof(IRequest)), null);
                }
            }

            foreach (var resource in DynamitControl.DynamitTypes)
            {
                foreach (var operation in Operations)
                {
                    var method = typeof(DynamitOperations).GetMethod(operation.ToString(),
                        BindingFlags.Public | BindingFlags.Instance);
                    ResourceOperations[resource][operation] = operation == Select
                        ? method.CreateDelegate(typeof(Func<,>)
                            .MakeGenericType(typeof(IRequest), typeof(IEnumerable<DDictionary>)), null)
                        : method.CreateDelegate(typeof(Action<,>)
                            .MakeGenericType(typeof(IEnumerable<DDictionary>), typeof(IRequest)), null);
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

        private static void CheckVirtualResources(ICollection<Type> virtualResources)
        {
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
                    var method = type.GetMethod(requiredMethod.ToString(), BindingFlags.Public | BindingFlags.Instance);
                    if (LocalizedInterface(requiredMethod, type).IsAssignableFrom(type))
                        ResourceOperations[type][requiredMethod] = requiredMethod == Select
                            ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                typeof(IEnumerable<>).MakeGenericType(type)), null)
                            : method.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<>)
                                .MakeGenericType(type), typeof(IRequest)), null);
                    else if (LocalizedInterface(requiredMethod, typeof(object)).IsAssignableFrom(type))
                        ResourceOperations[type][requiredMethod] = requiredMethod == Select
                            ? method.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IRequest),
                                typeof(IEnumerable<object>)), null)
                            : method.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(IEnumerable<object>),
                                typeof(IRequest)), null);
                    else throw new VirtualResourceMissingInterfaceImplementation(type, typeof(ISelector<>));
                }
                if (type.GetFields(BindingFlags.Public | BindingFlags.Instance).Any())
                    throw new VirtualResourceMemberException(
                        "A virtual resource cannot include public instance fields, " +
                        "only properties."
                    );
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