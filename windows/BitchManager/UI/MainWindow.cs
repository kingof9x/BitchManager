using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.Platform;
using static BitchManager.UI.Ui;

namespace BitchManager.UI;

/// <summary>Port of ContentView's main console window.</summary>
public sealed class MainWindow : Window
{
    private static readonly Color WindowBg = C(0.02, 0.05, 0.04);
    private readonly BitchBoyEngine _e;
    private readonly Grid _console = new() { Width = BitchBoyEngine.ConsoleBaseWidth };
    private readonly Viewbox _viewbox = new() { Stretch = Stretch.Uniform };
    private readonly Dictionary<StudioModuleID, Window> _moduleWindows = new();
    private TextBlock? _scaleLabel;

    public MainWindow(BitchBoyEngine engine)
    {
        _e = engine;
        Title = "BitchManager";
        Background = B(WindowBg);
        Icon = GlowImage.LoadResource("app.ico");
        MinWidth = 400;
        MinHeight = 260;
        Width = 1566 * engine.UiScale * 0.85;
        Height = 1000 * engine.UiScale * 0.85;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UseLayoutRounding = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

        var main = V(12, BuildTopNav(), BuildFaceplate());
        main.Margin = new Thickness(16, 14, 16, 14);
        main.VerticalAlignment = VerticalAlignment.Top;
        var mainHost = new Border { Child = main };

        _console.Children.Add(mainHost);
        _console.Children.Add(BuildBanner());
        _console.Children.Add(Dyn(_e, () => BuildSpotlight() ?? new Border(), nameof(BitchBoyEngine.SpotlightModule)));
        mainHost.Observe(_e, () =>
        {
            var overlay = _e.SpotlightModule != null;
            mainHost.Effect = overlay ? new BlurEffect { Radius = 14 } : null;
            mainHost.RenderTransformOrigin = new Point(0.5, 0.5);
            mainHost.RenderTransform = overlay ? new ScaleTransform(0.97, 0.97) : Transform.Identity;
        }, nameof(BitchBoyEngine.SpotlightModule));

        _viewbox.Child = _console;
        Content = _viewbox;

        this.Observe(_e, () => Topmost = _e.IsWindowAlwaysOnTop, nameof(BitchBoyEngine.IsWindowAlwaysOnTop));
        this.Observe(_e, SyncModuleWindows, nameof(BitchBoyEngine.DetachedModules), nameof(BitchBoyEngine.PinnedModuleWindows), nameof(BitchBoyEngine.IsWindowAlwaysOnTop));
        SizeChanged += (_, _) => UpdateScaleLabel();
        Closed += (_, _) => { foreach (var w in _moduleWindows.Values.ToList()) w.Close(); };
    }

    private double FitScale => _console.ActualWidth > 0 ? _viewbox.ActualWidth / _console.ActualWidth : 1.0;

    private void UpdateScaleLabel()
    {
        if (_scaleLabel != null) _scaleLabel.Text = $"SCALE {(int)Math.Round(FitScale * 100)}%";
    }

    private void ZoomTo(double scale)
    {
        _e.SetManualZoom(scale);
        var chromeW = ActualWidth - _viewbox.ActualWidth;
        var chromeH = ActualHeight - _viewbox.ActualHeight;
        var s = _e.UiScale;
        var wa = SystemParameters.WorkArea;
        Width = Math.Min(wa.Width, _console.ActualWidth * s + Math.Max(0, chromeW));
        Height = Math.Min(wa.Height, _console.ActualHeight * s + Math.Max(0, chromeH));
    }

    private Color Accent => _e.Palette.Accent;

    // ------------------------------------------------------------------------
    // Top navigation bar
    // ------------------------------------------------------------------------
    private UIElement BuildTopNav()
    {
        var themeProps = new[] { nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme) };

        // Badge + glowing title
        var badge = new Image { Source = GlowImage.LoadResource("badge.png"), Width = 36, Height = 36 };
        var badgeGlow = new DropShadowEffect { ShadowDepth = 0, BlurRadius = 10, Opacity = 0.65 };
        badge.Effect = badgeGlow;
        var title = T("BITCHMANAGER", 15, White);
        var titleGlow = new DropShadowEffect { ShadowDepth = 0, BlurRadius = 8, Opacity = 0.95 };
        title.Effect = titleGlow;
        var subtitle = T("BLOCK SYSTEM HARDWARE // LED & ACTION ENGINE", 7.5, W(0.6), FontWeights.Bold);
        var titleBlock = H(10, badge, V(1, title, subtitle));
        titleBlock.Observe(_e, () =>
        {
            badgeGlow.Color = Accent;
            titleGlow.Color = Accent;
            title.Foreground = Gradient(White, Accent.Op(0.96), false);
            subtitle.Foreground = B(_e.Palette.HeaderText.Op(0.75));
        }, themeProps);

        // Connection status pill (menu)
        var statusDot = Dot(8, Red);
        var statusTitle = T("", 8, White);
        var statusSub = T("", 6.5, W(0.6), FontWeights.Bold);
        var statusPill = MenuBtn(H(8, statusDot, V(1, statusTitle, statusSub)), StatusMenu, Black.Op(0.5), 8, 10, 5, W(1, 0.12));
        statusPill.Observe(_e, () =>
        {
            statusDot.Fill = B(_e.IsConnected ? C(0.2, 0.95, 0.45) : C(0.98, 0.25, 0.25));
            statusDot.Effect = new DropShadowEffect { ShadowDepth = 0, BlurRadius = 6, Color = _e.IsConnected ? Green : Red };
            statusTitle.Text = _e.IsConnected ? "ONLINE // CONNECTED" : "OFFLINE // DISCONNECTED";
            statusTitle.Foreground = B(_e.IsConnected ? C(0.3, 0.98, 0.55) : C(1.0, 0.45, 0.45));
            statusSub.Text = _e.IsConnected ? $"{_e.DeviceName} // Ready" : "Unplugged // Waiting for BitchBoy...";
        }, nameof(BitchBoyEngine.IsConnected), nameof(BitchBoyEngine.DeviceName));

