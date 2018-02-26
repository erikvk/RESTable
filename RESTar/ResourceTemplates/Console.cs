using System;

namespace RESTar.ResourceTemplates
{
    /// <summary>
    /// The status of a console terminal
    /// </summary>
    public enum ConsoleStatus
    {
        /// <summary>
        /// The Console is connected to a WebSocket, but 
        /// currently marked as paued.
        /// </summary>
        Paused,

        /// <summary>
        /// The Console is connected to a WebSocket and open,
        /// ready to receive console messages.
        /// </summary>
        Open
    }

    /// <inheritdoc />
    /// <summary>
    /// A resource template for a console. Consoles are simple terminal resources that 
    /// receive the commands OPEN, PAUSE and CLOSE, and that regurarly push messages 
    /// to all open terminals.
    /// </summary>
    public abstract class Console : ITerminal
    {
        /// <summary>
        /// The status of the console
        /// </summary>
        public ConsoleStatus Status { get; set; }

        /// <summary>
        /// Is this console status currently open?
        /// </summary>
        protected bool IsOpen => Status == ConsoleStatus.Open;

        /// <summary>
        /// Should the welcome text be shown when the terminal launces?
        /// </summary>
        public bool ShowWelcomeText { get; set; } = true;

        /// <summary>
        /// The header to use in welcome texts
        /// </summary>
        protected virtual string WelcomeHeader { get; } = null;

        /// <summary>
        /// The body to use in welcome texts. If long, include wrapping line breaks.
        /// </summary>
        protected virtual string WelcomeBody { get; } = null;

        private string GetWelcomeText()
        {
            var welcomeBody = WelcomeBody;
            if (welcomeBody != null)
                welcomeBody = welcomeBody + "\n\n";
            return $"### {WelcomeHeader ?? GetType().RESTarTypeName()} ###\n\n{welcomeBody}> Status: {Status}\n\n" +
                   (Status == ConsoleStatus.Open ? "" : "> To open the console, type OPEN\n") +
                   "> To pause, type PAUSE\n> To close, type CLOSE\n";
        }

        /// <inheritdoc />
        public virtual void Open()
        {
            if (ShowWelcomeText)
                WebSocket.SendText(GetWelcomeText());
        }

        /// <inheritdoc />
        public IWebSocket WebSocket { protected get; set; }

        /// <inheritdoc />
        public abstract void Dispose();

        /// <inheritdoc />
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();

        /// <inheritdoc />
        public bool SupportsTextInput { get; } = true;

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; } = false;

        /// <inheritdoc />
        public void HandleTextInput(string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "OPEN":
                    Status = ConsoleStatus.Open;
                    WebSocket.SendText("> Status: ACTIVE\n");
                    break;
                case "PAUSE":
                    Status = ConsoleStatus.Paused;
                    WebSocket.SendText("> Status: PAUSED\n");
                    break;
                case "CLOSE":
                    WebSocket.SendText("> Status: CLOSED\n");
                    WebSocket.SendToShell();
                    break;
                case var unrecognized:
                    WebSocket.SendText($"> Unknown command '{unrecognized}'");
                    break;
            }
        }
    }
}