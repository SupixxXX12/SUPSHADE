using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SUPSHADE.App.Models;

namespace SUPSHADE.App.Services;

/// <summary>
/// Detects the local Steam installation and reads its library folders / app manifests
/// to build a list of installed games. No network access, no game-specific logic —
/// this is the same approach any legitimate Steam-aware launcher uses.
/// </summary>
public static class SteamLibraryService
{
    public static string? FindSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                return path.Replace('/', '\\');
        }
        catch
        {
            // Registry not available (e.g. non-Windows dev machine) - fall through to defaults.
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        return Directory.Exists(fallback) ? fallback : null;
    }

    public static List<GameEntry> GetInstalledGames()
    {
        var results = new List<GameEntry>();
        var steamPath = FindSteamPath();
        if (steamPath == null) return results;

        foreach (var libraryPath in GetLibraryFolders(steamPath))
        {
            var steamAppsDir = Path.Combine(libraryPath, "steamapps");
            if (!Directory.Exists(steamAppsDir)) continue;

            foreach (var manifest in Directory.EnumerateFiles(steamAppsDir, "appmanifest_*.acf"))
            {
                var entry = ParseManifest(manifest, steamAppsDir);
                if (entry != null) results.Add(entry);
            }
        }

        return results.OrderBy(g => g.Name).ToList();
    }

    private static List<string> GetLibraryFolders(string steamPath)
    {
        var folders = new List<string> { steamPath };
        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath)) return folders;

        var text = File.ReadAllText(vdfPath);
        // Matches "path"   "D:\\SteamLibrary" style entries in the VDF (KeyValues) format.
        foreach (Match m in Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\""))
        {
            var p = m.Groups[1].Value.Replace("\\\\", "\\");
            if (Directory.Exists(p) && !folders.Contains(p, StringComparer.OrdinalIgnoreCase))
                folders.Add(p);
        }
        return folders;
    }

    private static GameEntry? ParseManifest(string manifestPath, string steamAppsDir)
    {
        try
        {
            var text = File.ReadAllText(manifestPath);

            string? appId = Regex.Match(text, "\"appid\"\\s*\"(\\d+)\"", RegexOptions.IgnoreCase).Groups[1].Value;
            string name = Regex.Match(text, "\"name\"\\s*\"([^\"]+)\"").Groups[1].Value;
            string installDir = Regex.Match(text, "\"installdir\"\\s*\"([^\"]+)\"").Groups[1].Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(installDir))
                return null;

            var fullPath = Path.Combine(steamAppsDir, "common", installDir);
            if (!Directory.Exists(fullPath)) return null;

            // Best-effort guess at the main executable: largest .exe in the root folder.
            var exe = Directory.EnumerateFiles(fullPath, "*.exe", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => new FileInfo(f).Length)
                .FirstOrDefault();

            return new GameEntry
            {
                Name = name,
                InstallPath = fullPath,
                ExecutableName = exe != null ? Path.GetFileName(exe) : string.Empty,
                Source = GameSource.Steam,
                SteamAppId = string.IsNullOrWhiteSpace(appId) ? null : appId
            };
        }
        catch
        {
            return null;
        }
    }
}
