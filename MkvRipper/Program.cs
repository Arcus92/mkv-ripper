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
    var sources = MediaSource.FromDirectory(inputDirectory).ToList();
    var outputs = mediaOutputDirectory.EnumerateOutputs().OrderBy(o => o.FileCreationTime).ToList();
    
    Console.WriteLine("Input:");
    foreach (var source in sources)
    {
        Console.WriteLine($" - {source.BaseName} ({FileHandler.FileSizeToText(source.FileSize)})");
    }
    Console.WriteLine("Output:");
    for (var i = 0; i < outputs.Count; i++)
    {
        var output = outputs[i];
        Console.WriteLine($"[{i + 1}] {output.BaseName} ({output.FileCount} file(s) - {FileHandler.FileSizeToText(output.FileSize)})");
    }

    Console.Write("<number> Rename entry, <R> Rename all files, <F> Fix subtitle names, <C> Convert all, <F> Fix subtitle names, <E> Exit - Input: ");
    var input = Console.ReadLine();
    if (input is null) continue;
    
    // Renaming output
    if (int.TryParse(input, out var index) && index > 0 && index <= outputs.Count)
    {
        var output = outputs[index - 1];

        Console.Write($"Renaming '{output.BaseName}' to: ");
        var name = Console.ReadLine();
        if (string.IsNullOrEmpty(name))
            continue;
        name = name.Trim();
        output.Rename(name);
        
    }

    // Convert
    if (input.Equals("c", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var source in sources)
        {
            var output = new MediaOutput(mediaOutputDirectory, source.BaseName);
            var tasks = new List<Task>();
            await foreach (var export in source.GetExportTasksAsync())
            {
                if (export.Exists(output)) continue;
                tasks.Add(export.ExportAsync(output));
            }

            await Task.WhenAll(tasks);
            source.Unload();
        }
    }
    
    // Fix subtitle
    if (input.Equals("f", StringComparison.OrdinalIgnoreCase))
    {
        var tool = new SubtitleFixer();
        foreach (var output in outputs)
        {
            tool.AutoRenameSubtitle(output);
        }
    }
    
    // Batch renaming
    if (input.Equals("r", StringComparison.OrdinalIgnoreCase))
    {
        Console.Write("Rename all files. Enter base name (like 'Bonus %'): ");
        var baseName = Console.ReadLine();
        if (string.IsNullOrEmpty(baseName))
        {
            continue;
        }

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
            output.Rename(name);
            counter++;
        }
    }
    
    // Exit
    if (input.Equals("e", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
}


