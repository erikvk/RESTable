using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;

namespace RESTable.Excel
{
    public static class ExtensionMethods
    {
        public static IServiceCollection AddExcelContentProvider(this IServiceCollection services)
        {
            services.Add(new ServiceDescriptor(typeof(IContentTypeProvider), new ExcelJsonAdapter()));
            return services;
        }
    }
}