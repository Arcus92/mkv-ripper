using MkvRipper.FFmpeg;
using MkvRipper.FFmpeg.Utils;
using MkvRipper.Utils;

namespace MkvRipper.MediaFiles;

public class ExportMkvTask : IExportMediaTask
{
    public ExportMkvTask(MediaSource source)
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
        return output.GetPath(".mkv");
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
                b.AnalyzeDuration(long.MaxValue);
                b.ProbeSize(long.MaxValue);
                
                var input = b.Input(Source.FileName);
                b.ConstantRateFactor(14);
                b.MaxRate(20000);
                b.BufferSize(25000);
                b.MapChapters(input);
                b.Map(input, StreamType.Video);
                b.Map(input, StreamType.Audio);
                b.Map(input, StreamType.Subtitle);
                b.Codec(StreamType.Video, "libx264");
                b.Codec(StreamType.Subtitle, "copy");
                b.Format("matroska");
                
                b.OverwriteOutput(false);
                b.Output(path);
            }, onUpdate: (update) =>
            {
                Console.WriteLine(update.GetProgressText());
            });
        });
    }
}