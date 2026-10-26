using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.Platform;
using static BitchManager.UI.Ui;

namespace BitchManager.UI.Modules;

/// <summary>CUSTOM FX STUDIO: frame sequencer, procedural wave synth and the saved show library.</summary>
public static class CustomFXPanel
{
    private static readonly (string Short, int Vel)[] Swatches =
        HardwareColor.AllItems.Select(i => (i.ShortName, i.Velocity)).ToArray();

    public static UIElement Build(BitchBoyEngine e, bool spotlight)
    {
        var playDot = Dot(6, W(0.4));
        var playText = T("", 7, W(0.6));
        var status = H(3, playDot, playText);
        status.Observe(e, () =>
        {
            playDot.Fill = B(e.IsPlayingCustomFX ? Green : W(0.4));
            playText.Foreground = B(e.IsPlayingCustomFX ? Green : W(0.6));
            playText.Text = e.IsPlayingCustomFX ? "PLAYING" : "IDLE";
        }, nameof(BitchBoyEngine.IsPlayingCustomFX));

        var header = ModuleChrome.Header(e, StudioModuleID.CustomFXStudio,
            ModuleChrome.AccentText(e, Icons.For("wand.and.stars"), 10), "CUSTOM FX STUDIO", status);
        var tabs = Dyn(e, () => H(3,
            ModuleChrome.Tab(e, "SEQUENCER", e.RightWingTab == "SEQUENCER", () => e.RightWingTab = "SEQUENCER"),
            ModuleChrome.Tab(e, "WAVE SYNTH", e.RightWingTab == "WAVESYNTH", () => e.RightWingTab = "WAVESYNTH"),
            ModuleChrome.Tab(e, $"MY SHOWS ({e.CustomEffects.Count})", e.RightWingTab == "LIBRARY", () => e.RightWingTab = "LIBRARY")),
            nameof(BitchBoyEngine.RightWingTab), nameof(BitchBoyEngine.CustomEffects), nameof(BitchBoyEngine.CurrentTheme));
        var body = Dyn(e, () => e.RightWingTab switch
        {
            "WAVESYNTH" => WaveSynth(e),
            "LIBRARY" => Library(e),
            _ => Sequencer(e),
        }, nameof(BitchBoyEngine.RightWingTab), nameof(BitchBoyEngine.CurrentTheme));
        return ModuleChrome.Frame(header, tabs, body, spotlight, e.Palette.Outline.Op(0.75));
    }

    private static TextBox NameField(BitchBoyEngine e) =>
        Field(() => e.EditingCustomFX.Name, v => e.EditingCustomFX.Name = v, 8, e, nameof(BitchBoyEngine.EditingCustomFX), "Effect name");

    private static UIElement PlayStop(BitchBoyEngine e) => Dyn(e, () => e.IsPlayingCustomFX
        ? IconTextBtn("stop.fill", "STOP", 7, White, Red.Op(0.8), e.StopCustomEffect, 4, 6, 4)
        : IconTextBtn("play.fill", "PLAY", 7, Black, Green, () => e.PlayCustomEffect(e.EditingCustomFX), 4, 6, 4),
        nameof(BitchBoyEngine.IsPlayingCustomFX));

