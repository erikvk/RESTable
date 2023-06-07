using System;

namespace RESTable;

internal class ApplicationServiceProvider : IApplicationServiceProvider, IServiceProvider
{
    public ApplicationServiceProvider(IServiceProvider injectedProvider)
    {
        InjectedProvider = injectedProvider;
    }

    private IServiceProvider InjectedProvider { get; }

    public object? GetService(Type serviceType)
    {
        return InjectedProvider.GetService(serviceType);
    }
}
