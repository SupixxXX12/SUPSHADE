using System.IO;
using System.Text.Json;

namespace SUPSHADE.App.Services;

public class AppSettings
{
    public bool TermsAccepted { get; set; } = false;
    public int TermsAcceptedVersion { get; set; } = 0;
    public bool TelemetryEnabled { get; set; } = false;
    public bool DarkTheme { get; set; } = true;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public List<PersistedGame> ManualGames { get; set; } = new();
    public List<string> InstalledGameIds { get; set; } = new();
}

public class PersistedGame
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string ExecutableName { get; set; } = string.Empty;
}

public static class SettingsService
{
    private static readonly string AppDataDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SUPSHADE");

    private static readonly string SettingsPath = Path.Combine(AppDataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null) return loaded;
            }
        }
        catch
        {
            // Corrupt or unreadable settings -> fall back to defaults rather than crash.
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(AppDataDir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
