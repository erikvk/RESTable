using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace RESTable.Json
{
    internal class RegisteredJsonConverter : IRegisteredJsonConverter
    {
        private Func<IServiceProvider, JsonConverter> GetInstanceDelegate { get; }

        public JsonConverter GetInstance(IServiceProvider serviceProvider) => GetInstanceDelegate(serviceProvider);

        public Type ConverterType { get; }

        internal RegisteredJsonConverter(Type converterType)
        {
            ConverterType = converterType;
            if (converterType.IsGenericTypeDefinition)
                GetInstanceDelegate = _ => throw new InvalidOperationException("Cannot instantiate a generic type definition");
            else GetInstanceDelegate = provider => (JsonConverter) ActivatorUtilities.CreateInstance(provider, converterType);
        }

        internal RegisteredJsonConverter(JsonConverter instance)
        {
            ConverterType = instance.GetType();
            GetInstanceDelegate = _ => instance;
        }
    }
}