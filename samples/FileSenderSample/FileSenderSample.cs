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
using RESTable;
using RESTable.AspNetCore;
using RESTable.Resources;

namespace FileSenderSample
{
    public class FileSenderSample
    {
        public static void Main(string[] args) => WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<FileSenderSample>()
            .Build()
            .Run();

        public void ConfigureServices(IServiceCollection services) => services
            .AddHttpContextAccessor()
            .Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true) // needed since RESTable still uses synchronous JSON serialization (Newtonsoft)
            .AddMvc(o => o.EnableEndpointRouting = false);

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
            app.UseWebSockets();
            RESTableConfig.Init(services: app.ApplicationServices);
            app.UseRESTableAspNetCore();
        }
    }

    /// <summary>
    /// Used to make some mock file uploads, available at wss://localhost:5001/restable/mockelymock
    /// </summary>
    [RESTable]
    public class MockelyMock : Terminal
    {
        protected override async Task Open()
        {
            await WebSocket.SendText("I'm a mockely mock!");
        }

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
            var startedConnections = FileSenderConnection
                .ActiveConnections
                .Where(c => !c.Deactivated && c.Status == "Started");
            foreach (var connection in startedConnections)
            {
                await connection.SendFile(file);
                file.Seek(0, SeekOrigin.Begin);
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

        public async Task Notify(FileStream fileStream, FileSenderConnection connection)
        {
            await WebSocket.SendText($"{DateTime.Now:HH:mm:ss} A file with path {fileStream.Name} ({fileStream.Length} bytes) was sent to the following connection:");
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
    /// This resource lets clients connect and receive files. Available at wss://localhost:5001/restable/filesenderconnection
    /// </summary>
    [RESTable]
    public class FileSenderConnection : Terminal, IAsyncDisposable
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
        public string Id { get; set; } = "Unknown";

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

        protected override async Task Open()
        {
            OpenedAt = DateTime.Now;
            ActiveConnections.Add(this);
            await WebSocket.SendText("I'm a connection!");
        }

        public async Task SendFile(FileStream stream)
        {
            foreach (var manager in FileSenderManager.Managers)
                await manager.Notify(stream, this);

            await WebSocket.SendBinary(stream);
            SentFilesCount += 1;
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
                        await SendFile(file);
                    }
                    await WebSocket.SendText("All files sent!");
                    break;

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
}