namespace RESTar.Logging
{
    internal struct HTTPEvent
    {
        internal ILogable Request;
        internal ILogable Response;
    }
}