﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTable.Admin;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Internal.Auth;
using RESTable.Meta.Internal;
using RESTable.NetworkProviders;
using RESTable.ProtocolProviders;
using RESTable.Resources;
using RESTable.WebSockets;
using RESTable.Linq;
using RESTable.Meta;
using static RESTable.Method;

namespace RESTable
{
    /// <summary>
    /// The main configuration class for the RESTable instance. Use Init() to 
    /// initialize the instance.
    /// </summary>
    public static class RESTableConfig
    {
        internal static IDictionary<string, IResource> ResourceFinder { get; private set; }
        internal static IDictionary<string, IResource> ResourceByName { get; private set; }
        internal static IDictionary<Type, IResource> ResourceByType { get; private set; }
        internal static ICollection<IResource> Resources => ResourceByName.Values;
        internal static HashSet<Uri> AllowedOrigins { get; private set; }
        internal static string[] ReservedNamespaces { get; private set; }
        internal static bool RequireApiKey { get; private set; }
        internal static bool AllowAllOrigins { get; private set; }
        internal static bool NeedsConfiguration => RequireApiKey || !AllowAllOrigins;
        private static string ConfigFilePath { get; set; }
        internal static bool Initialized { get; private set; }
        internal static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
        internal static readonly string Version;

        /// <summary>
        /// The REST methods available in RESTable
        /// </summary>
        public static Method[] Methods { get; }

        static RESTableConfig()
        {
            Methods = new[] {GET, POST, PATCH, PUT, DELETE, REPORT, HEAD};
            var version = typeof(RESTableConfig).Assembly.GetName().Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}";
            NewState();
        }

        /// <summary>
        /// Initiates the RESTable interface
        /// </summary>
        /// <param name="port">The port that RESTable should listen on</param>
        /// <param name="uri">The URI that RESTable should listen on. E.g. '/rest'</param>
        /// <param name="configFilePath">The path to the config file containing API keys and 
        /// allowed origins</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted as default?
        ///  (can be changed in settings during runtime)</param>
        /// <param name="nrOfErrorsToKeep">The number of days to save errors in the Error resource</param>
        /// <param name="requireApiKey">Should the REST API require an API key?</param>
        /// <param name="allowAllOrigins">Should any origin be allowed to make CORS requests?</param>
        /// <param name="lineEndings">The line endings to use when writing JSON</param>
        /// <param name="entityResourceProviders">External entity resource providers for the RESTable instance</param>
        /// <param name="protocolProviders">External protocol providers for the RESTable instance</param>
        /// <param name="contentTypeProviders">External content type providers for the RESTable instance</param>
        /// <param name="networkProviders">The network providers to register with RESTable</param>
        /// <param name="entityTypeContractResolvers">The entity type contract resolvers to register with RESTable</param>
        public static void Init
        (
            ushort port = 8282,
            string uri = "/rest",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            ushort nrOfErrorsToKeep = 2000,
            LineEndings lineEndings = LineEndings.Windows,
            IEnumerable<IEntityResourceProvider> entityResourceProviders = null,
            IEnumerable<IProtocolProvider> protocolProviders = null,
            IEnumerable<IContentTypeProvider> contentTypeProviders = null,
            IEnumerable<INetworkProvider> networkProviders = null,
            IEnumerable<IEntityTypeContractResolver> entityTypeContractResolvers = null
        )
        {
            try
            {
                ProcessUri(ref uri);
                Settings.Init(port, uri, prettyPrint, nrOfErrorsToKeep, lineEndings);
                EntityTypeResolverController.SetupEntityTypeResolvers(entityTypeContractResolvers?.ToArray());
                ResourceFactory.MakeResources(entityResourceProviders?.ToArray());
                ContentTypeController.SetupContentTypeProviders(contentTypeProviders?.ToList());
                ProtocolController.SetupProtocolProviders(protocolProviders?.ToList());
                RequireApiKey = requireApiKey;
                AllowAllOrigins = allowAllOrigins;
                ConfigFilePath = configFilePath;
                NetworkController.AddNetworkBindings(networkProviders?.ToArray());
                Initialized = true;
                UpdateConfiguration();
                DatabaseIndex.Init();
                ResourceFactory.BindControllers();
                ResourceFactory.FinalCheck();
                ProtocolController.OnInit();
                RegisterStaticIndexes();
                RunCustomMigrationLogic();
            }
            catch
            {
                Initialized = false;
                RequireApiKey = default;
                AllowAllOrigins = default;
                ConfigFilePath = default;
                NetworkController.RemoveNetworkBindings();
                NewState();
                throw;
            }
        }

