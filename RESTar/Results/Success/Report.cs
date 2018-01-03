using System.IO;
using Newtonsoft.Json;
using RESTar.Admin;
using RESTar.Serialization;

namespace RESTar.Results.Success
{
    public class Report : OK
    {
        internal Report(long count)
        {
            Body = new MemoryStream();
            using (var swr = new StreamWriter(Body, Serializer.UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                Serializer.Json.Formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
                Serializer.Json.Serialize(jwr, new {Count = count});
            }
            Body.Seek(0, SeekOrigin.Begin);
        }
    }
}