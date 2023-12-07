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
}