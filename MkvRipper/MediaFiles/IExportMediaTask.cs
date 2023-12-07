namespace MkvRipper.MediaFiles;

public interface IExportMediaTask
{
    /// <summary>
    /// Returns the full filename.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <returns></returns>
    string GetPath(MediaOutput output);

    /// <summary>
    /// Returns if the output file already exists.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <returns></returns>
    bool Exists(MediaOutput output) => File.Exists(GetPath(output));
    
    /// <summary>
    /// Export the media file.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <returns></returns>
    Task ExportAsync(MediaOutput output);
}