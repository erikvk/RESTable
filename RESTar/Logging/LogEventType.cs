namespace RESTar.Logging
{
    internal enum LogEventType
    {
        HttpInput,
        HttpOutput,
        WebSocketInput,
        WebSocketOutput,
        WebSocketOpen,
        WebSocketClose
    }
}