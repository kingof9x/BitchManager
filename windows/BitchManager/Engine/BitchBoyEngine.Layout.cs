using BitchManager.Models;
using BitchManager.Platform;

namespace BitchManager.Engine;

/// <summary>Themes, sound pack, window options and the module placement engine.</summary>
public sealed partial class BitchBoyEngine
{
    public const double ConsoleBaseWidth = 1566;

    // ---- Themes ----
    public List<CustomChassisTheme> CustomThemes { get; private set; } = [];
    public CustomChassisTheme ActiveCustomTheme { get => G(CustomChassisTheme.DefaultCustom()); set { S(value); Raise(nameof(Palette)); } }
    public ThemePalette Palette => ThemeColors.For(CurrentTheme, ActiveCustomTheme);

    // ---- Window ----
    public bool IsWindowAlwaysOnTop { get => G(false); set => S(value); }
    public double UiScale { get => G(1.0); set => S(value); }

    // ---- Modules ----
    public List<StudioModuleID> LeftWingModules { get; private set; } = [StudioModuleID.ShowControl, StudioModuleID.MemorySlots];
    public List<StudioModuleID> RightWingModules { get; private set; } = [StudioModuleID.MidiRouter, StudioModuleID.CustomFXStudio];
    public List<StudioModuleID> BottomRackModules { get; private set; } = [StudioModuleID.ReactiveKeyFX];
    public Dictionary<StudioModuleID, double> ModuleCustomHeights { get; } = new();
    public Dictionary<StudioModuleID, double> ModuleCustomWidths { get; } = new();
    public StudioModuleID? SpotlightModule { get => G<StudioModuleID?>(null); set => S(value); }
    public bool IsRearrangingModules { get => G(false); set => S(value); }

    /// <summary>Raised whenever modules move or resize, so the console can rebuild its wings.</summary>
    public int LayoutVersion { get => G(0); private set => S(value); }

    private void LoadLayoutAndSettings()
    {
        SoundPack = _settings.SoundPack;
        IsWindowAlwaysOnTop = _settings.AlwaysOnTop;
        UiScale = _settings.UiScale is >= 0.15 and <= 2.5 ? _settings.UiScale : 1.0;

        var themes = Storage.Load<List<CustomChassisTheme>>("themes.json");
        CustomThemes = themes is { Count: > 0 } ? themes : CustomChassisTheme.StarterThemes();
        ActiveCustomTheme = CustomThemes.FirstOrDefault(t => t.Id == _settings.ActiveCustomThemeId) ?? CustomThemes[0];
        CurrentTheme = _settings.Theme;

        LeftWingModules = _settings.LeftWingModules ?? [StudioModuleID.ShowControl, StudioModuleID.MemorySlots];
        RightWingModules = _settings.RightWingModules ?? [StudioModuleID.MidiRouter, StudioModuleID.CustomFXStudio];
        BottomRackModules = _settings.BottomRackModules ?? [StudioModuleID.ReactiveKeyFX];
        // Drop duplicates, then make sure all five modules are placed somewhere.
        var seen = new HashSet<StudioModuleID>();
        LeftWingModules = LeftWingModules.Where(seen.Add).ToList();
        RightWingModules = RightWingModules.Where(seen.Add).ToList();
        BottomRackModules = BottomRackModules.Where(seen.Add).ToList();
        foreach (var m in Enum.GetValues<StudioModuleID>().Where(m => !seen.Contains(m))) BottomRackModules.Add(m);

        foreach (var (k, v) in _settings.ModuleHeights)
            if (Raw.TryParse<StudioModuleID>(k, out var m)) ModuleCustomHeights[m] = v;
        foreach (var (k, v) in _settings.ModuleWidths)
            if (Raw.TryParse<StudioModuleID>(k, out var m)) ModuleCustomWidths[m] = v;
    }

    public void SaveSettings()
    {
        _settings.Theme = CurrentTheme;
        _settings.ActiveCustomThemeId = ActiveCustomTheme.Id;
        _settings.SoundPack = SoundPack;
        _settings.AlwaysOnTop = IsWindowAlwaysOnTop;
        _settings.UiScale = UiScale;
        _settings.ConnectFXEnabled = ConnectEffectEnabled;
        _settings.ConnectFXName = ConnectCustomFXName;
        _settings.LeftWingModules = LeftWingModules;
        _settings.RightWingModules = RightWingModules;
        _settings.BottomRackModules = BottomRackModules;
        _settings.ModuleHeights = ModuleCustomHeights.ToDictionary(p => p.Key.RawValue(), p => p.Value);
        _settings.ModuleWidths = ModuleCustomWidths.ToDictionary(p => p.Key.RawValue(), p => p.Value);
        Storage.Save("settings.json", _settings);
    }

