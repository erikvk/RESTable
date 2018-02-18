using System.IO;
using Newtonsoft.Json;
using RESTar.Admin;
using RESTar.Requests;
using RESTar.Serialization;
using RESTar.Serialization.NativeProtocol;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : OK
    {
        internal class ReportBody
        {
            public long Count { get; set; }
        }

        internal Report(ITraceable trace, long count) : base(trace)
        {
            Headers["RESTar-count"] = count.ToString();
            Body = new MemoryStream();
            using (var swr = new StreamWriter(Body, Serializer.UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                Serializer.Json.Formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
                Serializer.Json.Serialize(jwr, new ReportBody {Count = count});
            }
            Body.Seek(0, SeekOrigin.Begin);
        }
    }
}