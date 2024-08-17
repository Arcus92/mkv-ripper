namespace MkvRipper.Subtitles;

public readonly struct Subtitle
{
    /// <summary>
    /// Gets the subtitle text.
    /// </summary>
    public string Text { get; init; }
    
    /// <summary>
    /// Gets the starting time of the subtitle.
    /// </summary>
    public TimeSpan Start { get; init; }
    
    /// <summary>
    /// Gets the ending time of the subtitle.
    /// </summary>
    public TimeSpan End { get; init; }

    /// <summary>
    /// Gets the subtitle duration.
    /// </summary>
    public TimeSpan Duration => End - Start;
    
    public override string ToString()
    {
        return $"{Text}";
    }

    /// <summary>
    /// Converts the given subtitle language to a ISO 3166 language code.
    /// </summary>
    /// <param name="language">The original language.</param>
    /// <returns>The ISO 3166 language.</returns>
    public static string MapSubtitleLanguages(string language)
    {
        return language switch
        {
            "ger" => "deu",
            "fre" => "fra",
            "dut" => "nld",
            "ice" => "isl",
            _ => language
        };
    }
}