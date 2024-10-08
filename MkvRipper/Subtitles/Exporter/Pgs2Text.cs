using System.Text;
using MkvRipper.Subtitles.PGS;
using MkvRipper.Subtitles.PGS.Segments;
using MkvRipper.Subtitles.Utils;
using MkvRipper.Utils;
using Tesseract;

namespace MkvRipper.Subtitles.Exporter;

/// <summary>
/// Extension methods to extract the text of a PGS using OCR.
/// </summary>
public static class Pgs2Text
{
    /// <summary>
    /// Data for the current on-screen composition object.
    /// </summary>
    private struct ActiveCompositionObject
    {
        public DisplaySet DisplaySet { get; set; }
        public WindowDefinitionSegment.WindowDefinition Window { get; set; }
        public string Text { get; set; }
        public TimeSpan Start { get; set; }
    }
    
    /// <summary>
    /// Exports a PGS to a .sup file.
    /// </summary>
    /// <param name="pgs">The PGS to read the subtitles from.</param>
    /// <param name="language">The language of the subtitle track.</param>
    /// <returns>Returns the subtitles in text format.</returns>
    public static async IAsyncEnumerable<Subtitle> ToTextAsync(this IPresentationGraphicStream pgs, string language)
    {
        var activeCompositionObjects = new List<ActiveCompositionObject>();
        using var tesseract = await TesseractManager.Shared.GetEngineAsync(language);
        
        var ctxObjects = new List<ObjectDefinitionSegment>();
        var ctxPalettes = new Dictionary<ushort, PaletteDefinitionSegment>();
        var ctxWindows = new Dictionary<ushort, WindowDefinitionSegment.WindowDefinition>();
        
        await foreach (var displaySet in pgs.ReadAndCleanUpAsync())
        {
            var time = TimeSpan.FromMilliseconds(displaySet.PresentationTimestamp / 90.0);

            // Frees all previous segments
            if (displaySet.PresentationComposition.CompositionState != 0)
            {
                ctxObjects.Clear();
                ctxPalettes.Clear();
                ctxWindows.Clear();
            }

            // Collect all the active segments
            foreach (var ods in displaySet.ObjectDefinitions)
            {
                ctxObjects.Add(ods);
            }
            foreach (var pds in displaySet.PaletteDefinitions)
            {
                ctxPalettes[pds.Id] = pds;
            }
            foreach (var wds in displaySet.WindowDefinitions.SelectMany(w => w.Windows))
            {
                ctxWindows[wds.Id] = wds;
            }
            
            
            // TODO: Handle different windows and cropping.
            if (activeCompositionObjects.Count == 1)
            {
                // Only one text is on screen.
                var first = activeCompositionObjects[0];
                yield return new Subtitle()
                {
                    Text = first.Text,
                    Start = first.Start,
                    End = time,
                };
            }
            else if (activeCompositionObjects.Count > 1)
            {
                // There are two windows on screen. We'll just add the text together.
                // In the future, we could try to detect the window position and try to get the order right, but not for now.
                var first = activeCompositionObjects[0];
                var stringBuilder = new StringBuilder();
                foreach (var active in activeCompositionObjects)
                {
                    stringBuilder.AppendLine(active.Text);
                }
                
                yield return new Subtitle()
                {
                    Text = stringBuilder.ToString(),
                    Start = first.Start,
                    End = time,
                };
            }
            
            activeCompositionObjects.Clear();
            
            // Load the palette
            if (!ctxPalettes.TryGetValue(displaySet.PresentationComposition.PaletteId, out var palette))
                continue;

            foreach (var compositionObject in displaySet.PresentationComposition.CompositionObjects)
            {
                // Load the window
                if (!ctxWindows.TryGetValue(compositionObject.WindowId, out var window))
                    continue;
                
                using var pix = ToPix(ctxObjects.Where(ods => ods.Id == compositionObject.Id), palette);
                if (pix is null) continue;
            
                using var page = tesseract.Process(pix);
                var text = page.GetText();
                if (string.IsNullOrEmpty(text))
                    continue;
                
                activeCompositionObjects.Add(new ActiveCompositionObject()
                {
                    DisplaySet = displaySet,
                    Window = window,
                    Text = text,
                    Start = time
                });
            }
        }
    }
    
    /// <summary>
    /// Converts a composition to a picture for text recognition.
    /// </summary>
    /// <param name="objectDefinitionSegments">The active objects for this composition with the same id.</param>
    /// <param name="palette">The palette for the pixel data.</param>
    /// <returns></returns>
    private static Pix? ToPix(IEnumerable<ObjectDefinitionSegment> objectDefinitionSegments, 
        PaletteDefinitionSegment palette)
    {
        ushort width = 0;
        ushort height = 0;
        var data = new List<byte>();
        foreach (var ods in objectDefinitionSegments)
        {
            if (ods.IsFirstInSequence)
            {
                width = ods.Width;
                height = ods.Height;
            }
            
            data.AddRange(ods.Data);
        }

        // Nothing to read
        if (data.Count == 0)
            return null;
        
        var pix = Pix.Create(width, height, 32);
        var pixData = pix.GetData();
        
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