        var pink = C(1.0, 0.18, 0.57);
        var love = Box(T("❤️ Made with love - Taylor Fossen", 7.5, pink.Op(0.92)), Black.Op(0.48), 8, 8, 4, pink.Op(0.45));

        // Top most, scale
        var topMostIcon = Icon("pin", 8, White);
        var topMostText = T("TOP MOST", 7, White);
        var topMost = Btn(H(4, topMostIcon, topMostText), _e.ToggleWindowAlwaysOnTop, W(1, 0.08), 5, 7, 4);
        topMost.Observe(_e, () =>
        {
            var on = _e.IsWindowAlwaysOnTop;
            topMostText.Text = on ? "PINNED TOP" : "TOP MOST";
            topMostText.Foreground = B(on ? Black : White);
            topMostIcon.Foreground = B(on ? Black : White);
            topMost.Background = B(on ? Accent : W(1, 0.08));
        }, nameof(BitchBoyEngine.IsWindowAlwaysOnTop), nameof(BitchBoyEngine.CurrentTheme));

        _scaleLabel = T("SCALE 100%", 7, White);
        var scaleMenu = MenuBtn(_scaleLabel, () =>
            new[] { new MenuEntry("AUTO-FIT TO WINDOW", () => _e.Log("📐 UI Scaling set to Auto-Fit Window"), true), MenuEntry.Sep }
                .Concat(new[] { 15, 25, 35, 50, 65, 75, 85, 100, 115, 125, 140, 160, 180, 200 }
                    .Select(p => new MenuEntry($"ZOOM {p}%", () => ZoomTo(p / 100.0), Math.Abs(FitScale * 100 - p) < 3))),
            Accent.Op(0.22), 4, 6, 3);
        var scale = Box(H(3,
            TextBtn("−", 9, White, W(1, 0.1), () => ZoomTo(FitScale - 0.1), 4, 5, 2),
            scaleMenu,
            TextBtn("+", 9, White, W(1, 0.1), () => ZoomTo(FitScale + 0.1), 4, 5, 2)), Black.Op(0.45), 6, 3, 3);

