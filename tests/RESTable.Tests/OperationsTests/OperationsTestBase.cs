using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Requests;
using Xunit;

namespace RESTable.Tests.OperationsTests
{
    /// <summary>
    /// These tests make sure that the right resource delegates are called during RESTable request evaluation.
    /// </summary>
    /// <typeparam name="TResourceType"></typeparam>
    public class OperationsTestBase<TResourceType> : IClassFixture<RESTableFixture>, IAsyncDisposable where TResourceType : class
    {
        protected IEntityResource<TResourceType> Resource { get; }
        protected IRequest<TResourceType> Request { get; }
        protected OperationsTestsFlags OperationsTestsFlags { get; }

        public OperationsTestBase(RESTableFixture fixture)
        {
            fixture.Configure();
            Resource = (IEntityResource<TResourceType>) fixture
                .GetRequiredService<ResourceCollection>()
                .GetResource<TResourceType>();
            OperationsTestsFlags = fixture.OperationsTestsFlags;
            OperationsTestsFlags.Reset();
            Request = fixture.Context.CreateRequest<TResourceType>();
        }

        public ValueTask DisposeAsync() => Request.DisposeAsync();
    }
}