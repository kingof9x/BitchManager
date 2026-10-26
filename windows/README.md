# BitchManager for Windows

A native Windows port of the macOS SwiftUI BitchManager app for the BitchBoy V1 69-control MIDI console.
It is written in C# / WPF on .NET 8, and the whole UI is built in code so it mirrors the SwiftUI view tree closely.

The previous Electron attempt lives in `archive/electron-gemini/` for reference only.

## Getting the app

Every push builds `BitchManager.exe` on GitHub Actions (workflow **Windows build**). Open the run and download the
`BitchManager-win-x64` artifact. It is a single self-contained exe, so .NET does not need to be installed.

To build locally: install the .NET 8 SDK, then run

```
dotnet publish windows/BitchManager -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Differences from the Mac app

- **Virtual MIDI ports.** macOS creates virtual ports itself; Windows cannot. Install
  [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html) and create two ports named exactly
  `BitchBoy Virtual Out` and `BitchBoy Virtual In`. The router uses them as the virtual bus.
- **AppleScript actions** run as PowerShell (`powershell.exe -Command ...`).
- **QWERTY macros** send keys with `SendInput`. The Mac `cmd` modifier maps to `Ctrl`.
- **Sounds** use the built-in Windows sounds in `C:\Windows\Media`.
- **Saved data** (slots, custom FX, themes, settings) lives in `%APPDATA%\BitchManager` as JSON.

## Not ported yet

- Custom sound studio / uploading your own sounds
- Theme studio editor (built-in themes work)
- GitHub auto-updater
- Mac-only buttons (Audio MIDI Setup, MIDI Studio icon)
