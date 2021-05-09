namespace RESTable
{
    internal static class ShellExtensions
    {
        internal static int? ToNumber(this string tail)
        {
            if (tail is null || !int.TryParse(tail, out var nr))
                return null;
            return nr;
        }
    }
}