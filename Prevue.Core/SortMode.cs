namespace Prevue.Core;

public enum SortMode
{
    None,
    Ascending,
    Descending
}

public static class SortModeExtensions
{
    /// <summary>Sorts the items by their channel numbers. <see cref="SortMode.None"/> keeps the order.</summary>
    public static IEnumerable<T> SortByChannelNumber<T>(this IEnumerable<T> items, SortMode mode,
        Func<T, string?> number) => mode switch
    {
        SortMode.Ascending => items.OrderBy(number, ChannelNumberComparer.Instance),
        SortMode.Descending => items.OrderByDescending(number, ChannelNumberComparer.Instance),
        _ => items
    };
}
