using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized
{
    public class SerializedOptions : ISerialized
    {
        public string Status => "success";
        public OptionsBody[] Data { get; }
        public long DataCount => 1;

        public SerializedOptions(OptionsBody data)
        {
            Data = new[] {data};
        }
    }
}