using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

        internal IWebSocket GetWebSocket() => WebSocket!;

        internal void SetWebSocket(IWebSocket webSocket)
        {
            WebSocket = webSocket;
        }

        internal void SetTerminalResource(ITerminalResource terminalResource)
        {
            TerminalResource = terminalResource;
        }

        internal async Task OpenTerminal(CancellationToken cancellationToken) => await Open(cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// This method is called when the WebSocket is opened, and when data can be sent   
        /// and received by this terminal.
        /// </summary>
        protected virtual Task Open(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Performs an action on string input
        /// </summary>
        public virtual Task HandleTextInput(string input, CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Performs and action on binary input
        /// </summary>
        public virtual Task HandleBinaryInput(Stream input, CancellationToken cancellationToken) => Task.CompletedTask;

        internal bool SupportsTextInputInternal => SupportsTextInput;
        internal bool SupportsBinaryInputInternal => SupportsBinaryInput;

        /// <summary>
        /// Sends the current terminal over the connected websocket.
        /// </summary>
        protected async Task SendThis()
        {
            await WebSocket!.SendJson(this).ConfigureAwait(false);
        }

        /// <summary>
        /// Does this terminal support text input?
        /// </summary>
        /// <returns></returns>
        protected virtual bool SupportsTextInput => false;

        /// <summary>
        /// Does this terminal support binary input?
        /// </summary>
        /// <returns></returns>
        protected virtual bool SupportsBinaryInput => false;
    }
}