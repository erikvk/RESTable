namespace RESTable
{
    internal static class ShellExtensions
    {
        internal static int? ToNumber(this string tail)
        {
            if (tail == null || !int.TryParse(tail, out var nr))
                return null;
            return nr;
        }
    }
}