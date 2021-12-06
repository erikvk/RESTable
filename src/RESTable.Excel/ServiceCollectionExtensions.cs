#nullable enable

using System.Text;
using RESTable.ContentTypeProviders;
using RESTable.Excel;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExcelProvider(this IServiceCollection serviceCollection, Encoding? encoding = null)
    {
        serviceCollection.AddOptions<ExcelOptions>().Configure(o => o.Encoding = encoding ?? Encoding.Default);
        serviceCollection.AddSingleton<IContentTypeProvider, ExcelContentTypeProvider>();
        return serviceCollection;
    }
}