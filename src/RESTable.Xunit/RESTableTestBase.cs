using Xunit;

namespace RESTable.Xunit
{
    /// <summary>
    /// A test class for tests that depend on RESTable, that injects a RESTableFixture using Xunit IClassFixture.
    /// Services can be added to the fixture since it is a service collection. By default, only RESTable services,
    /// added with AddRESTable() are added. When services are added, Configure() should be called to configure the
    /// service provider. After Configure() has been called, no further services can be added.
    /// </summary>
    public class RESTableTestBase : IClassFixture<RESTableFixture>
    {
        protected RESTableFixture Fixture { get; }

        public RESTableTestBase(RESTableFixture fixture)
        {
            Fixture = fixture;
        }
    }
}