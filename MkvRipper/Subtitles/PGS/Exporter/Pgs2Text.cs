using MkvRipper.Subtitles.PGS.Segments;
using MkvRipper.Utils;
using Tesseract;

namespace MkvRipper.Subtitles.PGS.Exporter;

/// <summary>
/// Extension methods to extract the text of a PGS using OCR.
/// </summary>
public static class Pgs2Text
{
    /// <summary>
    /// Exports a PGS to a .sup file.
    /// </summary>
    /// <param name="pgs">The PGS to read the subtitles from.</param>
    /// <param name="language">The language of the subtitle track.</param>
    /// <returns>Returns the subtitles in text format.</returns>
    public static async IAsyncEnumerable<Subtitle> ToTextAsync(this IPresentationGraphicStream pgs, string language)
    {
        using var tesseract = await TesseractManager.Shared.GetEngineAsync(language);

        var currentWindowId = 0;
        var currentText = "";
        var currentStart = default(TimeSpan);

        await foreach (var displaySet in pgs.ReadAsync())
        {
            var time = TimeSpan.FromMilliseconds(displaySet.PresentationTimestamp / 90.0);

            // TODO: Handle different windows and cropping.
            if (!string.IsNullOrEmpty(currentText))
            {
                yield return new Subtitle()
                {
                    Text = currentText,
                    Start = currentStart,
                    End = time,
                };
                currentText = "";
            }
            
            if (displaySet.ObjectDefinitions.Count == 0) continue;

            using var pix = displaySet.ToPix();
            using var page = tesseract.Process(pix);
            var text = page.GetText();
            if (string.IsNullOrEmpty(text))
                continue;

            currentText = text;
            currentStart = time;
        }
    }
    
    /// <summary>
    /// Converts the current display set to a picture for text recognition.
    /// </summary>
    /// <param name="displaySet">The display set.</param>
    /// <returns></returns>
    public static Pix ToPix(this DisplaySet displaySet)
    {
        var paletteId = displaySet.PresentationComposition.PaletteId;
        ushort width = 0;
        ushort height = 0;
        var data = new List<byte>();
        foreach (var ods in displaySet.ObjectDefinitions)
        {
            if (ods.IsFirstInSequence)
            {
                width = ods.Width;
                height = ods.Height;
            }
            
            data.AddRange(ods.Data);
        }
        
        var pix = Pix.Create(width, height, 32);
        var pixData = pix.GetData();

        var palette = displaySet.PaletteDefinitions.First(p => p.Id == paletteId);
        RunLengthEncoding.Encode(data.ToArray(), (pixData, palette), WriteColorToPix);
        return pix;
    }
    
    
    /// <summary>
    /// Writes a pixel to the pix data.
    /// </summary>
    /// <param name="param">The pix data and the used palette.</param>
    /// <param name="x">The x position of the pixel.</param>
    /// <param name="y">The y position of the pixel.</param>
    /// <param name="b">The entry id in the current palette.</param>
    private static unsafe void WriteColorToPix((PixData, PaletteDefinitionSegment) param, ushort x, ushort y, byte b)
    {
        var pixData = param.Item1;
        var palette = param.Item2;
        if (!palette.Entries.TryGetValue(b, out var entry))
            return;
        var color = entry.ToPixColour();

        // Most subtitles use white text with black outline. This will map white to 1 and black / transparent to 0.
        var value = (byte)Math.Clamp((color.Red + color.Green + color.Blue) / 3.0 * (color.Alpha / 255.0), 0, 255);
        
        // Bit magic to get the position in the native image data.
        var pointer = (byte*)pixData.Data + y * (pixData.WordsPerLine << 2) + (x << 2);
        pointer[0] = 0;
        pointer[1] = 0;
        pointer[2] = 0;
        pointer[3] = value;
    }
    
    /// <summary>
    /// Converts a YCbCR palette entry to an RGBA color.
    /// </summary>
    /// <param name="entry">The palette entry.</param>
    /// <returns>Returns the pix color.</returns>
    private static PixColor ToPixColour(this PaletteDefinitionSegment.Entry entry)
    {
        var y = entry.Y;
        var cb = entry.Cb - 128;
        var cr = entry.Cr - 128;
        var r = (byte)Math.Clamp(Math.Round(y + 1.40200 * cr), 0, 255);
        var g = (byte)Math.Clamp(Math.Round(y - 0.34414 * cb - 0.71414 * cr), 0, 255);
        var b = (byte)Math.Clamp(Math.Round(y + 1.77200 * cb), 0, 255);
        return new PixColor(r, g, b, entry.A);
    }
}