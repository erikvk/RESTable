using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTable.Resources.Templates
{
    /// <inheritdoc />
    /// <summary>
    /// A simple resource template for creating terminal resources that expose a set of options. 
    /// To use, simply define the GetOptions() method that returns the options that should be 
    /// exposed by this resource. Input and output cannot be handled by the implementing class.
    /// using commands.
    /// </summary>
    public abstract class OptionsTerminal : Terminal
    {
        private IDictionary<string, Option> Options { get; }

        protected bool RunSilent { get; set; }

        public OptionsTerminal()
        {
            Options = new Dictionary<string, Option>(StringComparer.OrdinalIgnoreCase);
        }

        protected override async Task Open()
        {
            Options.Clear();
            foreach (var option in GetOptions())
                Options[option.Command] = option;
            await PrintOptions().ConfigureAwait(false);
        }

        public override async Task HandleTextInput(string input)
        {
            var (command, args) = input.TupleSplit(" ");
            switch (command.Trim())
            {
                case var cancel when cancel.EqualsNoCase("cancel"):
                    await WebSocket.DirectToShell().ConfigureAwait(false);
                    break;
                case var _ when Options.TryGetValue(command, out var option):
                    var argsArray = args?.Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    if (!RunSilent)
                        await WebSocket.SendText($"> {option!.Command}").ConfigureAwait(false);
                    try
                    {
                        await option.Action(argsArray).ConfigureAwait(false);
                        if (!RunSilent)
                            await WebSocket.SendText($"> Done!{Environment.NewLine}").ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        await WebSocket.SendException(e).ConfigureAwait(false);
                    }
                    await PrintOptions().ConfigureAwait(false);
                    break;
                case var unknown:
                    await WebSocket.SendText($"Unknown option '{unknown}'.").ConfigureAwait(false);
                    break;
            }
        }

        private async Task PrintOptions()
        {
            var stringBuilder = new StringBuilder($"### {GetType().GetRESTableTypeName()} ###{Environment.NewLine}{Environment.NewLine}");
            if (!Options.Any())
            {
                stringBuilder.Append($"  No available options.{Environment.NewLine}{Environment.NewLine}");
                stringBuilder.Append("> Type 'cancel' to return to the shell");
            }
            var first = true;
            foreach (var option in Options.Values)
            {
                if (!first)
                    stringBuilder.Append($"  - - - - - - - - - - - - - - - - - - - - - - - - - - - -  {Environment.NewLine}");
                stringBuilder.Append($"  Option:  {option.Command}{Environment.NewLine}  About:   {option.Description}{Environment.NewLine}");
                first = false;
            }
            stringBuilder.Append($"{Environment.NewLine}> Type an option to continue, or 'cancel' to return to the shell{Environment.NewLine}");
            await WebSocket.SendText(stringBuilder.ToString()).ConfigureAwait(false);
        }

        protected override bool SupportsTextInput => true;

        /// <summary>
        /// Provides the options to make available in this resource
        /// </summary>
        protected abstract IEnumerable<Option> GetOptions();
    }
}