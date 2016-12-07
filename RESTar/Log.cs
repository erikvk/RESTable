using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Logging;

namespace RESTar
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