namespace MkvRipper.MediaFiles;

public class MediaOutput
{
    public MediaOutput(MediaOutputDirectory directory, string baseName)
    {
        Directory = directory;
        BaseName = baseName;
    }
    
    /// <summary>
    /// Gets the base name of the output media.
    /// </summary>
    public string BaseName { get; private set; }
    
    /// <summary>
    /// Gets the file size of the main video.
    /// </summary>
    public long FileSize
    {
        get
        {
            var fileName = GetPath(".mp4");
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists) return 0;
            return fileInfo.Length;
        }
    }
    
    /// <summary>
    /// Gets the file creation time of the main video.
    /// </summary>
    public DateTime FileCreationTime
    {
        get
        {
            var fileName = GetPath(".mp4");
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists) return default;
            return fileInfo.CreationTime;
        }
    }

    /// <summary>
    /// Gets the number of files.
    /// </summary>
    public int FileCount => EnumerateFiles().Count();

    /// <summary>
    /// Gets the output directory.
    /// </summary>
    public MediaOutputDirectory Directory { get; }

    /// <summary>
    /// Gets the full filename for the given file extension.
    /// </summary>
    /// <param name="extension">The extension, starting with the dot.</param>
    /// <returns>The full file path.</returns>
    public string GetPath(string extension)
    {
        return Path.Combine(Directory.Path, $"{BaseName}{extension}");
    }

    /// <summary>
    /// Returns all files from this output.
    /// </summary>
    /// <param name="extension">The file extension filter.</param>
    /// <returns></returns>
    public IEnumerable<string> EnumerateFiles(string? extension = null)
    {
        var baseName = $"{BaseName}.";
        foreach (var path in System.IO.Directory.EnumerateFiles(Directory.Path))
        {
            var fileName = Path.GetFileName(path);
            if (fileName.StartsWith(baseName) && 
                (extension is null || fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
                yield return path;
        }
    }

    /// <summary>
    /// Renames all files from the output.
    /// </summary>
    /// <param name="baseName">The new base name.</param>
    public void Rename(string baseName)
    {
        foreach (var path in EnumerateFiles())
        {
            var fileName = Path.GetFileName(path);
            fileName = baseName + fileName[BaseName.Length..];
            var newPath = Path.Combine(Directory.Path, fileName);
            File.Move(path, newPath);
        }
        BaseName = baseName;
    }
}