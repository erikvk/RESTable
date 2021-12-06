using RESTable.Meta;

namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters a raised event of a type that was not identified at the time when
///     RESTableConfig.Init() was called.
/// </summary>
public class UnknownEventTypeException : RESTableException
{
    internal UnknownEventTypeException(IEvent @event) : base(ErrorCodes.UnknownEventType,
        $"Unknown event of type '{@event.GetType().GetRESTableTypeName()}' encountered. This type was not identified at " +
        "the time when RESTableConfig.Init() was called. Are you missing a 'RESTableAttribute' decoration?") { }
}