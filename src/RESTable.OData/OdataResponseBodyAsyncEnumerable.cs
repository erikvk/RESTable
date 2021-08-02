using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RESTable.Results;

namespace RESTable.OData
{
    public class OdataResponseBodyAsyncEnumerable<T> where T : class
    {
        private IEntities<T> Entities { get; }
        private long Counter { get; set; }
        private bool WriteMetadata { get; }

        [JsonPropertyName("@odata.context")]
        public string Context { get; }

        [JsonPropertyName("value")]
        public IAsyncEnumerable<T> Value => GetData();

        private async IAsyncEnumerable<T> GetData()
        {
            var asyncEnumerable = (IAsyncEnumerable<T>) Entities;
            await foreach (var item in asyncEnumerable.ConfigureAwait(false))
            {
                Counter += 1;
                yield return item;
            }
        }

        [JsonPropertyName("@odata.count"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? Count
        {
            get
            {
                if (Counter == 0)
                    Entities.MakeNoContent();
                if (!WriteMetadata)
                    return null;
                return Counter;
            }
        }

        [JsonPropertyName("@odata.nextLink"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NextPage { get; }

        public OdataResponseBodyAsyncEnumerable(IEntities<T> entities, ISerializedResult toSerialize, string context, bool writeMetadata)
        {
            Entities = entities;
            Context = context;
            WriteMetadata = writeMetadata;

            if (writeMetadata && toSerialize.HasNextPage)
            {
                NextPage = entities.GetNextPageLink(toSerialize.EntityCount, -1).ToUriString();
            }
        }
    }
}