        private static void NewState()
        {
            ResourceByType = new Dictionary<Type, IResource>();
            ResourceByName = new Dictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
            ResourceFinder = new ConcurrentDictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
            AllowedOrigins = new HashSet<Uri>();
            ReservedNamespaces = typeof(RESTableConfig).Assembly
                .GetTypes()
                .Select(type => type.Namespace?.ToLower())
                .Where(ns => ns != null)
                .Distinct()
                .ToArray();
            Authenticator.NewState();
        }

        internal static void UpdateConfiguration()
        {
            if (!Initialized) return;
            if (NeedsConfiguration && ConfigFilePath == null)
            {
                var (task, measure) = RequireApiKey
                    ? ("require API keys", "read keys and assign access rights")
                    : ("only allow some CORS origins", "know what origins to deny");
                throw new MissingConfigurationFile($"RESTable was set up to {task}, but needs to read settings from a configuration file in " +
                                                   $"order to {measure}. Provide a configuration file path in the call to RESTableConfig.Init. " +
                                                   "See the specification for more info.");
            }
            if (NeedsConfiguration) ReadConfig();
            AccessRights.ReloadRoot();
        }

        internal static FileStream MakeTempFile() => File.Create
        (
            path: $"{Path.GetTempPath()}{Guid.NewGuid()}.restable",
            bufferSize: 1048576,
            options: FileOptions.Asynchronous | FileOptions.DeleteOnClose
        );

        internal static void AddResource(IResource toAdd)
        {
            ResourceByName[toAdd.Name] = toAdd;
            ResourceByType[toAdd.Type] = toAdd;
            AddToResourceFinder(toAdd, ResourceFinder);
            UpdateConfiguration();
            toAdd.Type.GetDeclaredProperties();
        }

        internal static void RemoveResource(IResource toRemove)
        {
            ResourceByName.Remove(toRemove.Name);
            ResourceByType.Remove(toRemove.Type);
            ReloadResourceFinder();
            UpdateConfiguration();
        }

        private static void ReloadResourceFinder()
        {
            var newFinder = new ConcurrentDictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
            Resources.ForEach(r => AddToResourceFinder(r, newFinder));
            ResourceFinder = newFinder;
        }

        private static void AddToResourceFinder(IResource toAdd, IDictionary<string, IResource> finder)
        {
            string[] makeResourceParts(IResource resource)
            {
                switch (resource)
                {
                    case var _ when resource.IsInternal: return new[] {resource.Name};
                    case var _ when resource.IsInnerResource:
                        var dots = resource.Name.Count('.');
                        return resource.Name.Split(new[] {'.'}, dots);
                    default: return resource.Name.Split('.');
                }
            }

            var parts = makeResourceParts(toAdd);
            parts.ForEach((_, index) =>
            {
                var key = string.Join(".", parts.Skip(index));
                if (finder.ContainsKey(key))
                    finder[key] = null;
                else finder[key] = toAdd;
            });
        }

        private static void ProcessUri(ref string uri)
        {
            uri = uri?.Trim() ?? "/rest";
            if (!Regex.IsMatch(uri, RegEx.BaseUri))
                throw new FormatException("URI contained invalid characters. Can only contain " +
                                          "letters, numbers, forward slashes and underscores");
            if (uri[0] != '/') uri = $"/{uri}";
            uri = uri.TrimEnd('/');
        }

        private static void RegisterStaticIndexes() { }

        private static void RunCustomMigrationLogic() { }

        private static void ReadConfig()
        {
            try
            {
                dynamic config;
                using (var appConfig = File.OpenText(ConfigFilePath))
                {
                    var document = new XmlDocument();
                    document.Load(appConfig);
                    var jsonstring = JsonConvert.SerializeXmlNode(document);
                    config = JObject.Parse(jsonstring)["config"] as JObject;
                }
                if (config == null) throw new Exception();
                if (!AllowAllOrigins) ReadOrigins(config.AllowedOrigin);
                if (RequireApiKey) ReadApiKeys(config.ApiKey);
            }
            catch (Exception jse)
            {
                throw new Exception($"RESTable init error: Invalid config file: {jse.Message}");
            }
        }

