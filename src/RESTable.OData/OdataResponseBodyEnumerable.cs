using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RESTable.Results;

namespace RESTable.OData;

public class OdataResponseBodyEnumerable<T> where T : class
{
    public OdataResponseBodyEnumerable(IEntities<T> entities, ISerializedResult toSerialize, string context, bool writeMetadata)
    {
        Entities = entities;
        Context = context;
        WriteMetadata = writeMetadata;

        if (writeMetadata && toSerialize.HasNextPage) NextPage = entities.GetNextPageLink(toSerialize.EntityCount, -1).ToUriString();
    }

    private IEntities<T> Entities { get; }
    private long Counter { get; set; }
    private bool WriteMetadata { get; }

    [JsonPropertyName("@odata.context")] public string Context { get; }

    [JsonPropertyName("value")] public IEnumerable<T> Value => GetData();

    [JsonPropertyName("@odata.count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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

    [JsonPropertyName("@odata.nextLink")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextPage { get; }

    private IEnumerable<T> GetData()
    {
        var asyncEnumerable = (IAsyncEnumerable<T>) Entities;
        foreach (var item in asyncEnumerable.ToEnumerable())
        {
            Counter += 1;
            yield return item;
        }
    }
}