using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Resources.Templates;

/// <inheritdoc />
/// <summary>
///     A terminal resource template that expose a set of commands that can be invoked by
///     name (case insensitive) with an optional list of arguments. To use, simply define
///     the GetCommands() method that returns the commands that should be exposed by this
///     resource.
/// </summary>
public abstract class CommandTerminal : Terminal
{
    public CommandTerminal()
    {
        Commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
    }

    private IDictionary<string, Command> Commands { get; }

    public bool Silent { get; set; }

    protected override async Task Open(CancellationToken cancellationToken)
    {
        Commands.Clear();
        foreach (var command in GetCommands())
            Commands[command.Name] = command;
        if (!Silent)
            await PrintCommands(cancellationToken).ConfigureAwait(false);
    }

    public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
    {
        if (input.EqualsNoCase("help"))
        {
            await PrintCommands(cancellationToken).ConfigureAwait(false);
            return;
        }
        if (input.EqualsNoCase("silent"))
        {
            Silent = !Silent;
            return;
        }

        var (name, args) = input.TupleSplit(" ");
        switch (name.Trim())
        {
            case var cancel when cancel.EqualsNoCase("cancel"):
                await WebSocket.DirectToShell(cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case var _ when Commands.TryGetValue(name, out var command):
                var argsArray = args?.Split(new[] { ' ' }, command.MaxArgumentCount, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                try
                {
                    await command.Action(argsArray, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    await WebSocket.SendException(e, cancellationToken).ConfigureAwait(false);
                }
                break;
            case var unknown:
                await WebSocket.SendText($"Unknown command '{unknown}'.", cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    private async Task PrintCommands(CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"### {GetType().GetRESTableTypeName()} ###");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("Commands:");
        if (!Commands.Any())
        {
            stringBuilder.AppendLine("No available commands.");
            stringBuilder.AppendLine("Type 'cancel' to return to the shell");
        }
        var commandNameMaxLength = Commands.Values.Select(command => command.Name.Length).Max();
        var tabWidth = commandNameMaxLength + 3;
        foreach (var command in Commands.Values) stringBuilder.AppendLine($"  {command.Name.PadRight(tabWidth)}{command.Description}");
        stringBuilder.AppendLine();
        stringBuilder.Append("Type a command to continue, 'help' to print commands or 'cancel' to return to the shell");
        await WebSocket.SendText(stringBuilder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Provides the commands to make available in this resource
    /// </summary>
    protected abstract IEnumerable<Command> GetCommands();
}
