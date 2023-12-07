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
        /*var fileNames = Directory.EnumerateFiles(Path, "*.mp4").Select(System.IO.Path.GetFileName).ToList();

        while (fileNames.Count > 0)
        {
            var fileName = fileNames[0];
            if (fileName is null) throw new InvalidDataException();
            var pos = fileName.IndexOf('.');
            
        }*/
        
        foreach (var path in Directory.EnumerateFiles(Path, "*.mp4").Order())
        {
            var baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            yield return new MediaOutput(this, baseName);
        }
    }
}