        // Slot menu
        var slotIcon = Icon("music.mic", 9, White);
        var slotLetter = T("A", 8, White);
        var slotMenu = MenuBtn(H(4, slotIcon, slotLetter, Icon("chevron.down", 7, W(0.7))), SlotMenu, W(1, 0.08), 5, 7, 4);
        slotMenu.Observe(_e, () =>
        {
            slotIcon.Text = Icons.For(_e.CurrentSlot.IconName);
            slotIcon.Foreground = B(Leds.For(_e, _e.CurrentSlot.AccentVelocity));
            slotLetter.Text = $"SLOT {BitchBoyEngine.SlotLetters[_e.ActiveSlot % 8]}";
        }, nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.Slots));

        // Theme menu
        var themeName = T("", 7, White);
        var themeMenu = MenuBtn(H(4, T("🎨", 8, White), themeName), ThemeMenu, W(1, 0.08), 5, 7, 4);
        themeMenu.Observe(_e, () => themeName.Text = _e.CurrentTheme == ChassisTheme.CustomUser ? _e.ActiveCustomTheme.Name : _e.CurrentTheme.RawValue(), themeProps);

        // Sound menu
        var soundIcon = Icon("speaker.wave.2.fill", 8, White);
        var soundName = T("", 7, White);
        var soundMenu = MenuBtn(H(4, soundIcon, soundName), () =>
            Enum.GetValues<SoundPack>().Where(p => p != SoundPack.CustomUser)
                .Select(p => new MenuEntry(p.RawValue(), () => _e.SetSoundPack(p), _e.SoundPack == p)), W(1, 0.08), 5, 7, 4);
        soundMenu.Observe(_e, () =>
        {
            soundIcon.Text = Icons.For(_e.SoundPack == SoundPack.Mute ? "speaker.slash.fill" : "speaker.wave.2.fill");
            soundName.Text = _e.SoundPack.RawValue();
        }, nameof(BitchBoyEngine.SoundPack));

        // Modules
        var modIcon = Icon("square.grid.2x2.fill", 8, White);
        var modText = T("MODULES", 7, White);
        var modBtn = Btn(H(4, modIcon, modText), _e.ToggleRearrangeModulesMode, W(1, 0.08), 5, 7, 4);
        modBtn.Observe(_e, () =>
        {
            var on = _e.IsRearrangingModules;
            modText.Text = on ? "DONE" : "MODULES";
            modIcon.Text = Icons.For(on ? "checkmark.circle.fill" : "square.grid.2x2.fill");
            modBtn.Background = B(on ? Gold : W(1, 0.08));
            modText.Foreground = modIcon.Foreground = B(on ? Black : White);
        }, nameof(BitchBoyEngine.IsRearrangingModules));
        var modMenu = MenuBtn(Icon("chevron.down", 7, White), () => new[]
        {
            new MenuEntry(_e.IsRearrangingModules ? "Lock Module Layout" : "Rearrange & Resize Modules", _e.ToggleRearrangeModulesMode),
            MenuEntry.Sep,
            new MenuEntry("Reset Layout to Default", _e.ResetModuleLayout),
            new MenuEntry("All Modules Compact (210 px)", () => { foreach (var m in Enum.GetValues<StudioModuleID>()) _e.SetModuleHeight(m, 210); }),
            new MenuEntry("All Modules Standard (340 px)", () => { foreach (var m in Enum.GetValues<StudioModuleID>()) _e.SetModuleHeight(m, 340); }),
        }, W(1, 0.08), 5, 5, 4);

        var rainbow = Btn(H(4, T("✨🌈", 8, White), T("RAINBOW REFRESH", 7, White)), _e.RunRainbowWaveRefresh,
            Clear, 5, 7, 4, C(0.8, 0.4, 1.0).Op(0.6));
        rainbow.Background = new LinearGradientBrush(C(0.9, 0.2, 0.5, 0.45), C(0.2, 0.5, 1.0, 0.45), 0);

        // FX menu + stop
        var fxIcon = Icon("sparkles", 8, White);
        var fxText = T("", 7, White);
        var fxMenu = MenuBtn(H(4, fxIcon, fxText, Icon("chevron.down", 7, W(0.8))), FxMenu, C(0.08, 0.25, 0.24), 5, 7, 4);
        var stopFx = IconTextBtn("stop.fill", "STOP FX", 7, White, Red.Op(0.85), () => { _e.StopCustomEffect(); _e.ApplyFXMode(LEDFXMode.StaticFX); }, 5, 7, 4);
        fxMenu.Observe(_e, () =>
        {
            var active = _e.CurrentFXMode != LEDFXMode.StaticFX || _e.IsPlayingCustomFX;
            fxText.Text = _e.IsPlayingCustomFX ? $"CUSTOM: {_e.ActiveCustomFXName}" : $"FX: {_e.CurrentFXMode.RawValue()}";
            fxIcon.Text = Icons.For(_e.IsPlayingCustomFX ? "wand.and.stars" : FxIcon(_e.CurrentFXMode));
            fxMenu.Background = B(active ? C(0.92, 0.35, 0.65, 0.85) : C(0.08, 0.25, 0.24));
            stopFx.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        }, nameof(BitchBoyEngine.CurrentFXMode), nameof(BitchBoyEngine.IsPlayingCustomFX), nameof(BitchBoyEngine.ActiveCustomFXName));

        var bar = H(12, titleBlock, statusPill, Spacer(), love, topMost, scale, slotMenu, themeMenu, soundMenu, H(3, modBtn, modMenu), rainbow, fxMenu, stopFx);
        bar.Height = 50;
        return bar;
    }

    public static string FxIcon(LEDFXMode m) => m switch
    {
        LEDFXMode.StaticFX => "power", LEDFXMode.RainbowContinuous => "rainbow", LEDFXMode.KnightRider => "bolt.horizontal.fill",
        LEDFXMode.MatrixRain => "cloud.rain.fill", LEDFXMode.CyberPulse => "waveform.path", LEDFXMode.VuMeter => "slider.vertical.3",
        LEDFXMode.FireEmbers => "flame.fill", LEDFXMode.WaterRipple => "circle.dotted", LEDFXMode.Starfield => "sparkles",
        LEDFXMode.AudioStrobe => "bolt.fill", LEDFXMode.Heartbeat => "heart.fill", LEDFXMode.PlasmaWave => "wind",
        LEDFXMode.SpiralVortex => "circle.hexagonpath", LEDFXMode.EqDance => "chart.bar.xaxis", _ => "arrow.triangle.2.circlepath",
    };

    private IEnumerable<MenuEntry> StatusMenu()
    {
        yield return new MenuEntry("Rescan for BitchBoy Hardware", _e.ScanForBitchBoy);
        yield return new MenuEntry(_e.ConnectEffectEnabled ? "Disable On-Connect FX" : "Enable On-Connect FX", _e.ToggleConnectEffectEnabled);
        yield return new MenuEntry("Choose On-Connect FX", Children: _e.CustomEffects
            .Select(fx => new MenuEntry(fx.Name, () => _e.SetAsOnConnectFX(fx), fx.Name == _e.ConnectCustomFXName)).ToList());
        yield return MenuEntry.Sep;
        yield return new MenuEntry("Test On-Connect Notification FX", () => _e.TriggerBitchBoyConnectedNotificationFX(true));
        yield return MenuEntry.Sep;
        yield return MenuEntry.Label($"Installed Version: v{_e.AppVersion}");
        yield return MenuEntry.Label(_e.VirtualPortsFound ? "Virtual port: BitchBoy Virtual Out (found)" : "Virtual port: create 'BitchBoy Virtual Out' in loopMIDI");
    }

    private IEnumerable<MenuEntry> SlotMenu()
    {
        for (var i = 0; i < _e.Slots.Count; i++)
        {
            var idx = i;
            yield return new MenuEntry(_e.Slots[i].Name, () => _e.SelectSlot(idx), _e.ActiveSlot == i);
        }
        yield return MenuEntry.Sep;
        yield return new MenuEntry("💾 Save Current Slot", () => { _e.SaveCurrentSlot(); _e.Log("💾 Presets saved to internal memory."); });
        yield return new MenuEntry("📋 Clone to Next Slot", _e.CloneCurrentSlot);
        yield return new MenuEntry("⤓ Export Slot JSON to Clipboard", () => _e.ExportPresetToJson());
        yield return new MenuEntry("⤒ Import Slot JSON from Clipboard", _e.ImportPresetFromClipboard);
        yield return MenuEntry.Sep;
        yield return new MenuEntry("🚨 Blackout / Panic All LEDs", _e.BlackoutPanic);
    }

    private IEnumerable<MenuEntry> ThemeMenu()
    {
        yield return MenuEntry.Label("BUILT-IN CHASSIS THEMES");
        foreach (var th in Enum.GetValues<ChassisTheme>().Where(t => t != ChassisTheme.CustomUser))
            yield return new MenuEntry(th.RawValue(), () => _e.SetTheme(th), _e.CurrentTheme == th);
        yield return MenuEntry.Sep;
        yield return MenuEntry.Label("CUSTOM GLOW THEMES");
        foreach (var ct in _e.CustomThemes)
            yield return new MenuEntry(ct.Name, () => _e.ApplyCustomTheme(ct), _e.CurrentTheme == ChassisTheme.CustomUser && _e.ActiveCustomTheme.Id == ct.Id);
    }

    private IEnumerable<MenuEntry> FxMenu()
    {
        foreach (var m in Enum.GetValues<LEDFXMode>())
            yield return new MenuEntry($"{Icons.For(FxIcon(m))}  {m.RawValue()}", () => _e.ApplyFXMode(m), !_e.IsPlayingCustomFX && _e.CurrentFXMode == m);
        yield return MenuEntry.Sep;
        yield return MenuEntry.Label("★ USER CUSTOM LIGHT SHOWS");
        foreach (var fx in _e.CustomEffects)
            yield return new MenuEntry($"★ {fx.Name}", () => _e.PlayCustomEffect(fx.Clone()), _e.IsPlayingCustomFX && _e.ActiveCustomFXName == fx.Name);
        yield return MenuEntry.Sep;
        yield return new MenuEntry("Stop & Restore Preset LEDs", () => { _e.StopCustomEffect(); _e.ApplyFXMode(LEDFXMode.StaticFX); });
    }

    // ------------------------------------------------------------------------
    // Faceplate
    // ------------------------------------------------------------------------
    private UIElement BuildFaceplate()
    {
        var face = new Grid();
        var pcb = new Border { CornerRadius = new CornerRadius(20) };
        var bezel = new Border { CornerRadius = new CornerRadius(20), BorderThickness = new Thickness(5.5) };
        pcb.Observe(_e, () =>
        {
            var p = _e.Palette;
            pcb.Background = B(p.Pcb);
            var g = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            for (var i = 0; i < p.Bezel.Length; i++) g.GradientStops.Add(new GradientStop(p.Bezel[i], p.Bezel.Length == 1 ? 0 : i / (double)(p.Bezel.Length - 1)));
            bezel.BorderBrush = g;
        }, nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme));
        face.Children.Add(pcb);
        face.Children.Add(bezel);

        var screws = new Grid { Margin = new Thickness(7) };
        foreach (var (h, v) in new[] { (HorizontalAlignment.Left, VerticalAlignment.Top), (HorizontalAlignment.Center, VerticalAlignment.Top), (HorizontalAlignment.Right, VerticalAlignment.Top),
                                       (HorizontalAlignment.Left, VerticalAlignment.Bottom), (HorizontalAlignment.Center, VerticalAlignment.Bottom), (HorizontalAlignment.Right, VerticalAlignment.Bottom) })
            screws.Children.Add(new ScrewRivet(13).Align(h, v));
        face.Children.Add(screws);

        var deck = BuildCenterDeck();
        var rackHost = Dyn(_e, BuildBottomRack, nameof(BitchBoyEngine.LayoutVersion));
        var center = V(12, deck, rackHost);
        center.Width = 894;
        var left = Dyn(_e, () => BuildWing(_e.LeftWingModules, ModuleZone.LeftWing), nameof(BitchBoyEngine.LayoutVersion));
        var right = Dyn(_e, () => BuildWing(_e.RightWingModules, ModuleZone.RightWing), nameof(BitchBoyEngine.LayoutVersion));
        var row = H(20, left, center, right);
        row.CrossV = VerticalAlignment.Top;
        row.HorizontalAlignment = HorizontalAlignment.Center;
        row.Margin = new Thickness(18);
        face.Children.Add(row);

        face.Observe(_e, () =>
        {
            var ch = _e.ChassisContentHeight;
            face.Height = ch + 36;
            center.Height = ch;
        }, nameof(BitchBoyEngine.LayoutVersion), nameof(BitchBoyEngine.IsRearrangingModules));
        return face;
    }

    private UIElement BuildWing(List<StudioModuleID> mods, ModuleZone zone)
    {
        if (mods.Count == 0 && !_e.IsRearrangingModules) return new Border { Width = 0 };
        var stack = V(10);
        foreach (var m in mods) stack.Add(ModuleChrome.Container(_e, m, zone));
        if (_e.IsRearrangingModules) stack.Add(ModuleChrome.DropTarget(_e, zone));
        var scroll = Scroll(stack);
        scroll.Width = 274;
        scroll.Height = _e.ChassisContentHeight;
        return scroll;
    }

    private UIElement BuildBottomRack()
    {
        var headerColor = _e.Palette.HeaderText;
        if (_e.BottomRackModules.Count == 0 && !_e.IsRearrangingModules)
        {
            var dock = MenuBtn(H(3, Icon("plus.circle.fill", 7, Accent), T("DOCK MODULE HERE", 6.8, Accent)), () =>
                Enum.GetValues<StudioModuleID>().Where(m => _e.CurrentZone(m) != ModuleZone.BottomRack)
                    .Select(m => new MenuEntry($"Dock {m.RawValue()} Here", () => _e.MoveModuleToZone(m, ModuleZone.BottomRack))), W(1, 0.08), 4, 7, 3);
            var shelf = V(6,
                H(6, Icon("rectangle.bottomthird.inset.filled", 8.5, Accent), T("CONSOLE EXPANSION BAY // BOTTOM RACK READY", 7.8, headerColor), Spacer(), dock),
                Box(V(6,
                    Icon("arrow.down.and.line.horizontal.and.arrow.up", 14, Accent.Op(0.6)),
                    T("BOTTOM RACK EXPANSION BAY", 8, White).Align(HorizontalAlignment.Center),
                    T("Drag any module here from wings, or use the menu above to dock a module in this space", 7, W(0.55), FontWeights.Bold).Align(HorizontalAlignment.Center)),
                    Black.Op(0.28), 8, 8, 14, _e.Palette.Outline.Op(0.6)));
            var b = Box(shelf, Black.Op(0.32), 10, 8, 8, _e.Palette.Outline.Op(0.4));
            ModuleChrome.MakeZoneDrop(_e, b, ModuleZone.BottomRack);
            return b;
        }

        var row = H(12);
        row.CrossV = VerticalAlignment.Top;
        foreach (var m in _e.BottomRackModules)
        {
            var c = ModuleChrome.Container(_e, m, ModuleZone.BottomRack);
            c.Width = _e.ModuleWidth(m);
            row.Add(c);
        }
        if (_e.IsRearrangingModules) row.Add(ModuleChrome.DropTarget(_e, ModuleZone.BottomRack).Size(220));
        var rack = V(5,
            H(5, Icon("rectangle.bottomthird.inset.filled", 8, Accent), T("CONSOLE EXPANSION BAY // BOTTOM RACK", 7.5, headerColor), Spacer()),
            Scroll(row, horizontal: true));
        var box = Box(rack, Black.Op(0.35), 10, 8, 8, _e.Palette.Outline.Op(0.4));
        ModuleChrome.MakeZoneDrop(_e, box, ModuleZone.BottomRack);
        return box;
    }

    private UIElement BuildCenterDeck()
    {
        var col1 = V(8, Section1KeyGrid(), Section2Strip(), Section4VerticalFaders());
        col1.Width = 414;

        var logo = new GlowImage("logo.png", 254, 68, () => { SoundEffect.MascotSqueak.Play(_e.SoundPack); _e.Log("✨ BITCHBOY LOGO ACTIVATED"); }, _e).Size(254, 68);
        var col2 = V(10, Section5HorizontalFaders().Size(254), logo, BuildInspector().Size(254, 256));
        col2.Width = 254;
        var col3 = V(10, Section6Knobs().Size(194), Section3KeypadAndMascot().Size(194));
        col3.Width = 194;

        var right = H(14, col2, col3);
        right.CrossV = VerticalAlignment.Top;
        var deck = H(18, col1, right);
        deck.CrossV = VerticalAlignment.Top;
        deck.Height = 530;
        return deck;
    }

    private Stack SectionHeader(string title, UIElement? trailing, double width = 410)
    {
        var t = T(title, 8.5, White);
        t.Observe(_e, () => t.Foreground = B(_e.Palette.HeaderText), nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme));
        var h = H(4, t, Spacer(), trailing);
        if (width > 0) h.Width = width;
        return h;
    }

    private Border KeyTray(UIElement content)
    {
        var b = Box(content, C(0.08, 0.04, 0.04, 0.8), 10, 6, 6);
        b.BorderThickness = new Thickness(1.2);
        b.Observe(_e, () => b.BorderBrush = B(_e.Palette.Outline.Op(0.85)), nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme));
        b.HorizontalAlignment = HorizontalAlignment.Left;
        return b;
    }

    private UIElement Section1KeyGrid()
    {
        var rows = V(4);
        for (var r = 0; r < 4; r++)
        {
            var row = H(4);
            for (var c = 0; c < 8; c++) row.Add(new KeyCap(_e, r * 8 + c));
            rows.Add(row);
        }
        return V(4, SectionHeader("1. TOP-LEFT 8X4 KEY GRID (32 KEYS)", T("CLICK KEY OR PRESS BITCHBOY", 7.5, C(0.15, 0.85, 0.95), FontWeights.Bold)), KeyTray(rows));
    }

    private UIElement Section2Strip()
    {
        var row = H(4);
        for (var i = 32; i < 40; i++) row.Add(new KeyCap(_e, i));
        return V(4, SectionHeader("2. MIDDLE 1X8 KEY STRIP (8 KEYS)", T("ICON SHOWS MAPPED ACTION", 7.5, C(0.96, 0.78, 0.35), FontWeights.Bold)), KeyTray(row));
    }

    private UIElement Section4VerticalFaders()
    {
        var vuText = T("", 7, W(0.6), FontWeights.Bold);
        var vuBtn = Btn(H(3, Icon("slider.vertical.3", 8, W(0.6)), vuText), () =>
        {
            if (!_e.VuMeterLEDsEnabled) _e.ApplyFXMode(LEDFXMode.VuMeter);
            else _e.ApplyFXMode(LEDFXMode.StaticFX);
        }, Black.Op(0.5), 5, 6, 2, Mint.Op(0.5));
        vuBtn.Observe(_e, () =>
        {
            vuText.Text = _e.VuMeterLEDsEnabled ? "VU METER LEDS: ON" : "VU METER LEDS: OFF";
            vuText.Foreground = B(_e.VuMeterLEDsEnabled ? Mint : W(0.6));
        }, nameof(BitchBoyEngine.VuMeterLEDsEnabled));

        var row = H(4);
        for (var i = 49; i < 57; i++) row.Add(new VerticalFader(_e, i));
        return V(4, SectionHeader("4. BOTTOM-LEFT 8 VERTICAL FADERS (CC 0–7)", vuBtn), Box(row, Black.Op(0.35), 10, 6, 6).Align(HorizontalAlignment.Left));
    }

    private FrameworkElement Section5HorizontalFaders()
    {
        var rows = V(3);
        for (var i = 57; i < 61; i++) rows.Add(new HorizontalFader(_e, i));
        return V(5, SectionHeader("5. 4 HORIZONTAL FADERS (CC 8–11)", null, 0), Box(rows, Black.Op(0.35), 10, 6, 6));
    }

    private FrameworkElement Section6Knobs()
    {
        var rows = V(4);
        for (var r = 0; r < 4; r++)
        {
            var row = H(8, new Knob(_e, 61 + r * 2), new Knob(_e, 62 + r * 2));
            row.HorizontalAlignment = HorizontalAlignment.Center;
            rows.Add(row);
        }
        return V(5, SectionHeader("6. 2X4 KNOBS (CC 20–27)", null, 0), Box(rows, Black.Op(0.35), 10, 6, 6));
    }

    private FrameworkElement Section3KeypadAndMascot()
    {
        var speech = T("", 7, Accent);
        var bubble = Box(speech, Black.Op(0.85), 8, 6, 3);
        bubble.HorizontalAlignment = HorizontalAlignment.Center;
        bubble.VerticalAlignment = VerticalAlignment.Top;
        bubble.Margin = new Thickness(0, -18, 0, 0);
        bubble.Observe(_e, () =>
        {
            speech.Text = _e.MascotSpeech;
            speech.Foreground = B(Accent);
            bubble.BorderBrush = B(Accent);
            bubble.BorderThickness = new Thickness(1);
            bubble.Visibility = _e.MascotSpeechVisible ? Visibility.Visible : Visibility.Collapsed;
        }, nameof(BitchBoyEngine.MascotSpeech), nameof(BitchBoyEngine.MascotSpeechVisible), nameof(BitchBoyEngine.CurrentTheme));

        var mascot = new GlowImage("mascot.png", 160, 96, _e.TriggerMascotTap, _e).Size(186, 96);
        var mascotScale = new ScaleTransform(1, 1);
        var mascotHost = new Border { Child = mascot, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = mascotScale };
        mascotHost.Observe(_e, () =>
        {
            var anim = new DoubleAnimation(_e.MascotScale, TimeSpan.FromMilliseconds(250)) { EasingFunction = new BackEase { Amplitude = 0.4 } };
            mascotScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            mascotScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }, nameof(BitchBoyEngine.MascotScale));

        var mascotArea = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        mascotArea.Children.Add(mascotHost);
        mascotArea.Children.Add(bubble);

        var rows = V(4);
        for (var r = 0; r < 3; r++)
        {
            var row = H(4);
            for (var c = 0; c < 3; c++) row.Add(new KeyCap(_e, 40 + r * 3 + c));
            rows.Add(row);
        }
        var tray = KeyTray(rows);
        tray.HorizontalAlignment = HorizontalAlignment.Center;
        var header = SectionHeader("3. 3X3 KEYPAD (9 KEYS)", null, 0);
        header.HorizontalAlignment = HorizontalAlignment.Center;

        var footer = V(1,
            T("CE 🗑", 8, W(0.5), FontWeights.Bold).Align(HorizontalAlignment.Center),
            T("BitchBoy", 8, W(0.6)).Align(HorizontalAlignment.Center),
            T("BLOCK SYSTEM HARDWARE", 6, W(0.4)).Align(HorizontalAlignment.Center));
        return V(5, mascotArea, header, tray, footer);
    }

    // ------------------------------------------------------------------------
    // Center inspector HUD
    // ------------------------------------------------------------------------
    private FrameworkElement BuildInspector()
    {
        var grid = new Grid();
        var bg = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = B(C(0.05, 0.10, 0.09)),
            BorderThickness = new Thickness(3),
            BorderBrush = Gradient(W(0.95), W(0.75)),
        };
        grid.Children.Add(bg);
        var screws = new Grid { Margin = new Thickness(4) };
        foreach (var (h, v) in new[] { (HorizontalAlignment.Left, VerticalAlignment.Top), (HorizontalAlignment.Right, VerticalAlignment.Top), (HorizontalAlignment.Left, VerticalAlignment.Bottom), (HorizontalAlignment.Right, VerticalAlignment.Bottom) })
            screws.Children.Add(new ScrewRivet(9).Align(h, v));
        grid.Children.Add(screws);

        var body = Dyn(_e, BuildInspectorBody, nameof(BitchBoyEngine.SelectedIndex), nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme));
        body.Padding = new Thickness(10);
        grid.Children.Add(body);
        return grid;
    }

    private UIElement BuildInspectorBody()
    {
        var c = _e.SelectedControl;
        var idx = _e.SelectedIndex;
        var props = new[] { nameof(ControlItem.CurrentLED), nameof(ControlItem.IdleVelocity), nameof(ControlItem.ActiveVelocity),
                            nameof(ControlItem.FeedbackMode), nameof(ControlItem.ActionType), nameof(ControlItem.LiveValue) };

        var chip = Box(T($"{c.Label} // N{c.Number}", 9, Black), Accent, 4, 6, 2);
        var multi = Box(T("", 7.5, Black), Cyan, 8, 5, 2);
        multi.Observe(_e, () =>
        {
            ((TextBlock)multi.Child).Text = $"{_e.SelectedIndices.Count} SELECTED";
            multi.Visibility = _e.SelectedIndices.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }, nameof(BitchBoyEngine.SelectedIndices));
        var ledSwatch = new Border { Width = 14, Height = 14, CornerRadius = new CornerRadius(4), BorderBrush = B(White.Op(0.4)), BorderThickness = new Thickness(1) };
        var ledLabel = T("", 7, White);
        var top = H(4, chip, Spacer(), multi, H(4, ledSwatch, ledLabel));
        top.Observe(c, () =>
        {
            var col = Leds.For(_e, c.CurrentLED);
            ledSwatch.Background = B(col);
            ledLabel.Text = HardwareColor.FormattedLabel(c.CurrentLED);
            ledLabel.Foreground = B(col);
        }, nameof(ControlItem.CurrentLED));

        var nameRow = H(4, T("NAME:", 7.5, W(0.6), FontWeights.Bold),
            Field(() => c.CustomName, v => c.CustomName = v, 8, c, nameof(ControlItem.CustomName), "Custom Name").Fill());
        var zone = T(c.Zone, 7.5, _e.Palette.HeaderText, FontWeights.Bold);
        var mode = T("", 8, C(0.96, 0.78, 0.35), FontWeights.SemiBold);
        mode.Observe(c, () => mode.Text = $"Mode: {c.FeedbackMode.RawValue()} // {c.ActionType.RawValue()}", nameof(ControlItem.FeedbackMode), nameof(ControlItem.ActionType));

        var stack = V(6, top, nameRow, zone, mode);

        if (c.HasLED)
        {
            IEnumerable<ControlItem> Targets() => (_e.SelectedIndices.Count > 1 ? _e.SelectedIndices.Select(i => _e.Controls[i]) : [c]).Where(t => t.HasLED);
            UIElement VelRow(string label, Func<int> get, Action<int> set, string prop)
            {
                var dot = Dot(7, White);
                var txt = T("", 7.5, White);
                var lbl = T("", 7.5, W(0.7), FontWeights.Bold);
                var slider = new MiniSlider { Minimum = 0, Maximum = 127, Step = 1, Accent = C(0.85, 0.35, 0.30) };
                slider.ValueChanged += v => { foreach (var t in Targets()) set2(t, (int)v); };
                void set2(ControlItem t, int v) { if (t == c) set(v); else SetOn(t, prop, v); }
                var row = V(2, H(4, lbl, Spacer(), dot, txt), slider);
                row.Observe(c, () =>
                {
                    var v = get();
                    var col = Leds.For(_e, v);
                    dot.Fill = B(col);
                    txt.Text = HardwareColor.FormattedLabel(v);
                    txt.Foreground = B(col);
                    if (!slider.IsMouseCaptured) slider.Value = v;
                }, prop);
                row.Observe(_e, () => lbl.Text = _e.SelectedIndices.Count > 1 ? $"{label} (BATCH):" : $"{label}:", nameof(BitchBoyEngine.SelectedIndices));
                return row;
            }

            stack.Add(VelRow("IDLE LED", () => c.IdleVelocity, v => SetOn(c, nameof(ControlItem.IdleVelocity), v), nameof(ControlItem.IdleVelocity)));
            stack.Add(VelRow("PRESSED LED", () => c.ActiveVelocity, v => SetOn(c, nameof(ControlItem.ActiveVelocity), v), nameof(ControlItem.ActiveVelocity)));

            var swatches = H(3);
            foreach (var vel in HardwareColor.KnownVelocities)
            {
                var v = vel;
                var dot = new Clickable(() =>
                {
                    foreach (var t in Targets()) t.ActiveVelocity = v;
                    _e.SaveCurrentSlot();
                    SoundEffect.KnobTick.Play(_e.SoundPack);
                }, 1.15) { Width = 12, Height = 12, CornerRadius = new CornerRadius(6), Background = B(Leds.For(_e, v)) };
                dot.Observe(c, () =>
                {
                    var sel = c.ActiveVelocity == v;
                    dot.BorderBrush = B(White);
                    dot.BorderThickness = new Thickness(sel ? 1.5 : 0);
                }, nameof(ControlItem.ActiveVelocity));
                swatches.Add(dot);
            }
            stack.Add(V(2, T("QUICK COLOR PALETTE:", 6.5, W(0.55), FontWeights.Bold), swatches));

            var amber = C(0.96, 0.78, 0.35);
            stack.Add(H(4,
                TextBtn("FILL 8x4", 7.5, amber, Black.Op(0.4), () => _e.SetZoneLEDs(0, 32, c.IdleVelocity), 5, 4, 3),
                TextBtn("FILL 1x8", 7.5, amber, Black.Op(0.4), () => _e.SetZoneLEDs(32, 40, c.IdleVelocity), 5, 4, 3),
                TextBtn("FILL 3x3", 7.5, amber, Black.Op(0.4), () => _e.SetZoneLEDs(40, 49, c.IdleVelocity), 5, 4, 3),
                TextBtn("ALL 49", 7.5, amber, Black.Op(0.4), () => _e.SetZoneLEDs(0, 49, c.IdleVelocity), 5, 4, 3)));
        }
        else
        {
            var cc = T("", 8, C(0.96, 0.78, 0.35), FontWeights.Bold);
            cc.Margin = new Thickness(0, 10, 0, 10);
            cc.Observe(c, () => cc.Text = $"ANALOG CC VALUE: {c.LiveValue}", nameof(ControlItem.LiveValue));
            stack.Add(cc);
        }

        var rx = T("", 7.5, Accent);
        rx.TextAlignment = TextAlignment.Right;
        rx.Observe(_e, () => rx.Text = _e.LastStreamMessage, nameof(BitchBoyEngine.LastStreamMessage));
        var dock = new DockPanel { LastChildFill = true };
        var foot = H(4, T("RX STREAM:", 7, W(0.5), FontWeights.Bold), rx.Fill());
        DockPanel.SetDock(foot, Dock.Bottom);
        dock.Children.Add(foot);
        dock.Children.Add(stack);
        _ = props;
        _ = idx;
        return dock;
    }

    private void SetOn(ControlItem t, string prop, int v)
    {
        if (prop == nameof(ControlItem.IdleVelocity))
        {
            t.IdleVelocity = v;
            t.CurrentLED = v;
            _e.SendNoteOn(t.Number, v);
        }
        else t.ActiveVelocity = v;
    }

    // ------------------------------------------------------------------------
    // Overlays
    // ------------------------------------------------------------------------
    private UIElement BuildBanner()
    {
        var bannerGreen = C(0.2, 0.98, 0.55);
        var badge = new Image { Source = GlowImage.LoadResource("badge.png"), Width = 32, Height = 32, Effect = new DropShadowEffect { Color = bannerGreen, ShadowDepth = 0, BlurRadius = 8 } };
        var title = T("", 11, C(0.25, 1.0, 0.60));
        var sub = T("", 8.5, White, FontWeights.Bold);
        var close = TextBtn("✕", 11, White.Op(0.75), Clear, () => _e.ShowConnectionBanner = false, 6, 2, 0);
        var banner = Box(H(12, badge, V(2, title, sub), close), C(0.03, 0.16, 0.12, 0.96), 14, 18, 10, bannerGreen, 1.8);
        banner.Effect = new DropShadowEffect { Color = bannerGreen, ShadowDepth = 4, Direction = 270, BlurRadius = 18, Opacity = 0.75 };
        banner.HorizontalAlignment = HorizontalAlignment.Center;
        banner.VerticalAlignment = VerticalAlignment.Top;
        banner.Margin = new Thickness(0, 54, 0, 0);
        banner.Observe(_e, () =>
        {
            title.Text = _e.ConnectionBannerTitle;
            sub.Text = _e.ConnectionBannerSubtitle;
            banner.Visibility = _e.ShowConnectionBanner ? Visibility.Visible : Visibility.Collapsed;
        }, nameof(BitchBoyEngine.ShowConnectionBanner), nameof(BitchBoyEngine.ConnectionBannerTitle), nameof(BitchBoyEngine.ConnectionBannerSubtitle));
        return banner;
    }

    private UIElement? BuildSpotlight()
    {
        if (_e.SpotlightModule is not { } mod) return null;
        var grid = new Grid();
        var dim = new Border { Background = B(Black.Op(0.65)) };
        dim.MouseLeftButtonUp += (_, _) => _e.SpotlightModule = null;
        grid.Children.Add(dim);

        Clickable Preset(string label, double h) => TextBtn(label, 6.5, W(0.75), W(1, 0.08), () => _e.SetModuleHeight(mod, h), 4, 6, 3);
        var header = H(8,
            Icon(ModuleChrome.IconName(mod), 13, Accent),
            V(1,
                H(6, T($"CENTER SPOTLIGHT // {mod.RawValue().ToUpperInvariant()}", 10, White), Dot(6, Green), T("LIVE", 7, Green)),
                T("Enlarged studio view • Edit settings or pop out to floating window", 7, W(0.6), FontWeights.Normal)),
            Spacer(),
            H(2, Preset("COMPACT", 210), Preset("STANDARD", 340), Preset("EXPANDED", 480)),
            IconTextBtn("macwindow.badge.plus", "OPEN IN OWN WINDOW", 7.5, Accent, W(1, 0.14), () => _e.PopOutModuleToWindow(mod), 6, 8, 5),
            IconTextBtn("xmark.circle.fill", "RETURN TO DOCK", 7.5, Black, Accent, () => _e.SpotlightModule = null, 6, 8, 5));
        var headerBox = new Border
        {
            Child = header,
            Padding = new Thickness(14, 10, 14, 10),
            Background = B(C(0.04, 0.10, 0.09, 0.98)),
            BorderBrush = B(Accent.Op(0.3)),
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        var dock = new DockPanel();
        DockPanel.SetDock(headerBox, Dock.Top);
        dock.Children.Add(headerBox);
        dock.Children.Add(ModuleChrome.Content(_e, mod, true));
        var panel = new Border
        {
            Child = dock,
            Width = 720,
            Height = 600,
            CornerRadius = new CornerRadius(14),
            Background = B(C(0.03, 0.05, 0.06, 0.98)),
            BorderBrush = B(Accent),
            BorderThickness = new Thickness(1.5),
            ClipToBounds = true,
            Effect = new DropShadowEffect { Color = Accent, ShadowDepth = 8, Direction = 270, BlurRadius = 24, Opacity = 0.45 },
        };
        grid.Children.Add(panel);
        return grid;
    }

    // ------------------------------------------------------------------------
    // Floating module windows
    // ------------------------------------------------------------------------
    private void SyncModuleWindows()
    {
        foreach (var m in _e.DetachedModules.Where(m => !_moduleWindows.ContainsKey(m)).ToList())
        {
            var size = ModuleChrome.IdealWindowSize(m);
            var w = new Window
            {
                Title = $"BitchManager // {m.RawValue()}",
                Width = size.Width * 1.25,
                Height = size.Height * 1.25,
                Background = B(C(0.02, 0.06, 0.05)),
                Owner = this,
                ShowInTaskbar = true,
                Icon = Icon,
                Content = new Viewbox { Stretch = Stretch.Uniform, Child = ModuleChrome.FloatingBody(_e, m).Size(size.Width, size.Height) },
            };
            var mod = m;
            w.Closed += (_, _) =>
            {
                _moduleWindows.Remove(mod);
                _e.DockModuleBackFromWindow(mod);
            };
            _moduleWindows[m] = w;
            w.Show();
        }
        foreach (var (m, w) in _moduleWindows.ToList())
        {
            if (!_e.DetachedModules.Contains(m)) { _moduleWindows.Remove(m); w.Close(); continue; }
            w.Topmost = _e.IsModuleWindowTopMost(m);
        }
    }
}
