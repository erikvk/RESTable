using System.IO;
using Newtonsoft.Json;
using RESTar.Admin;
using RESTar.Serialization;
using RESTar.Serialization.NativeProtocol;

namespace RESTar.Results.Success
{
    internal class ReportBody
    {
        public long Count { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : OK
    {
        internal IRequest Request { get; }
        internal ReportBody ReportBody { get; }

        internal Report(IRequest request, long count) : base(request)
        {
            Request = request;
            ReportBody = new ReportBody {Count = count};
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