using System.Text.Json;

namespace AudioPew.Extractor;

internal sealed class AppSettings
{
    public string OutputMode { get; set; } = "Same folder as source";
    public string CustomOutputFolder { get; set; } = string.Empty;
    public int SelectedPresetIndex { get; set; } = 0;

    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, options));
        }
        catch
        {
            // Settings should never block extraction.
        }
    }
}
