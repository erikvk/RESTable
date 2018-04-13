using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Auth;
using RESTar.Reflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Results;
using RESTar.Sc;
using Starcounter;
using static RESTar.Method;
using IResource = RESTar.Resources.IResource;

namespace RESTar
{
    /// <summary>
    /// The main configuration class for the RESTar instance. Use Init() to 
    /// initialize the instance.
    /// </summary>
    public static class RESTarConfig
    {
        internal static IDictionary<string, IResource> ResourceFinder { get; private set; }
        internal static IDictionary<string, IResource> ResourceByName { get; private set; }
        internal static IDictionary<Type, IResource> ResourceByType { get; private set; }
        internal static ICollection<IResource> Resources => ResourceByName.Values;
        internal static List<Uri> AllowedOrigins { get; private set; }
        internal static string[] ReservedNamespaces { get; private set; }
        internal static bool RequireApiKey { get; private set; }
        internal static bool AllowAllOrigins { get; private set; }
        internal static bool NeedsConfiguration => RequireApiKey || !AllowAllOrigins;
        private static string ConfigFilePath { get; set; }
        internal static bool Initialized { get; private set; }
        internal static readonly Method[] Methods = {GET, POST, PATCH, PUT, DELETE, REPORT, HEAD};
        internal static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
        internal static readonly string Version = typeof(RESTarConfig).Assembly.GetName().Version.ToString();

        internal static FileStream MakeTempFile() => File.Create
        (
            path: $"{Path.GetTempPath()}{Guid.NewGuid()}.restar",
            bufferSize: 1048576,
            options: FileOptions.Asynchronous | FileOptions.DeleteOnClose
        );

        static RESTarConfig() => NewState();

        private static void NewState()
        {
            ResourceByType = new Dictionary<Type, IResource>();
            ResourceByName = new Dictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
            ResourceFinder = new ConcurrentDictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
            AllowedOrigins = new List<Uri>();
            ReservedNamespaces = typeof(RESTarConfig).Assembly
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
                var (task, measure) = default((string, string));
                if (RequireApiKey)
                    (task, measure) = ("require API keys", "read keys and assign access rights");
                else if (!AllowAllOrigins)
                    (task, measure) = ("only allow some CORS origins", "know what origins to deny");
                throw new MissingConfigurationFile($"RESTar was set up to {task}, but needs to read settings from a configuration file in " +
                                                   $"order to {measure}. Provide a configuration file path in the call to RESTarConfig.Init. " +
                                                   "See the specification for more info.");
            }
            if (NeedsConfiguration) ReadConfig();
            AccessRights.ReloadRoot();
        }

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

