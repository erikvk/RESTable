using RESTar.Internal;
using RESTar.Meta.Internal;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a raised event of a type that was not identified at the time when
    /// RESTarConfig.Init() was called.
    /// </summary>
    public class UnknownEventTypeException : RESTarException
    {
        internal UnknownEventTypeException(IEventInternal @event) : base(ErrorCodes.UnknownEventType,
            $"Unknown event of type '{@event.GetType().RESTarTypeName()}' encountered. This type was not identified at " +
            "the time when RESTarConfig.Init() was called.") { }
    }
}