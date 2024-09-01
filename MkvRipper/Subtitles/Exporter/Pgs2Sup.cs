using MkvRipper.Subtitles.PGS;
using MkvRipper.Subtitles.Utils;

namespace MkvRipper.Subtitles.Exporter;

/// <summary>
/// Extension methods to converts a PGS to a a .sup file.
/// </summary>
public static class Pgs2Sup
{
    /// <summary>
    /// Exports a PGS to a .sup file.
    /// </summary>
    /// <param name="pgs">The PGS to read the subtitles from.</param>
    /// <param name="filename">The .sup filename.</param>
    public static async Task WriteToSupFileAsync(this IPresentationGraphicStream pgs, string filename)
    {
        await using var stream = new FileStream(filename, FileMode.Create);
        await WriteToSupFileAsync(pgs, stream);
    }
    
    /// <summary>
    /// Exports a PGS to a .sup file.
    /// </summary>
    /// <param name="pgs">The PGS to read the subtitles from.</param>
    /// <param name="stream">The .sup stream.</param>
    public static async Task WriteToSupFileAsync(this IPresentationGraphicStream pgs, Stream stream)
    {
        using var writer = new BigEndianBinaryWriter(stream);
        var displaySets = pgs.ReadAndCleanUpAsync();
        await foreach (var displaySet in displaySets)
        {
            displaySet.Write(writer, includeHeader: true);
        }
    }
}