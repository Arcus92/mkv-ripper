using MkvRipper.MediaFiles;

namespace MkvRipper.Tools;

/// <summary>
/// Tool to rename subtitle files and detect forced subtitles.
/// </summary>
public class SubtitleFixer
{
    private record SubtitleFile(string Path, string Language, int TrackNumber, long FileSize);

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
        AutoRenameSubtitle(output, ".srt", ".sup");
    }
    
    /// <summary>
    /// Rename the subtitle tracks and try to guess the forced subtitles by naming conventions.
    /// </summary>
    /// <param name="output">The video output.</param>
    /// <param name="extensions">The subtitle extensions.</param>
    private void AutoRenameSubtitle(MediaOutput output, params string[] extensions)
    {
        var mainExtension = extensions.First();
        
        // Collect all subtitles and extract the language and track number.
        var allSubtitles = new List<SubtitleFile>();
        foreach (var file in output.EnumerateFiles(extensions).Order())
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
            
            if (allSubtitles.Any(x => x.Language == language && x.TrackNumber == trackNumber))
                continue;

            var fileInfo = new FileInfo(file);
            var fileSize = fileInfo.Length;

            allSubtitles.Add(new SubtitleFile(file,  language, trackNumber, fileSize));
        }

        
        // Try to detect the subtitle name:
        // The first subtitle of every language is the main track.
        // If the very next track number is the same language, this is most likely a forced track.
        // Every following track is an 'extra' track.
        foreach (var language in allSubtitles.Select(s => s.Language).Distinct())
        {
            // Fetch all track for this language.
            var subtitles = allSubtitles.Where(s => s.Language == language).OrderBy(s => s.TrackNumber).ToList();

            const long limit = 1024 * 10;
            var findOver = false;
            var findUnder = false;
            foreach (var subtitle in subtitles)
            {
                if (subtitle.FileSize > limit)
                    findOver = true;
                else
                {
                    if (findUnder)
                    {
                        findUnder = false;
                        break;
                    }
                    findUnder = true;
                }
            }

            var detectForced = findOver && findUnder;

            var index = 0;
            var extras = 0;
            foreach (var subtitle in subtitles)
            {
                string newName;

                if (detectForced && subtitle.FileSize <= limit)
                {
                    newName = $".{language}.forced";
                    detectForced = false;
                }
                // First one is always the main track
                else if (index == 0)
                {
                    newName = $".{language}";
                    index++;
                }
                else
                {
                    newName = $".{language}.extra{++extras}";
                    index++;
                }

                var baseFileName = Path.GetFileNameWithoutExtension(subtitle.Path);
                foreach (var extension in extensions)
                {
                    var oldFileName = Path.Combine(output.Directory.Path, $"{baseFileName}{extension}");
                    var newFileName = Path.Combine(output.Directory.Path, $"{output.BaseName}{newName}{extension}");
                    if (!File.Exists(oldFileName)) continue;
                    File.Move(oldFileName, newFileName);
                }
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