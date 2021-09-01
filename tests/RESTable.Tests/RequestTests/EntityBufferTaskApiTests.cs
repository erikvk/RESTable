﻿using System.Threading.Tasks;
using RESTable.Requests;
using Xunit;

namespace RESTable.Tests.RequestTests
{
    public class EntityBufferTaskApiTests : RESTableTestBase
    {
        [Fact]
        public async Task EntitiesTests()
        {
            var context = Fixture.Context;

            var _ = await context.Entities<GetApiTestResource>();
            var entitiesRanged = await context.Entities<GetApiTestResource>().Within(..5);
            Assert.Equal(5, entitiesRanged.Length);
        }

        [Fact]
        public async Task RangeTests()
        {
            var context = Fixture.Context;

            var all = await context.Get<GetApiTestResource>(..);
            Assert.Equal(60, all.Length);

            var nr3To10 = await context.Get<GetApiTestResource>(3..10);
            Assert.Equal(7, nr3To10.Length);
            Assert.Equal(3, nr3To10.Span[0].Number);
            Assert.Equal(9, nr3To10.Span[^1].Number);

            var last1 = await context.Entities<GetApiTestResource>()[^1];
            Assert.Equal(59, last1.Number);

            var lastThree = await context.Get<GetApiTestResource>(^3..);
            Assert.Equal(3, lastThree.Length);
            var last2 = lastThree.Span[2];
            Assert.Equal(59, last2.Number);
        }

        [Fact]
        public async Task SliceTests()
        {
            var context = Fixture.Context;

            var all = context.Entities<GetApiTestResource>();

            var first30 = all[..30];
            var first30Buffer = await first30;
            Assert.Equal(30, first30Buffer.Length);

            var tentotwenty = first30[10..20];
            var tentotwentyBuffer = await tentotwenty;
            Assert.Equal(10, tentotwentyBuffer.Length);
            Assert.Equal(10, tentotwentyBuffer.Span[0].Number);
            Assert.Equal(19, tentotwentyBuffer.Span[^1].Number);

            var nr19 = await tentotwenty[^1];
            Assert.Equal(19, nr19.Number);
            var nr13 = await tentotwenty[3];
            Assert.Equal(13, nr13.Number);

            var fifteenTo18 = await tentotwenty[^5..^1];
            Assert.Equal(15, fifteenTo18.Span[0].Number);
            Assert.Equal(18, fifteenTo18.Span[^1].Number);
        }

        [Fact]
        public async Task FilterTests()
        {
            var context = Fixture.Context;

            var all = context.Entities<GetApiTestResource>();

            var greaterThanOrEqual10 = all.Where("Number", Operators.GREATER_THAN_OR_EQUALS, 10);
            var greaterThan10Buffer = await greaterThanOrEqual10;
            Assert.Equal(50, greaterThan10Buffer.Length);

            var all3 = await greaterThanOrEqual10.WithNoConditions();
            Assert.Equal(60, all3.Length);
        }

        [Fact]
        public async Task PatchInPlaceTests()
        {
            var context = Fixture.Context;

            var all = context.Entities<GetApiPatchInPlaceTestResource>();

            var third = await all[2];
            third.Number = 10;
            var last = await all[^1];
            last.Number = 11;

            var allBuffer = await all;
            Assert.Equal(10, allBuffer.Span[2].Number);
            Assert.Equal(11, allBuffer.Span[^1].Number);
        }

        [Fact]
        public async Task ExplicitPatchTests()
        {
            var context = Fixture.Context;

            var all = context.Entities<GetApiExplicitPatchTestResource>();
            var allBuffer = await all;

            var third = allBuffer.Span[2];
            third.Number = 10;
            var last = allBuffer.Span[^1];
            last.Number = 11;

            var allBuffer2 = await all;

            // No effect yet
            Assert.Equal(2, allBuffer2.Span[2].ActualNumber);
            Assert.Equal(4, allBuffer2.Span[^1].ActualNumber);

            // We run a patch with the buffer containing the changes. 
            // we get back the updated entities, and can see that their state is correct
            var updated = await all.Patch(allBuffer);
            Assert.Equal(2, updated.Length);
            Assert.Equal(10, updated.Span[0].ActualNumber);
            Assert.Equal(11, updated.Span[1].ActualNumber);

            // Now the changes are propagated to the resource
            var allBuffer3 = await all;
            Assert.Equal(10, allBuffer3.Span[2].ActualNumber);
            Assert.Equal(11, allBuffer3.Span[^1].ActualNumber);
        }

        public EntityBufferTaskApiTests(RESTableFixture fixture) : base(fixture)
        {
            Fixture.Configure();
        }
    }
}