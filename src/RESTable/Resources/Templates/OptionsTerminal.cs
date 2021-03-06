﻿using System;
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
        private IReadOnlyDictionary<string, Option> _options { get; set; }

        protected override async Task Open()
        {
            _options = GetOptions().SafeToDictionary(o => o.Command, StringComparer.OrdinalIgnoreCase);
            await PrintOptions().ConfigureAwait(false);
        }

        public override async Task HandleTextInput(string input)
        {
            var (command, args) = input.TSplit(" ");
            switch (command.Trim())
            {
                case var cancel when cancel.EqualsNoCase("cancel"):
                    await WebSocket.DirectToShell().ConfigureAwait(false);
                    break;
                case var _ when _options.TryGetValue(command, out var option):
                    var argsArray = args?.Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                    await WebSocket.SendText($"> {option.Command}").ConfigureAwait(false);
                    try
                    {
                        await option.Action(argsArray).ConfigureAwait(false);
                        await WebSocket.SendText("> Done!\n").ConfigureAwait(false);
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
            await WebSocket.SendText(stringBuilder.ToString()).ConfigureAwait(false);
        }

        protected override bool SupportsTextInput { get; } = true;

        /// <summary>
        /// Provides the options to make available in this resource
        /// </summary>
        protected abstract IEnumerable<Option> GetOptions();
    }
}