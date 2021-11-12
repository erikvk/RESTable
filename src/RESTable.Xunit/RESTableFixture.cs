using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.Requests;

namespace RESTable.Xunit
{
    public class RESTableFixture : ServiceCollection, IServiceProvider
    {
        public RESTableContext Context
        {
            get
            {
                var client = ServiceProvider.GetRequiredService<RootClient>();
                return new RESTableContext(client, ServiceProvider);
            }
        }

        private IServiceProvider? _serviceProvider;

        private IServiceProvider ServiceProvider
        {
            get => _serviceProvider ?? throw new Exception("Not configured!");
            set => _serviceProvider = value;
        }

        public void Configure()
        {
            this.TryAddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            ServiceProvider = this.BuildServiceProvider();
            // Manually initialize RESTable since there's no host
            ActivatorUtilities.CreateInstance<RESTableInitializer>(ServiceProvider);
        }

        public object? GetService(Type serviceType) => ServiceProvider.GetService(serviceType);

        public RESTableFixture()
        {
            this.AddRESTable();
        }
    }
}