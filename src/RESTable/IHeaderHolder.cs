using RESTable.Internal;
using RESTable.Requests;

namespace RESTable
{
    public interface IProtocolHolder : IHeaderHolder
    {
        string ProtocolIdentifier { get; }
        CachedProtocolProvider CachedProtocolProvider { get; }
    }

    internal class RemoteRequestProtocolHolder : IProtocolHolder
    {
        public string TraceId => Context.InitialTraceId;
        public bool ExcludeHeaders => false;
        public string ProtocolIdentifier => CachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
        
        public RESTableContext Context { get; }
        public Headers Headers { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }
        public string HeadersStringCache { get; set; }

        public RemoteRequestProtocolHolder(RESTableContext context, Headers headers, CachedProtocolProvider cachedProtocolProvider)
        {
            Context = context;
            Headers = headers;
            CachedProtocolProvider = cachedProtocolProvider;
        }
    }

    /// <summary>
    /// Defines the operations of an entity that holds headers
    /// </summary>
    public interface IHeaderHolder : ITraceable
    {
        /// <summary>
        /// The headers of the logable entity
        /// </summary>
        Headers Headers { get; }

        /// <summary>
        /// A string cache of the headers
        /// </summary>
        string HeadersStringCache { get; set; }

        /// <summary>
        /// Should headers be excluded?
        /// </summary>
        bool ExcludeHeaders { get; }
    }
}