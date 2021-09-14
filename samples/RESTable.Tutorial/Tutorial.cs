using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;
using RESTable.Linq;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Sqlite;
using RESTable.WebSockets;
using static System.StringComparison;
using static RESTable.Method;
using static RESTable.Tutorial.Gender;

namespace RESTable.Tutorial
{
    public static class ExtensionMethods
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>
        (
            this IReceivableSourceBlock<T> source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
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
        public static Task Main(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Tutorial>()
            .Build()
            .RunAsync();

        public void ConfigureServices(IServiceCollection services) => services
            .AddRESTable()
            .AddODataProvider()
            .AddSqliteProvider()
//            .Configure<SqliteOptions>(o => o.SqliteDatabasePath = "./database")
            .AddExcelProvider()
            .AddHttpContextAccessor();

        public void Configure(IApplicationBuilder app) => app
            .UseWebSockets()
            .UseRESTableAspNetCore();
    }

    [RESTable(GET)]
    public record MyRecord(string Name, int Number) : ISelector<MyRecord>
    {
        public IEnumerable<MyRecord> Select(IRequest<MyRecord> request)
        {
            yield return new MyRecord("Foo", 123);
            yield return new MyRecord("Bar", 456);
            yield return new MyRecord("Boo", 789);
        }
    }

    /// <summary>
    /// Database is a subset of https://github.com/fivethirtyeight/data/tree/master/comic-characters
    /// - which is, in turn, taken from Marvel and DC Comics respective sites.
    /// </summary>
    [Sqlite(customTableName: "Heroes"), RESTable]
    public class Superhero : SqliteTable
    {
        public string? Name { get; set; }

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

        [RESTableMember(hide: true)]
        public int Year { get; set; }

        [RESTableMember(hide: true)]
        public string? Id { get; set; }

        [RESTableMember(hide: true)]
        public string? Sex { get; set; }
    }

    public enum Gender
    {
        Male,

        Female,
        Other
    }

    [RESTable(GET)]
    public class SuperheroReport : IAsyncSelector<SuperheroReport>
    {
        public int NumberOfSuperheroes { get; set; }
        public int NumberOfFemaleHeroes { get; set; }
        public int NumberOfMaleHeroes { get; set; }
        public int NumberOfOtherGenderHeroes { get; set; }
        public Superhero? NewestSuperhero { get; set; }

        /// <summary>
        /// This method returns an IEnumerable of the resource type. RESTable will call this
        /// on GET requests and send the results back to the client as e.g. JSON.
        /// </summary>
        public async IAsyncEnumerable<SuperheroReport> SelectAsync(IRequest<SuperheroReport> request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var count = 0;
            var newest = default(Superhero);
            var genderCount = new int[3];

            await foreach (var superhero in request.Context.CreateRequest<Superhero>().GetResultEntities(cancellationToken))
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

    #region A bunch of test things to try

    public interface IMyTest
    {
        string? Name { get; }
    }

    [RESTable, InMemory]
    public class MyDict : Dictionary<string, object?>
    {
        public string? Name { get; set; }
        public MyDict? Inner { get; set; }
        public int? InnerId => Inner?.GetHashCode();
        public List<MyDict> Inners { get; set; } = new();
    }

    [RESTable, InMemory]
    public class MyTest : IValidator<IMyTest>, IMyTest
    {
        public string? Name { get; set; }

        private Dictionary<string, object> dict;

        public Dictionary<string, object> Dict => dict;

        public int DictId => dict.GetHashCode();

        public MyTest? Testy { get; set; }

        [RESTableConstructor]
        public MyTest()
        {
            dict = new Dictionary<string, object>();
        }

        public IEnumerable<InvalidMember> GetInvalidMembers(IMyTest entity, RESTableContext context)
        {
            if (entity.Name == "Bananas")
                yield return this.MemberInvalid(t => t.Name, "can't be 'Bananas'");
        }
    }

    [RESTable]
    public class TerminalTester : Terminal, IAsyncDisposable
    {
        private IWebSocket Connection { get; set; } = null!;

        protected override async Task Open(CancellationToken cancellationToken)
        {
            Connection = await new ClientWebSocketBuilder(WebSocket.Context)
                .WithUri("wss://localhost:5001/restable/myterminaltest")
                .Connect(cancellationToken);
        }

        public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            var tasks = new Task[10];
            var text = "Text";
            var binary = Encoding.UTF8.GetBytes("Binary bananas are the best!");

            Task fragmentedTask;

            // Should lead to frame fragmentation if RESTable does not limit writing threads
            await using (var binaryMessage = await Connection.GetMessageStream(false, cancellationToken))
            {
                await binaryMessage.WriteAsync(binary, cancellationToken);

                // This should not completed until the binary message is sent and the semaphore is released
                // If MaxNumberOfConcurrentWriters > 1, this will lead to frame fragmentation where the text 
                // frame will appear in the binary message (and, being a final frame) close the message.
                fragmentedTask = Task.Run(async () => await Connection.SendText(text, cancellationToken), cancellationToken);

                await Task.Delay(1000, cancellationToken);

                if (fragmentedTask.IsCompleted)
                    throw new Exception("Fragmented task completed!");

                await binaryMessage.WriteAsync(binary, cancellationToken);
            }

            await Task.Delay(1000, cancellationToken);

            if (!fragmentedTask.IsCompleted)
                throw new Exception("Fragmented task not completed!");
        }

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }

    [RESTable]
    public class MyTerminalTest : Terminal
    {
        public override Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override async Task HandleBinaryInput(Stream input, CancellationToken cancellationToken)
        {
            using var streamReader = new StreamReader(input);

            var str = await streamReader.ReadToEndAsync();
        }
    }


    // ReSharper disable UnusedParameter.Local

    [RESTable]
    public class MyTerminal : Terminal
    {
        private Task? RunTask { get; set; }

        protected override Task Open(CancellationToken cancellationToken)
        {
            var observable = Services.GetRequiredService<ITerminalObservable<Shell>>();
            RunTask = Task.Run(async () =>
            {
                await foreach (var terminal in observable.ToAsyncEnumerable().WithCancellation(cancellationToken))
                {
                    await WebSocket.SendText(terminal.TerminalResource.Name, cancellationToken);
                }
            }, cancellationToken);
            return WebSocket.SendText("Now open!", cancellationToken);
        }

        public override Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            return WebSocket.SendText(input, cancellationToken);
        }
    }

    // ReSharper restore UnusedParameter.Local

    [RESTable]
    public class ShellChatter : Terminal
    {
        protected override async Task Open(CancellationToken cancellationToken)
        {
            var context = WebSocket.Context.GetRequiredService<RootContext>();
            await WebSocket.SendText("Opening ShellChatter!", cancellationToken).ConfigureAwait(false);
            await new ClientWebSocketBuilder(context)
                .WithUri("wss://localhost:5001/restable/")
                .OnOpen(async (ws, ct) => await ws.SendText("Hi", ct))
                .HandleTextInput(async (ws, text, ct) =>
                {
                    await WebSocket.SendText(text, ct);
                    await Task.Delay(1000, ct);
                    const string outgoing = "Hi";
                    await ws.SendText(outgoing, ct);
                    await WebSocket.SendText($"(sent: '{outgoing}')", ct);
                })
                .Connect(cancellationToken);
        }

        public override Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [RESTable(GET)]
    public class Test2 : IAsyncSelector<Test2>
    {
        public static readonly BufferBlock<int> BufferBlock = new();

        private static int Count;

        public int Number { get; set; }

        public async IAsyncEnumerable<Test2> SelectAsync(IRequest<Test2> request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var number = (int) (request.Conditions.Pop(nameof(Number), Operators.EQUALS)?.Value ?? 0);
            for (var i = 0; i < number; i += 1)
            {
                await BufferBlock.SendAsync(Count += 1, cancellationToken).ConfigureAwait(false);
            }
            yield break;
        }
    }

    [RESTable(GET)]
    public class Test : IAsyncSelector<Test>
    {
        public int Value { get; set; }

        public async IAsyncEnumerable<Test> SelectAsync(IRequest<Test> request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var value in Test2.BufferBlock.ToAsyncEnumerable(cancellationToken: cancellationToken))
            {
                yield return new Test {Value = value};
            }
        }
    }

    [RESTable(GET)]
    public class Test3 : IAsyncSelector<Test3>
    {
        public int Value { get; set; }

        public async IAsyncEnumerable<Test3> SelectAsync(IRequest<Test3> request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new Test3 {Value = 0};
            await Task.Delay(2000, cancellationToken);
            yield return new Test3 {Value = 1};
            await Task.Delay(2000, cancellationToken);
            yield return new Test3 {Value = 2};
            await Task.Delay(2000, cancellationToken);
            yield return new Test3 {Value = 3};
            await Task.Delay(2000, cancellationToken);
            yield return new Test3 {Value = 4};
            await Task.Delay(2000, cancellationToken);
        }
    }

    [RESTable, Sqlite]
    public class Person : ElasticSqliteTable, IValidator<Person>
    {
        public string? Name { get; set; }

        public IEnumerable<InvalidMember> GetInvalidMembers(Person entity, RESTableContext context)
        {
            if (entity.Name == "Banarne")
                yield return this.MemberInvalid(e => e.Name, "Banarne is not a real name!");
        }
    }

    [RESTable]
    public class PersonController : SqliteResourceController<PersonController, Person> { }

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

        private string? _name;

        /// <summary>
        /// The name of the connected chat room participant. To change this, we can write
        /// #terminal {"Name": "new name"} while in the chat room.
        /// </summary>
        public string? Name
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
        public string?[] Members => Terminals.Select(t => t.Name).ToArray();

        /// <summary>
        /// The number of connected participants.
        /// </summary>
        public int NumberOfMembers => Terminals.Count();

        /// <summary>
        /// Used internally to track if the participant is initiated. Invisible in the API.
        /// </summary>
        private bool Initiated;

        protected override async Task Open(CancellationToken cancellationToken)
        {
            Name = GetUniqueName(Name);
            await SendToAll($"# {Name} has joined the chat room.", cancellationToken);
            await WebSocket.SendText(
                $"# Welcome to the chat room! Your name is \"{Name}\" (type QUIT to return to the shell)", cancellationToken);
            Initiated = true;
        }

        /// <summary>
        /// Creates a unique name for a participant, or deal with edge cases like a participant naming
        /// themselves nothing or "Chatbot".
        /// </summary>
        private string GetUniqueName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name, "chatbot", OrdinalIgnoreCase))
                name = "Chatter";
            if (!Terminals.Any(c => string.Equals(c.Name, name, OrdinalIgnoreCase)))
                return name;
            var modifier = 2;
            var tempName = $"{name} {modifier}";
            while (Terminals.Any(c => string.Equals(c.Name, tempName, OrdinalIgnoreCase)))
                tempName = $"{name} {modifier++}";
            return tempName;
        }

        public async ValueTask DisposeAsync()
        {
            await SendToAll($"# {Name} left the chat room.");
        }

        public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
                await WebSocket.DirectToShell(cancellationToken: cancellationToken);
        }

        private async Task SendToAll(string message, CancellationToken cancellationToken = new()) => await Terminals
            .Combine()
            .CombinedWebSocket
            .SendText(message, cancellationToken);
    }

    #endregion
}

#endregion