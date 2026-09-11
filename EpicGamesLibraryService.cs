using System.IO;
using System.Text.Json;
using SUPSHADE.App.Models;

namespace SUPSHADE.App.Services;

/// <summary>
/// Optional second library source: Epic Games Launcher stores per-game install manifests
/// as JSON under ProgramData. Same generic "read what's installed" approach as Steam -
/// no per-title special casing. Additional launchers (GOG, Rockstar, Battle.net, etc.)
/// can be added the same way if you want a fully unified library later.
/// </summary>
public static class EpicGamesLibraryService
{
    private static readonly string ManifestsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Epic", "EpicGamesLauncher", "Data", "Manifests");

    public static List<GameEntry> GetInstalledGames()
    {
        var results = new List<GameEntry>();
        if (!Directory.Exists(ManifestsDir)) return results;

        foreach (var file in Directory.EnumerateFiles(ManifestsDir, "*.item"))
        {
            try
            {
                var json = File.ReadAllText(file);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string name = root.TryGetProperty("DisplayName", out var n) ? n.GetString() ?? "" : "";
                string installLocation = root.TryGetProperty("InstallLocation", out var loc) ? loc.GetString() ?? "" : "";
                string launchExe = root.TryGetProperty("LaunchExecutable", out var exe) ? exe.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(name) || !Directory.Exists(installLocation))
                    continue;

                results.Add(new GameEntry
                {
                    Name = name,
                    InstallPath = installLocation,
                    ExecutableName = launchExe,
                    Source = GameSource.Manual // shown as "Epic" in UI via separate label if desired
                });
            }
            catch
            {
                // Skip unreadable/partial manifests.
            }
        }

        return results;
    }
}
