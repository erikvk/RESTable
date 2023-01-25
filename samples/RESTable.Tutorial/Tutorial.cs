using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RESTable.AspNetCore;
using RESTable.Linq;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Resources.Templates;
using RESTable.WebSockets;
using static System.StringComparison;
using static RESTable.Method;

namespace RESTable.Tutorial;

public static class ExtensionMethods
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>
    (
        this IReceivableSourceBlock<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        while (await source.OutputAvailableAsync(cancellationToken))
        while (source.TryReceive(out var item))
            yield return item;
        await source.Completion; // Propagate possible exception
    }
}

#region Tutorial 1

/// <summary>
///     A simple RESTable application
/// </summary>
public class Tutorial
{
    public Tutorial(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    private IConfiguration Configuration { get; }

    public static Task Main(string[] args)
    {
        return WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Tutorial>()
            .Build()
            .RunAsync();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRESTable()
            .AddODataProvider()
            .AddExcelProvider()
            .AddHttpContextAccessor();
    }

    public void Configure(IApplicationBuilder app)
    {
        app
            .UseWebSockets()
            .UseRESTableAspNetCore();
    }
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

public enum Gender
{
    Male,
    Female,
    Other
}

#region A bunch of test things to try

public class Statics
{
    public string? N { get; set; }
}

[RESTable]
[InMemory]
public class TestResource
{
    public Version? Version { get; set; }

    [JsonConstructor]
    public TestResource(string s, int i)
    {
        S = s;
        I = i;
    }

    public string S { get; }
    public int I { get; }

    [RESTableMember(mergeOntoOwner: true)] public Dictionary<string, object?> InnerDynamics { get; } = new();

    [RESTableMember(mergeOntoOwner: true)] public Statics? InnerStatics { get; set; }
}

public interface IMyTest
{
    string? Name { get; }
}

[RESTable(GET, PATCH)]
public class MySomDict : ResourceWrapper<SomeDict>, IAsyncSelector<SomeDict>, IAsyncUpdater<SomeDict>
{
    private static HashSet<SomeDict> Things { get; } = new() { new SomeDict { ["Foo"] = "Bar" } };

    public IAsyncEnumerable<SomeDict> SelectAsync(IRequest<SomeDict> request, CancellationToken cancellationToken)
    {
        return Things.ToAsyncEnumerable();
    }

    public IAsyncEnumerable<SomeDict> UpdateAsync(IRequest<SomeDict> request, CancellationToken cancellationToken)
    {
        return request.GetInputEntitiesAsync();
    }
}

public class SomeDict : Dictionary<string, string> { }

[RESTable(GET, HEAD)]
public class MyBinary : IBinary<MyBinary>
{
    public BinaryResult Select(IRequest<MyBinary> request)
    {
        var bytes = Encoding.UTF8.GetBytes("FOobar boaoskdkasd okasd pokasdp okasdpo kapdokwdpaokwdpo kadpoakwdp okawdp okawd pokawdp okawdpo kpaowkd ");

        return new BinaryResult((str, ct) => str.WriteAsync(bytes, ct).AsTask(), "text/plain", bytes.LongLength);
    }
}

[RESTable]
[InMemory]
public class MyDict : Dictionary<string, object?>
{
    [RESTableMember(hide: false)] public string? Name { get; set; }

    [RESTableMember(hide: false)] public new int Count => base.Count;

    public MyDict? Inner { get; set; }
    public int? InnerId => Inner?.GetHashCode();
    public List<MyDict> Inners { get; set; } = new();
}

[RESTable]
[InMemory]
public class MyTest : IValidator<IMyTest>, IMyTest
{
    [RESTableConstructor]
    public MyTest()
    {
        Dict = new Dictionary<string, object>();
    }

    public Dictionary<string, object> Dict { get; }

    public int DictId => Dict.GetHashCode();

    public MyTest? Testy { get; set; }
    public string? Name { get; set; }

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

    public ValueTask DisposeAsync()
    {
        return Connection.DisposeAsync();
    }

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

[RESTable]
public class MyOptionsTest : CommandTerminal
{
    protected override IEnumerable<Command> GetCommands()
    {
        yield return new Command("g", "", 0, _ => { });
        yield return new Command("Simple", "Does a thing", 0, _ => { });
        yield return new Command("PrettyLongActually", "Does a thing too", 0, _ => { });
        yield return new Command("PrettyLongA4", "Has a pretty long and winding description of what it does. Usage: first this then that also foo", 0, _ => { });
    }
}

// ReSharper disable UnusedParameter.Local
[RESTable]
public class MyTerminal : Terminal
{
    protected override Task Open(CancellationToken cancellationToken)
    {
        return WebSocket.SendText("Now open!", cancellationToken);
    }

    public override Task HandleTextInput(string input, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

// ReSharper restore UnusedParameter.Local

[RESTable]
public class ShellChatter : Terminal
{
    protected override async Task Open(CancellationToken cancellationToken)
    {
        var context = WebSocket.Context.GetRequiredService<RootContext>();
        await WebSocket.SendText("Opening ShellChatter!", cancellationToken);
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
        var val = request.Conditions.HasParameter(nameof(Count), out int v);


        var number = (int) (request.Conditions.Pop(nameof(Number), Operators.EQUALS)?.Value ?? 0);
        for (var i = 0; i < number; i += 1) await BufferBlock.SendAsync(Count += 1, cancellationToken);
        yield break;
    }
}

[RESTable(GET)]
public class Test : IAsyncSelector<Test>
{
    public int Value { get; set; }

    public async IAsyncEnumerable<Test> SelectAsync(IRequest<Test> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var value in Test2.BufferBlock.ToAsyncEnumerable(cancellationToken))
        {
            yield return new Test { Value = value };
        }
    }
}

[RESTable(GET)]
public class Test3 : IAsyncSelector<Test3>
{
    public int Value { get; set; }

    public async IAsyncEnumerable<Test3> SelectAsync(IRequest<Test3> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new Test3 { Value = 0 };
        await Task.Delay(2000, cancellationToken);
        yield return new Test3 { Value = 1 };
        await Task.Delay(2000, cancellationToken);
        yield return new Test3 { Value = 2 };
        await Task.Delay(2000, cancellationToken);
        yield return new Test3 { Value = 3 };
        await Task.Delay(2000, cancellationToken);
        yield return new Test3 { Value = 4 };
        await Task.Delay(2000, cancellationToken);
    }
}

#endregion

#region Tutorial 2

/// <summary>
///     "ChatRoom" is an appropriate name for the resource from the client's perspective, even though
///     each instance of this resource will work more like a chat participant.
/// </summary>
[RESTable]
public class ChatRoom : Terminal, IAsyncDisposable
{
    private string? _name;

    /// <summary>
    ///     Used internally to track if the participant is initiated. Invisible in the API.
    /// </summary>
    private bool Initiated;

    /// <summary>
    ///     This collection holds all ChatRoom instances
    /// </summary>
    private IEnumerable<ChatRoom> Terminals => Services.GetRequiredService<ITerminalCollection<ChatRoom>>();

    /// <summary>
    ///     The name of the connected chat room participant. To change this, we can write
    ///     #terminal {"Name": "new name"} while in the chat room.
    /// </summary>
    public string? Name
    {
        get => _name;
        set
        {
            var name = GetUniqueName(value);
            if (Initiated)
                SendToOthers($"# {_name} has changed name to \"{name}\"").Wait();
            _name = name;
        }
    }

    /// <summary>
    ///     A read-only list of all chat room participants (names).
    /// </summary>
    public string?[] Members => Terminals.Select(t => t.Name).ToArray();

    /// <summary>
    ///     The number of connected participants.
    /// </summary>
    public int NumberOfMembers => Terminals.Count();

    public async ValueTask DisposeAsync()
    {
        await SendToOthers($"# {Name} left the chat room.");
    }

    protected override async Task Open(CancellationToken cancellationToken)
    {
        Name = GetUniqueName(Name);
        await SendToOthers($"# {Name} has joined the chat room.", cancellationToken);
        await WebSocket.SendText(
            $"# Welcome to the chat room! Your name is \"{Name}\" (type QUIT to return to the shell)", cancellationToken);
        Initiated = true;
    }

    /// <summary>
    ///     Creates a unique name for a participant, or deal with edge cases like a participant naming
    ///     themselves nothing or "Chatbot".
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

    public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;
        if (string.Equals(input, "quit", OrdinalIgnoreCase))
            await WebSocket.DirectToShell(cancellationToken: cancellationToken);
        await SendToOthers($"> {Name}: {input}", cancellationToken).ConfigureAwait(false);
    }

    private async Task SendToOthers(string message, CancellationToken cancellationToken = new())
    {
        await Terminals
            .Where(t => t != this)
            .Combine()
            .CombinedWebSocket
            .SendText(message, cancellationToken)
            .ConfigureAwait(false);
    }
}

#endregion

#endregion
