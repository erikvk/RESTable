using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamit;
using Newtonsoft.Json;

namespace RESTar.Serialization
{
    internal class DDictionaryConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var kvps = serializer.Deserialize<KeyValuePair<string, object>[]>(reader);
            if (existingValue is DDictionary ddict)
            {
                serializer.Populate();
            }
        }

        public override bool CanConvert(Type objectType) => objectType.IsSubclassOf(typeof(DDictionary));
    }
}