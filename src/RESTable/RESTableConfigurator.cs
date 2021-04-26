using System;
using System.Text.RegularExpressions;
using RESTable.Auth;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta.Internal;
using RESTable.Meta;

namespace RESTable
{
    /// <summary>
    /// The main configuration class for the RESTable instance. Use Init() to 
    /// initialize the instance.
    /// </summary>
    public class RESTableConfigurator
    {
        private ResourceFactory ResourceFactory { get; }
        private ProtocolProviderManager ProtocolProviderManager { get; }
        private RESTableConfiguration Configuration { get; }

        public TypeCache TypeCache { get; }
        public ResourceCollection ResourceCollection { get; }

        internal RootAccess RootAccess { get; }

        public RESTableConfigurator
        (
            TypeCache typeCache,
            ResourceCollection resourceCollection,
            ResourceFactory resourceFactory,
            ProtocolProviderManager protocolProviderManager,
            RESTableConfiguration configuration,
            IJsonProvider jsonProvider,
            RootAccess rootAccess
        )
        {
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
            ResourceFactory = resourceFactory;
            ProtocolProviderManager = protocolProviderManager;
            Configuration = configuration;
            RootAccess = rootAccess;
            ApplicationServicesAccessor.JsonProvider = jsonProvider;
            ApplicationServicesAccessor.ResourceCollection = resourceCollection;
            ApplicationServicesAccessor.TypeCache = typeCache;
        }

        public bool IsConfigured { get; private set; }

        public void ConfigureRESTable
        (
            string uri = "/restable",
            ushort nrOfErrorsToKeep = 2000
        )
        {
            ValidateUri(ref uri);
            Configuration.Update
            (
                rootUri: uri,
                nrOfErrorsToKeep: nrOfErrorsToKeep
            );
            ResourceCollection.SetDependencies(this, TypeCache);
            ResourceFactory.SetConfiguration(this);
            ResourceFactory.MakeResources();
            IsConfigured = true;
            UpdateConfiguration();
            ResourceFactory.BindControllers();
            ResourceFactory.FinalCheck();
            ProtocolProviderManager.OnInit();
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

        public void UpdateConfiguration()
        {
            RootAccess.Load();
        }
    }
}