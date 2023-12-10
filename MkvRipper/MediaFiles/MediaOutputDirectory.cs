namespace MkvRipper.MediaFiles;

public class MediaOutputDirectory
{
    public MediaOutputDirectory(string path)
    {
        Path = path;
    }
    
    /// <summary>
    /// Gets the output path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Returns all outputs in this directory.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<MediaOutput> EnumerateOutputs()
    {
        foreach (var path in Directory.EnumerateFiles(Path, "*.mp4").Order())
        {
            var baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            yield return new MediaOutput(this, baseName);
        }
    }
    
    /// <summary>
    /// Returns all files with the given extension in the output directory.
    /// </summary>
    /// <param name="extension">The file extension starting with a dot.</param>
    /// <returns></returns>
    public IEnumerable<string> EnumerateFiles(string extension)
    {
        return Directory.EnumerateFiles(Path, $"*{extension}").Order();
    }
}