    // ------------------------------------------------------------------------
    // Themes & sounds
    // ------------------------------------------------------------------------
    public void SetTheme(ChassisTheme theme)
    {
        CurrentTheme = theme;
        SaveSettings();
        Play(SoundEffect.PresetSwitch);
    }

    public void ApplyCustomTheme(CustomChassisTheme theme)
    {
        ActiveCustomTheme = theme;
        CurrentTheme = ChassisTheme.CustomUser;
        SaveSettings();
        Play(SoundEffect.PresetSwitch);
        Log($"🎨 Applied Custom Theme: '{theme.Name}'");
    }

    public void SaveCustomThemes() => Storage.Save("themes.json", CustomThemes);

    public void SetSoundPack(SoundPack pack)
    {
        SoundPack = pack;
        SaveSettings();
        SoundEffect.PresetSwitch.Play(pack);
    }

    public void ToggleWindowAlwaysOnTop()
    {
        IsWindowAlwaysOnTop = !IsWindowAlwaysOnTop;
        SaveSettings();
        Play(SoundEffect.KnobTick);
        Log(IsWindowAlwaysOnTop ? "📌 Window pinned Always On Top" : "📌 Window unpinned (normal level)");
    }

    public void SetManualZoom(double scale)
    {
        UiScale = Math.Clamp(Math.Round(scale * 20.0) / 20.0, 0.15, 2.2);
        SaveSettings();
        Log($"🔍 Window & UI scaled to {(int)Math.Round(UiScale * 100)}%");
    }

    // ------------------------------------------------------------------------
    // Module placement
    // ------------------------------------------------------------------------
    public double ModuleHeight(StudioModuleID m) => ModuleCustomHeights.TryGetValue(m, out var h) ? h : m.DefaultSlotHeight();
    public double ModuleWidth(StudioModuleID m) => ModuleCustomWidths.TryGetValue(m, out var w) ? w : 435;

    public ModuleZone CurrentZone(StudioModuleID m) =>
        LeftWingModules.Contains(m) ? ModuleZone.LeftWing :
        RightWingModules.Contains(m) ? ModuleZone.RightWing :
        BottomRackModules.Contains(m) ? ModuleZone.BottomRack : ModuleZone.LeftWing;

    private void LayoutChanged(bool save = true)
    {
        if (save) SaveSettings();
        LayoutVersion++;
    }

    public void MoveModuleToZone(StudioModuleID module, ModuleZone zone, int? atIndex = null)
    {
        if (SpotlightModule == module) SpotlightModule = null;
        LeftWingModules.Remove(module);
        RightWingModules.Remove(module);
        BottomRackModules.Remove(module);
        var list = zone switch
        {
            ModuleZone.RightWing => RightWingModules,
            ModuleZone.BottomRack => BottomRackModules,
            _ => LeftWingModules,
        };
        list.Insert(Math.Min(atIndex ?? list.Count, list.Count), module);
        LayoutChanged();
        Play(SoundEffect.KeyTap);
        Log($"📍 Moved '{module.ShortName()}' to {zone.RawValue()}");
    }

    public void MoveModuleWithinZone(StudioModuleID module, int direction)
    {
        var list = CurrentZone(module) switch
        {
            ModuleZone.RightWing => RightWingModules,
            ModuleZone.BottomRack => BottomRackModules,
            _ => LeftWingModules,
        };
        var i = list.IndexOf(module);
        var j = i + direction;
        if (i < 0 || j < 0 || j >= list.Count) return;
        (list[i], list[j]) = (list[j], list[i]);
        LayoutChanged();
        Play(SoundEffect.KeyTap);
    }

    public void CycleModuleSizePreset(StudioModuleID m)
    {
        var h = ModuleHeight(m);
        ModuleCustomHeights[m] = h < 240 ? 340 : h < 380 ? 480 : h < 520 ? 210 : 340;
        LayoutChanged();
        Play(SoundEffect.KnobTick);
    }

    public string ModuleSizePresetLabel(StudioModuleID m)
    {
        if (!ModuleCustomHeights.TryGetValue(m, out var h)) return "AUTO";
        return h <= 230 ? "COMPACT" : h <= 360 ? "STD" : "TALL";
    }

    public void SetModuleHeight(StudioModuleID m, double height, bool save = true)
    {
        ModuleCustomHeights[m] = Math.Clamp(height, 160, 650);
        LayoutChanged(save);
    }

    public void ResetModuleHeight(StudioModuleID m)
    {
        ModuleCustomHeights.Remove(m);
        LayoutChanged();
        Play(SoundEffect.PresetSwitch);
    }

    public void CycleModuleWidthPreset(StudioModuleID m)
    {
        var w = ModuleWidth(m);
        ModuleCustomWidths[m] = w < 360 ? 435 : w < 520 ? 620 : w < 750 ? 894 : 320;
        LayoutChanged();
        Play(SoundEffect.KnobTick);
    }

