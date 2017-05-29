using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using Starcounter;
using static RESTar.RESTarMethods;
using static RESTar.RESTarPresets;
using static RESTar.Settings;
using Request = RESTar.Requests.Request;

namespace RESTar
{
    [RESTar(ReadOnly)]
    public class ErrorCode : ISelector<ErrorCode>
    {
        public string Name { get; private set; }
        public int Code { get; private set; }

        public IEnumerable<ErrorCode> Select(IRequest request)
        {
            var values = Enum.GetValues(typeof(ErrorCodes)).Cast<int>().ToList();
            return Enum
                .GetNames(typeof(ErrorCodes))
                .Select((name, index) => new ErrorCode {Name = name, Code = values[index]})
                .Filter(request.Conditions)
                .ToList();
        }
    }

    [Database, RESTar(GET, DELETE, Viewable = true)]
    public class Error
    {
        public string Id => this.GetObjectID();
        public DateTime Time;
        public string ResourceName;
        public RESTarMethods Method;
        public ErrorCodes ErrorCode;
        public string StackTrace;
        public string Message;
        public string Uri;
        public string Headers;
        public string Body;

        internal Error(ErrorCodes errorCode, Exception e, Request request)
        {
            Time = DateTime.Now;
            ResourceName = (request.Resource?.Name ?? "<unknown>") +
                           (request.Resource?.Alias != null ? $" ({request.Resource.Alias})" : "");
            Method = request.Method;
            ErrorCode = errorCode;
            StackTrace = e.StackTrace + e.InnerException?.StackTrace;
            Message = e.TotalMessage();
            Body = request.Body;
            Uri = request.ScRequest.Uri;
            if (request.ScRequest.HeadersDictionary != null)
                Headers = string.Join(" | ", request.ScRequest.HeadersDictionary
                    .Select(pair => $"{pair.Key}: {pair.Value}"));
        }

        private static DateTime Checked;

        internal static void ClearOld()
        {
            if (Checked >= DateTime.Now.Date) return;
            var matches = Db.SQL<Error>($"SELECT t FROM {typeof(Error).FullName} t WHERE t.\"Time\" <?",
                DateTime.Now.AddDays(0 - _DaysToSaveErrors));
            foreach (var match in matches)
                Scheduling.ScheduleTask(() => Db.TransactAsync(() => match.Delete()));
            Checked = DateTime.Now.Date;
        }
    }
}