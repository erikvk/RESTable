using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IJsonSerializerOptionsAccessor
    {
        public JsonSerializerOptions Options { get; }
    }
}