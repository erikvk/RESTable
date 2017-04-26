using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Dynamit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using static RESTar.RESTarMethods;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public static class RESTarConfig
    {
        internal static ICollection<IResource> Resources => NameResources.Values;
        internal static readonly IDictionary<string, IResource> NameResources = new Dictionary<string, IResource>();
        internal static readonly IDictionary<Type, IResource> TypeResources = new Dictionary<Type, IResource>();
        internal static readonly IDictionary<IResource, Type> IEnumTypes = new Dictionary<IResource, Type>();
        internal static readonly IDictionary<string, AccessRights> ApiKeys = new Dictionary<string, AccessRights>();
        private static readonly IDictionary<Type, IEnumerable<PropertyInfo>> Properties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        internal static readonly List<Uri> AllowedOrigins = new List<Uri>();
        internal static readonly RESTarMethods[] Methods = {GET, POST, PATCH, PUT, DELETE};
        internal static bool RequireApiKey { get; private set; }
        internal static bool AllowAllOrigins { get; private set; }
        private static string ConfigFilePath;

        internal static readonly ConcurrentDictionary<string, AccessRights> AuthTokens =
            new ConcurrentDictionary<string, AccessRights>();

        private static void UpdateAuthInfo()
        {
            if (ConfigFilePath != null)
                ReadConfig();
        }

        internal static void AddResource(IResource toAdd)
        {
            NameResources[toAdd.Name.ToLower()] = toAdd;
            TypeResources[toAdd.TargetType] = toAdd;
            IEnumTypes[toAdd] = typeof(IEnumerable<>).MakeGenericType(toAdd.TargetType);
            UpdateAuthInfo();
            Properties[toAdd.TargetType] = toAdd.TargetType.Properties();
        }

        internal static void RemoveResource(IResource toRemove)
        {
            NameResources.Remove(toRemove.Name.ToLower());
            TypeResources.Remove(toRemove.TargetType);
            IEnumTypes.Remove(toRemove);
            UpdateAuthInfo();
        }

        internal static IEnumerable<PropertyInfo> GetPropertyList(Type type)
        {
            if (Properties.ContainsKey(type))
                return Properties[type];
            return Properties[type] = type.Properties();
        }

        /// <summary>
        /// Initiates the RESTar interface
        /// </summary>
        /// <param name="port">The port that RESTar should listen on</param>
        /// <param name="uri">The URI that RESTar should listen on. E.g. '/rest'</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted as default?
        ///  (can be changed in settings during runtime)</param>
        /// <param name="camelCase">Should resources be parsed and serialized using camelCase as 
        /// opposed to default PascalCase?</param>
        /// <param name="localTimes">Should datetimes be handled as local times or as UTC?</param>
        /// <param name="daysToSaveErrors">The number of days to save errors in the Error resource</param>
        public static void Init
        (
            ushort port = 8282,
            string uri = "/rest",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            bool camelCase = false,
            bool localTimes = true,
            ushort daysToSaveErrors = 30
        )
        {
            if (uri.Trim().First() != '/')
                uri = $"/{uri}";

            foreach (var type in typeof(object).GetSubclasses().Where(t => t.HasAttribute<RESTarAttribute>()))
                ResourceHelper.AutoMakeResource(type);

            foreach (var dynamicResource in DB.All<DynamicResource>())
                AddResource(dynamicResource);

            RequireApiKey = requireApiKey;
            AllowAllOrigins = allowAllOrigins;
            ConfigFilePath = configFilePath;
            ReadConfig();
            DynamitConfig.Init(true, true);
            Settings.Init(uri, port, prettyPrint, camelCase, localTimes, daysToSaveErrors);
            Log.Init();
            Handlers.Register(uri);
        }

        private static void ReadConfig()
        {
            if (!RequireApiKey && AllowAllOrigins) return;
            if (ConfigFilePath == null)
                throw new Exception(
                    "RESTar init error: No config file path to get API keys and/or allowed origins from");
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
                if (config == null)
                    throw new Exception();
                if (!AllowAllOrigins)
                    SetupOrigins(config.AllowedOrigin);
                if (RequireApiKey)
                    SetupApiKeys(config.ApiKey);
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
                    Action<JToken> GetAccessRight = null;
                    GetAccessRight = token =>
                    {
                        switch (token.Type)
                        {
                            case JTokenType.Object:
                                access.Add(new AccessRight
                                {
                                    Resources = token["Resource"].Value<string>().FindResources(),
                                    AllowedMethods = token["Methods"].Value<string>().ToUpper().ToMethodsList()
                                });
                                break;
                            case JTokenType.Array:
                                token.ForEach(GetAccessRight);
                                break;
                        }
                    };
                    GetAccessRight(keyToken["AllowAccess"]);
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