using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Auth;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using static RESTar.RESTarMethods;

namespace RESTar
{
    /// <summary>
    /// The main configuration class for the RESTar instance. Use Init() to 
    /// initialize the instance.
    /// </summary>
    public static class RESTarConfig
    {
        internal static readonly IDictionary<string, IResource> ResourceByName;
        internal static readonly IDictionary<Type, IResource> ResourceByType;
        internal static readonly IDictionary<string, AccessRights> ApiKeys;
        internal static readonly ConcurrentDictionary<string, AccessRights> AuthTokens;
        internal static IEnumerable<IResource> Resources => ResourceByName.Values;
        internal static readonly List<Uri> AllowedOrigins;
        internal static readonly RESTarMethods[] Methods = {GET, POST, PATCH, PUT, DELETE};
        internal static bool RequireApiKey { get; private set; }
        internal static bool AllowAllOrigins { get; private set; }
        private static string ConfigFilePath;
        internal static bool Initialized { get; private set; }

        static RESTarConfig()
        {
            ApiKeys = new Dictionary<string, AccessRights>();
            ResourceByType = new Dictionary<Type, IResource>();
            ResourceByName = new Dictionary<string, IResource>();
            AuthTokens = new ConcurrentDictionary<string, AccessRights>();
            AllowedOrigins = new List<Uri>();
            AuthTokens.TryAdd(Authenticator.AppToken, AccessRights.Root);
        }

        private static void UpdateAuthInfo()
        {
            if (ConfigFilePath != null) ReadConfig();
        }

        internal static void AddResource(IResource toAdd)
        {
            ResourceByName[toAdd.Name.ToLower()] = toAdd;
            ResourceByType[toAdd.TargetType] = toAdd;
            UpdateAuthInfo();
            toAdd.GetStaticProperties();
        }

        internal static void RemoveResource(IResource toRemove)
        {
            ResourceByName.Remove(toRemove.Name.ToLower());
            ResourceByType.Remove(toRemove.TargetType);
            UpdateAuthInfo();
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
        /// <param name="camelCase">Should resources be parsed and serialized using camelCase as 
        /// opposed to default PascalCase?</param>
        /// <param name="daysToSaveErrors">The number of days to save errors in the Error resource</param>
        /// <param name="viewEnabled">Should the view be enabled?</param>
        /// <param name="setupMenu">Shoud a menu be setup automatically in the view?</param>
        /// <param name="requireApiKey">Should the REST API require an API key?</param>
        /// <param name="allowAllOrigins">Should any origin be allowed to make CORS requests?</param>
        public static void Init
        (
            ushort port = 8282,
            string uri = "/rest",
            bool viewEnabled = false,
            bool setupMenu = false,
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            bool camelCase = false,
            ushort daysToSaveErrors = 30)
        {
            uri = uri ?? "/rest";
            uri = uri.Trim();
            if (uri.Contains("?")) throw new ArgumentException("URI cannot contain '?'", nameof(uri));
            var appName = Starcounter.Application.Current.Name;
            if (uri.EqualsNoCase(appName))
                throw new ArgumentException($"URI cannot be the same as the application name ({appName})");
            if (uri.First() != '/') uri = $"/{uri}";
            Settings.Init(port, uri, viewEnabled, prettyPrint, camelCase, daysToSaveErrors);
            typeof(object).GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .ForEach(t => Do.TryCatch(() => Resource.AutoMakeResource(t), e => throw (e.InnerException ?? e)));
            DB.All<DynamicResource>().ForEach(AddResource);
            RequireApiKey = requireApiKey;
            AllowAllOrigins = allowAllOrigins;
            ConfigFilePath = configFilePath;
            ReadConfig();
            DynamitConfig.Init(true, true);
            Log.Init();
            Handlers.Register(setupMenu);
            Initialized = true;
        }

        private static void ReadConfig()
        {
            if (!RequireApiKey && AllowAllOrigins) return;
            if (ConfigFilePath == null)
                throw new Exception("RESTar init error: No config file path given for API keys and/or allowed origins");
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
                if (!AllowAllOrigins) SetupOrigins(config.AllowedOrigin);
                if (RequireApiKey) SetupApiKeys(config.ApiKey);
            }
            catch (Exception jse)
            {
                throw new Exception($"RESTar init error: Invalid config file syntax: {jse.Message}");
            }
        }

        private static void SetupOrigins(JToken originToken)
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
                    originToken.ForEach(SetupOrigins);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetupApiKeys(JToken keyToken)
        {
            switch (keyToken.Type)
            {
                case JTokenType.Object:
                    var keyString = keyToken["Key"].Value<string>();
                    if (string.IsNullOrWhiteSpace(keyString))
                        throw new Exception("An API key was invalid");
                    var key = keyString.SHA256();
                    var access = new List<AccessRight>();

                    void getAccessRight(JToken token)
                    {
                        switch (token.Type)
                        {
                            case JTokenType.Object:
                                access.Add(new AccessRight
                                {
                                    Resources = token["Resource"].Value<string>().FindResources(),
                                    AllowedMethods = token["Methods"].Value<string>().ToUpper().ToMethodsArray()
                                });
                                break;
                            case JTokenType.Array:
                                token.ForEach(getAccessRight);
                                break;
                        }
                    }

                    getAccessRight(keyToken["AllowAccess"]);
                    ApiKeys[key] = access.ToAccessRights();
                    break;
                case JTokenType.Array:
                    keyToken.ForEach(SetupApiKeys);
                    break;
                default:
                    throw new Exception("Invalid API key XML syntax in config file");
            }
        }
    }
}