using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.SQLite;
using static System.StringComparison;
using static RESTable.Method;
using static RESTable.Tutorial.Gender;

namespace RESTable.Tutorial
{
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
            .AddSqliteProvider("./database")
            .AddExcelProvider()
            .AddJsonProvider()
            .AddRESTable()
            .AddHttpContextAccessor()
            .Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true)
            .AddMvc(o => o.EnableEndpointRouting = false);

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
            app.UseWebSockets();
            app.UseRESTableAspNetCore();
        }
    }

    /// <summary>
    /// Database is a subset of https://github.com/fivethirtyeight/data/tree/master/comic-characters
    /// - which is, in turn, taken from Marvel and DC Comics respective sites.
    /// </summary>
    [SQLite(CustomTableName = "Heroes"), RESTable]
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
            get => Year == 0 ? (int?) null : Year;
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

            await using var innerRequest = request.Context.CreateRequest<Superhero>();
            await foreach (var superhero in await innerRequest.EvaluateToEntities())
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

        public IEnumerable<InvalidMember> Validate(Person entity)
        {
            if (entity.Name == "Banarne")
                yield return this.Invalidate(e => e.Name, "Banarne is not a real name!");
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
        private static readonly TerminalSet<ChatRoom> Terminals = new();

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
        public int NumberOfMembers => Terminals.Count;

        /// <summary>
        /// Used internally to track if the participant is initiated. Invisible in the API.
        /// </summary>
        private bool Initiated;

        protected override async Task Open()
        {
            Name = GetUniqueName(Name);
            await SendToAll($"# {Name} has joined the chat room.");
            Terminals.Add(this);
            await WebSocket.SendText(
                $"# Welcome to the chat room! Your name is \"{Name}\" (type QUIT to return to the shell)");
            Initiated = true;
        }

        /// <summary>
        /// Creates a unique name for a participant, or deal with edge cases like a participant naming
        /// themselves nothing or "Chatbot".
        /// </summary>
        private static string GetUniqueName(string Name)
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
            Terminals.Remove(this);
            await SendToAll($"# {Name} left the chat room.");
        }

        public override async Task HandleTextInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
                await WebSocket.DirectToShell();
        }

        private static async Task SendToAll(string message)
        {
            var tasks = Terminals.Select(terminal => terminal.WebSocket.SendText(message));
            await Task.WhenAll(tasks);
        }

        protected override bool SupportsTextInput { get; } = true;
    }
}

#endregion