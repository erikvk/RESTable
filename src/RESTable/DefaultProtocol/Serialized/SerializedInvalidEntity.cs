using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized;

public class SerializedInvalidEntity : ISerializedError
{
    public SerializedInvalidEntity(InvalidInputEntity invalidEntity, string? uri)
    {
        InvalidEntity = invalidEntity;
        Data = invalidEntity.InvalidEntity.InvalidMembers.ToDictionary(m => m.MemberName, m => m.Message);
        Uri = uri;
    }

    private InvalidInputEntity InvalidEntity { get; }

    public Dictionary<string, string> Data { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private long? InvalidEntityIndex => InvalidEntity.InvalidEntity.Index;

    public double TimeElapsedMs => InvalidEntity.TimeElapsed.GetRESTableElapsedMs();

    public string Status => "fail";

    public string? ErrorType => typeof(InvalidInputEntity).FullName;

    public ErrorCodes ErrorCode => InvalidEntity.ErrorCode;

    public string Message => InvalidEntity.Message;

    public string? MoreInfoAt => InvalidEntity.Headers.Error;

    public string TimeStamp => DateTime.UtcNow.ToString("O");

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uri { get; }
}
