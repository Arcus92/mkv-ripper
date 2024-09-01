using MkvRipper.Utils;
using Tesseract;

namespace MkvRipper.Subtitles.Utils;

public class TesseractManager
{
    /// <summary>
    /// Gets the shared instance
    /// </summary>
    public static TesseractManager Shared { get; } = new();

    /// <summary>
    /// Gets the path to the trained datasets.
    /// </summary>
    public string DataPath { get; set; } = "./tessdata";
    
    /// <summary>
    /// Prepares and returns the tessaract engine for the given language.
    /// </summary>
    /// <param name="language"></param>
    /// <returns></returns>
    public async Task<TesseractEngine> GetEngineAsync(string language)
    {
        await DownloadTrainedDataIfNeeded(language);
        return new TesseractEngine("./tessdata", language, EngineMode.Default);
    }
    
    /// <summary>
    /// The lock used for downloading.
    /// </summary>
    private readonly SemaphoreSlim _downloadLock = new(1, 1);
    
    private async Task DownloadTrainedDataIfNeeded(string language)
    {
        var dataPath = Path.Combine(DataPath, $"{language}.traineddata");
        if (!File.Exists(dataPath))
        {
            await _downloadLock.WaitAsync();
            try
            {
                // Check if it was downloaded in the meantime...
                if (File.Exists(dataPath)) return;

                var dataUrl = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata";
                await FileHandler.HandleAsync(dataPath, async path =>
                {
                    Console.WriteLine($"Couldn't find trained data for '{language}'. Start download from GitHub...");
                    Directory.CreateDirectory(DataPath);
                    
                    var client = new HttpClient();
                    await using var stream = await client.GetStreamAsync(dataUrl);
                    await using var output = new FileStream(path, FileMode.Create);
                    await stream.CopyToAsync(output);
                    Console.WriteLine($"Trained data for '{language}' was downloaded!");
                });
                
            }
            finally
            {
                _downloadLock.Release();
            }
        }
    }
}