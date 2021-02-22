using System.Threading.Tasks;
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
        [RESTableMember(ignore: true)] protected IWebSocket WebSocket { get; private set; }

        internal void SetWebSocket(IWebSocket webSocket) => WebSocket = webSocket;

        internal async Task OpenTerminal() => await Open().ConfigureAwait(false);

        /// <summary>
        /// This method is called when the WebSocket is opened, and when data can be sent   
        /// and received by this terminal.
        /// </summary>
        protected virtual Task Open() => Task.CompletedTask;

        /// <summary>
        /// Performs an action on string input
        /// </summary>
        public virtual Task HandleTextInput(string input) => Task.CompletedTask;

        /// <summary>
        /// Performs and action on binary input
        /// </summary>
        public virtual Task HandleBinaryInput(byte[] input) => Task.CompletedTask;

        internal bool SupportsTextInputInternal => SupportsTextInput;
        internal bool SupportsBinaryInputInternal => SupportsBinaryInput;

        /// <summary>
        /// Sends the current terminal over the websocket
        /// </summary>
        protected async Task SendThis()
        {
            await WebSocket.SendJson(this).ConfigureAwait(false);
        }

        /// <summary>
        /// Does this terminal support text input?
        /// </summary>
        /// <returns></returns>
        protected virtual bool SupportsTextInput { get; } = false;

        /// <summary>
        /// Does this terminal support binary input?
        /// </summary>
        /// <returns></returns>
        protected virtual bool SupportsBinaryInput { get; } = false;
    }
}