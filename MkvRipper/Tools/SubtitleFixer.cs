using MkvRipper.MediaFiles;

namespace MkvRipper.Tools;

/// <summary>
/// Tool to rename subtitle files and detect forced subtitles.
/// </summary>
public class SubtitleFixer
{
    private record SubtitleFile(string Path, string Language, int TrackNumber);

    private const bool ForcedSubtitleRule = false;

    /// <summary>
    /// Tries to fix the subtitles in the output directory.
    /// </summary>
    /// <param name="outputDirectory">The output directory.</param>
    public async Task ExecuteAsync(MediaOutputDirectory outputDirectory)
    {
        await FixEmptySrtFilesAsync(outputDirectory);
        foreach (var output in outputDirectory.EnumerateOutputs())
        {
            AutoRenameSubtitle(output);
        }
    }
    
    /// <summary>
    /// Rename the subtitle tracks and try to guess the forced subtitles by naming conventions.
    /// </summary>
    /// <param name="output">The video output.</param>
    private void AutoRenameSubtitle(MediaOutput output)
    {
        AutoRenameSubtitle(output, ".sup");
        AutoRenameSubtitle(output, ".srt");
    }
    
    /// <summary>
    /// Rename the subtitle tracks and try to guess the forced subtitles by naming conventions.
    /// </summary>
    /// <param name="output">The video output.</param>
    /// <param name="extension">The subtitle extension.</param>
    private void AutoRenameSubtitle(MediaOutput output, string extension)
    {
        // Collect all subtitles and extract the language and track number.
        var allSubtitles = new List<SubtitleFile>();
        foreach (var file in output.EnumerateFiles(extension).Order())
        {
            var indexExt = file.LastIndexOf('.');
            if (indexExt < 0) continue;
            var indexLang = file.LastIndexOf('.', indexExt - 1);
            if (indexLang < 0) continue;
            var indexTrack = file.LastIndexOf('.', indexLang - 1);
            if (indexTrack < 0) continue;

            var language = file.Substring(indexLang + 1, indexExt - indexLang - 1);
            var trackName = file.Substring(indexTrack + 1, indexLang - indexTrack - 1);
            if (!int.TryParse(trackName, out var trackNumber)) continue;

            allSubtitles.Add(new SubtitleFile(file,  language, trackNumber));
        }

        
        // Try to detect the subtitle name:
        // The first subtitle of every language is the main track.
        // If the very next track number is the same language, this is most likely a forced track.
        // Every following track is an 'extra' track.
        foreach (var language in allSubtitles.Select(s => s.Language).Distinct())
        {
            // Fetch all track for this language.
            var subtitles = allSubtitles.Where(s => s.Language == language).ToList();
            
            var extras = 0;
            for (var i = 0; i < subtitles.Count; i++)
            {
                var subtitle = subtitles[i];
                string newExtension;

                // First one is always the main track
                if (i == 0)
                {
                    newExtension = $".{language}{extension}";
                }
                else
                {
                    var prev = subtitles[i - 1];
                    if (i == 1 && (prev.TrackNumber == subtitle.TrackNumber - 1 || !ForcedSubtitleRule))
                    {
                        newExtension = $".{language}.forced{extension}";
                    }
                    else
                    {
                        newExtension = $".{language}.extra{++extras}{extension}";
                    }
                }

                var newFileName = Path.Combine(output.Directory.Path, $"{output.BaseName}{newExtension}");
                File.Move(subtitle.Path, newFileName);
            }
        }
    }

    /// <summary>
    /// Searches for empty .srt files and adding a generic empty subtitle line, so tools like Jellyfin wont ignore that
    /// track.
    /// </summary>
    /// <param name="output">The output directory.</param>
    private async Task FixEmptySrtFilesAsync(MediaOutputDirectory output)
    {
        foreach (var file in output.EnumerateFiles(".srt"))
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.Length != 0) continue;

            await using var writer = new StreamWriter(file);
            
            await writer.WriteLineAsync("1");
            await writer.WriteLineAsync("00:00:00,000 --> 00:00:000,000");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync();
        }
    }
}