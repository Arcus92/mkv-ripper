using MkvRipper.MediaFiles;

namespace MkvRipper.Tools;

/// <summary>
/// Tool to rename subtitle files and detect forced subtitles.
/// </summary>
public class SubtitleFixer
{
    public void AutoRenameSubtitle(MediaOutput output)
    {
        AutoRenameSubtitle(output, ".sup");
        AutoRenameSubtitle(output, ".srt");
    }

    private record SubtitleFile(string Path, string Language, int TrackNumber);

    private const bool ForcedSubtitleRule = false;
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
}