        internal static void ReloadResourceFinder()
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
            parts.ForEach((item, index) =>
            {
                var key = string.Join(".", parts.Skip(index));
                if (finder.ContainsKey(key))
                    finder[key] = null;
                else finder[key] = toAdd;
            });
        }

        /// <summary>
        /// Initiates the RESTar interface
        /// </summary>
        /// <param name="port">The port that RESTar should listen on</param>
        /// <param name="uri">The URI that RESTar should listen on. E.g. '/rest'</param>
        /// <param name="configFilePath">The path to the config file containing API keys and 
        /// allowed origins</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted as default?
        ///  (can be changed in settings during runtime)</param>
        /// <param name="daysToSaveErrors">The number of days to save errors in the Error resource</param>
        /// <param name="requireApiKey">Should the REST API require an API key?</param>
        /// <param name="allowAllOrigins">Should any origin be allowed to make CORS requests?</param>
        /// <param name="lineEndings">The line endings to use when writing JSON</param>
        /// <param name="resourceProviders">External resource providers for the RESTar instance</param>
        /// <param name="protocolProviders">External protocol providers for the RESTar instance</param>
        /// <param name="contentTypeProviders">External content type providers for the RESTar instance</param>
        public static void Init
        (
            ushort port = 8282,
            string uri = "/rest",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            ushort daysToSaveErrors = 30,
            LineEndings lineEndings = LineEndings.Windows,
            IEnumerable<ResourceProvider> resourceProviders = null,
            IEnumerable<IProtocolProvider> protocolProviders = null,
            IEnumerable<IContentTypeProvider> contentTypeProviders = null
        )
        {
            try
            {
                ProcessUri(ref uri);
                Settings.Init(port, uri, false, prettyPrint, daysToSaveErrors, lineEndings);
                Log.Init();
                DynamitConfig.Init(true, true);
                ResourceFactory.MakeResources(resourceProviders?.ToArray());
                ContentTypeController.SetupContentTypeProviders(contentTypeProviders?.ToList());
                ProtocolController.SetupProtocolProviders(protocolProviders?.ToList());
                RequireApiKey = requireApiKey;
                AllowAllOrigins = allowAllOrigins;
                ConfigFilePath = configFilePath;
                var networkProviders = new INetworkProvider[] {new ScNetworkProvider()};
                NetworkController.AddNetworkBindings(networkProviders);
                Initialized = true;
                UpdateConfiguration();
                DatabaseIndex.Init();
                DbOutputFormat.Init();
                ResourceFactory.FinalCheck();
            }
            catch
            {
                Initialized = false;
                RequireApiKey = default;
                AllowAllOrigins = default;
                ConfigFilePath = default;
                NetworkController.RemoveNetworkBindings();
                Settings.Clear();
                NewState();
                throw;
            }
        }

        private static void ProcessUri(ref string uri)
        {
            uri = uri?.Trim() ?? "/rest";
            if (!Regex.IsMatch(uri, RegEx.BaseUri))
                throw new FormatException("URI contained invalid characters. Can only contain " +
                                          "letters, numbers, forward slashes and underscores");
            var appName = Application.Current.Name;
            if (uri.EqualsNoCase(appName))
                throw new ArgumentException($"URI must differ from application name ({appName})", nameof(appName));
            if (uri[0] != '/') uri = $"/{uri}";
            uri = uri.TrimEnd('/');
        }

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
                throw new Exception($"RESTar init error: Invalid config file syntax: {jse.Message}");
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ReadApiKeys(JToken apiKeyToken)
        {
            switch (apiKeyToken)
            {
                case JObject apiKey:
                    var keyString = apiKey["Key"].Value<string>();
                    if (keyString == null || Regex.IsMatch(keyString, @"[\(\)]") || !Regex.IsMatch(keyString, RegEx.ApiKey))
                        throw new Exception(
                            "An API key contained invalid characters. Must be a non-empty string, not containing whitespace or parentheses, " +
                            "and only containing ASCII characters from 33 to 126");
                    var key = keyString.SHA256();
                    var accessRightList = new List<AccessRight>();

                    void recurseAllowAccess(JToken allowAccessToken)
                    {
                        switch (allowAccessToken)
                        {
                            case JObject allowAccess:
                                var resourceSet = new HashSet<IResource>();

                                void recurseResources(JToken resourceToken)
                                {
                                    switch (resourceToken)
                                    {
                                        case JValue value when value.Value is string resourceString:
                                            var iresources = Resource.SafeFindMany(resourceString);
                                            var includingInner = iresources.Union(iresources
                                                .Cast<IResourceInternal>()
                                                .Where(r => r.InnerResources != null)
                                                .SelectMany(r => r.InnerResources));
                                            resourceSet.UnionWith(includingInner);
                                            return;
                                        case JArray resources:
                                            resources.ForEach(recurseResources);
                                            return;
                                        default: throw new Exception("Invalid API key XML syntax in config file");
                                    }
                                }

                                recurseResources(allowAccess["Resource"]);
                                accessRightList.Add(new AccessRight
                                {
                                    Resources = resourceSet.OrderBy(r => r.Name).ToList(),
                                    AllowedMethods = allowAccess["Methods"].Value<string>().ToUpper().ToMethodsArray()
                                });
                                return;

                            case JArray allowAccesses:
                                allowAccesses.ForEach(recurseAllowAccess);
                                return;

                            default: throw new Exception("Invalid API key XML syntax in config file");
                        }
                    }

                    recurseAllowAccess(apiKeyToken["AllowAccess"]);
                    var accessRights = AccessRights.ToAccessRights(accessRightList);
                    foreach (var resource in Resources.Where(r => r.GETAvailableToAll))
                    {
                        if (!accessRights.TryGetValue(resource, out var methods))
                            accessRights.Add(resource, new[] {GET, REPORT, HEAD});
                        else
                            accessRights[resource] = methods
                                .Union(new[] {GET, REPORT, HEAD})
                                .OrderBy(i => i, MethodComparer.Instance)
                                .ToArray();
                    }
                    if (Authenticator.ApiKeys.TryGetValue(key, out var existing))
                        accessRights.ForEach(existing.Put);
                    else Authenticator.ApiKeys[key] = accessRights;
                    break;
                case JArray apiKeys:
                    apiKeys.ForEach(ReadApiKeys);
                    break;
                default: throw new Exception("Invalid API key XML syntax in config file");
            }
        }
    }
}