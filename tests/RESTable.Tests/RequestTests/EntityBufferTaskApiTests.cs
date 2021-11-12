using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Xunit;
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
        public async Task PutAtZeroWhenEmpty()
        {
            var context = Fixture.Context;
            var all = context.Entities<GetApiExplicitEmptyPatchTestResource1>();
            var allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);
            var entity = new GetApiExplicitEmptyPatchTestResource1(100);
            var ok = await all.Put(0, entity);
            Assert.True(ok);
            var allbuffer2 = await all;
            Assert.Equal(1, allbuffer2.Length);
        }

        [Fact]
        public async Task PutAtGreaterThanZeroWhenEmpty()
        {
            var context = Fixture.Context;
            var all = context.Entities<GetApiExplicitEmptyPatchTestResource2>();
            var allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);
            var entity = new GetApiExplicitEmptyPatchTestResource2(100);
            var ok = await all.Put(5, entity);
            Assert.True(ok);

            var allbuffer2 = await all;
            Assert.Equal(1, allbuffer2.Length);
            Assert.Equal(100, allbuffer2.Span[0].Number);

            // This slice should still be empty
            var five = await all.Slice(5..6);
            Assert.Equal(0, five.Length);
        }

        [Fact]
        public async Task InsertAndDelete()
        {
            var context = Fixture.Context;
            var all = context.Entities<GetApiExplicitEmptyPatchTestResource3>();
            var allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);

            var ok = await all.Insert(new GetApiExplicitEmptyPatchTestResource3(1));
            Assert.True(ok);

            var allBuffer2 = await all;
            Assert.Equal(1, allBuffer2.Length);

            ok = await all.Insert(new GetApiExplicitEmptyPatchTestResource3(2));
            Assert.True(ok);
            var allBuffer3 = await all;
            Assert.Equal(2, allBuffer3.Length);
            Assert.Equal(1, allBuffer3.Span[0].Id);
            Assert.Equal(2, allBuffer3.Span[1].Id);

            ok = await all.Insert(new GetApiExplicitEmptyPatchTestResource3(3));
            Assert.True(ok);
            var allBuffer4 = await all;
            Assert.Equal(3, allBuffer4.Length);
            Assert.Equal(1, allBuffer4.Span[0].Id);
            Assert.Equal(2, allBuffer4.Span[1].Id);
            Assert.Equal(3, allBuffer4.Span[2].Id);

            await all.Delete(1);
            var allBuffer5 = await all;
            Assert.Equal(2, allBuffer5.Length);
            Assert.Equal(3, allBuffer5.Span[1].Id);

            await all.Delete();
            var allBuffer6 = await all;
            Assert.Equal(0, allBuffer6.Length);
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

        [Fact]
        public async Task SinglePatchTests()
        {
            var context = Fixture.Context;

            var all = context.Entities<GetApiExplicitEmptyPatchTestResource4>();
            var allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);

            await context.Insert(new GetApiExplicitEmptyPatchTestResource4(100));

            var first = await context.First<GetApiExplicitEmptyPatchTestResource4>();
            Assert.Equal(100, first.Number);
            first.Number = 200;
            var ok = await context.Patch(0, first);
            Assert.True(ok);
            Assert.Equal(200, first.ActualNumber);
        }

        [Fact]
        public async Task ThreeWaysToPutAndTheyreAllEquivalent()
        {
            var context = Fixture.Context;

            // 1

            var all = context.Entities<GetApiExplicitEmptyPatchTestResource5>();
            var allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);

            {
                var first = all[..1];
                var firstRawBuffer = await first.Raw;
                var item = firstRawBuffer.Span[0] ??= new GetApiExplicitEmptyPatchTestResource5(200);
                item.Number = 300;
                await first.Put(item);

                var firstBuffer2 = await first;
                Assert.Equal(1, firstBuffer2.Length);
                Assert.Equal(300, firstBuffer2.Span[0].ActualNumber);
            }

            await context.Delete<GetApiExplicitEmptyPatchTestResource5>(..);
            allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);

            // 2

            {
                var item = await all.TryAt(0) ?? new GetApiExplicitEmptyPatchTestResource5(200);
                item.Number = 300;
                await all.Slice(0).Put(item);

                var firstBuffer2 = await all[..1];
                Assert.Equal(1, firstBuffer2.Length);
                Assert.Equal(300, firstBuffer2.Span[0].ActualNumber);
            }

            await context.Delete<GetApiExplicitEmptyPatchTestResource5>(..);
            allBuffer = await all;
            Assert.Equal(0, allBuffer.Length);

            // 3

            {
                var first = all.Single(0);
                var item = await first.TryFirst ?? new GetApiExplicitEmptyPatchTestResource5(200);
                item.Number = 300;
                await first.Put(item);

                var firstBuffer2 = await first;
                Assert.Equal(1, firstBuffer2.Length);
                Assert.Equal(300, firstBuffer2.Span[0].ActualNumber);
            }
        }


        public EntityBufferTaskApiTests(RESTableFixture fixture) : base(fixture)
        {
            Fixture.Configure();
        }
    }
}