        private static void ReadOrigins(JToken originToken)
        {
            if (originToken == null) return;
            switch (originToken.Type)
            {
                case JTokenType.String:
                    var value = originToken.Value<string>();
                    if (string.IsNullOrWhiteSpace(value))
                        return;
                    AllowedOrigins.Add(new Uri(value));
                    break;
                case JTokenType.Array:
                    originToken.ForEach(ReadOrigins);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private static void ReadApiKeys(JToken apiKeyToken)
        {
            IEnumerable<string> recurseApiKeys(JToken token)
            {
                switch (token)
                {
                    case JObject apiKey:
                        var keyString = apiKey["Key"].Value<string>();
                        if (keyString == null || Regex.IsMatch(keyString, @"[\(\)]") || !Regex.IsMatch(keyString, RegEx.ApiKey))
                            throw new Exception("An API key contained invalid characters. Must be a non-empty string, not containing " +
                                                "whitespace or parentheses, and only containing ASCII characters 33 through 126");
                        var key = keyString.SHA256();

                        IEnumerable<AccessRight> recurseAllowAccess(JToken allowAccessToken)
                        {
                            switch (allowAccessToken)
                            {
                                case JObject allowAccess:

                                    IEnumerable<IResource> recurseResources(JToken resourceToken)
                                    {
                                        switch (resourceToken)
                                        {
                                            case JValue {Value: string resourceString} value:
                                                var iresources = Meta.Resource.SafeFindMany(resourceString);
                                                var includingInner = iresources.Union(iresources
                                                    .Cast<IResourceInternal>()
                                                    .Where(r => r.InnerResources != null)
                                                    .SelectMany(r => r.InnerResources));
                                                foreach (var resource in includingInner)
                                                    yield return resource;
                                                yield break;
                                            case JArray resources:
                                                foreach (var resource in resources.SelectMany(recurseResources))
                                                    yield return resource;
                                                yield break;
                                            default: throw new Exception("Invalid API key XML syntax in config file");
                                        }
                                    }

                                    yield return new AccessRight
                                    (
                                        resources: recurseResources(allowAccess["Resource"])
                                            .OrderBy(r => r.Name)
                                            .ToList(),
                                        allowedMethods: allowAccess["Methods"]
                                            .Value<string>()
                                            .ToUpper()
                                            .ToMethodsArray()
                                    );
                                    yield break;

                                case JArray allowAccesses:
                                    foreach (var allowAccess in allowAccesses.SelectMany(recurseAllowAccess))
                                        yield return allowAccess;
                                    yield break;

                                default: throw new Exception("Invalid API key XML syntax in config file");
                            }
                        }

                        var accessRights = AccessRights.ToAccessRights(recurseAllowAccess(token["AllowAccess"]), key);
                        foreach (var resource in Resources.Where(r => r.GETAvailableToAll))
                        {
                            if (accessRights.TryGetValue(resource, out var methods))
                                accessRights[resource] = methods
                                    .Union(new[] {GET, REPORT, HEAD})
                                    .OrderBy(i => i, MethodComparer.Instance)
                                    .ToArray();
                            else accessRights[resource] = new[] {GET, REPORT, HEAD};
                        }
                        if (Authenticator.ApiKeys.TryGetValue(key, out var existing))
                        {
                            existing.Clear();
                            accessRights.ForEach(pair => existing[pair.Key] = pair.Value);
                        }
                        else Authenticator.ApiKeys[key] = accessRights;
                        yield return key;
                        yield break;

                    case JArray apiKeys:
                        foreach (var _key in apiKeys.SelectMany(recurseApiKeys))
                            yield return _key;
                        yield break;

                    default: throw new Exception("Invalid API key XML syntax in config file");
                }
            }

            var currentKeys = recurseApiKeys(apiKeyToken).ToList();
            Authenticator.ApiKeys.Keys.Except(currentKeys).ToList().ForEach(key =>
            {
                if (Authenticator.ApiKeys.TryGetValue(key, out var accessRights))
                {
                    WebSocketController.RevokeAllWithKey(key).Wait();
                    accessRights.Clear();
                }
                Authenticator.ApiKeys.Remove(key);
            });
        }
    }
}