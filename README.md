# BitchManager — Block System Hardware // LED & Action Engine

**BitchManager** is the hardware companion, lighting studio, and MIDI/OSC/Macro router for the **BitchBoy V1** 69-control hardware console.

---

## 🪟 Windows .exe Build & GitHub Actions Workflow

A dedicated GitHub Actions CI/CD workflow is configured in [`.github/workflows/build-windows.yml`](.github/workflows/build-windows.yml) to automatically compile and package Windows executables:

### Generated Windows Executables:
1. **NSIS Installer (`BitchManager Setup 1.0.0.exe`)**: Standard Windows installer with start menu & desktop shortcuts and custom install path selection.
2. **Portable Executable (`BitchManager 1.0.0.exe`)**: Single-file standalone executable that runs directly without installation.

### How to Trigger the Windows Build:
- **Automatic on Git Push / Tag**: Pushing to `main` or creating a release tag like `v1.0.0` triggers the workflow.
- **Manual Trigger**:
  1. Go to your repository on GitHub.
  2. Click the **Actions** tab.
  3. Select **"Build Windows Executable (.exe)"** from the left sidebar.
  4. Click **Run workflow** -> Select branch (`main`) -> Click the green **Run workflow** button.
  5. Once finished, download the **`BitchManager-Windows-x64`** artifact directly from the workflow summary page!
  6. If a tag is pushed, it automatically creates a GitHub Release with the `.exe` files attached.

---

## ✨ Key Features
- **Full 69-Control Hardware Console**:
  - **49 RGB/Multi-Velocity LED Keys**: 8×4 Top Grid (32 keys), 1×8 Middle Strip (8 keys), and 3×3 Right Keypad (9 keys).
  - **20 Continuous Controllers**: 8 Vertical Faders, 4 Horizontal Crossfaders, and 8 Rotary Encoders.
- **Reactive Mechanical Keyboard Key-Press FX Engine**:
  - Live interactive 2D spatial effects triggered by physical BitchBoy key presses, on-screen clicks, or physical keyboard typing: **Ripple Ring**, **Laser Cross**, **Supernova**, **Heat Trail**, **Meteor Drop**, and **Arc Lightning** with adjustable wave speed, dynamic Prism Rainbow mode, and neon color swatches.
- **Custom Lighting FX Studio**:
  - Multi-frame keyframe sequencer, direct 49-key chassis painter, and procedural Wave Synthesizer with 15+ built-in hardware light shows.
- **8-Slot Customizable Memory Bank (`SLOT A` – `SLOT H`)**:
  - Instant bank switching (`BANK A–D` / `BANK E–H`), custom slot icons, color badges, 1-click Save, Clone, and JSON Clipboard Export/Import.
- **MIDI Output Router & Patchbay**:
  - Real Web MIDI API integration, virtual Web synth playback, multi-DAW routing, channel remapping, semitone transpose, and velocity curves.
- **Macro Action Engine**:
  - Trigger multi-step QWERTY keystroke sequences, OSC packets, or AppleScript automations from any hardware key.
