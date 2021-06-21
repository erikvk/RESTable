using System;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Resources.Templates
{
    /// <inheritdoc />
    /// <summary>
    /// A resource template for feeds. Feeds are simple terminal resources that 
    /// receive the commands OPEN, PAUSE and CLOSE, and that regurarly push messages 
    /// to all open terminals.
    /// </summary>
    public abstract class FeedTerminal : Terminal
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
        protected virtual string WelcomeHeader => null;

        /// <summary>
        /// The body to use in welcome texts. If long, include wrapping line breaks.
        /// </summary>
        protected virtual string WelcomeBody => null;

        private string GetWelcomeText()
        {
            var welcomeBody = WelcomeBody;
            if (welcomeBody is not null)
                welcomeBody = welcomeBody + Environment.NewLine + Environment.NewLine;
            return
                $"### {WelcomeHeader ?? GetType().GetRESTableTypeName()} ###{Environment.NewLine}{Environment.NewLine}" +
                $"{welcomeBody}> Status: {Status}{Environment.NewLine}{Environment.NewLine}{(IsOpen ? "" : $"> To open the feed, type OPEN{Environment.NewLine}")}> To pause, type PAUSE" +
                $"{Environment.NewLine}> To close, type CLOSE{Environment.NewLine}";
        }

        protected override async Task Open(CancellationToken cancellationToken)
        {
            if (ShowWelcomeText)
                await WebSocket.SendText(GetWelcomeText(), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool SupportsTextInput => true;

        /// <inheritdoc />
        public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "OPEN":
                    Status = FeedStatus.OPEN;
                    await WebSocket.SendText("> Status: OPEN" + Environment.NewLine, cancellationToken).ConfigureAwait(false);
                    break;
                case "PAUSE":
                    Status = FeedStatus.PAUSED;
                    await WebSocket.SendText("> Status: PAUSED" + Environment.NewLine, cancellationToken).ConfigureAwait(false);
                    break;
                case "CLOSE":
                    await WebSocket.SendText("> Status: CLOSED" + Environment.NewLine, cancellationToken).ConfigureAwait(false);
                    await WebSocket.DirectToShell(cancellationToken: cancellationToken).ConfigureAwait(false);
                    break;
                case var unrecognized:
                    await WebSocket.SendText($"> Unknown command '{unrecognized}'", cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }
}