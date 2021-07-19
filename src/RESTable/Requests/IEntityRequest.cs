namespace RESTable.Requests
{
    internal interface IEntityRequest : IRequest
    {
        IMacro? Macro { get; }
    }
}