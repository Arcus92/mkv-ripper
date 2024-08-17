using Matroska.Models;
using MkvRipper.Subtitles.PGS;
using MkvRipper.Subtitles.PGS.Exporter;
using MkvRipper.Utils;

namespace MkvRipper.MediaFiles;

public class ExportSupTask : IExportMediaTask
{
    public ExportSupTask(MediaSource source, TrackEntry track, string langauge)
    {
        Source = source;
        Track = track;
        Language = langauge;
    }
    
    /// <summary>
    /// Gets the source media file.
    /// </summary>
    public MediaSource Source { get; }

    /// <summary>
    /// Gets the subtitle track.
    /// </summary>
    public TrackEntry Track { get; }

    /// <summary>
    /// Gets the subtitle language.
    /// </summary>
    public string Language { get; }

    /// <inheritdoc />
    public string GetPath(MediaOutput output)
    {
        return output.GetPath($".{Track.TrackNumber}.{Language}.sup");
    }
    
    /// <inheritdoc />
    public async Task ExportAsync(MediaOutput output)
    {
        var fileName = GetPath(output);

        var matroska = await Source.LoadMatroskaAsync();
        var pgs = new MatroskaPresentationGraphicStream(matroska, Track.TrackNumber);
        await FileHandler.HandleAsync(fileName, async path =>
        {
            await pgs.WriteToSupFileAsync(path);
        });
    }
}