using System;
using System.Text.Json.Serialization;

namespace RESTable.Json
{
    public interface IRegisteredJsonConverter
    {
        JsonConverter GetInstance(IServiceProvider serviceProvider);
    }
}