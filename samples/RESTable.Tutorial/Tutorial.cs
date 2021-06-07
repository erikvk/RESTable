using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;
using RESTable.Linq;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.SQLite;
using RESTable.WebSockets;
using static System.StringComparison;
using static RESTable.Method;
using static RESTable.Tutorial.Gender;

namespace RESTable.Tutorial
{
    public static class ExtensionMethods
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IReceivableSourceBlock<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await source.OutputAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                while (source.TryReceive(out var item))
                {
                    yield return item;
                }
            }
            await source.Completion.ConfigureAwait(false); // Propagate possible exception
        }
    }

    #region Tutorial 1

    /// <summary>
    /// A simple RESTable application
    /// </summary>
    public class Tutorial
    {
        public static void Main(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Tutorial>()
            .Build()
            .Run();

        public void ConfigureServices(IServiceCollection services) => services
            .AddODataProvider()
            .AddSqliteProvider(dbPath: "./database")
            .AddExcelProvider()
            .AddJsonProvider()
            .AddRESTable()
            .AddHttpContextAccessor()
            .Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

        public void Configure(IApplicationBuilder app) => app
            .UseWebSockets()
            .UseRESTableAspNetCore();
    }

    /// <summary>
    /// Database is a subset of https://github.com/fivethirtyeight/data/tree/master/comic-characters
    /// - which is, in turn, taken from Marvel and DC Comics respective sites.
    /// </summary>
    [SQLite(customTableName: "Heroes"), RESTable]
    public class Superhero : SQLiteTable
    {
        public string Name { get; set; }

        public bool HasSecretIdentity
        {
            get => Id == "Secret Identity";
            set => Id = value ? "Secret Identity" : "Public Identity";
        }

        public Gender Gender
        {
            get => Sex switch
            {
                "Male Characters" => Male,
                "Female Characters" => Female,
                _ => Other
            };
            set => Sex = value switch
            {
                Male => "Male Characters",
                Female => "Female Characters",
                _ => "Other"
            };
        }

        public int? YearIntroduced
        {
            get => Year == 0 ? null : Year;
            set => Year = value.GetValueOrDefault();
        }

        [RESTableMember(hide: true)] public int Year { get; set; }
        [RESTableMember(hide: true)] public string Id { get; set; }
        [RESTableMember(hide: true)] public string Sex { get; set; }
    }

    public enum Gender
    {
        Male,

        Female,
        Other
    }

    public interface IMyTest
    {
        string Name { get; }
    }

    [RESTable, InMemory]
    public class MyTest : IValidator<IMyTest>, IMyTest
    {
        public string Name { get; set; }

        public IEnumerable<InvalidMember> Validate(IMyTest entity, RESTableContext context)
        {
            if (entity.Name == "Bananas")
                yield return this.MemberInvalid(t => t.Name, "can't be 'Bananas'");
        }
    }

    [RESTable(GET)]
    public class Test2 : IAsyncSelector<Test2>
    {
        public static BufferBlock<int> BufferBlock = new();

        private static int Count;

        public int Number { get; set; }

        public async IAsyncEnumerable<Test2> SelectAsync(IRequest<Test2> request)
        {
            var number = (int) (request.Conditions.Pop(nameof(Number), Operators.EQUALS)?.Value ?? 0);
            for (var i = 0; i < number; i += 1)
            {
                await BufferBlock.SendAsync(Count += 1).ConfigureAwait(false);
            }
            yield break;
        }
    }

    [RESTable(GET)]
    public class Test : IAsyncSelector<Test>
    {
        public int Value { get; set; }

        public async IAsyncEnumerable<Test> SelectAsync(IRequest<Test> request)
        {
            await foreach (var value in Test2.BufferBlock.ToAsyncEnumerable())
            {
                yield return new Test {Value = value};
            }
        }
    }

    [RESTable(GET)]
    public class Test3 : IAsyncSelector<Test3>
    {
        public int Value { get; set; }

        public async IAsyncEnumerable<Test3> SelectAsync(IRequest<Test3> request)
        {
            yield return new Test3 {Value = 0};
            await Task.Delay(2000);
            yield return new Test3 {Value = 1};
            await Task.Delay(2000);
            yield return new Test3 {Value = 2};
            await Task.Delay(2000);
            yield return new Test3 {Value = 3};
            await Task.Delay(2000);
            yield return new Test3 {Value = 4};
            await Task.Delay(2000);
        }
    }

    [RESTable(GET)]
    public class SuperheroReport : IAsyncSelector<SuperheroReport>
    {
        public int NumberOfSuperheroes { get; set; }
        public int NumberOfFemaleHeroes { get; set; }
        public int NumberOfMaleHeroes { get; set; }
        public int NumberOfOtherGenderHeroes { get; set; }
        public Superhero NewestSuperhero { get; set; }

        /// <summary>
        /// This method returns an IEnumerable of the resource type. RESTable will call this
        /// on GET requests and send the results back to the client as e.g. JSON.
        /// </summary>
        public async IAsyncEnumerable<SuperheroReport> SelectAsync(IRequest<SuperheroReport> request)
        {
            var count = 0;
            var newest = default(Superhero);
            var genderCount = new int[3];

            var innerRequest = request.Context.CreateRequest<Superhero>();
            await using var superheroes = await innerRequest.GetResultEntities();
            await foreach (var superhero in superheroes)
            {
                if (count == 0)
                    newest = superhero;
                count += 1;
                genderCount[(int) superhero.Gender] += 1;
                if (superhero.Year > newest?.Year)
                    newest = superhero;
            }

            yield return new SuperheroReport
            {
                NumberOfSuperheroes = count,
                NumberOfFemaleHeroes = genderCount[(int) Female],
                NumberOfMaleHeroes = genderCount[(int) Male],
                NumberOfOtherGenderHeroes = genderCount[(int) Other],
                NewestSuperhero = newest
            };
        }
    }

    [RESTable, SQLite]
    public class Person : ElasticSQLiteTable, IValidator<Person>
    {
        public string Name { get; set; }

        public IEnumerable<InvalidMember> Validate(Person entity, RESTableContext context)
        {
            if (entity.Name == "Banarne")
                yield return this.MemberInvalid(e => e.Name, "Banarne is not a real name!");
        }
    }

    [RESTable]
    public class PersonController : SQLiteResourceController<PersonController, Person> { }

    #endregion

    #region Tutorial 2

    /// <summary>
    /// "ChatRoom" is an appropriate name for the resource from the client's perspective, even though
    /// each instance of this resource will work more like a chat participant.
    /// </summary>
    [RESTable]
    public class ChatRoom : Terminal, IAsyncDisposable
    {
        /// <summary>
        /// This collection holds all ChatRoom instances
        /// </summary>
        private IEnumerable<ChatRoom> Terminals => Services.GetRequiredService<ITerminalCollection<ChatRoom>>();

        private string _name;

        /// <summary>
        /// The name of the connected chat room participant. To change this, we can write
        /// #terminal {"Name": "new name"} while in the chat room.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                var name = GetUniqueName(value);
                if (Initiated)
                    SendToAll($"# {_name} has changed name to \"{name}\"").Wait();
                _name = name;
            }
        }

        /// <summary>
        /// A read-only list of all chat room participants (names).
        /// </summary>
        public string[] Members => Terminals.Select(t => t.Name).ToArray();

        /// <summary>
        /// The number of connected participants.
        /// </summary>
        public int NumberOfMembers => Terminals.Count();

        /// <summary>
        /// Used internally to track if the participant is initiated. Invisible in the API.
        /// </summary>
        private bool Initiated;

        protected override async Task Open()
        {
            Name = GetUniqueName(Name);
            await SendToAll($"# {Name} has joined the chat room.");
            await WebSocket.SendText(
                $"# Welcome to the chat room! Your name is \"{Name}\" (type QUIT to return to the shell)");
            Initiated = true;
        }

        /// <summary>
        /// Creates a unique name for a participant, or deal with edge cases like a participant naming
        /// themselves nothing or "Chatbot".
        /// </summary>
        private string GetUniqueName(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name) || string.Equals(Name, "chatbot", OrdinalIgnoreCase))
                Name = "Chatter";
            if (!Terminals.Any(c => string.Equals(c.Name, Name, OrdinalIgnoreCase)))
                return Name;
            var modifier = 2;
            var tempName = $"{Name} {modifier}";
            while (Terminals.Any(c => string.Equals(c.Name, tempName, OrdinalIgnoreCase)))
                tempName = $"{Name} {modifier++}";
            return tempName;
        }

        public async ValueTask DisposeAsync()
        {
            await SendToAll($"# {Name} left the chat room.");
        }

        public override async Task HandleTextInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
                await WebSocket.DirectToShell();
        }

        private async Task SendToAll(string message) => await Terminals
            .Combine()
            .CombinedWebSocket
            .SendText(message);

        protected override bool SupportsTextInput => true;
    }
}

#endregion