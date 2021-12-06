using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Xunit;

namespace RESTable.Tests.OperationsTests;

/// <summary>
///     These tests make sure that the right resource delegates are called during RESTable request evaluation.
/// </summary>
/// <typeparam name="TResourceType"></typeparam>
public class OperationsTestBase<TResourceType> : RESTableTestBase, IAsyncDisposable where TResourceType : class
{
    public OperationsTestBase(RESTableFixture fixture) : base(fixture)
    {
        fixture.AddSingleton<OperationsTestsFlags>();
        fixture.Configure();
        Resource = (IEntityResource<TResourceType>) fixture
            .GetRequiredService<ResourceCollection>()
            .GetResource<TResourceType>();
        OperationsTestsFlags = fixture.GetRequiredService<OperationsTestsFlags>();
        OperationsTestsFlags.Reset();
        Request = fixture.Context.CreateRequest<TResourceType>();
    }

    protected IEntityResource<TResourceType> Resource { get; }
    protected IRequest<TResourceType> Request { get; }
    protected OperationsTestsFlags OperationsTestsFlags { get; }

    public ValueTask DisposeAsync()
    {
        return Request.DisposeAsync();
    }
}