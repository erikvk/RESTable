using System;
using System.Collections.Generic;
using System.Linq;
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
        private RootAccess RootAccess { get; }
        private TypeCache TypeCache { get; }
        private ResourceCollection ResourceCollection { get; }


        public RESTableConfigurator
        (
            TypeCache typeCache,
            ResourceCollection resourceCollection,
            ResourceFactory resourceFactory,
            ProtocolProviderManager protocolProviderManager,
            RESTableConfiguration configuration,
            IJsonProvider jsonProvider,
            RootAccess rootAccess,
            IEnumerable<IStartupActivator> startupActivators
        )
        {
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
            ResourceFactory = resourceFactory;
            ProtocolProviderManager = protocolProviderManager;
            Configuration = configuration;
            RootAccess = rootAccess;
            _ = startupActivators.ToList();
            ApplicationServicesAccessor.JsonProvider = jsonProvider;
            ApplicationServicesAccessor.ResourceCollection = resourceCollection;
            ApplicationServicesAccessor.TypeCache = typeCache;
        }

        public bool IsConfigured { get; private set; }

        public void ConfigureRESTable(string rootUri = "/restable")
        {
            ValidateRootUri(ref rootUri);
            Configuration.Update(rootUri: rootUri);
            ResourceCollection.SetDependencies(this, TypeCache, RootAccess);
            ResourceFactory.SetConfiguration(this);
            ResourceFactory.MakeResources();
            IsConfigured = true;
            RootAccess.Load();
            ResourceFactory.BindControllers();
            ResourceFactory.FinalCheck();
            ProtocolProviderManager.OnInit();
        }

        private static void ValidateRootUri(ref string uri)
        {
            uri = uri?.Trim() ?? "/rest";
            if (!Regex.IsMatch(uri, RegEx.BaseUri))
                throw new FormatException("The URI contained invalid characters. It can only contain " +
                                          "letters, numbers, forward slashes and underscores");
            if (uri[0] != '/') uri = $"/{uri}";
            uri = uri.TrimEnd('/');
        }
    }
}