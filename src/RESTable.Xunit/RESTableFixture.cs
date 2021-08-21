﻿using System;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;

namespace RESTable.Tests
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
            ServiceProvider = this.BuildServiceProvider();
            var configurator = ServiceProvider.GetRequiredService<RESTableConfigurator>();
            configurator.ConfigureRESTable();
        }

        public object? GetService(Type serviceType) => ServiceProvider.GetService(serviceType);

        public RESTableFixture()
        {
            this.AddRESTable();
        }
    }
}