using MkvRipper.Subtitles.Exporter;
using MkvRipper.Subtitles.PGS;
using MkvRipper.Utils;

namespace MkvRipper.MediaFiles;

public class ExportSrtFromSupTask : IExportMediaTask
{
    public ExportSrtFromSupTask(ExportSubtitleFromVideoTask parent)
    {
        ParentTask = parent;
    }

    public ExportSubtitleFromVideoTask ParentTask { get; }
    
    /// <inheritdoc />
    public string GetPath(MediaOutput output)
    {
        return output.GetPath($".{ParentTask.StreamIndex}.{ParentTask.Language}.srt");
    }
    
    /// <inheritdoc />
    public async Task ExportAsync(MediaOutput output)
    {
        var fileName = GetPath(output);
        var pgsFileName = ParentTask.GetPath(output);

        // If the input file doesn't exist, we need to wait for the parent task to finish to create the PGS file.
        if (!File.Exists(pgsFileName))
        {
            await ParentTask.WaitAsync();
        }
        
        var pgs = new SupFilePresentationGraphicStream(pgsFileName);
        await FileHandler.HandleAsync(fileName, async path =>
        {
            await pgs.WriteToSrtFileAsync(path, ParentTask.Language);
        });
    }
}