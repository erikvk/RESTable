using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.ContentTypeProviders;
using RESTable.Excel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExcelProvider(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddJsonProvider();
            serviceCollection.TryAddSingleton<IContentTypeProvider, ExcelContentTypeProvider>();
            return serviceCollection;
        }
    }
}