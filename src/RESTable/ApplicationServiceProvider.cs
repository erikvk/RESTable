using System;

namespace RESTable
{
    internal class ApplicationServiceProvider : IApplicationServiceProvider, IServiceProvider
    {
        private IServiceProvider InjectedProvider { get; }

        public ApplicationServiceProvider(IServiceProvider injectedProvider) => InjectedProvider = injectedProvider;

        public object? GetService(Type serviceType) => InjectedProvider.GetService(serviceType);
    }
}