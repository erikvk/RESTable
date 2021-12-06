namespace RESTable.Results;

internal class UnknownEvent : NotFound
{
    internal UnknownEvent(string info) : base(ErrorCodes.UnknownEventType, info) { }
}