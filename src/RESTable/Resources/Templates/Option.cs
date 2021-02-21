using System;
using System.Linq;
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
}