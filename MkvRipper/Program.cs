using MkvRipper.FFmpeg;
using MkvRipper.FFmpeg.Utils;
using MkvRipper.MediaFiles;
using MkvRipper.Tools;
using MkvRipper.Utils;

if (args.Length != 2)
{
    Console.WriteLine("Please provide the input and output directory as arguments!");
    return;
}

var inputDirectory = args[0];
var outputDirectory = args[1];
Directory.CreateDirectory(outputDirectory);
var mediaOutputDirectory = new MediaOutputDirectory(outputDirectory);

while (true)
{
    Console.WriteLine();
    Console.WriteLine($"Input: {inputDirectory}");
    Console.WriteLine($"Output: {outputDirectory}");
    Console.WriteLine("-------------------------------------------------------------");
    
    var sources = MediaSource.FromDirectory(inputDirectory).ToList();
    var outputs = mediaOutputDirectory.EnumerateOutputs().OrderBy(o => o.FileCreationTime).ToList();
    
    Console.Write("<C> Convert, <R> Rename output, <S> Split input, <F> Fix subtitles, <E> Exit - Input: ");
    var input = Console.ReadLine();
    if (input is null) continue;
    
    // Convert
    if (input.Equals("c", StringComparison.OrdinalIgnoreCase))
    {
        var total = sources.Count;
        var index = 0;
        foreach (var source in sources)
        {
            index++;
            var output = new MediaOutput(mediaOutputDirectory, source.BaseName);
            var tasks = new List<Task>();
            await foreach (var export in source.GetExportMp4TasksAsync())
            {
                if (export.Exists(output)) continue;
                var task = export.ExportAsync(output);
                //await task;
                tasks.Add(task);
            }

            if (tasks.Count == 0)
                continue;
            
            Console.WriteLine($"---- [{index}/{total}] {source.BaseName} ----");
            await Task.WhenAll(tasks);
            source.Unload();
        }
    }
    // Rename
    else if (input.Equals("r", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Outputs:");
        for (var i = 0; i < outputs.Count; i++)
        {
            var entry = outputs[i];
            Console.WriteLine($"[{i + 1}] {entry.BaseName} ({entry.FileCount} file(s) - {FileHandler.FileSizeToText(entry.FileSize)})");
        }
        Console.WriteLine("<all> Batch rename all files");
        Console.Write("Select the output to rename: ");
        input = Console.ReadLine();
        if (input is null) continue;

        if (input.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            // Renaming all files
            Console.Write("Rename all files. Enter base name (like 'Bonus %'): ");
            var baseName = Console.ReadLine();
            if (string.IsNullOrEmpty(baseName))
                continue;

            if (baseName.IndexOf('%') < 0)
            {
                Console.WriteLine("Base name must contain '%'!");
                continue;
            }

            Console.Write("Enter start index: ");
            var startText = Console.ReadLine();
            if (!int.TryParse(startText, out var start))
            {
                Console.WriteLine("Invalid input!");
                continue;
            }

            var counter = start;
            foreach (var output in outputs)
            {
                var name = baseName.Replace("%", counter.ToString());
                name = FileHandler.RemoveInvalidCharsFromFilename(name).Trim();
                output.Rename(name);
                counter++;
            }
        }
        else
        {
            // Renaming single output
            if (!int.TryParse(input, out var index) || index <= 0 || index > outputs.Count) continue;
            var output = outputs[index - 1];

            Console.Write($"Renaming '{output.BaseName}' to: ");
            var name = Console.ReadLine();
            if (string.IsNullOrEmpty(name))
                continue;
            name = FileHandler.RemoveInvalidCharsFromFilename(name).Trim();
            output.Rename(name);
        }
    }
    // Split
    else if (input.Equals("s", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Inputs:");
        for (var i = 0; i < sources.Count; i++)
        {
            var entry = sources[i];
            Console.WriteLine($"[{i + 1}] {entry.BaseName} ({FileHandler.FileSizeToText(entry.FileSize)})");
        }
        
        Console.Write("Select the input to split: ");
        input = Console.ReadLine();
        if (input is null) continue;
        
        // Renaming output
        if (!int.TryParse(input, out var index) || index <= 0 || index > sources.Count) continue;
        var source = sources[index - 1];
        
        
        // Load the chapters to split
        var ffmpeg = new Engine();
        var metadata = await ffmpeg.GetMetadataAsync(source.FileName);

        for (var i = 0; i < metadata.Chapters.Length; i++)
        {
            var chapter = metadata.Chapters[i];
            var duration = chapter.End - chapter.Start;
            Console.WriteLine($"[{i + 1}] {chapter.Start:hh':'mm':'ss} - {chapter.End:hh':'mm':'ss}: {chapter.Title} ({duration:hh':'mm':'ss})");
        }

        Console.Write("Select chapter(s) to split: ");
        input = Console.ReadLine();
        if (input is null) continue;
        
        var inputChapters = input.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        var splits = new List<(TimeSpan, TimeSpan)>();
        var lastSplit = TimeSpan.Zero;
        var invalid = false;
        foreach (var inputChapter in inputChapters)
        {
            if (!int.TryParse(inputChapter, out index) || index <= 0 || index > metadata.Chapters.Length)
            {
                invalid = true;
                continue;
            }
            var chapter = metadata.Chapters[index - 1];
            splits.Add((lastSplit, chapter.Start));
            lastSplit = chapter.Start;
        }
        if (invalid) continue;
        splits.Add((lastSplit, metadata.Duration));
        
        // Splits the segments using FFmpeg
        var counter = 0;
        var baseName = Path.GetFileNameWithoutExtension(source.FileName);
        foreach (var (start, end) in splits)
        {
            counter++;
            var fileName = Path.Combine(inputDirectory, $"{baseName}_{counter}.mkv");
            var fileNameMeta = Path.Combine(inputDirectory, $"{baseName}_{counter}.ffmeta");
            
            // Rebuild the metadata by adjusting the chapter names.
            var chapterIndex = 0;
            var metaFile = new MetadataFile
            {
                Chapters = metadata.Chapters
                    .Where(c => c.Start >= start && c.End <= end)
                    .Select(c => new ChapterMetadata() {
                    Id = c.Id,
                    InputId = c.InputId,
                    Title = $"Chapter {++chapterIndex:00}",
                    Start = c.Start,
                    End = c.End
                }).ToArray()
            };
            await metaFile.Save(fileNameMeta);
            
            Console.WriteLine($"Split {start:hh':'mm':'ss} - {end:hh':'mm':'ss}: {Path.GetFileName(fileName)}");
            
            // Run FFmpeg to split a segment from the source file without transcoding.
            await FileHandler.HandleAsync(fileName, async path =>
            {
                await ffmpeg.ConvertAsync((b) =>
                {
                    var inputVideo = b.Input(source.FileName);
                    var inputMeta = b.Input(fileNameMeta);
                    b.MapChapters(inputMeta);
                    b.Map(inputVideo, StreamType.Video);
                    b.Map(inputVideo, StreamType.Audio);
                    b.Map(inputVideo, StreamType.Subtitle);
                    b.Codec("copy");
                    b.Format("matroska");
                    b.Seek(start);
                    b.Duration(end - start);
                    
                    b.OverwriteOutput();
                    b.Output(path);
                }, onUpdate: (update) =>
                {
                    Console.WriteLine(update.GetProgressText());
                });
            });
            
            File.Delete(fileNameMeta);
        }
    }
    // Fix subtitle
    else if (input.Equals("f", StringComparison.OrdinalIgnoreCase))
    {
        var tool = new SubtitleFixer();
        await tool.ExecuteAsync(mediaOutputDirectory);
    }
    // Exit
    else if (input.Equals("e", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
}
