using System;
using System.Text.Json;
using RESTable.ContentTypeProviders;

namespace RESTable.Json
{
    public readonly struct JsonElementValueProvider : IValueProvider
    {
        private JsonElement JsonElement { get; }
        private IJsonProvider JsonProvider { get; }

        public JsonElementValueProvider(JsonElement jsonElement, IJsonProvider jsonProvider)
        {
            JsonElement = jsonElement;
            JsonProvider = jsonProvider;
        }

        public T? GetValue<T>() => JsonProvider.ToObject<T>(JsonElement);
        public object? GetValue(Type targetType) => JsonProvider.ToObject(JsonElement, targetType);
    }
}