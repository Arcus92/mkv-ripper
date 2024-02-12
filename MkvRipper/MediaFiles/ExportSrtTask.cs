using MkvRipper.FFmpeg;
using MkvRipper.Utils;

namespace MkvRipper.MediaFiles;

public class ExportSrtTask : IExportMediaTask
{
    public ExportSrtTask(MediaSource source, int streamIndex, string language)
    {
        Source = source;
        StreamIndex = streamIndex;
        Language = language;
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

    /// <inheritdoc />
    public string GetPath(MediaOutput output)
    {
        return output.GetPath($".{StreamIndex}.{Language}.srt");
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
                b.Format("srt");
                b.OverwriteOutput(false);
                b.Output(path);
            });
        });
    }
}