    // ------------------------------------------------------------------------
    // SEQUENCER
    // ------------------------------------------------------------------------
    private static UIElement Sequencer(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;

        var top = H(4, NameField(e).Fill(), PlayStop(e),
            IconTextBtn("square.and.arrow.down.fill", "SAVE", 7, Black, accent, () => { e.SaveEditingCustomFXToLibrary(); SoundEffect.KeyTap.Play(e.SoundPack); }, 4, 6, 4),
            IconTextBtn("bolt.fill", "SET CONNECT", 7, Yellow, White.Op(0.1), () => e.SetAsOnConnectFX(e.EditingCustomFX.Clone()), 4, 6, 4)
                .Tip("Play this effect when the BitchBoy is plugged in"));

        var paint = Ui.CheckRow(() => e.PaintOnMainChassis, v => e.PaintOnMainChassis = v,
            T("PAINT ON MAIN CHASSIS KEYS", 7, White), accent, e, nameof(BitchBoyEngine.PaintOnMainChassis));

        // Mini canvas: 8x4 grid | 1x8 strip | 3x3 pad
        var grid = V(2);
        for (var r = 0; r < 4; r++)
        {
            var row = H(2);
            for (var c = 0; c < 8; c++) row.Add(new MiniCanvasKey(e, r * 8 + c));
            grid.Add(row);
        }
        var strip = H(2);
        for (var i = 32; i < 40; i++) strip.Add(new MiniCanvasKey(e, i));
        var pad = V(2);
        for (var r = 0; r < 3; r++)
        {
            var row = H(2);
            for (var c = 0; c < 3; c++) row.Add(new MiniCanvasKey(e, 40 + r * 3 + c));
            pad.Add(row);
        }
        var canvas = Box(H(8,
            V(4, grid, new Border { Height = 1, Background = B(White.Op(0.15)) }, strip),
            new Border { Width = 1, Background = B(White.Op(0.15)) },
            pad).Align(HorizontalAlignment.Center), Black.Op(0.55), 6, 6, 6);

        var swatches = Dyn(e, () => Grid(7, 3, Swatches.Select(s =>
        {
            var sel = e.CustomBrushVelocity == s.Vel;
            return (UIElement)Btn(H(2, Dot(6, s.Vel == 0 ? W(0.25) : Leds.For(e, s.Vel)), T(s.Short, 6, sel ? White : W(0.7))),
                () => e.CustomBrushVelocity = s.Vel, sel ? White.Op(0.15) : Black.Op(0.3), 3, 3, 2, sel ? White.Op(0.6) : null);
        })), nameof(BitchBoyEngine.CustomBrushVelocity), nameof(BitchBoyEngine.LedPalette));

        var timeline = Dyn(e, () =>
        {
            var fx = e.EditingCustomFX;
            var frames = H(2);
            for (var i = 0; i < fx.Frames.Count; i++)
            {
                var idx = i;
                var on = e.EditingFrameIndex == i;
                frames.Add(TextBtn($"F{i + 1}", 6.5, on ? Black : W(0.75), on ? accent : White.Op(0.08), () => e.SetEditingFrame(idx), 3, 5, 3));
            }
            return V(4,
                H(3, T($"TIMELINE ({fx.Frames.Count} FRAMES)", 6.5, W(0.6), FontWeights.Bold), Spacer(),
                    TextBtn("+ADD", 6.5, Green, White.Op(0.08), e.AddCustomFXFrame),
                    TextBtn("DUP", 6.5, accent, White.Op(0.08), e.DuplicateCustomFXFrame),
                    TextBtn("DEL", 6.5, Red, White.Op(0.08), e.DeleteCustomFXFrame)),
                Scroll(frames, true));
        }, nameof(BitchBoyEngine.EditingCustomFX), nameof(BitchBoyEngine.EditingFrameIndex));

        Clickable Tool(string icon, string tip, Action a) => Btn(Icon(icon, 9, White), a, White.Op(0.08), 3, 5, 3).Tip(tip);
        var tools = V(3,
            H(3, T("SHIFT:", 6.5, W(0.6), FontWeights.Bold),
                Tool("arrow.left", "Shift left", () => e.ShiftCurrentFrame(-1, 0)),
                Tool("arrow.right", "Shift right", () => e.ShiftCurrentFrame(1, 0)),
                Tool("arrow.up", "Shift up", () => e.ShiftCurrentFrame(0, -1)),
                Tool("arrow.down", "Shift down", () => e.ShiftCurrentFrame(0, 1)),
                Tool("arrow.left.and.right.righttriangle.left.righttriangle.right", "Mirror", e.MirrorCurrentFrameHorizontal),
                Tool("circle.lefthalf.filled", "Invert", e.InvertCurrentFrame)),
            H(3, T("GEN:", 6.5, W(0.6), FontWeights.Bold),
                TextBtn("CLEAR", 6.5, W(0.8), White.Op(0.08), e.ClearCurrentCustomFXFrame),
                TextBtn("FILL", 6.5, W(0.8), White.Op(0.08), () => e.FillCurrentCustomFXFrame(e.CustomBrushVelocity)),
                TextBtn("✨ SPARKS", 6.5, Yellow, White.Op(0.08), e.RandomizeCurrentFrameSparks),
                TextBtn("🌈 RAINBOW", 6.5, Cyan, White.Op(0.08), e.ApplyRainbowToCurrentFrame)));

        var fpsSlider = new MiniSlider { Minimum = 1, Maximum = 24, Step = 1, Accent = accent, Value = e.EditingCustomFX.Fps };
        var fpsText = T("", 7, accent);
        fpsSlider.ValueChanged += v => { e.EditingCustomFX.Fps = v; fpsText.Text = $"{v:0} FPS"; };
        fpsSlider.Observe(e, () => { fpsSlider.Value = e.EditingCustomFX.Fps; fpsText.Text = $"{e.EditingCustomFX.Fps:0} FPS"; },
            nameof(BitchBoyEngine.EditingCustomFX));

        var loop = Dyn(e, () =>
        {
            var row = H(2);
            foreach (var m in Enum.GetValues<CustomFXLoopMode>())
            {
                var on = e.EditingCustomFX.LoopMode == m;
                row.Add(TextBtn(m.RawValue(), 6.5, on ? Black : W(0.75), on ? accent : White.Op(0.06),
                    () => { e.EditingCustomFX.LoopMode = m; e.TouchEditingFX(); }, 3, 5, 3).Fill());
            }
            return row;
        }, nameof(BitchBoyEngine.EditingCustomFX));

        return V(6, top, paint, canvas,
            V(3, ModuleChrome.HeaderText(e, "AUTHENTIC HARDWARE COLOR PALETTE", 7), swatches),
            timeline, tools,
            H(5, T("SPEED:", 6.5, W(0.6), FontWeights.Bold), fpsSlider.Fill(), fpsText),
            H(5, T("LOOP:", 6.5, W(0.6), FontWeights.Bold), loop.Fill()));
    }

