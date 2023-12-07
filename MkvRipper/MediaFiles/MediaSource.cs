using Matroska;
using Matroska.Models;

namespace MkvRipper.MediaFiles;

public class MediaSource
{
    public MediaSource(string fileName)
    {
        FileName = fileName;
        _fileInfo = new FileInfo(FileName);
    }
    
    /// <summary>
    /// Gets the filename of this media.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The file info
    /// </summary>
    private readonly FileInfo _fileInfo;

    /// <summary>
    /// Gets the file size.
    /// </summary>
    public long FileSize => _fileInfo.Length;

    /// <summary>
    /// Gets the base name without any file extensions.
    /// </summary>
    public string BaseName => Path.GetFileNameWithoutExtension(FileName);

    /// <summary>
    /// The loaded matroska document
    /// </summary>
    private MatroskaDocument? _matroska;

    private readonly SemaphoreSlim _matroskaLock = new(1, 1);
    
    /// <summary>
    /// Load and returns the matroska document.
    /// </summary>
    /// <returns></returns>
    public async Task<MatroskaDocument> LoadMatroskaAsync()
    {
        if (_matroska is not null) return _matroska;
        await _matroskaLock.WaitAsync();
        try
        {
            await using var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read);
        
            // TODO: This loads the whole .mkv file in memory. Currently this isn't an issue for my system, but I should 
            // find another solution to partially read the file.
            var matroska = MatroskaSerializer.Deserialize(stream);
            _matroska = matroska;
            return matroska;
        }
        finally
        {
            _matroskaLock.Release();
        }
    }
    
    /// <summary>
    /// Returns all export tasks for this media file.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<IExportMediaTask> GetExportTasksAsync()
    {
        // Returns the video.
        yield return new ExportMp4Task(this);
        
        // Returns all subtitles from the matroska file.
        var matroska = await LoadMatroskaAsync();
        if (matroska.Segment.Tracks is null) yield break;
        foreach (var track in matroska.Segment.Tracks.TrackEntries.Where(t => t.TrackType == 17))
        {
            yield return new ExportSupTask(this, track);
            yield return new ExportSrtTask(this, track);
        }
    }

    /// <summary>
    /// Returns all sources from the given directory.
    /// </summary>
    /// <param name="directory">The source directory.</param>
    /// <returns></returns>
    public static IEnumerable<MediaSource> FromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }
        foreach (var fileName in Directory.EnumerateFiles(directory, "*.mkv"))
        {
            yield return new MediaSource(fileName);
        }
    }

    /// <summary>
    /// Unloads the matroska data.
    /// </summary>
    public void Unload()
    {
        _matroska = null;
        GC.Collect();
    }
}