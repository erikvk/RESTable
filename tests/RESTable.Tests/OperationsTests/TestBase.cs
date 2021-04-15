using RESTable.Meta;
using RESTable.Requests;
using Xunit;

namespace RESTable.Tests
{
    /// <summary>
    /// These tests make sure that the right resource delegates are called during RESTable request evaluation.
    /// </summary>
    /// <typeparam name="TResourceType"></typeparam>
    public class TestBase<TResourceType> : IClassFixture<RESTableFixture> where TResourceType : class
    {
        protected IEntityResource<TResourceType> Resource { get; }
        protected IRequest<TResourceType> Request { get; }
        protected OperationsTestsFlags OperationsTestsFlags { get; }

        public TestBase(RESTableFixture fixture)
        {
            Resource = fixture.Configurator.ResourceCollection.GetResource<TResourceType>() as IEntityResource<TResourceType>;
            OperationsTestsFlags = fixture.OperationsTestsFlags;
            OperationsTestsFlags.Reset();
            Request = new MockContext(fixture.ServiceProvider).CreateRequest<TResourceType>();
        }
    }
}