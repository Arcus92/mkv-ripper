using System.Text;
using MkvRipper.Subtitles.PGS;

namespace MkvRipper.Subtitles.Exporter;

/// <summary>
/// Extension methods to converts a PGS to a a text-based .str file.
/// </summary>
public static class Pgs2Srt
{
    /// <summary>
    /// Exports a PGS to a .srt file.
    /// </summary>
    /// <param name="pgs">The PGS to read the subtitles from.</param>
    /// <param name="filename">The .srt filename.</param>
    /// <param name="language">The language to use for the OCR.</param>
    public static async Task WriteToSrtFileAsync(this IPresentationGraphicStream pgs, string filename, string language)
    {
        await using var stream = new FileStream(filename, FileMode.Create);
        await WriteToSrtFileAsync(pgs, stream, language);
    }
    
    /// <summary>
    /// Exports a PGS to a .srt file.
    /// </summary>
    /// <param name="pgs">The PGS to read the subtitles from.</param>
    /// <param name="stream">The .srt stream.</param>
    /// <param name="language">The language to use for the OCR.</param>
    public static async Task WriteToSrtFileAsync(this IPresentationGraphicStream pgs, Stream stream, string language)
    {
        var subtitles = pgs.ToTextAsync(language);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        var counter = 1;
        await foreach (var subtitle in subtitles)
        {
            await writer.WriteLineAsync($"{counter++}");
            await writer.WriteLineAsync($@"{subtitle.Start:hh\:mm\:ss\,fff} --> {subtitle.End:hh\:mm\:ss\,fff}");
            await writer.WriteLineAsync(subtitle.Text);
            await writer.WriteLineAsync();
        }
    }
}