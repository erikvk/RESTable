using RESTable.Internal;

namespace RESTable
{
    public interface IProtocolHolder : IHeaderHolder
    {
        string ProtocolIdentifier { get; }
        CachedProtocolProvider CachedProtocolProvider { get; }
    }
}