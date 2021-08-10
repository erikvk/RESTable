﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RESTable.Auth;
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
        private TypeCache TypeCache { get; }
        private ResourceCollection ResourceCollection { get; }
        private ResourceFactory ResourceFactory { get; }
        private ProtocolProviderManager ProtocolProviderManager { get; }
        private RESTableConfiguration Configuration { get; }
        private RootAccess RootAccess { get; }
        private IEnumerable<IStartupActivator> StartupActivators { get; }

        public RESTableConfigurator
        (
            TypeCache typeCache,
            ResourceCollection resourceCollection,
            ResourceFactory resourceFactory,
            ProtocolProviderManager protocolProviderManager,
            RESTableConfiguration configuration,
            RootAccess rootAccess,
            IEnumerable<IStartupActivator> startupActivators,
            IApplicationServiceProvider applicationServiceProvider
        )
        {
            TypeCache = typeCache;
            ResourceCollection = resourceCollection;
            ResourceFactory = resourceFactory;
            ProtocolProviderManager = protocolProviderManager;
            Configuration = configuration;
            RootAccess = rootAccess;
            StartupActivators = startupActivators;
            ApplicationServicesAccessor.ApplicationServiceProvider = applicationServiceProvider;
        }

        public bool IsConfigured { get; private set; }

        public void ConfigureRESTable(string rootUri = "/restable")
        {
            if (rootUri is null)
                throw new ArgumentNullException(nameof(rootUri));
            ValidateRootUri(ref rootUri);
            Configuration.RootUri = rootUri;
            ResourceCollection.SetDependencies(this, TypeCache, RootAccess);
            ResourceFactory.MakeResources();
            IsConfigured = true;
            RootAccess.Load();
            ResourceFactory.BindControllers();
            ProtocolProviderManager.OnInit();
            ResourceFactory.FinalCheck();
            var startupTasks = StartupActivators.Select(activator => activator.Activate());
            Task.WhenAll(startupTasks).Wait();
        }

        private static void ValidateRootUri(ref string uri)
        {
            uri = uri.Trim();
            if (!Regex.IsMatch(uri, RegEx.BaseUri))
                throw new FormatException("The URI contained invalid characters. It can only contain " +
                                          "letters, numbers, forward slashes and underscores");
            if (uri[0] != '/') uri = $"/{uri}";
            uri = uri.TrimEnd('/');
        }
    }
}