using System.Linq;
using RESTable.Meta;

namespace RESTable
{
    public class RESTableConfiguration
    {
        public Method[] Methods { get; }
        public string Version { get; }
        public string[] ReservedNamespaces { get; }
        public string RootUri { get; private set; }
        public bool RequireApiKey { get; private set; }
        public bool AllowAllOrigins { get; private set; }
        public bool NeedsConfigurationFile => RequireApiKey || !AllowAllOrigins;
        public string ConfigurationFilePath { get; private set; }
        public ushort NrOfErrorsToKeep { get; private set; }

        public RESTableConfiguration()
        {
            Methods = EnumMember<Method>.Values;
            var version = typeof(RESTableConfigurator).Assembly.GetName().Version;
            if (version is not null) 
                Version = $"{version.Major}.{version.Minor}.{version.Build}";
            ReservedNamespaces = typeof(RESTableConfigurator).Assembly
                .GetTypes()
                .Select(type => type.Namespace?.ToLower())
                .Where(ns => ns != null)
                .Distinct()
                .ToArray();
        }

        internal void Update
        (
            string rootUri,
            bool requireApiKey,
            bool allowAllOrigins,
            string configurationFilePath,
            ushort nrOfErrorsToKeep = 2000
        )
        {
            RootUri = rootUri;
            RequireApiKey = requireApiKey;
            AllowAllOrigins = allowAllOrigins;
            ConfigurationFilePath = configurationFilePath;
            NrOfErrorsToKeep = nrOfErrorsToKeep;
        }
    }
}