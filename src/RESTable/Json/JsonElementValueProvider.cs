using System;
using System.Text.Json;
using RESTable.ContentTypeProviders;

namespace RESTable.Json;

public readonly struct JsonElementValueProvider : IValueProvider
{
    private JsonElement JsonElement { get; }
    private IJsonProvider JsonProvider { get; }
    private JsonSerializerOptions Options { get; }

    public JsonElementValueProvider(JsonElement jsonElement, IJsonProvider jsonProvider, JsonSerializerOptions options)
    {
        JsonElement = jsonElement;
        JsonProvider = jsonProvider;
        Options = options;
    }

    public T? GetValue<T>()
    {
        return JsonProvider.ToObject<T>(JsonElement, Options);
    }

    public object? GetValue(Type targetType)
    {
        return JsonProvider.ToObject(JsonElement, targetType, Options);
    }
}
