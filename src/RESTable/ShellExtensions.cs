namespace RESTable;

internal static class ShellExtensions
{
    internal static int? AsNumber(this string? tail)
    {
        if (tail is null || !int.TryParse(tail, out var nr))
            return null;
        return nr;
    }
}