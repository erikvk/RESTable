using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized;

public class SerializedOptions : ISerialized
{
    public SerializedOptions(OptionsBody data)
    {
        Data = new[] {data};
    }

    public OptionsBody[] Data { get; }
    public long DataCount => 1;
    public string Status => "success";
}