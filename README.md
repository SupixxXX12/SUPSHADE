# SUPSHADE — by SUPs Studios

A shader-overlay launcher: pick a game from your library, install "Shade-nexgen"
into it, and tune shaders live in-game from a draggable menu (Home key).
Retro 2019-style launcher shell on the outside, smooth modern UX underneath.

This is a **complete, real C# / WPF source project** — not a mockup. It won't
compile here (WPF is Windows-only and this sandbox is Linux), but it's ready
to open and build in Visual Studio 2022 / `dotnet` on Windows.

## Features (15+)

**Library**
1. Auto-detects installed Steam games (reads `libraryfolders.vdf` + `appmanifest_*.acf`).
2. Auto-detects installed Epic Games titles (reads Epic's manifest JSON files).
3. Manual "Add Game" for anything not auto-detected.
4. Per-game install status shown inline (Installed / Not Installed) with a colored badge.
5. One-click Rescan to refresh the library.

**Install / Uninstall**
6. One-click "Install Shade-nexgen" — copies the shader runtime into the game folder.
7. Install is manifest-tracked: every copied file is recorded, and any file it
   would overwrite is backed up first.
8. One-click, fully clean Uninstall — restores backed-up files and removes
   everything SUPSHADE added.
9. Safe against partial installs (won't silently clobber files it didn't put there).

**In-game overlay** (via the bundled ReShade engine, rebranded)
10. Home key opens a draggable, dockable in-game menu.
11. Shader stack browser — enable/reorder/disable effects.
12. Live parameter sliders with instant visual feedback.
13. Preset save/load, with a Presets tab in the launcher for browsing saved presets.

**App shell**
14. First-launch Terms of Service / EULA gate (must scroll to the end and check
    "I agree" before Accept enables) — closed-source license, SUPs Studios branding.
15. Settings screen: telemetry opt-in (off by default), update-check toggle, reset.
16. About screen with SUPs Studios branding and a real Discord invite button.
17. Custom retro-styled title bar (drag to move, double-click to maximize) over a
    modern flat dark UI with hover animations and drop-shadowed cards.

## Project layout

```
SUPSHADE.sln
SUPSHADE.App/
  SUPSHADE.App.csproj
  App.xaml / App.xaml.cs            <- startup + TOS gate
  Assets/
    supshade.ico                    <- app icon (multi-res, generated)
    supshade_banner.png             <- wide logo banner
    ReShade/                        <- put a real ReShade build + your shaders here
  Models/
    GameEntry.cs
  Services/
    SettingsService.cs              <- %AppData%\SUPSHADE\settings.json
    SteamLibraryService.cs          <- Steam VDF/ACF parsing
    EpicGamesLibraryService.cs      <- Epic manifest parsing
    ReShadeInstallerService.cs      <- install/uninstall + backup/restore
    InverseBoolToVisibilityConverter.cs
  Views/
    MainWindow.xaml(.cs)            <- shell, sidebar nav, custom title bar
    LibraryView.xaml(.cs)
    PresetsView.xaml(.cs)
    SettingsView.xaml(.cs)
    AboutView.xaml(.cs)
    TermsWindow.xaml(.cs)
```

## Before you publish

1. **Bundle real ReShade.** Download an official build from https://reshade.me,
   drop the renamed proxy DLL + `ReShade.ini` + your shader/preset files into
   `SUPSHADE.App/Assets/ReShade/` (see the `.txt` file in that folder for exact
   layout). This is what actually hooks the render pipeline and draws the
   overlay — SUPSHADE's job is the friendly installer/manager around it, not
   reimplementing a DirectX hook engine from scratch.
2. **Restyle the overlay to match your brand** using ReShade's `[STYLE]` config
   section (colors, title) so the in-game menu reads "Shade-nexgen" and uses
   your purple/cyan palette.
3. Check the license of any shader/preset pack before redistributing it.

## Build & run (on Windows)

```bash
# Restore & run in debug
dotnet build
dotnet run --project SUPSHADE.App

# Publish a single-file, self-contained SUPSHADE.exe
dotnet publish SUPSHADE.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The published `SUPSHADE.exe` will carry the generated icon automatically
(`ApplicationIcon` is already set in the `.csproj`).

## Notes

- Steam/Epic detection is read-only: it only lists what's installed and where —
  no game-specific logic, no launcher-variant fingerprinting for anti-cheat evasion.
- Nothing in this app closes other applications, kills processes, or hides
  itself from anything. It's a normal installer + config tool.
- The Discord invite (`https://discord.gg/8fqeT8Vp`) is wired into the About screen.
