using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MkvRipper.Utils;

namespace MkvRipper.FFmpeg;

public class Engine
{
    public Engine(string? ffmpegPath = null)
    {
        Binary = ffmpegPath ?? "ffmpeg";
    }
    
    /// <summary>
    /// Gets the path to the ffmpeg binary.
    /// </summary>
    public string Binary { get; }
    
    /// <summary>
    /// Runs ffmpeg with the given arguments.
    /// </summary>
    /// <param name="builderCallback">The argument builder callback.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public async Task ConvertAsync(Action<CommandBuilder> builderCallback, CancellationToken cancellationToken)
    {
        var builder = new CommandBuilder();
        builderCallback(builder);
        await ConvertAsync(builder.Arguments, cancellationToken);
    }
    
    /// <summary>
    /// Runs ffmpeg with the given arguments.
    /// </summary>
    /// <param name="arguments">The ffmpeg arguments.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public async Task ConvertAsync(string arguments, CancellationToken cancellationToken)
    {
        var process = new Process();
        process.StartInfo = new ProcessStartInfo()
        {
            FileName = Binary,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        process.OutputDataReceived += (_, args) =>
        {
            Console.WriteLine(args.Data);
        };
        process.ErrorDataReceived += (_, args) =>
        {
            Console.Error.WriteLine(args.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new IOException($"FFmpeg returned {process.ExitCode}!");
        }
    }
    
    #region Probe

    /// <summary>
    /// Returns the metadata of the given file using ffmpeg.
    /// </summary>
    /// <param name="path">The input file.</param>
    /// <returns></returns>
    public async Task<InputMetadata> GetMetadataAsync(string path)
    {
        var arguments = $"-i \"{path}\"";
        
        var process = new Process();
        process.StartInfo = new ProcessStartInfo()
        {
            FileName = Binary,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        
        process.Start();
        await process.WaitForExitAsync();

        var intentReader = new IntentTextReader(process.StandardError, 2);
        
        // Read root block
        await foreach (var lineRoot in intentReader.ReadBlockAsync())
        {
            if (lineRoot.StartsWith("Input #"))
            {
                var input = new InputMetadata();
                var streams = new List<StreamMetadata>();
                await foreach (var lineInput in intentReader.BeginAndReadBlockAsync())
                {
                    if (lineInput.StartsWith("Metadata:"))
                    {
                        await foreach (var (name, value) in intentReader.BeginAndReadBlockPropertiesAsync())
                        {
                            switch (name)
                            {
                                case "title":
                                    input.Title = value;
                                    break;
                                case "encoder":
                                    input.Encoder = value;
                                    break;
                            }
                        }
                    }
                    else if (lineInput.StartsWith("Chapters:"))
                    {
                        var chapters = new List<ChapterMetadata>();
                        await foreach (var lineChapters in intentReader.BeginAndReadBlockAsync())
                        {
                            var chapter = CreateChapterMetadataByLine(lineChapters);
                            await foreach (var lineChapter in intentReader.BeginAndReadBlockAsync())
                            {
                                if (lineChapter.StartsWith("Metadata:"))
                                {
                                    await foreach (var (name, value) in intentReader.BeginAndReadBlockPropertiesAsync())
                                    {
                                        switch (name)
                                        {
                                            case "title":
                                                chapter.Title = value;
                                                break;
                                        }
                                    }
                                }
                            }
                            chapters.Add(chapter);
                        }

                        input.Chapters = chapters.ToArray();
                    }
                    else if (lineInput.StartsWith("Stream #"))
                    {
                        var stream = CreateStreamMetadataByLine(lineInput);
                        await foreach (var lineStream in intentReader.BeginAndReadBlockAsync())
                        {
                            if (lineStream.StartsWith("Metadata:"))
                            {
                                await foreach (var (name, value) in intentReader.BeginAndReadBlockPropertiesAsync())
                                {
                                    switch (name)
                                    {
                                        case "title":
                                            stream.Title = value;
                                            break;
                                    }
                                }
                            }
                        }
                        
                        streams.Add(stream);
                    }
                }

                input.Streams = streams.ToArray();
                return input;
            }
        }
        
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads the chapter metadata from the given line.
    /// </summary>
    /// <param name="line">The line that start with 'Chapter #'</param>
    /// <returns>Returns the stream metadata.</returns>
    private static ChapterMetadata CreateChapterMetadataByLine(string line)
    {
        var chapter = new ChapterMetadata();

        var indexInputId = line.IndexOf('#');
        if (indexInputId < 0) return chapter;
        
        var indexStreamId = line.IndexOf(':', indexInputId + 1);
        if (indexStreamId < 0) return chapter;

        chapter.InputId = ulong.Parse(line.Substring(indexInputId + 1, indexStreamId - indexInputId - 1));

        var indexEnd = line.IndexOf(':', indexStreamId + 1);
        if (indexEnd < 0) return chapter;
        
        chapter.Id = ulong.Parse(line.Substring(indexStreamId + 1, indexEnd - indexStreamId - 1));
        
        return chapter;
    }
    
    /// <summary>
    /// Reads the stream metadata from the given line.
    /// </summary>
    /// <param name="line">The line that start with 'Stream #'</param>
    /// <returns>Returns the stream metadata.</returns>
    private static StreamMetadata CreateStreamMetadataByLine(string line)
    {
        var stream = new StreamMetadata();

        var indexInputId = line.IndexOf('#');
        if (indexInputId < 0) return stream;
        
        var indexStreamId = line.IndexOf(':', indexInputId + 1);
        if (indexStreamId < 0) return stream;

        stream.InputId = ulong.Parse(line.Substring(indexInputId + 1, indexStreamId - indexInputId - 1));
        
        var indexLanguageStart = line.IndexOf('(', indexStreamId + 1);
        if (indexLanguageStart < 0) return stream;
        
        stream.Id = ulong.Parse(line.Substring(indexStreamId + 1, indexLanguageStart - indexStreamId - 1));
        
        var indexLanguageEnd = line.IndexOf(')', indexLanguageStart + 1);
        if (indexLanguageEnd < 0) return stream;

        stream.Language = line.Substring(indexLanguageStart + 1, indexLanguageEnd - indexLanguageStart - 1);
        
        var indexType = line.IndexOf(':', indexLanguageEnd + 1);
        if (indexType < 0) return stream;
            

        var indexEnd = line.IndexOf(':', indexType + 1);
        if (indexEnd < 0) return stream;

        var type = line.Substring(indexType + 1, indexEnd - indexType - 1).Trim();
        stream.Type = type switch
        {
            "Video" => StreamType.Video,
            "Audio" => StreamType.Audio,
            "Subtitle" => StreamType.Subtitle,
            "Data" => StreamType.Data,
            "Attachment" => StreamType.Attachment,
            _ => throw new ArgumentException($"Unknown stream type: '{type}'")
        };
        
        return stream;
    }
    
    #endregion Prope
}