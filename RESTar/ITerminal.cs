namespace RESTar
{
    /// <summary>
    /// ITerminal defines the functionality of a RESTar terminal. The terminal will be
    /// made available as a WebSocket endpoint in the API, and each ITerminal type will be
    /// instantiated, using the Create method, when targeted in a request or from the 
    /// RESTar WebSocket shell.
    /// </summary>
    public interface ITerminal
    {
        /// <summary>
        /// The WebSocket connected to this terminal. To add custom logic that runs when 
        /// the WebSocket is assigned, for example sending a welcome message, add it to 
        /// the setter of this property.
        /// </summary>
        IWebSocket WebSocket { set; }

        /// <summary>
        /// A description for the terminal
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Performs an action on string input
        /// </summary>
        void HandleTextInput(string input);

        /// <summary>
        /// Performs and action on binary input
        /// </summary>
        void HandleBinaryInput(byte[] input);

        /// <summary>
        /// Does this terminal support text input and output?
        /// </summary>
        /// <returns></returns>
        bool HandlesText { get; }

        /// <summary>
        /// Does this terminal support binary input and output?
        /// </summary>
        /// <returns></returns>
        bool HandlesBinary { get; }
    }
}