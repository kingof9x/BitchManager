using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.Platform;
using static BitchManager.UI.Ui;

namespace BitchManager.UI.Modules;

/// <summary>REACTIVE KEY FX // LIVE: per-keypress light animations (ripples, lasers, lightning...).</summary>
public static class ReactiveFXPanel
{
    private static readonly int[] Swatches = [72, 61, 96, 74, 87, 33, 78, 41, 67, 95, 83, 3];

    public static string ModeIcon(ReactiveKeyFXMode m) => m switch
    {
        ReactiveKeyFXMode.Ripple => "dot.radiowaves.left.and.right",
        ReactiveKeyFXMode.CrossLaser => "scope",
        ReactiveKeyFXMode.Supernova => "sun.max.fill",
        ReactiveKeyFXMode.HeatTrail => "flame.fill",
        ReactiveKeyFXMode.MeteorRain => "cloud.bolt.rain.fill",
        ReactiveKeyFXMode.ArcLightning => "bolt.fill",
        _ => "poweroff",
    };

    private static ReactiveKeyFXMode _lastActiveMode = ReactiveKeyFXMode.Ripple;

    private static Color ActiveColor(BitchBoyEngine e) =>
        e.ReactiveFXRainbowColor ? Cyan : Leds.For(e, e.ReactiveFXColorVelocity);

    public static UIElement Build(BitchBoyEngine e, bool spotlight)
    {
        var toggle = Dyn(e, () =>
        {
            var on = e.ReactiveKeyFXMode != ReactiveKeyFXMode.Off;
            return TextBtn(on ? "ACTIVE" : "OFF", 6.5, on ? Black : W(0.7), on ? Green : White.Op(0.1), () =>
            {
                if (on)
                {
                    _lastActiveMode = e.ReactiveKeyFXMode;
                    e.ReactiveKeyFXMode = ReactiveKeyFXMode.Off;
                    e.ActiveReactivePulses.Clear();
                    e.RestorePresetLEDs();
                }
                else e.ReactiveKeyFXMode = _lastActiveMode;
                SoundEffect.KnobTick.Play(e.SoundPack);
            }, 8, 6, 2);
        }, nameof(BitchBoyEngine.ReactiveKeyFXMode));

        var header = ModuleChrome.Header(e, StudioModuleID.ReactiveKeyFX,
            ModuleChrome.AccentText(e, Icons.For("waveform.circle.fill"), 10), "REACTIVE KEY FX // LIVE", toggle);

        var body = Dyn(e, () => Body(e), nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.LedPalette));
        var frame = ModuleChrome.Frame(header, null, body, spotlight, e.Palette.Outline.Op(0.75));
        frame.Observe(e, () => frame.BorderBrush = B(ActiveColor(e).Op(0.6)),
            nameof(BitchBoyEngine.ReactiveFXColorVelocity), nameof(BitchBoyEngine.ReactiveFXRainbowColor), nameof(BitchBoyEngine.LedPalette));
        return frame;
    }

    private static UIElement Body(BitchBoyEngine e)
    {
        var modes = Dyn(e, () => Grid(3, 4, Enum.GetValues<ReactiveKeyFXMode>().Where(m => m != ReactiveKeyFXMode.Off).Select(m =>
        {
            var on = e.ReactiveKeyFXMode == m;
            var c = ActiveColor(e);
            return (UIElement)Btn(V(3,
                    Icon(ModeIcon(m), 13, on ? c : W(0.6)).Align(HorizontalAlignment.Center),
                    T(m.RawValue(), 6.3, on ? White : W(0.65)).Align(HorizontalAlignment.Center)),
                () => { e.ReactiveKeyFXMode = m; e.TriggerRandomDemoReactiveFX(); SoundEffect.KeyTap.Play(e.SoundPack); },
                on ? c.Op(0.15) : Black.Op(0.35), 6, 4, 7, on ? c.Op(0.7) : White.Op(0.08));
        })), nameof(BitchBoyEngine.ReactiveKeyFXMode), nameof(BitchBoyEngine.ReactiveFXColorVelocity), nameof(BitchBoyEngine.ReactiveFXRainbowColor));

        var speed = new MiniSlider { Minimum = 0.4, Maximum = 3.0, Step = 0.1, Accent = e.Palette.Accent, Value = e.ReactiveFXSpeed };
        var speedText = T("", 7, e.Palette.Accent);
        speed.ValueChanged += v => e.ReactiveFXSpeed = v;
        speed.Observe(e, () => { speed.Value = e.ReactiveFXSpeed; speedText.Text = $"{e.ReactiveFXSpeed:0.0}x"; }, nameof(BitchBoyEngine.ReactiveFXSpeed));

        var colors = Dyn(e, () =>
        {
            var row = H(3);
            var prism = e.ReactiveFXRainbowColor;
            row.Add(Btn(H(2, T("🌈", 8, White), T("PRISM", 6.5, prism ? Black : W(0.8))),
                () => e.ReactiveFXRainbowColor = true, prism ? Cyan : White.Op(0.08), 8, 5, 2));
            foreach (var v in Swatches)
            {
                var vel = v;
                var sel = !prism && e.ReactiveFXColorVelocity == vel;
                row.Add(Btn(new Border
                {
                    Width = 12, Height = 12, CornerRadius = new CornerRadius(6), Background = B(Leds.For(e, vel)),
                    BorderBrush = B(sel ? White : Black.Op(0.4)), BorderThickness = new Thickness(sel ? 2 : 1),
                }, () => { e.ReactiveFXColorVelocity = vel; e.ReactiveFXRainbowColor = false; }, Clear, 6, 0, 0));
            }
            var label = prism ? "PRISM RAINBOW (CYCLES PER PULSE)"
                : HardwareColor.AllItems.FirstOrDefault(i => i.Velocity == e.ReactiveFXColorVelocity)?.Name ?? $"VEL {e.ReactiveFXColorVelocity}";
            return V(4, row, T($"COLOR: {label}", 6.5, ActiveColor(e)));
        }, nameof(BitchBoyEngine.ReactiveFXColorVelocity), nameof(BitchBoyEngine.ReactiveFXRainbowColor));

        var pulses = T("", 6.5, W(0.6));
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) => pulses.Text = $"ACTIVE PULSES: {e.ActiveReactivePulses.Count}";
        pulses.Loaded += (_, _) => timer.Start();
        pulses.Unloaded += (_, _) => timer.Stop();

        var test = IconTextBtn("dot.radiowaves.left.and.right", "TEST RIPPLE PULSE", 7, Black, e.Palette.Accent, () =>
        {
            if (e.ReactiveKeyFXMode == ReactiveKeyFXMode.Off) e.ReactiveKeyFXMode = _lastActiveMode;
            e.TriggerRandomDemoReactiveFX();
        }, 4, 6, 5);

        return V(7,
            ModuleChrome.HeaderText(e, "KEYPRESS ANIMATION MODE:", 7),
            modes,
            H(5, T("WAVE SPEED:", 6.5, W(0.6), FontWeights.Bold), speed.Fill(), speedText),
            V(3, T("PULSE COLOR:", 6.5, W(0.6), FontWeights.Bold), colors),
            H(5, test.Fill(), pulses));
    }
}
