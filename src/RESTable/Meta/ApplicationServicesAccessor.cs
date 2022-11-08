using System;
using Microsoft.Extensions.DependencyInjection;

namespace RESTable.Meta;

internal static class ApplicationServicesAccessor
{
    internal static IServiceProvider ApplicationServiceProvider { get; set; } = null!;

    internal static T GetRequiredService<T>() where T : notnull
    {
        return ApplicationServiceProvider.GetRequiredService<T>();
    }
}
