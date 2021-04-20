using System;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;

namespace RESTable.Tests.OperationsTests
{
    public class RESTableFixture
    {
        public RESTableConfigurator Configurator { get; }
        public IServiceProvider ServiceProvider { get; }
        public OperationsTestsFlags OperationsTestsFlags { get; }
        public RESTableContext Context { get; }
        
        public RESTableFixture()
        {
            OperationsTestsFlags = new OperationsTestsFlags();
            ServiceProvider = new ServiceCollection()
                .AddRESTable()
                .AddJsonProvider()
                .AddSingleton(OperationsTestsFlags)
                .BuildServiceProvider();
            Configurator = ServiceProvider
                .GetRequiredService<RESTableConfigurator>();
            var client = ServiceProvider.GetRequiredService<RootClient>();
            Context = new RESTableContext(client, ServiceProvider);
            Configurator.ConfigureRESTable();
        }
    }
}