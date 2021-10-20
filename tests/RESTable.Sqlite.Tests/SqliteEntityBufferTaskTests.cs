using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Resources;
using RESTable.Xunit;
using Xunit;

namespace RESTable.Sqlite.Tests
{
    [RESTable, Sqlite]
    public class Table : SqliteTable
    {
        public int? Number { get; set; }
    }

    public class SqliteEntityBufferTaskTests : RESTableTestBase
    {
        private void Compare(int index, ReadOnlyMemory<Table> buffer)
        {
            Assert.Equal(index, buffer.Span[index].Number);
        }

        [Fact]
        public async Task Test()
        {
            var context = Fixture.Context;

            var table = context.Entities<Table>();

            var tableBuffer1 = await table;
            Assert.Equal(2, tableBuffer1.Length);
            Compare(0, tableBuffer1);
            Compare(1, tableBuffer1);

            await table.Insert(new Table {Number = 2});
            var tableBuffer2 = await table;
            Assert.Equal(3, tableBuffer2.Length);
            Compare(0, tableBuffer2);
            Compare(1, tableBuffer2);
            Compare(2, tableBuffer2);

            var deleted = await table.Delete();
            Assert.Equal(3, deleted);

            var five = await table.Insert(new Table {Number = 0}, new Table {Number = 1}, new Table {Number = 2}, new Table {Number = 3}, new Table {Number = 4});
            Assert.Equal(5, five.Length);
        }

        public SqliteEntityBufferTaskTests(RESTableFixture fixture) : base(fixture)
        {
            fixture.AddSqliteProvider();
            fixture.Configure();
            var all = Sqlite<Table>.Select();
            Sqlite<Table>.Delete(all).AsTask().Wait();
            var inserted = Sqlite<Table>.Insert(new Table {Number = 0}, new Table {Number = 1}).AsTask().Result;
            Assert.Equal(2, inserted);
        }
    }
}