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
            if (welcomeBody != null)
                welcomeBody = welcomeBody + "\n\n";
            return $"### {WelcomeHeader ?? GetType().GetRESTableTypeName()} ###\n\n{welcomeBody}> Status: {Status}\n\n" +
                   (IsOpen ? "" : "> To open the feed, type OPEN\n") +
                   "> To pause, type PAUSE\n> To close, type CLOSE\n";
        }

        protected override async Task Open()
        {
            if (ShowWelcomeText)
                await WebSocket.SendText(GetWelcomeText()).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool SupportsTextInput => true;

        /// <inheritdoc />
        public override async Task HandleTextInput(string input)
        {
            switch (input.ToUpperInvariant().Trim())
            {
                case "": break;
                case "OPEN":
                    Status = FeedStatus.OPEN;
                    await WebSocket.SendText("> Status: OPEN\n").ConfigureAwait(false);
                    break;
                case "PAUSE":
                    Status = FeedStatus.PAUSED;
                    await WebSocket.SendText("> Status: PAUSED\n").ConfigureAwait(false);
                    break;
                case "CLOSE":
                    await WebSocket.SendText("> Status: CLOSED\n").ConfigureAwait(false);
                    await WebSocket.DirectToShell().ConfigureAwait(false);
                    break;
                case var unrecognized:
                    await WebSocket.SendText($"> Unknown command '{unrecognized}'").ConfigureAwait(false);
                    break;
            }
        }
    }
}