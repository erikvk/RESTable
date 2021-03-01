using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.Requests;

namespace RESTable
{
    /// <summary>
    /// A protocol holder that uses the default protocol provider
    /// </summary>
    public class DefaultProtocolHolder : IProtocolHolder
    {
        public bool ExcludeHeaders => false;
        public string ProtocolIdentifier => CachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;

        public RESTableContext Context { get; }
        public Headers Headers { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }
        public string HeadersStringCache { get; set; }

        public DefaultProtocolHolder(RESTableContext context, Headers headers = null)
        {
            Context = context;
            Headers = headers ?? new Headers();
            CachedProtocolProvider = Context.Services.GetService<ProtocolController>().DefaultProtocolProvider;
        }
    }
}