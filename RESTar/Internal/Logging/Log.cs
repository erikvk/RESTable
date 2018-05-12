using Starcounter.Logging;

namespace RESTar.Internal.Logging
{
    internal static class Log
    {
        private static LogSource StarcounterLog;
        internal static void Init() => StarcounterLog = new LogSource("RESTar");
        internal static void Info(object message) => StarcounterLog.LogNotice(message.ToString());
        internal static void Warn(object message) => StarcounterLog.LogWarning(message.ToString());
        internal static void Fatal(object message) => StarcounterLog.LogCritical(message.ToString());
        internal static void Error(object message) => StarcounterLog.LogError(message.ToString());
        internal static void Debug(object message) => StarcounterLog.LogNotice(message.ToString());
    }
}