    // ------------------------------------------------------------------------
    // WAVE SYNTH
    // ------------------------------------------------------------------------
    private static UIElement WaveSynth(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;

        Clickable EnumPicker<TEnum>(Func<TEnum> get, Action<TEnum> set) where TEnum : struct, Enum
        {
            var text = T("", 7, White);
            var btn = MenuBtn(H(3, text, Spacer(), Icon("chevron.down", 6.5, W(0.7))), () =>
                Enum.GetValues<TEnum>().Select(v => new MenuEntry(v.RawValue(), () => { set(v); e.TouchEditingFX(); }, get().Equals(v))),
                Black.Op(0.5), 4, 5, 3);
            btn.Observe(e, () => text.Text = get().RawValue(), nameof(BitchBoyEngine.EditingCustomFX));
            return btn;
        }

        UIElement SliderRow(string label, double min, double max, double step, Func<double> get, Action<double> set, Func<double, string> fmt, Func<Color>? color = null)
        {
            var s = new MiniSlider { Minimum = min, Maximum = max, Step = step, Accent = accent };
            var txt = T("", 7, accent);
            void Refresh()
            {
                s.Value = get();
                txt.Text = fmt(get());
                if (color != null) txt.Foreground = B(color());
            }
            s.ValueChanged += v => { set(v); txt.Text = fmt(v); if (color != null) txt.Foreground = B(color()); };
            s.Observe(e, Refresh, nameof(BitchBoyEngine.EditingCustomFX), nameof(BitchBoyEngine.LedPalette));
            return H(5, T(label, 6.5, W(0.6), FontWeights.Bold).Size(62), s.Fill(), txt.Size(46));
        }

        var fx = () => e.EditingCustomFX;
        return V(6,
            H(4, NameField(e).Fill(), PlayStop(e)),
            Box(V(5,
                ModuleChrome.HeaderText(e, "PROCEDURAL WAVE GENERATOR:", 7),
                H(5, Labeled("WAVEFORM:", EnumPicker(() => fx().WaveShape, v => fx().WaveShape = v)).Fill(),
                    Labeled("DIRECTION:", EnumPicker(() => fx().WaveDirection, v => fx().WaveDirection = v)).Fill()),
                SliderRow("DENSITY:", 0.3, 3.0, 0.1, () => fx().WaveFrequency, v => fx().WaveFrequency = v, v => $"{v:0.0}x"),
                SliderRow("SPEED:", 2, 25, 1, () => fx().Fps, v => fx().Fps = v, v => $"{v:0} FPS"),
                SliderRow("MIN COLOR:", 0, 127, 1, () => fx().MinVelocity, v => fx().MinVelocity = (int)v, v => $"VEL {v:0}", () => Leds.For(e, fx().MinVelocity)),
                SliderRow("MAX COLOR:", 0, 127, 1, () => fx().MaxVelocity, v => fx().MaxVelocity = (int)v, v => $"VEL {v:0}", () => Leds.For(e, fx().MaxVelocity))),
                Black.Op(0.35), 6, 6, 6),
            H(3,
                IconTextBtn("dot.radiowaves.left.and.right", "STREAM LIVE", 7, Black, accent, () =>
                {
                    var live = e.EditingCustomFX;
                    live.IsProcedural = true;
                    e.PlayCustomEffect(live);
                }, 4, 6, 5).Fill(),
                IconTextBtn("film.stack", "BAKE", 7, White, White.Op(0.12), e.BakeProceduralWaveToFrames, 4, 6, 5)
                    .Tip("Render the wave into editable frames").Fill(),
                IconTextBtn("square.and.arrow.down.fill", "SAVE", 7, White, White.Op(0.12), e.SaveEditingCustomFXToLibrary, 4, 6, 5).Fill()));
    }

