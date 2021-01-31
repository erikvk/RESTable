﻿using System;
using System.Threading.Tasks;
using RESTable.WebSockets;

namespace RESTable.Resources.Templates
{
    /// <summary>
    /// The status of a feed terminal
    /// </summary>
    public enum FeedStatus
    {
        /// <summary>
        /// The Feed is connected to a WebSocket, but 
        /// currently marked as paused.
        /// </summary>
        PAUSED,

        /// <summary>
        /// The Feed is connected to a WebSocket and open,
        /// ready to receive messages.
        /// </summary>
        OPEN
    }

    /// <inheritdoc />
    /// <summary>
    /// A resource template for feeds. Feeds are simple terminal resources that 
    /// receive the commands OPEN, PAUSE and CLOSE, and that regurarly push messages 
    /// to all open terminals.
    /// </summary>
    public abstract class FeedTerminal : ITerminal
    {
        /// <summary>
        /// The status of the feed
        /// </summary>
        public FeedStatus Status { get; set; }

        /// <summary>
        /// Is this feed's status currently Open?
        /// </summary>
        protected bool IsOpen => Status == FeedStatus.OPEN;

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
            return $"### {WelcomeHeader ?? GetType().GetRESTableTypeName()} ###\n\n{welcomeBody}> Status: {Status}\n\n" +
                   (IsOpen ? "" : "> To open the feed, type OPEN\n") +
                   "> To pause, type PAUSE\n> To close, type CLOSE\n";
        }

        /// <inheritdoc />
        public virtual async Task Open()
        {
            if (ShowWelcomeText)
                await WebSocket.SendText(GetWelcomeText());
        }

        /// <inheritdoc />
        public IWebSocket WebSocket { protected get; set; }

        public abstract ValueTask DisposeAsync();

        /// <inheritdoc />
        public Task HandleBinaryInput(byte[] input) => throw new NotImplementedException();

        /// <inheritdoc />
        public bool SupportsTextInput { get; } = true;

        /// <inheritdoc />
        public bool SupportsBinaryInput { get; } = false;

        /// <inheritdoc />
        public async Task HandleTextInput(string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "OPEN":
                    Status = FeedStatus.OPEN;
                    await WebSocket.SendText("> Status: OPEN\n");
                    break;
                case "PAUSE":
                    Status = FeedStatus.PAUSED;
                    await WebSocket.SendText("> Status: PAUSED\n");
                    break;
                case "CLOSE":
                    await WebSocket.SendText("> Status: CLOSED\n");
                    WebSocket.DirectToShell();
                    break;
                case var unrecognized:
                    await WebSocket.SendText($"> Unknown command '{unrecognized}'");
                    break;
            }
        }
    }
}