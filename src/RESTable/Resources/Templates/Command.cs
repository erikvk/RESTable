using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Resources.Templates
{
    /// <summary>
    /// Encodes a command for use in CommandTerminal subclasses
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The command string (case insensitive) to register the action for
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the command
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The maximum number of arguments to parse from the invocation string
        /// </summary>
        public int MaxArgumentCount { get; }

        /// <summary>
        /// The action to perform on the arguments (can be empty) when the command is called
        /// </summary>
        public Func<string[], CancellationToken, ValueTask> Action { get; }

        /// <summary>
        /// Creates a new instance of the <see cref="Templates.Command"/> class.
        /// </summary>
        /// <param name="name">The command (case insensitive) to register the action for</param>
        /// <param name="description">The command (case insensitive) to register the action for</param>
        /// <param name="maxArgumentCount">The maximum number of arguments to parse from the invocation string</param>
        /// <param name="task">The action to perform on the arguments (can be empty) when the command is called</param>
        public Command(string name, string description, int maxArgumentCount, Func<string[], CancellationToken, ValueTask> task)
        {
            if (name.Any(char.IsWhiteSpace))
                throw new ArgumentException($"Invalid command '{name}'. Commands cannot contain whitespace.");
            Name = name;
            if (string.IsNullOrWhiteSpace(description))
                description = "No description";
            Description = description;
            MaxArgumentCount = maxArgumentCount;
            Action = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Templates.Command"/> class.
        /// </summary>
        /// <param name="name">The command (case insensitive) to register the action for</param>
        /// <param name="description">The command (case insensitive) to register the action for</param>
        /// <param name="maxArgumentCount">The maximum number of arguments to parse from the invocation string</param>
        /// <param name="action">The action to perform on the arguments (can be empty) when the command is called</param>
        public Command(string name, string description, int maxArgumentCount, Action<string[]> action) : this
        (
            name,
            description,
            maxArgumentCount,
            task: (strings, _) =>
            {
                action(strings);
                return default;
            }
        ) { }
    }
}