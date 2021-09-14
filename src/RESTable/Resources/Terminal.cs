using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.WebSockets;

namespace RESTable.Resources
{
    /// <summary>
    /// ITerminal defines the functionality of a RESTable terminal. The terminal will be
    /// made available as a WebSocket endpoint in the API, and each ITerminal type will be
    /// instantiated, using the Create method, when targeted in a request or from the 
    /// RESTable WebSocket shell.
    /// </summary>
    public abstract class Terminal
    {
        /// <summary>
        /// The WebSocket connected to this terminal, if any. Will be set when the terminal
        /// is opened.
        /// </summary>
        protected IWebSocket WebSocket { get; private set; } = null!;

        [RESTableMember(hide: true)]
        public ITerminalResource TerminalResource { get; private set; } = null!;

        /// <summary>
        /// The services available to this terminal, if any. Will be accessible when the terminal
        /// is opened. 
        /// </summary>
        protected IServiceProvider Services => WebSocket!.Context;

        /// <summary>
        /// Does this terminal support text input?
        /// </summary>
        /// <returns></returns>
        internal bool SupportsTextInput { get; }

        /// <summary>
        /// Does this terminal support binary input?
        /// </summary>
        /// <returns></returns>
        internal bool SupportsBinaryInput { get; }

        protected Terminal(bool supportsTextInput, bool supportsBinaryInput)
        {
            SupportsTextInput = supportsTextInput;
            SupportsBinaryInput = supportsBinaryInput;
        }

        protected Terminal()
        {
            var type = GetType();
            SupportsTextInput = type.GetMethod(nameof(HandleTextInput))!.IsImplemented();
            SupportsBinaryInput = type.GetMethod(nameof(HandleBinaryInput))!.IsImplemented();
        }

        internal IWebSocket GetWebSocket() => WebSocket;

        internal void SetWebSocket(IWebSocket webSocket) => WebSocket = webSocket;

        internal void SetTerminalResource(ITerminalResource terminalResource) => TerminalResource = terminalResource;

        internal async Task OpenTerminal(CancellationToken cancellationToken)
        {
            var terminalSubject = Services.GetRequiredService<TerminalSubjectAccessor>().Subject;
            terminalSubject.OnNext(this);
            await Open(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is called when the WebSocket is opened, and when data can be sent   
        /// and received by this terminal.
        /// </summary>
        protected virtual Task Open(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Performs an action on string input
        /// </summary>
        [MethodNotImplemented]
        public virtual Task HandleTextInput(string input, CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <summary>
        /// Performs and action on binary input
        /// </summary>
        [MethodNotImplemented]
        public virtual Task HandleBinaryInput(Stream input, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}