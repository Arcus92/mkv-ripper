using MkvRipper.FFmpeg;
using MkvRipper.Utils;

namespace MkvRipper.MediaFiles;

public class ExportMp4Task : IExportMediaTask
{
    public ExportMp4Task(MediaSource source)
    {
        Source = source;
    }
    
    /// <summary>
    /// Gets the source media file.
    /// </summary>
    public MediaSource Source { get; }

    /// <inheritdoc />
    public string GetPath(MediaOutput output)
    {
        return output.GetPath(".mp4");
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
                b.ConstantRateFactor(14);
                b.MaxRate(20000);
                b.BufferSize(25000);
                b.MapChapters(input);
                b.MapMetadata(-1);
                b.Map(input, StreamType.Video);
                b.Map(input, StreamType.Audio);
                b.Codec(StreamType.Video, "libx264");
                b.Format("mp4");
                b.OverwriteOutput(false);
                b.Output(path);
            }, onUpdate: (update) =>
            {
                Console.WriteLine($"[{Math.Floor(update.Percentage * 100):0}%] {update.Current:hh\\:mm\\:ss} / {update.Duration:hh\\:mm\\:ss}");
            });
        });
    }
}