using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Internal.Auth;
using RESTable.Meta.Internal;
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
    public class RESTableConfigurator
    {
        public TypeCache TypeCache { get; }
        public ResourceCollection ResourceCollection { get; }
        public EntityTypeResolverController EntityTypeResolverController { get; }
        public ResourceFactory ResourceFactory { get; }
        public ContentTypeController ContentTypeController { get; }
        public ProtocolController ProtocolController { get; }
        public RESTableConfiguration Configuration { get; }
        public WebSocketController WebSocketController { get; }
        public Authenticator Authenticator { get; }
        internal RootAccess RootAccess { get; }

        public RESTableConfigurator
        (
            TypeCache typeCache,
            ResourceCollection resourceCollection,
            EntityTypeResolverController entityTypeResolverController,
            ResourceFactory resourceFactory,
            ContentTypeController contentTypeController,
            ProtocolController protocolController,
            RESTableConfiguration configuration,
            WebSocketController webSocketController,
            IJsonProvider jsonProvider,
            Authenticator authenticator,
            RootAccess rootAccess
        )
        {
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
            EntityTypeResolverController = entityTypeResolverController;
            ResourceFactory = resourceFactory;
            ContentTypeController = contentTypeController;
            ProtocolController = protocolController;
            Configuration = configuration;
            WebSocketController = webSocketController;
            Authenticator = authenticator;
            RootAccess = rootAccess;
            ApplicationServicesAccessor.JsonProvider = jsonProvider;
            ApplicationServicesAccessor.ResourceCollection = resourceCollection;
            ApplicationServicesAccessor.TypeCache = typeCache;
        }

        public bool IsConfigured { get; private set; }

        public void ConfigureRESTable
        (
            string uri = "/restable",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            ushort nrOfErrorsToKeep = 2000
        )
        {
            ValidateUri(ref uri);
            Configuration.Update
            (
                rootUri: uri,
                requireApiKey: requireApiKey,
                allowAllOrigins: allowAllOrigins,
                configurationFilePath: configFilePath,
                nrOfErrorsToKeep: nrOfErrorsToKeep
            );
            ResourceCollection.SetDependencies(this, TypeCache);
            ResourceFactory.SetConfiguration(this);
            ResourceFactory.MakeResources();
            IsConfigured = true;
            UpdateConfiguration();
            ResourceFactory.BindControllers();
            ResourceFactory.FinalCheck();
            ProtocolController.OnInit();
        }

        private static void ValidateUri(ref string uri)
        {
            uri = uri?.Trim() ?? "/rest";
            if (!Regex.IsMatch(uri, RegEx.BaseUri))
                throw new FormatException("The URI contained invalid characters. It can only contain " +
                                          "letters, numbers, forward slashes and underscores");
            if (uri[0] != '/') uri = $"/{uri}";
            uri = uri.TrimEnd('/');
        }

        internal void UpdateConfiguration()
        {
            if (Configuration.NeedsConfigurationFile && Configuration.ConfigurationFilePath == null)
            {
                var (task, measure) = Configuration.RequireApiKey
                    ? ("require API keys", "read keys and assign access rights")
                    : ("only allow some CORS origins", "know what origins to deny");
                throw new MissingConfigurationFile($"RESTable was set up to {task}, but needs to read settings from a configuration file in " +
                                                   $"order to {measure}. Provide a configuration file path in the call to RESTableConfig.Init. " +
                                                   "See the specification for more info.");
            }
            if (Configuration.NeedsConfigurationFile)
                ReadConfigurationFile(Configuration);
            RootAccess.Load();
        }

        private void ReadConfigurationFile(RESTableConfiguration configuration)
        {
            try
            {
                dynamic config;
                using (var appConfig = File.OpenText(configuration.ConfigurationFilePath))
                {
                    var document = new XmlDocument();
                    document.Load(appConfig);
                    var jsonstring = JsonConvert.SerializeXmlNode(document);
                    config = JObject.Parse(jsonstring)["config"] as JObject;
                }
                if (config == null) throw new Exception();
                if (!configuration.AllowAllOrigins)
                {
                    Authenticator.AllowedOrigins.Clear();
                    ReadOrigins(config.AllowedOrigin);
                }
                if (configuration.RequireApiKey)
                    ReadApiKeys(config.ApiKey);
            }
            catch (Exception jse)
            {
                throw new Exception($"RESTable init error: Invalid config file: {jse.Message}");
            }
        }

        private void ReadOrigins(JToken originToken)
        {
            if (originToken == null) return;
            switch (originToken.Type)
            {
                case JTokenType.String:
                    var value = originToken.Value<string>();
                    if (string.IsNullOrWhiteSpace(value))
                        return;
                    Authenticator.AllowedOrigins.Add(new Uri(value));
                    break;
                case JTokenType.Array:
                    originToken.ForEach(ReadOrigins);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void ReadApiKeys(JToken apiKeyToken)
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
                                            case JValue {Value: string resourceString}:
                                                var iresources = ResourceCollection.SafeFindResources(resourceString);
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
                        foreach (var resource in ResourceCollection.Where(r => r.GETAvailableToAll))
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