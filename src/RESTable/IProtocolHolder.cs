using RESTable.ContentTypeProviders;
using RESTable.Internal;

namespace RESTable;

public interface IProtocolHolder : IContentTypeHolder, IHeaderHolder
{
    string ProtocolIdentifier { get; }
    CachedProtocolProvider CachedProtocolProvider { get; }
}