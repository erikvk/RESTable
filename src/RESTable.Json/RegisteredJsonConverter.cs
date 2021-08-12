using System;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace RESTable.Json
{
    internal class RegisteredJsonConverter : IRegisteredJsonConverter
    {
        private Func<IServiceProvider, JsonConverter> GetInstanceDelegate { get; }

        public JsonConverter GetInstance(IServiceProvider serviceProvider) => GetInstanceDelegate(serviceProvider);

        internal RegisteredJsonConverter(Type converterType)
        {
            GetInstanceDelegate = provider => (JsonConverter) ActivatorUtilities.CreateInstance(provider, converterType);
        }

        internal RegisteredJsonConverter(JsonConverter instance)
        {
            GetInstanceDelegate = _ => instance;
        }
    }
}