using System.Text;

namespace MkvRipper.FFmpeg;

/// <summary>
/// A class to build the ffmpeg command line.
/// </summary>
public class CommandBuilder
{
    /// <summary>
    /// The internal string builder.
    /// </summary>
    private readonly StringBuilder _arguments = new();

    /// <summary>
    /// Gets the arguments.
    /// </summary>
    public string Arguments => _arguments.ToString();

    /// <summary>
    /// The number of inputs.
    /// </summary>
    private int _inputs;

    /// <summary>
    /// Adds an input to the convert and returns it's index.
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>Returns the id of the input.</returns>
    public int Input(string path)
    {
        _arguments.Append($"-i \"{path}\" ");
        return _inputs++;
    }

    /// <summary>
    /// Sets the decryption key.
    /// </summary>
    /// <param name="key">The decryption key.</param>
    public void DecryptionKey(string key)
    {
        _arguments.Append($"-decryption_key {key} ");
    }
    
    /// <summary>
    /// Forces an output format.
    /// </summary>
    /// <param name="extension">The output file extension without dot.</param>
    public void Format(string extension)
    {
        _arguments.Append($"-f {extension} ");
    }

    /// <summary>
    /// Sets if the output file should be overwritten if it exists.
    /// </summary>
    /// <param name="overwrite">
    /// If set to true, the file will be overwritten without asking.
    /// If set to false, the converts fails it the exists. 
    /// </param>
    public void OverwriteOutput(bool overwrite = true)
    {
        _arguments.Append(overwrite ? "-y " : "-n ");
    }

    /// <summary>
    /// Sets the max bit rate.
    /// </summary>
    /// <param name="value">The bit rate in k.</param>
    public void MaxRate(int value)
    {
        _arguments.Append($"-maxrate {value}k ");
    }
    
    /// <summary>
    /// Sets the buffer size.
    /// </summary>
    /// <param name="value">The buffer size in k.</param>
    public void BufferSize(int value)
    {
        _arguments.Append($"-bufsize {value}k ");
    }

    /// <summary>
    /// Sets the constant rate factor.
    /// </summary>
    /// <param name="value">The constant rate factor.</param>
    public void ConstantRateFactor(int value)
    {
        _arguments.Append($"-crf {value} ");
    }

    /// <summary>
    /// Sets the codec to use for the given stream types.
    /// </summary>
    /// <param name="streamType">The stream type to set the codec.</param>
    /// <param name="codec">The codec to use.</param>
    public void Codec(StreamType streamType, string codec)
    {
        _arguments.Append($"-c:{streamType.Identifier()} {codec} ");
    }
    
    /// <summary>
    /// Sets the codec to use for the given stream types.
    /// </summary>
    /// <param name="streamType">The stream type to set the codec.</param>
    /// <param name="streamIndex">The index of the stream to set the codec.</param>
    /// <param name="codec">The codec to use.</param>
    public void Codec(StreamType streamType, int streamIndex, string codec)
    {
        _arguments.Append($"-c:{streamType.Identifier()}:{streamIndex} {codec} ");
    }
    
    /// <summary>
    /// Maps all streams from the given input file.
    /// </summary>
    /// <param name="inputId">The id of the input file.</param>
    public void Map(int inputId)
    {
        _arguments.Append($"-map {inputId} ");
    }
    
    /// <summary>
    /// Maps all streams of the given type from the given input file.
    /// </summary>
    /// <param name="inputId">The id of the input file.</param>
    /// <param name="streamType">The stream type to map.</param>
    /// <param name="optional">If set, the convert will not fail if no streams were found.</param>
    public void Map(int inputId, StreamType streamType, bool optional = false)
    {
        _arguments.Append($"-map {inputId}:{streamType.Identifier()}{(optional?"?":"")} ");
    }
    
    /// <summary>
    /// Maps the streams of the given type with the given index from the given input file.
    /// </summary>
    /// <param name="inputId">The id of the input file.</param>
    /// <param name="streamType">The stream type to map.</param>
    /// <param name="streamIndex">The index of the stream to map.</param>
    /// <param name="optional">If set, the convert will not fail if no streams were found.</param>
    public void Map(int inputId, StreamType streamType, int streamIndex, bool optional = false)
    {
        _arguments.Append($"-map {inputId}:{streamType.Identifier()}:{streamIndex}{(optional?"?":"")} ");
    }

    /// <summary>
    /// Maps the chapters from the given input stream.
    /// </summary>
    /// <param name="inputId">The id of the input file.</param>
    public void MapChapters(int inputId)
    {
        _arguments.Append($"-map_chapters {inputId} ");
    }
    
    /// <summary>
    /// Maps the metadata from the given input stream.
    /// </summary>
    /// <param name="inputId">The id of the input file.</param>
    public void MapMetadata(int inputId)
    {
        _arguments.Append($"-map_metadata {inputId} ");
    }

    /// <summary>
    /// Seeks to the given timestamp.
    /// </summary>
    /// <param name="value">The timestamp to seek to.</param>
    public void Seek(TimeSpan value)
    {
        _arguments.Append($"-ss {value} ");
    }
    
    /// <summary>
    /// Seeks to the given timestamp in the input file.
    /// </summary>
    /// <param name="value">The timestamp to seek to.</param>
    /// <param name="inputId">The id of the input file</param>
    public void Seek(TimeSpan value, int inputId)
    {
        _arguments.Append($"-ss {value} {inputId} ");
    }
    
    /// <summary>
    /// Sets the duration.
    /// </summary>
    /// <param name="value">The duration.</param>
    public void Duration(TimeSpan value)
    {
        _arguments.Append($"-t {value} ");
    }
    
    /// <summary>
    /// Sets the duration of the input file.
    /// </summary>
    /// <param name="value">The duration.</param>
    /// <param name="inputId">The id of the input file</param>
    public void Duration(TimeSpan value, int inputId)
    {
        _arguments.Append($"-t {value} {inputId} ");
    }
    
    /// <summary>
    /// Sets the filter to use for the given stream types.
    /// </summary>
    /// <param name="streamType">The stream type to set the filter.</param>
    /// <param name="filter">The filter definition.</param>
    public void Filter(StreamType streamType, string filter)
    {
        _arguments.Append($"-filter:{streamType.Identifier()} \"{filter}\" ");
    }

    /// <summary>
    /// Sets the analysis duration for the following input.
    /// </summary>
    /// <param name="value">The analysis duration.</param>
    public void AnalyzeDuration(long value)
    {
        _arguments.Append($"-analyzeduration {value} ");
    }
    
    /// <summary>
    /// Sets the probe size for the following input.
    /// </summary>
    /// <param name="value">The probe size.</param>
    public void ProbeSize(long value)
    {
        _arguments.Append($"-probesize {value} ");
    }
    
    /// <summary>
    /// Defines the output path. Must be the last argument.
    /// </summary>
    /// <param name="path">The file path.</param>
    public void Output(string path)
    {
        _arguments.Append($"\"{path}\" ");
    }
}