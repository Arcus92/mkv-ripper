namespace MkvRipper.FFmpeg;

public enum StreamType
{
    Video,
    Audio,
    Subtitle,
    Data,
    Attachment
}

public static class StreamTypeHelper
{
    /// <summary>
    /// Returns the stream identifier from the given type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static char Identifier(this StreamType type)
    {
        return type switch
        {
            StreamType.Video => 'v',
            StreamType.Audio => 'a',
            StreamType.Subtitle => 's',
            StreamType.Data => 'd',
            StreamType.Attachment => 't',
            _ => throw new ArgumentException($"Unknown value: {type}", nameof(type))
        };
    }
}