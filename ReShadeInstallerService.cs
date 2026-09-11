using System.IO;
using System.Text.Json;
using SUPSHADE.App.Models;

namespace SUPSHADE.App.Services;

/// <summary>
/// Installs/uninstalls "Shade-nexgen" (SUPSHADE's shader overlay) into a target game's
/// install folder. This wraps a real ReShade distribution rather than re-implementing
/// DirectX/Vulkan hooking from scratch - ReShade's overlay already supports a
/// configurable open-menu hotkey, draggable/dockable windows, live shader parameter
/// sliders and preset save/load, which covers the "draggable menu, mess with shaders"
/// requirement out of the box. Bundle the official ReShade DLLs + your own default
/// preset/shader pack under Assets/ReShade before publishing.
/// </summary>
public static class ReShadeInstallerService
{
    private const string ManifestFileName = ".supshade-install.json";

    private class InstallManifest
    {
        public List<string> CopiedFiles { get; set; } = new();
        public List<string> CreatedDirectories { get; set; } = new();
        public string InstalledVersion { get; set; } = "1.0.0";
        public DateTime InstalledAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Root folder containing the bundled ReShade runtime + default SUPSHADE shader pack.
    /// Populate this at build time: Assets/ReShade/dxgi.dll (or d3d11.dll, depending on the
    /// game's render API), ReShade.ini, ReShadePreset.ini, Shaders/, Textures/.
    /// </summary>
    private static string BundledRuntimeDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ReShade");

    public static bool IsInstalled(GameEntry game) =>
        File.Exists(Path.Combine(game.InstallPath, ManifestFileName));

    public static InstallResult Install(GameEntry game, string proxyDllName = "dxgi.dll")
    {
        if (!Directory.Exists(game.InstallPath))
            return InstallResult.Fail("Game install folder not found.");

        if (!Directory.Exists(BundledRuntimeDir))
            return InstallResult.Fail("Bundled shader runtime is missing from the SUPSHADE install. Reinstall SUPSHADE.");

        var manifest = new InstallManifest();

        try
        {
            foreach (var sourceFile in Directory.EnumerateFiles(BundledRuntimeDir, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(BundledRuntimeDir, sourceFile);
                var destination = Path.Combine(game.InstallPath, relative);

                var destDir = Path.GetDirectoryName(destination)!;
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    manifest.CreatedDirectories.Add(destDir);
                }

                // Never silently overwrite a file we didn't put there.
                if (File.Exists(destination) && !manifest.CopiedFiles.Contains(destination))
                {
                    var backup = destination + ".supshade-backup";
                    if (!File.Exists(backup))
                        File.Copy(destination, backup, overwrite: false);
                }

                File.Copy(sourceFile, destination, overwrite: true);
                manifest.CopiedFiles.Add(destination);
            }

            var manifestPath = Path.Combine(game.InstallPath, ManifestFileName);
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            game.IsShadeInstalled = true;
            return InstallResult.Ok();
        }
        catch (Exception ex)
        {
            return InstallResult.Fail($"Install failed: {ex.Message}");
        }
    }

    public static InstallResult Uninstall(GameEntry game)
    {
        var manifestPath = Path.Combine(game.InstallPath, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            game.IsShadeInstalled = false;
            return InstallResult.Ok(); // nothing to remove
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<InstallManifest>(File.ReadAllText(manifestPath)) ?? new InstallManifest();

            foreach (var file in manifest.CopiedFiles)
            {
                var backup = file + ".supshade-backup";
                if (File.Exists(backup))
                {
                    File.Copy(backup, file, overwrite: true);
                    File.Delete(backup);
                }
                else if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }

            foreach (var dir in manifest.CreatedDirectories.OrderByDescending(d => d.Length))
            {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }

            File.Delete(manifestPath);
            game.IsShadeInstalled = false;
            return InstallResult.Ok();
        }
        catch (Exception ex)
        {
            return InstallResult.Fail($"Uninstall failed: {ex.Message}");
        }
    }
}

public class InstallResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public static InstallResult Ok() => new() { Success = true };
    public static InstallResult Fail(string error) => new() { Success = false, Error = error };
}
