using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized
{
    [UseDefaultConverter]
    public class SerializedEntitiesAsyncEnumerable<T> : ISerialized where T : class
    {
        public string Status => "success";

        public IAsyncEnumerable<T> Data => GetData();

        private async IAsyncEnumerable<T> GetData()
        {
            var asyncEnumerable = (IAsyncEnumerable<T>) Entities;
            await foreach (var item in asyncEnumerable.ConfigureAwait(false))
            {
                Counter += 1;
                yield return item;
            }
        }

        public long DataCount
        {
            get
            {
                if (Counter == 0)
                    Entities.MakeNoContent();
                return Counter;
            }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PreviousPage { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NextPage { get; }

        public double TimeElapsedMs => Entities.TimeElapsed.GetRESTableElapsedMs();

        internal IEntities<T> Entities { get; }
        private long Counter { get; set; }

        public SerializedEntitiesAsyncEnumerable(IEntities<T> entities, ISerializedResult toSerialize)
        {
            Entities = entities;

            if (toSerialize.HasPreviousPage)
            {
                PreviousPage = entities.GetPreviousPageLink(toSerialize.EntityCount).ToUriString();
            }
            if (toSerialize.HasNextPage)
            {
                NextPage = entities.GetNextPageLink(toSerialize.EntityCount, -1).ToUriString();
            }
        }
    }
}