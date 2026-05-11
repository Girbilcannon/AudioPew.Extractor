namespace AudioPew.Extractor;

internal sealed record AudioPreset(
    string DisplayName,
    string Extension,
    string[] FfmpegArgs)
{
    public override string ToString() => DisplayName;

    public static readonly AudioPreset Wav24 = new(
        "WAV - Best for Editing 48kHz 24-bit",
        ".wav",
        ["-vn", "-acodec", "pcm_s24le", "-ar", "48000"]);

    public static readonly AudioPreset Wav16 = new(
        "WAV - Standard 48kHz 16-bit",
        ".wav",
        ["-vn", "-acodec", "pcm_s16le", "-ar", "48000"]);

    public static readonly AudioPreset M4A = new(
        "M4A - High Quality 320 kbps",
        ".m4a",
        ["-vn", "-c:a", "aac", "-b:a", "320k"]);

    public static readonly AudioPreset Mp3 = new(
        "MP3 - High Quality 320 kbps",
        ".mp3",
        ["-vn", "-c:a", "libmp3lame", "-b:a", "320k"]);

    public static readonly AudioPreset CopyOriginal = new(
        "Copy Original Audio - Fast/Lossless when possible",
        ".m4a",
        ["-vn", "-c:a", "copy"]);

    public static IReadOnlyList<AudioPreset> All { get; } =
    [
        Wav24,
        Wav16,
        M4A,
        Mp3,
        CopyOriginal
    ];
}
