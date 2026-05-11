using System.Diagnostics;

namespace AudioPew.Extractor;

internal sealed class FfmpegRunner
{
    public string FfmpegPath { get; }

    public bool IsAvailable => File.Exists(FfmpegPath);

    public FfmpegRunner()
    {
        FfmpegPath = Path.Combine(AppContext.BaseDirectory, "Tools", "ffmpeg.exe");
    }

    public string BuildOutputPath(string inputPath, AudioPreset preset, string outputFolder)
    {
        string safeFolder = string.IsNullOrWhiteSpace(outputFolder)
            ? Path.GetDirectoryName(inputPath) ?? AppContext.BaseDirectory
            : outputFolder;

        string baseName = Path.GetFileNameWithoutExtension(inputPath);
        string outputPath = Path.Combine(safeFolder, $"{baseName}_audio{preset.Extension}");

        int n = 2;
        while (File.Exists(outputPath))
        {
            outputPath = Path.Combine(safeFolder, $"{baseName}_audio_{n}{preset.Extension}");
            n++;
        }

        return outputPath;
    }

    public async Task<int> ExtractAsync(
        string inputPath,
        string outputPath,
        AudioPreset preset,
        Action<string> log,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
            throw new FileNotFoundException("ffmpeg.exe was not found.", FfmpegPath);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? AppContext.BaseDirectory);

        using var process = new Process();
        process.StartInfo.FileName = FfmpegPath;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;

        process.StartInfo.ArgumentList.Add("-y");
        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add(inputPath);

        foreach (string arg in preset.FfmpegArgs)
            process.StartInfo.ArgumentList.Add(arg);

        process.StartInfo.ArgumentList.Add(outputPath);

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                log(e.Data);
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                log(e.Data);
        };

        log($"> ffmpeg {string.Join(" ", process.StartInfo.ArgumentList.Select(QuoteIfNeeded))}");

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode;
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
