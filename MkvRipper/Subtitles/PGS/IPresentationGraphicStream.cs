namespace MkvRipper.Subtitles.PGS;

public interface IPresentationGraphicStream
{
    /// <summary>
    /// Reads the presentation graphic stream and returns all display sets.
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<DisplaySet> ReadAsync();
}