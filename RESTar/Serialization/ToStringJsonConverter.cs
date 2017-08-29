using System;
using Newtonsoft.Json;

namespace RESTar.Serialization
{
    internal class ToStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => true;
        public override void WriteJson(JsonWriter w, object v, JsonSerializer s) => w.WriteValue(v.ToString());
        public override bool CanRead => false;

        public override object ReadJson(JsonReader r, Type t, object e, JsonSerializer s) =>
            throw new NotImplementedException();
    }
}