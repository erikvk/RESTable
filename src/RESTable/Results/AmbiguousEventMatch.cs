namespace RESTable.Results
{
    internal class AmbiguousEventMatch : NotFound
    {
        internal AmbiguousEventMatch(string info) : base(ErrorCodes.AmbiguousMatch, info) { }
    }
}