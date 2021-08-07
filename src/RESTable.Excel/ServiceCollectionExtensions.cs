#nullable enable

using System;
using RESTable.ContentTypeProviders;
using RESTable.Excel;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExcelProvider(this IServiceCollection serviceCollection, Action<ExcelSettings>? excelSettingsAction = null)
        {
            var excelSettings = new ExcelSettings();
            excelSettingsAction?.Invoke(excelSettings);
            serviceCollection.AddSingleton<ExcelSettings>(excelSettings);
            serviceCollection.AddJson();
            serviceCollection.AddSingleton<IContentTypeProvider, ExcelContentTypeProvider>();
            return serviceCollection;
        }
    }
}