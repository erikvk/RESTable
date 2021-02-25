using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable;
using RESTable.ProtocolProviders;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRESTable
        (
            this IServiceCollection serviceCollection,
            string uri = "/restable",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            ushort nrOfErrorsToKeep = 2000,
            LineEndings lineEndings = LineEndings.Environment
        )
        {
            serviceCollection.AddSingleton<IProtocolProvider, DefaultProtocolProvider>();
            serviceCollection.TryAddSingleton(pr => new RESTableConfig
            (
                uri: uri,
                requireApiKey: requireApiKey,
                allowAllOrigins: allowAllOrigins,
                configFilePath: configFilePath,
                prettyPrint: prettyPrint,
                nrOfErrorsToKeep: nrOfErrorsToKeep,
                lineEndings: lineEndings,
                services: pr
            ));
            return serviceCollection;
        }
    }
}