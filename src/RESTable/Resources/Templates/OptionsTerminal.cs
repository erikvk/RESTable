using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTable.Resources.Templates
{
    /// <summary>
    /// Encodes an option for use in OptionsTerminal subclasses
    /// </summary>
    public class Option
    {
        /// <summary>
        /// The command (case insensitive) to register the action for
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// A description of the option
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The action to perform on the arguments (can be empty) when the command is called
        /// </summary>
        public Func<string[], ValueTask> Action { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="Option"/> class.
        /// </summary>
        /// <param name="command">The command (case insensitive) to register the action for</param>
        /// <param name="description">The command (case insensitive) to register the action for</param>
        /// <param name="task">The action to perform on the arguments (can be empty) when the command is called</param>
        public Option(string command, string description, Func<string[], ValueTask> task)
        {
            if (command.Any(char.IsWhiteSpace))
                throw new ArgumentException($"Invalid option command '{command}'. Commands cannot contain whitespace.");
            if (command.ToUpperInvariant() == "CANCEL")
                throw new ArgumentException($"Invalid option command '{command}'. 'Cancel' is reserved.");
            Command = command;
            if (string.IsNullOrWhiteSpace(description))
                Description = "No description";
            Description = description;
            Action = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Option"/> class.
        /// </summary>
        /// <param name="command">The command (case insensitive) to register the action for</param>
        /// <param name="description">The command (case insensitive) to register the action for</param>
        /// <param name="action">The action to perform on the arguments (can be empty) when the command is called</param>
        public Option(string command, string description, Action<string[]> action) : this(command, description, strings =>
        {
            action(strings);
            return default;
        }) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// A simple resource template for creating terminal resources that expose a set of options. 
    /// To use, simply define the GetOptions() method that returns the options that should be 
    /// exposed by this resource. Input and output cannot be handled by the implementing class.
    /// using commands.
    /// </summary>
    public abstract class OptionsTerminal : Terminal
    {
        private IReadOnlyDictionary<string, Option> _options { get; set; }

        protected override async Task Open()
        {
            _options = GetOptions().SafeToDictionary(o => o.Command, StringComparer.OrdinalIgnoreCase);
            await PrintOptions();
        }

        public override async Task HandleTextInput(string input)
        {
            var (command, args) = input.TSplit(" ");
            switch (command.Trim())
            {
                case var cancel when cancel.EqualsNoCase("cancel"):
                    await WebSocket.DirectToShell();
                    break;
                case var _ when _options.TryGetValue(command, out var option):
                    var argsArray = args?.Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                    await WebSocket.SendText($"> {option.Command}");
                    try
                    {
                        await option.Action(argsArray);
                        await WebSocket.SendText("> Done!\n");
                    }
                    catch (Exception e)
                    {
                        await WebSocket.SendException(e);
                    }
                    await PrintOptions();
                    break;
                case var unknown:
                    await WebSocket.SendText($"Unknown option '{unknown}'.");
                    break;
            }
        }

        private async Task PrintOptions()
        {
            var stringBuilder = new StringBuilder($"### {GetType().GetRESTableTypeName()} ###\n\n");
            if (!_options.Any())
            {
                stringBuilder.Append("  No available options.\n\n");
                stringBuilder.Append("> Type 'cancel' to return to the shell");
            }
            var first = true;
            foreach (var option in _options.Values)
            {
                if (!first)
                    stringBuilder.Append("  - - - - - - - - - - - - - - - - - - - - - - - - - - - -  \n");
                stringBuilder.Append($"  Option:  {option.Command}\n  About:   {option.Description}\n");
                first = false;
            }
            stringBuilder.Append("\n> Type an option to continue, or 'cancel' to return to the shell\n");
            await WebSocket.SendText(stringBuilder.ToString());
        }

        public override bool SupportsTextInput { get; } = true;

        /// <summary>
        /// Provides the options to make available in this resource
        /// </summary>
        protected abstract IEnumerable<Option> GetOptions();
    }
}