    public string ModuleWidthPresetLabel(StudioModuleID m)
    {
        if (!ModuleCustomWidths.TryGetValue(m, out var w)) return "HALF";
        return w <= 340 ? "COMPACT" : w <= 480 ? "HALF" : w <= 700 ? "WIDE" : "FULL";
    }

    public void ToggleSpotlightModule(StudioModuleID m)
    {
        Play(SoundEffect.PresetSwitch);
        SpotlightModule = SpotlightModule == m ? null : m;
    }

    public void ToggleRearrangeModulesMode()
    {
        Play(SoundEffect.KnobTick);
        IsRearrangingModules = !IsRearrangingModules;
        LayoutVersion++;
        Log(IsRearrangingModules ? "📱 Module Placement & Dynamic Resizing Mode: ACTIVE" : "✅ Module Layout Locked");
    }

    // ---- Floating windows (the UI layer opens/closes the actual windows) ----
    public HashSet<StudioModuleID> DetachedModules { get; } = [];
    public HashSet<StudioModuleID> PinnedModuleWindows { get; } = [];

    public void PopOutModuleToWindow(StudioModuleID m)
    {
        if (SpotlightModule == m) SpotlightModule = null;
        DetachedModules.Add(m);
        Raise(nameof(DetachedModules));
        LayoutVersion++;
        Play(SoundEffect.PresetSwitch);
        Log($"⧉ Popped out '{m.RawValue()}' into standalone window");
    }

    public void DockModuleBackFromWindow(StudioModuleID m)
    {
        if (!DetachedModules.Remove(m)) return;
        Raise(nameof(DetachedModules));
        LayoutVersion++;
        Play(SoundEffect.KnobTick);
        Log($"↙ Docked '{m.RawValue()}' back into main console");
    }

    public bool IsModuleWindowTopMost(StudioModuleID m) => PinnedModuleWindows.Contains(m) || IsWindowAlwaysOnTop;

    public void ToggleModuleWindowTopMost(StudioModuleID m)
    {
        if (!PinnedModuleWindows.Remove(m)) PinnedModuleWindows.Add(m);
        Raise(nameof(PinnedModuleWindows));
        Play(SoundEffect.KnobTick);
    }

    public void SwapModules(StudioModuleID first, StudioModuleID second)
    {
        if (first == second) return;
        var z1 = CurrentZone(first);
        var z2 = CurrentZone(second);
        if (z1 == z2)
        {
            var list = z1 switch { ModuleZone.RightWing => RightWingModules, ModuleZone.BottomRack => BottomRackModules, _ => LeftWingModules };
            int i = list.IndexOf(first), j = list.IndexOf(second);
            if (i < 0 || j < 0) return;
            (list[i], list[j]) = (list[j], list[i]);
            LayoutChanged();
            Play(SoundEffect.KeyTap);
        }
        else
        {
            var list = z2 switch { ModuleZone.RightWing => RightWingModules, ModuleZone.BottomRack => BottomRackModules, _ => LeftWingModules };
            MoveModuleToZone(first, z2, list.IndexOf(second));
        }
    }

    public void SetModuleWidth(StudioModuleID m, double width)
    {
        ModuleCustomWidths[m] = Math.Clamp(width, 260, 894);
        LayoutChanged();
    }

    // ---- Console dimensions (same formulas as the Mac app) ----
    public double ChassisContentHeight
    {
        get
        {
            double Wing(List<StudioModuleID> mods) => mods.Count == 0 && !IsRearrangingModules ? 0
                : mods.Sum(m => ModuleHeight(m) + 10 + (IsRearrangingModules ? 22 : 0)) + (IsRearrangingModules ? 60 : 0);
            var rack = BottomRackModules.Count == 0 && !IsRearrangingModules ? 0
                : (BottomRackModules.Count == 0 ? 240 : BottomRackModules.Max(ModuleHeight)) + 52 + (IsRearrangingModules ? 22 : 0);
            return Math.Max(530, Math.Max(530 + 12 + rack, Math.Max(Wing(LeftWingModules), Wing(RightWingModules))));
        }
    }

    public void ResetModuleLayout()
    {
        Play(SoundEffect.PresetSwitch);
        LeftWingModules = [StudioModuleID.ShowControl, StudioModuleID.MemorySlots];
        RightWingModules = [StudioModuleID.MidiRouter, StudioModuleID.CustomFXStudio];
        BottomRackModules = [StudioModuleID.ReactiveKeyFX];
        ModuleCustomHeights.Clear();
        ModuleCustomWidths.Clear();
        LayoutChanged();
        Log("🔄 Reset Module Layout to Default Positions");
    }
}
