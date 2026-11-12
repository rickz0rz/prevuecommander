namespace Prevue.Core;

/// <summary>Compares channel numbers such as "4", "38", "2.1" and "6-1" by their major and minor numbers.</summary>
public sealed class ChannelNumberComparer : IComparer<string?>
{
    public static readonly ChannelNumberComparer Instance = new();

    public int Compare(string? x, string? y) => Key(x).CompareTo(Key(y));

    /// <summary>Numbers that are not valid sort after all the valid numbers.</summary>
    private static (int Major, int Minor) Key(string? number)
    {
        var parts = (number ?? string.Empty).Split('.', '-');
        var major = int.TryParse(parts[0], out var value) ? value : int.MaxValue;
        var minor = parts.Length > 1 && int.TryParse(parts[1], out var second) ? second : 0;
        return (major, minor);
    }
}
