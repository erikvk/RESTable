using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Dynamit;
using Microsoft.CSharp.RuntimeBinder;
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
        internal static readonly List<Uri> AllowedOrigins = new List<Uri>();
        internal static readonly RESTarMethods[] Methods = {GET, POST, PATCH, PUT, DELETE};
        internal static bool RequireApiKey { get; private set; }
        internal static bool AllowAllOrigins { get; private set; }

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
        /// <param name="port">The port that RESTar should listen on</param>
        /// <param name="uri">The URI that RESTar should listen on. E.g. '/rest'</param>
        /// <param name="prettyPrint">Should JSON output be pretty print formatted as default?
        ///  (can be changed in settings during runtime)</param>
        /// <param name="camelCase">Should resources be parsed and serialized using camelCase as 
        /// opposed to default PascalCase?</param>
        /// <param name="localTimes">Should datetimes be handled as local times or as UTC?</param>
        public static void Init
        (
            ushort port = 8282,
            string uri = "/rest",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            bool camelCase = false,
            bool localTimes = true
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
            SetupConfig(configFilePath);
            DynamitConfig.Init(true, true);
            Settings.Init(uri, port, prettyPrint, camelCase, localTimes);
            Log.Init();
            Handlers.Register(uri);
        }

        private static void SetupConfig(string filePath)
        {
            if (!RequireApiKey && AllowAllOrigins) return;
            if (filePath == null)
                throw new Exception("RESTar init error: No config file path to get API keys and/or allowed origins from");
            try
            {
                dynamic config;
                using (var appConfig = File.OpenText(filePath))
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

        private static void SetupOrigins(JToken allowedOrigin)
        {
            switch (allowedOrigin.Type)
            {
                case JTokenType.String:
                    AllowedOrigins.Add(new Uri(allowedOrigin.Value<string>()));
                    break;
                case JTokenType.Array:
                    AllowedOrigins.AddRange(allowedOrigin.Select(i => new Uri(i.Value<string>())));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SetupApiKeys(JToken apiKey)
        {
            switch (apiKey.Type)
            {
                case JTokenType.Object:
                    List<AccessRight> access;
                    var key = apiKey["Key"].Value<string>().MD5();
                    var accessToken = apiKey["AllowAccess"];
                    switch (accessToken.Type)
                    {
                        case JTokenType.Object:
                            access = new List<AccessRight>
                            {
                                new AccessRight
                                {
                                    Resources = accessToken["Resource"].Value<string>().FindResources(),
                                    AllowedMethods = accessToken["Methods"].Value<string>().ToUpper().ToMethodsList()
                                }
                            };
                            break;
                        case JTokenType.Array:
                            access = accessToken.Select(item => new AccessRight
                            {
                                Resources = item["Resource"].Value<string>().FindResources(),
                                AllowedMethods = item["Methods"].Value<string>().ToUpper().ToMethodsList()
                            }).ToList();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    ApiKeys[key] = access.ToAccessRights();
                    break;

                case JTokenType.Array:
                    var array = (JArray) apiKey;
                    foreach (var jToken in array)
                        SetupApiKeys((JObject) jToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}