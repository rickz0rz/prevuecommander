using System.Globalization;
using System.IO.Compression;

namespace PrevueCommander.WeatherGov;

/// <summary>
/// Finds the location of a ZIP code. The table has the center of each ZIP Code Tabulation Area (ZCTA) of the 2024
/// Gazetteer of the U.S. Census Bureau. weather.gov needs a latitude and a longitude, and it does not accept a ZIP code.
/// </summary>
public static class ZipCodes
{
    private static readonly Lazy<Dictionary<string, (double Latitude, double Longitude)>> Table = new(Load);

    /// <summary>The location of a ZIP code with 5 digits, or null if the table does not have it.</summary>
    public static (double Latitude, double Longitude)? Find(string zip)
    {
        zip = zip.Trim();
        // A ZIP+4 code, for example 48201-1234, has the same location as its first 5 digits.
        if (zip.Length == 10 && zip[5] == '-')
            zip = zip[..5];
        return Table.Value.TryGetValue(zip, out var location) ? location : null;
    }

    private static Dictionary<string, (double, double)> Load()
    {
        using var resource = typeof(ZipCodes).Assembly.GetManifestResourceStream("PrevueCommander.WeatherGov.zcta.csv.gz")
                             ?? throw new InvalidOperationException("The ZIP code table is missing from the program.");
        using var reader = new StreamReader(new GZipStream(resource, CompressionMode.Decompress));
        var table = new Dictionary<string, (double, double)>(34000);
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split(',');
            table[parts[0]] = (double.Parse(parts[1], CultureInfo.InvariantCulture),
                double.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        return table;
    }
}
