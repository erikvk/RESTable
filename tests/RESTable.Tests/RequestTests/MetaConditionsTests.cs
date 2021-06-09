using System.Linq;
using System.Threading.Tasks;
using RESTable.Results;
using Xunit;

namespace RESTable.Tests.RequestTests
{
    public class MetaConditionsTests : RequestTestBase
    {
        #region Unsafe

        #endregion

        #region Limit

        [Fact]
        public async Task PositiveLimitLimitsEntities()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Limit = 3;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(3, serialized.EntityCount);
        }

        [Fact]
        public async Task ZeroLimitReturnsNoEntities()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Limit = 0;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(0, serialized.EntityCount);
        }

        [Fact]
        public async Task NegativeLimitReturnsAllEntities()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Limit = -1;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(20, serialized.EntityCount);
        }

        #endregion

        #region Offset

        [Fact]
        public async Task PositiveOffsetOffsetsEntities()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Offset = 5;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(15, serialized.EntityCount);
            var first = await result.ToEntities<TestResource>().FirstAsync();
            Assert.Equal(6, first.Id);
        }

        [Fact]
        public async Task ZeroOffsetAppliesNoOffset()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Offset = 0;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(20, serialized.EntityCount);
        }

        [Fact]
        public async Task MaxValueOffsetReturnsNoEntities()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Offset = int.MaxValue;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(0, serialized.EntityCount);
        }

        [Fact]
        public async Task MinValueOffsetReturnsNoEntities()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Offset = int.MinValue;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(0, serialized.EntityCount);
        }
        
        [Fact]
        public async Task NegativeOffsetAppliesNegativeSkip()
        {
            await using var request = Context.CreateRequest<TestResource>();
            request.Selector = () => TestResource.Generate(20);
            request.MetaConditions.Offset = -5;

            await using var result = await request.GetResult();
            await using var serialized = await result.Serialize();
            Assert.Equal(5, serialized.EntityCount);
            var first = await result.ToEntities<TestResource>().FirstAsync();
            Assert.Equal(16, first.Id);
            var last = await result.ToEntities<TestResource>().LastAsync();
            Assert.Equal(20, last.Id);
        }

        #endregion

        #region Order_asc

        #endregion

        #region Order_desc

        #endregion

        #region Select

        #endregion

        #region Add

        #endregion

        #region Rename

        #endregion

        #region Distinct

        #endregion

        #region Search

        #endregion

        #region Search_regex

        #endregion

        #region Safepost

        #endregion
        
        public MetaConditionsTests(RESTableFixture fixture) : base(fixture) { }
    }
}