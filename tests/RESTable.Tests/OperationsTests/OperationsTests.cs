using System;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Requests;
using Xunit;

namespace RESTable.Tests
{
    public class OperationsTestsFlags
    {
        public bool SelectorWasCalled { get; set; }
        public bool AsyncSelectorWasCalled { get; set; }
        public bool InserterWasCalled { get; set; }
        public bool AsyncInserterWasCalled { get; set; }
        public bool UpdaterWasCalled { get; set; }
        public bool AsyncUpdaterWasCalled { get; set; }
        public bool DeleterWasCalled { get; set; }
        public bool AsyncDeleterWasCalled { get; set; }
        public bool CounterWasCalled { get; set; }
        public bool AsyncCounterWasCalled { get; set; }
        public bool ValidatorWasCalled { get; set; }
        public bool AuthenticatorWasCalled { get; set; }
        public bool AsyncAuthenticatorWasCalled { get; set; }

        public void Reset()
        {
            SelectorWasCalled = false;
            AsyncSelectorWasCalled = false;
            InserterWasCalled = false;
            AsyncInserterWasCalled = false;
            UpdaterWasCalled = false;
            AsyncUpdaterWasCalled = false;
            DeleterWasCalled = false;
            AsyncDeleterWasCalled = false;
            CounterWasCalled = false;
            AsyncCounterWasCalled = false;
            ValidatorWasCalled = false;
            AuthenticatorWasCalled = false;
            AsyncAuthenticatorWasCalled = false;
        }
    }

    public class RESTableFixture : IDisposable
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

        public void Dispose()
        {
            Configurator.Dispose();
        }
    }

    public class OperationsTests<TResourceType> : IClassFixture<RESTableFixture> where TResourceType : class
    {
        protected IEntityResource<TResourceType> Resource { get; }
        protected IRequest<TResourceType> Request { get; }
        protected  OperationsTestsFlags OperationsTestsFlags { get; }

        public OperationsTests(RESTableFixture fixture)
        {
            Resource = fixture.Configurator.ResourceCollection.GetResource<TResourceType>() as IEntityResource<TResourceType>;
            OperationsTestsFlags = fixture.OperationsTestsFlags;
            OperationsTestsFlags.Reset();
            Request = new MockContext(fixture.ServiceProvider).CreateRequest<TResourceType>();
        }
    }
}