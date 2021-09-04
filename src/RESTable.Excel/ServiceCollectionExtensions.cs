#nullable enable

using System;
using Microsoft.Extensions.Options;
using RESTable.ContentTypeProviders;
using RESTable.Excel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExcelProvider(this IServiceCollection serviceCollection, Action<OptionsBuilder<ExcelOptions>>? optionsAction = null)
        {
            var builder = serviceCollection.AddOptions<ExcelOptions>();
            optionsAction?.Invoke(builder);
            serviceCollection.AddSingleton<IContentTypeProvider, ExcelContentTypeProvider>();
            return serviceCollection;
        }
    }
}