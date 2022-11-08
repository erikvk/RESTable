using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized;

public class SerializedEntitiesEnumerable<T> : ISerialized where T : class
{
    public SerializedEntitiesEnumerable(IEntities<T> entities, ISerializedResult toSerialize)
    {
        Entities = entities;

        if (toSerialize.HasPreviousPage) PreviousPage = entities.GetPreviousPageLink(toSerialize.EntityCount).ToUriString();
        if (toSerialize.HasNextPage) NextPage = entities.GetNextPageLink(toSerialize.EntityCount, -1).ToUriString();
    }

    private IEntities<T> Entities { get; }
    private long Counter { get; set; }

    public IEnumerable<T> Data => GetData();

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

    public string Status => "success";

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
