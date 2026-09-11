namespace SUPSHADE.App.Models;

public enum GameSource
{
    Steam,
    Manual
}

public class GameEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string ExecutableName { get; set; } = string.Empty;
    public GameSource Source { get; set; } = GameSource.Manual;
    public string? SteamAppId { get; set; }

    /// <summary>True once SUPSHADE (the shader overlay) has been installed into this game's folder.</summary>
    public bool IsShadeInstalled { get; set; }

    public string StatusText => IsShadeInstalled ? "SUPSHADE Installed" : "Not Installed";
}