    // ------------------------------------------------------------------------
    // MY SHOWS (library)
    // ------------------------------------------------------------------------
    private static UIElement Library(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;
        var actions = Grid(2, 4, [
            IconTextBtn("plus", "NEW BLANK SHOW", 7, Black, accent, () => { e.CreateNewBlankCustomFX(); e.RightWingTab = "SEQUENCER"; }, 4, 6, 4),
            IconTextBtn("bolt.fill", "TEST CONNECT FX", 7, Yellow, White.Op(0.1), () => e.TriggerBitchBoyConnectedNotificationFX(true), 4, 6, 4),
            IconTextBtn("square.and.arrow.up", "EXPORT", 7, White, White.Op(0.1), e.ExportCustomFXToClipboard, 4, 6, 4),
            IconTextBtn("square.and.arrow.down", "IMPORT", 7, White, White.Op(0.1), e.ImportCustomFXFromClipboard, 4, 6, 4),
        ]);

        var list = Dyn(e, () =>
        {
            var v = V(4);
            if (e.CustomEffects.Count == 0) v.Add(T("No saved shows yet.", 7, W(0.5)));
            foreach (var fx in e.CustomEffects)
            {
                var isConnect = e.ConnectCustomFXName == fx.Name;
                var running = e.IsPlayingCustomFX && e.ActiveCustomFXName == fx.Name;
                var badge = isConnect ? Box(T("ON-CONNECT", 5.8, Black), Yellow, 3, 3, 1) : null;
                var run = running
                    ? TextBtn("STOP", 6.5, White, Red.Op(0.8), e.StopCustomEffect, 3, 6, 3)
                    : TextBtn("RUN", 6.5, Black, Green, () => e.PlayCustomEffect(fx.Clone()), 3, 6, 3);
                v.Add(Box(H(5,
                    Icon(fx.IsProcedural ? "waveform" : "film", 10, accent),
                    V(1, H(4, T(fx.Name, 7.5, White), badge),
                        T(fx.IsProcedural ? $"WAVE • {fx.WaveShape.RawValue()}" : $"{fx.Frames.Count} FRAMES • {fx.Fps:0} FPS • {fx.LoopMode.RawValue()}", 6.2, W(0.55), FontWeights.Bold)),
                    Spacer(),
                    Btn(Icon("bolt.fill", 9, isConnect ? Yellow : W(0.5)), () => e.SetAsOnConnectFX(fx), White.Op(0.06), 3, 4, 3).Tip("Use as on-connect FX"),
                    run,
                    Btn(T("✏️", 9, White), () => { e.SelectCustomFXForEditing(fx); e.RightWingTab = fx.IsProcedural ? "WAVESYNTH" : "SEQUENCER"; }, White.Op(0.06), 3, 4, 3).Tip("Edit"),
                    Btn(T("🗑️", 9, White), () => e.DeleteSavedCustomFX(fx), Red.Op(0.15), 3, 4, 3).Tip("Delete")),
                    running ? Green.Op(0.1) : Black.Op(0.3), 6, 6, 5, running ? Green.Op(0.5) : White.Op(0.08)));
            }
            return v;
        }, nameof(BitchBoyEngine.CustomEffects), nameof(BitchBoyEngine.IsPlayingCustomFX), nameof(BitchBoyEngine.ActiveCustomFXName),
           nameof(BitchBoyEngine.ConnectCustomFXName));

        return V(6, actions, list);
    }
}
