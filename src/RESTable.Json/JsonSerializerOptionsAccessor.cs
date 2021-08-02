using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public class JsonSerializerOptionsAccessor
    {
        internal JsonSerializerOptions Options { get; }

        public JsonSerializerOptionsAccessor(JsonSerializerOptions options)
        {
            Options = options;
        }
    }
}