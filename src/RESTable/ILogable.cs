using System;
using System.Threading.Tasks;

namespace RESTable
{
    /// <inheritdoc cref="RESTable.ITraceable" />
    /// <inheritdoc cref="RESTable.IHeaderHolder" />
    /// <summary>
    /// Defines the operations of something that can be logged
    /// </summary>
    public interface ILogable : IHeaderHolder, ITraceable
    {
        /// <summary>
        /// The log event type
        /// </summary>
        MessageType MessageType { get; }

        /// <summary>
        /// The message to log
        /// </summary>
        ValueTask<string> GetLogMessage();

        /// <summary>
        /// The content to log
        /// </summary>
        ValueTask<string> GetLogContent();

        /// <summary>
        /// The date and time of this logable instance
        /// </summary>
        DateTime LogTime { get; }
    }
}