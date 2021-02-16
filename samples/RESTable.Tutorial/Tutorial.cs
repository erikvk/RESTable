using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RESTable.Excel;
using RESTable.OData;
using RESTable.ProtocolProviders;
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
            .AddSingleton<IProtocolProvider, ODataProtocolProvider>()
            .AddSingleton<IEntityResourceProvider>(new SQLiteEntityResourceProvider("./database"))
            .AddExcelContentProvider()
            .AddHttpContextAccessor()
            .AddMvc(o => o.EnableEndpointRouting = false);

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
            app.UseWebSockets();
            RESTableConfig.Init
            (
                uri: "/restable",
                requireApiKey: true,
                configFilePath: "./Config.xml",
                services: app.ApplicationServices
            );
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

    #endregion

    #region Tutorial 2

    /// <summary>
    /// RESTable will generate an instance of this class when a client makes a GET request to /chatbot
    /// with a WebSocket handshake.
    /// </summary>
    [RESTable]
    public class Chatbot : ITerminal
    {
        /// <summary>
        /// Each time this class is instantiated, an IWebSocket instance will be assigned to the
        /// WebSocket property. This object holds the WebSocket connection to the connected client.
        /// We can, for example, send text to the client by making a call to WebSocket.SendText().
        /// </summary>
        public IWebSocket WebSocket { private get; set; }

        /// <summary>
        /// This method is called when the WebSocket is opened towards this Chatbot instance. A perfect
        /// time to send a welcome message.
        /// </summary>
        public async Task Open() => await WebSocket.SendText(
            "> Hi, I'm a chatbot! Type anything, and I'll try my best to answer. I like to tell jokes... " +
            "(type QUIT to return to the shell)"
        );

        /// <summary>
        /// Here we inform RESTable that instances of Chatbot can handle text input
        /// </summary>
        public bool SupportsTextInput { get; } = true;

        /// <summary>
        /// ... but not binary input
        /// </summary>
        public bool SupportsBinaryInput { get; } = false;

        /// <summary>
        /// This method defines the logic that is run when an incoming text message is received over the
        /// WebSocket that is assigned to this terminal.
        /// </summary>
        public async Task HandleTextInput(string input)
        {
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
            {
                await WebSocket.DirectToShell();
                return;
            }

            var response = await GetChatbotResponse(input);
            await WebSocket.SendText(response);
        }

        internal static async Task<string> GetChatbotResponse(string input)
        {
            var response = await ChatbotAPI.GetResponse(input);
            return response.result?.fulfillment?.speech ?? "I have no response to that. Sorry...";
        }

        /// <summary>
        /// We still need to implement this method, but it is never called, since SupportsBinaryInput is
        /// set to false.
        /// </summary>
        public Task HandleBinaryInput(byte[] input) => throw new NotImplementedException();

        #region DialogFlow API

        /// <summary>
        /// A simple API for a pre-defined DialogFlow chatbot.
        /// </summary>
        private static class ChatbotAPI
        {
            private const string AccessToken = "6d7be132f63e48bab18531ec41364673";

            private static readonly AuthenticationHeaderValue Authorization = new("Bearer", AccessToken);
            private static readonly HttpClient HttpClient = new();
            private static readonly string SessionId = Guid.NewGuid().ToString();

            /// <summary>
            /// Sends the input to the chatbot service API, and returns the text response
            /// </summary>
            internal static async Task<dynamic> GetResponse(string input)
            {
                var uri = $"https://api.dialogflow.com/v1/query?v=20170712&query={WebUtility.UrlEncode(input)}" +
                          $"&lang=en&sessionId={SessionId}&timezone={TimeZoneInfo.Local.DisplayName}";
                var message = new HttpRequestMessage(HttpMethod.Get, uri) {Headers = {Authorization = Authorization}};
                using var response = await HttpClient.SendAsync(message);
                var responseString = await response.Content.ReadAsStringAsync();
                return JObject.Parse(responseString);
            }
        }

        #endregion

        /// <summary>
        /// If the terminal resource has additional resources tied to an instance, this is were we release
        /// them.
        /// </summary>
        public ValueTask DisposeAsync() => default;
    }

    /// <summary>
    /// "ChatRoom" is an appropriate name for the resource from the client's perspective, even though
    /// each instance of this resource will work more like a chat participant.
    /// </summary>
    [RESTable]
    public class ChatRoom : ITerminal
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

        public async Task Open()
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

        public async Task HandleTextInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
            {
                await WebSocket.DirectToShell();
                return;
            }

            await SendToAll($"> {Name}: {input}");
            if (input.Length > 5 && input.StartsWith("@bot ", OrdinalIgnoreCase))
            {
                var message = input.Split("@bot ")[1];
                var response = Chatbot.GetChatbotResponse(message);
                await SendToAll($"> Chatbot: {response}");
            }
        }

        private static async Task SendToAll(string message)
        {
            var tasks = Terminals.Select(terminal => terminal.WebSocket.SendText(message));
            await Task.WhenAll(tasks);
        }

        public IWebSocket WebSocket { private get; set; }
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;
        public Task HandleBinaryInput(byte[] input) => throw new NotImplementedException();
    }
}

#endregion