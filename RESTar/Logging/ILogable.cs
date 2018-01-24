using RESTar.Requests;

namespace RESTar.Logging
{
    internal interface ILogable : ITraceable
    {
        LogEventType LogEventType { get; }
        string LogMessage { get; }
        string LogContent { get; }
        long LogContentLength { get; }
        Headers Headers { get; }
        string HeadersStringCache { get; set; }
        bool ExcludeHeaders { get; }
    }
}