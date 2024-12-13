using MkvRipper.FFmpeg;
using MkvRipper.Utils;

namespace MkvRipper.MediaFiles;

public class ExportSubtitleFromVideoTask : IExportMediaTask
{
    public ExportSubtitleFromVideoTask(MediaSource source, int streamIndex, string language, string extension, string format)
    {
        Source = source;
        StreamIndex = streamIndex;
        Language = language;
        Extension = extension;
        Format = format;
    }

    /// <summary>
    /// Gets the source media file.
    /// </summary>
    public MediaSource Source { get; }
    
    /// <summary>
    /// Gets the language.
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// Gets the track id.
    /// </summary>
    public int StreamIndex { get; }
    
    /// <summary>
    /// Gets the outputs file extension.
    /// </summary>
    public string Extension { get; }
    
    /// <summary>
    /// Gets the FFmpeg format description.
    /// </summary>
    public string Format { get; }
    
    /// <inheritdoc />
    public string GetPath(MediaOutput output)
    {
        return output.GetPath($".{StreamIndex}.{Language}{Extension}");
    }
    
    private readonly SemaphoreSlim _waiter = new(0, 1);

    /// <summary>
    /// Waits for this task to be finished.
    /// </summary>
    public async Task WaitAsync()
    {
        await _waiter.WaitAsync();
    }

    /// <inheritdoc />
    public async Task ExportAsync(MediaOutput output)
    {
        var fileName = GetPath(output);
        
        var ffmpeg = new Engine();
        await FileHandler.HandleAsync(fileName, async path =>
        {
            await ffmpeg.ConvertAsync(b =>
            {
                var input = b.Input(Source.FileName);
                b.Map(input, StreamType.Subtitle, StreamIndex);
                b.Codec("copy");
                b.Format(Format);
                b.OverwriteOutput(false);
                b.Output(path);
            });
        });

        _waiter.Release();
    }
    
    public static ExportSubtitleFromVideoTask Srt(MediaSource source, int streamIndex, string language)
    {
        return new ExportSubtitleFromVideoTask(source, streamIndex, language, ".srt", "srt");
    }
    
    public static ExportSubtitleFromVideoTask Pgs(MediaSource source, int streamIndex, string language)
    {
        return new ExportSubtitleFromVideoTask(source, streamIndex, language, ".sup", "sup");
    }
    
    public static ExportSubtitleFromVideoTask Ass(MediaSource source, int streamIndex, string language)
    {
        return new ExportSubtitleFromVideoTask(source, streamIndex, language, ".ass", "ass");
    }
}