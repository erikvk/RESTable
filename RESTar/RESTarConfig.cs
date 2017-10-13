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
using RESTar.Admin;
using RESTar.Auth;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using static RESTar.Methods;
using static RESTar.Requests.Handlers;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// The main configuration class for the RESTar instance. Use Init() to 
    /// initialize the instance.
    /// </summary>
    public static class RESTarConfig
    {
        internal static IDictionary<string, IResource> ResourceFinder { get; private set; }
        internal static readonly IDictionary<string, IResource> ResourceByName;
        internal static readonly IDictionary<Type, IResource> ResourceByType;
        internal static readonly IDictionary<string, AccessRights> ApiKeys;
        internal static readonly ConcurrentDictionary<string, AccessRights> AuthTokens;
        internal static ICollection<IResource> Resources => ResourceByName.Values;
        internal static readonly List<Uri> AllowedOrigins;
        internal static readonly Methods[] Methods = {GET, POST, PATCH, PUT, DELETE};
        internal static bool RequireApiKey { get; private set; }
        internal static bool AllowAllOrigins { get; private set; }
        private static string ConfigFilePath;
        internal static bool Initialized { get; private set; }

        static RESTarConfig()
        {
            ApiKeys = new Dictionary<string, AccessRights>();
            ResourceByType = new Dictionary<Type, IResource>();
            ResourceByName = new Dictionary<string, IResource>();
            ResourceFinder = new ConcurrentDictionary<string, IResource>();
            AuthTokens = new ConcurrentDictionary<string, AccessRights>();
            AllowedOrigins = new List<Uri>();
            AuthTokens.TryAdd(Authenticator.AppToken, AccessRights.Root);
        }

        internal static void UpdateAuthInfo()
        {
            if (!Initialized) return;
            if (ConfigFilePath != null) ReadConfig();
            AccessRights.Root = Resources
                .ToDictionary(r => r, r => Methods)
                .CollectDict(dict => new AccessRights(dict));
        }

        internal static void AddResource(IResource toAdd)
        {
            ResourceByName[toAdd.Name.ToLower()] = toAdd;
            ResourceByType[toAdd.Type] = toAdd;
            AddToResourceFinder(toAdd, ResourceFinder);
            UpdateAuthInfo();
            toAdd.GetStaticProperties();
        }

        internal static void RemoveResource(IResource toRemove)
        {
            ResourceByName.Remove(toRemove.Name.ToLower());
            ResourceByType.Remove(toRemove.Type);
            ReloadResourceFinder();
            UpdateAuthInfo();
        }

        internal static void ReloadResourceFinder()
        {
            var newFinder = new ConcurrentDictionary<string, IResource>();
            Resources.ForEach(r => AddToResourceFinder(r, newFinder));
            ResourceFinder = newFinder;
        }

        private static void AddToResourceFinder(IResource toAdd, IDictionary<string, IResource> finder)
        {
            string[] makeparts(IResource resource)
            {
                switch (resource)
                {
                    case var _ when resource.IsInternal: return new[] {resource.Name};
                    case var _ when resource.IsInnerResource:
                        var dots = resource.Name.Count('.');
                        return resource.Name.ToLower().Split(new[] {'.'}, dots);
                    default: return resource.Name.ToLower().Split('.');
                }
            }

            var parts = makeparts(toAdd);
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
        /// <param name="viewEnabled">Should the view be enabled?</param>
        /// <param name="setupMenu">Shoud a menu be setup automatically in the view?</param>
        /// <param name="requireApiKey">Should the REST API require an API key?</param>
        /// <param name="allowAllOrigins">Should any origin be allowed to make CORS requests?</param>
        /// <param name="addOns">The add-ons to enable for this RESTar instance</param>
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
            ushort daysToSaveErrors = 30,
            AddOnInfo[] addOns = null)
        {
            #region Fail-fast inits

            uri = uri?.Trim() ?? "/rest";
            if (uri.Contains("?")) throw new ArgumentException("URI cannot contain '?'", nameof(uri));
            var appName = Application.Current.Name;
            if (uri.EqualsNoCase(appName))
                throw new ArgumentException($"URI must differ from application name ({appName})", nameof(appName));
            if (uri[0] != '/') uri = $"/{uri}";
            Settings.Init(port, uri, viewEnabled, prettyPrint, daysToSaveErrors);

            #endregion

            #region Independant inits

            Log.Init();
            DynamitConfig.Init(true, true);

            #endregion

            #region Resource declarations

            typeof(object).GetSubclasses()
                .Where(t => t.HasAttribute<RESTarAttribute>())
                .ForEach(t =>
                {
                    if (t.GetCustomAttributes().Any(a => a.GetType().FullName == "RESTar.SQLite.SQLiteAttribute"))
                        return;
                    Do.TryCatch(() => Resource.AutoRegister(t), e => throw (e.InnerException ?? e));
                });

            #region Migrate resource aliases

            foreach (var alias in Db.SQL<ResourceAlias>("SELECT t FROM RESTar.ResourceAlias t").ToList())
            {
                Transact.Trans(() =>
                {
                    new Admin.ResourceAlias
                    {
                        Alias = alias.Alias,
                        _resource = alias.Resource
                    };
                    alias.Delete();
                });
            }

            #endregion

            #region Migrate dynamic resource formats

            var unnamedcount = 1;
            foreach (var resource in DynamicResource.All)
            {
                if (resource.TableName == null && resource.Name.Contains("RESTar.DynamicResource"))
                {
                    var alias = Admin.ResourceAlias.ByResource(resource.Name);
                    Transact.Trans(() =>
                    {
                        resource.TableName = resource.Name;
                        resource.Name = "RESTar.Dynamic." + (alias?.Alias ?? "UntitledResource" + unnamedcount);
                        unnamedcount += 1;
                        if (alias != null)
                            alias._resource = resource.Name;
                    });
                }
            }

            #endregion

            DynamicResource.All.ForEach(Resource.RegisterDynamicResource);
            // AddOns.Init(addOns?.GroupBy(a => a.AddOn).Select(g => g.FirstOrDefault()));

            Resources.GroupBy(r => r.ParentResourceName)
                .Where(group => group.Key != null)
                .ForEach(group =>
                {
                    var parentResource = (IResourceInternal) Resource.SafeGet(group.Key);
                    if (parentResource == null)
                        throw new ResourceDeclarationException(
                            $"Resource types {string.Join(", ", group.Select(item => $"'{item.Name}'"))} are declared " +
                            $"within the scope of another class '{group.Key}', that is not a RESTar resource. Inner " +
                            "resources must be declared within a resource class.");
                    parentResource.InnerResources = group.ToList();
                });

            #endregion

            #region Finishing inits

            RequireApiKey = requireApiKey;
            AllowAllOrigins = allowAllOrigins;
            ConfigFilePath = configFilePath;
            RegisterRESTHandlers(setupMenu);
            Initialized = true;
            UpdateAuthInfo();

            #endregion
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
                    if (string.IsNullOrWhiteSpace(keyString))
                        throw new Exception("An API key was invalid");
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
                    var accessRights = accessRightList.ToAccessRights();
                    var availableResources = Resource<AvailableResource>.Get;
                    if (!accessRights.ContainsKey(availableResources))
                        accessRights.Add(availableResources, new[] {GET});
                    ApiKeys[key] = accessRights;
                    break;
                case JArray apiKeys:
                    apiKeys.ForEach(ReadApiKeys);
                    break;
                default: throw new Exception("Invalid API key XML syntax in config file");
            }
        }
    }
}