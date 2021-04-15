using System;
using System.Collections.Generic;
using System.IO;
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
using RESTable.WebSockets;

namespace FileSenderSample
{
    #region ASP.NET app code

    public class FileSenderSample
    {
        public static void Main(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<FileSenderSample>()
            .Build()
            .Run();

        public void ConfigureServices(IServiceCollection services) => services
            .AddRESTable()
            .AddJsonProvider()
            .Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true) // needed since RESTable still uses synchronous JSON serialization (Newtonsoft)
            .AddHttpContextAccessor();

        public void Configure(IApplicationBuilder app ) => app
            .UseWebSockets()
            .UseRESTableAspNetCore();
    }

    #endregion

    /// <summary>
    /// This resource lets clients connect and receive files. Available at wss://localhost:5001/restable/filesenderconnection
    /// </summary>
    [RESTable]
    public class FileSenderConnection : Terminal, IAsyncDisposable, IValidator<FileSenderConnection>
    {
        // We store all active connections here, so we can use them from a separate resource
        internal static TerminalSet<FileSenderConnection> ActiveConnections { get; } = new();

        // Public properties are visible for the client:
        public string Status { get; private set; } = "Stopped";

        // Admin override for deactivating this connection
        internal bool Deactivated { get; set; }

        /// <summary>
        /// We expect clients to set this. We could have a mechanism where this is
        /// required when the connection is established, and immutable when the connection is active.
        /// </summary>
        [RESTableMember(readOnly: true)]
        public string Id { get; set; }

        /// <summary>
        /// This is a read-only file count, set here on the server
        /// </summary>
        public long SentFilesCount { get; internal set; }

        /// <summary>
        /// The time this connection has been up
        /// </summary>
        public string Uptime => (DateTime.Now - OpenedAt).ToString("c");

        /// <summary>
        /// The IP of the connected client
        /// </summary>
        public string ClientIp => WebSocket.Context.Client.ClientIp;

        private DateTime OpenedAt { get; set; }

        public IEnumerable<InvalidMember> Validate(FileSenderConnection entity, RESTableContext context)
        {
            foreach (var existing in ActiveConnections)
            {
                if (existing.Id == entity.Id)
                    throw new Exception($"A connection with id {existing.Id} is already connected");
            }
            
            if (string.IsNullOrWhiteSpace(entity.Id))
                yield return this.Invalidate(i => i.Id, "Expected a non-null, non-whitespace ID for this connection");
        }

        protected override async Task Open()
        {
            OpenedAt = DateTime.Now;
            ActiveConnections.Add(this);
            await WebSocket.SendText($"Hi, I'm a connection named {Id}!");
        }

        public async Task FileSent(string fileName, long fileLength)
        {
            SentFilesCount += 1;
            foreach (var manager in FileSenderManager.Managers)
                await manager.Notify(fileName, fileLength, this);
        }

        public override async Task HandleTextInput(string input)
        {
            if (Deactivated)
            {
                await WebSocket.SendText("OOPS, this connection is deactivated! ¯\\_(ツ)_/¯ ");
                return;
            }

            switch (input.ToUpper())
            {
                case "HI":
                    await WebSocket.SendText("Hello");
                    break;

                case "START":
                    await WebSocket.SendText("Started!");
                    Status = "Started";
                    break;

                case "STOP":
                    await WebSocket.SendText("Stopped!");
                    Status = "Stopped";
                    break;

                case "SEND ALL":
                    foreach (var file in MockelyMock.GetAllFilePaths().Select(File.OpenRead))
                    {
                        await WebSocket.SendBinary(file);
                        await FileSent(file.Name, file.Length);
                    }
                    await WebSocket.SendText("All files sent!");
                    break;

                case var blank when string.IsNullOrWhiteSpace(blank): break;

                default:
                    await WebSocket.SendText("Unrecognized command " + input);
                    break;
            }
        }

        protected override bool SupportsTextInput => true;

        public ValueTask DisposeAsync()
        {
            ActiveConnections.Remove(this);
            return default;
        }
    }

    /// <summary>
    /// This resource gives an administrator an overview over opened file sender connections. Available at wss://localhost:5001/restable/filesendermanager
    /// </summary>
    [RESTable]
    public class FileSenderManager : Terminal, IAsyncDisposable
    {
        internal static TerminalSet<FileSenderManager> Managers { get; } = new();

        protected override async Task Open()
        {
            Managers.Add(this);
            await WebSocket.SendText("I'm a manager!");
        }

        public ValueTask DisposeAsync()
        {
            Managers.Remove(this);
            return default;
        }

        public async Task Notify(string fileName, long fileLength, FileSenderConnection connection)
        {
            await WebSocket.SendText($"{DateTime.Now:HH:mm:ss} A file with path {fileName} ({fileLength} bytes) was sent to the following connection:");
            await WebSocket.SendJson(connection);
        }

        public IEnumerable<FileSenderConnection> ActiveConnections => FileSenderConnection.ActiveConnections;

        public override async Task HandleTextInput(string input)
        {
            var commandArg = input.Split(" ");
            var command = commandArg[0].ToUpper();
            var id = commandArg.ElementAtOrDefault(1);

            switch (command)
            {
                case "INFO":
                    await WebSocket.SendJson(ActiveConnections);
                    break;

                case "DEACTIVATE":
                {
                    var connection = ActiveConnections.FirstOrDefault(c => c.Id == id);
                    if (connection != null)
                    {
                        connection.Deactivated = true;
                        await WebSocket.SendText("Done!");
                    }
                    else await WebSocket.SendText($"Found no connection with Id {id}");

                    break;
                }
                case "ACTIVATE":
                {
                    var connection = ActiveConnections.FirstOrDefault(c => c.Id == id);
                    if (connection != null)
                    {
                        connection.Deactivated = false;
                        await WebSocket.SendText("Done!");
                    }
                    else await WebSocket.SendText($"Found no connection with Id {id}");
                    break;
                }

                default:
                    await WebSocket.SendText("Unrecognized command " + input);
                    break;
            }
        }

        protected override bool SupportsTextInput => true;
    }

    /// <summary>
    /// Used to make some mock file uploads, available at wss://localhost:5001/restable/mockelymock
    /// </summary>
    [RESTable]
    public class MockelyMock : Terminal
    {
        protected override async Task Open() => await WebSocket.SendText("I'm a mockely mock!");

        /// <summary>
        /// This resource just sends a file to all file sender connections without
        /// asking any questions.
        /// </summary>
        public override async Task HandleTextInput(string input)
        {
            var path = $"C:/SampleFiles/{input}.txt";
            FileStream file;
            try
            {
                file = File.OpenRead(path);
            }
            catch
            {
                await WebSocket.SendText($"Nope, {path} is not a valid path");
                return;
            }
            var combinedTerminals = FileSenderConnection
                .ActiveConnections
                .Where(c => !c.Deactivated && c.Status == "Started")    
                .CombineTerminals();
            
            if (combinedTerminals.Count == 0)
            {
                await WebSocket.SendText("No started connections");
                return;
            }

            await combinedTerminals.CombinedWebSocket.SendBinary(file);
            foreach (var terminal in combinedTerminals)
            {
                await terminal.FileSent(file.Name, file.Length);
            }
        }

        public static List<string> GetAllFilePaths() => new()
        {
            "C:/SampleFiles/Sample1.txt",
            "C:/SampleFiles/Sample2.txt",
            "C:/SampleFiles/Sample3.txt"
        };

        protected override bool SupportsTextInput => true;
    }
}