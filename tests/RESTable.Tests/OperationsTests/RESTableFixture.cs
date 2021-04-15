using System;
using Microsoft.Extensions.DependencyInjection;

namespace RESTable.Tests
{
    public class RESTableFixture
    {
        public RESTableConfigurator Configurator { get; }
        public IServiceProvider ServiceProvider { get; }
        public OperationsTestsFlags OperationsTestsFlags { get; }

        public RESTableFixture()
        {
            OperationsTestsFlags = new OperationsTestsFlags();
            ServiceProvider = new ServiceCollection()
                .AddRESTable()
                .AddJsonProvider()
                .AddSingleton(OperationsTestsFlags)
                .BuildServiceProvider();
            Configurator = ServiceProvider
                .GetService<RESTableConfigurator>();
            Configurator.ConfigureRESTable();
        }
    }
}