using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable.SQLite.Example
{
    using RESTable;

    /// <summary>
    /// A simple RESTable application
    /// </summary>
    public class Program
    {
        public static void Main()
        {
            new ServiceCollection()
                .AddSqliteProvider("\\data_debug2")
                .AddJsonProvider()
                .AddRESTable()
                .BuildServiceProvider()
                .GetRequiredService<RESTableConfigurator>()
                .ConfigureRESTable(requireApiKey: true, configFilePath: "./Config.xml");

            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests.
            // The 'configFilePath' points towards the configuration file, which contains API keys. In this case,
            //   this file is located in the project folder.
            // The 'resourceProviders' parameter is used for SQLite integration
        }
    }

    [RESTable]
    public class ScResource
    {
        public string STR { get; set; }
        public int INT { get; set; }
        public DateTime DATETIME { get; set; }
        public decimal DECIMAL { get; set; }
    }

    public enum MyEnum
    {
        A,
        B,
        C
    }

    [RESTable, SQLite]
    public class EnumTest : SQLiteTable
    {
        public int Int { get; set; }
        public MyEnum Enum { get; set; }

        protected override Task OnInsert()
        {
            Enum = MyEnum.B;
            return base.OnInsert();
        }
    }

    [RESTable, SQLite]
    public class SQLiteResource : SQLiteTable
    {
        public string STR { get; set; }
        public int INT { get; set; }
        public DateTime DATETIME { get; set; }
        public decimal DECIMAL { get; set; }

        public int STRLength => STR.Length;
    }

    [SQLite]
    public class SQLiteResource2 : SQLiteTable
    {
        public string STR { get; set; }
        public int INT { get; set; }
        public DateTime DATETIME { get; set; }
        public decimal DECIMAL { get; set; }

        public int STRLength => STR.Length;
    }

    [RESTable(GET)]
    public class SuperheroReport : IAsyncSelector<SuperheroReport>
    {
        public long NumberOfSuperheroes { get; private set; }
        public Superhero FirstSuperheroInserted { get; private set; }
        public Superhero LastSuperheroInserted { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// This method returns an IEnumerable of the resource type. RESTable will call this 
        /// on GET requests and send the results back to the client as e.g. JSON.
        /// </summary>
        public async IAsyncEnumerable<SuperheroReport> SelectAsync(IRequest<SuperheroReport> request)
        {
            var superHeroesOrdered = await SQLite<Superhero>
                .Select()
                .OrderBy(r => r.RowId)
                .ToListAsync();
            yield return new SuperheroReport
            {
                NumberOfSuperheroes = await SQLite<Superhero>.Count(),
                FirstSuperheroInserted = superHeroesOrdered.FirstOrDefault(),
                LastSuperheroInserted = superHeroesOrdered.LastOrDefault()
            };
        }
    }

    [SQLite(customTableName: "Heroes"), RESTable]
    public class Superhero : SQLiteTable
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Sex { get; set; }

        [SQLiteMember(columnName: "YearIntroduced")]
        public int Year { get; set; }
    }

    public abstract class MyElastic : ElasticSQLiteTable
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public Settings Settings => Settings.Instance;

        public DateTime Time { get; set; } = DateTime.UtcNow;
    }

    [RESTable]
    public class Event : SQLiteResourceController<Event, MyElastic> { }

    [SQLite, RESTable]
    public class Resource1 : SQLiteTable
    {
        public int Sbyte { get; set; }
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Ushort { get; set; }
        public int Int { get; set; }
        public int Uint { get; set; }
        public long Long { get; set; }
        public long Ulong { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public string String { get; set; }
        public bool Bool { get; set; }
        public DateTime DateTime { get; set; }
    }

    [SQLite, RESTable]
    public class Resource2 : SQLiteTable
    {
        public int Sbyte { get; set; }
        public byte Byte { get; set; }
        public short Short { get; set; }
        public int Ushort { get; set; }
        public int Int { get; set; }
        public int Uint { get; set; }
        public long Long { get; set; }
        public long Ulong { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public string String { get; set; }
        public bool Bool { get; set; }
        public DateTime DateTime { get; set; }
    }
}