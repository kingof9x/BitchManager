using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.Platform;
using static BitchManager.UI.Ui;

namespace BitchManager.UI.Modules;

/// <summary>MIDI ROUTER // OUT: virtual port, output targets, transformer, telemetry scope and macro pads.</summary>
public static class MidiRouterPanel
{
    public static UIElement Build(BitchBoyEngine e, bool spotlight)
    {
        var statusDot = Dot(6, Green);
        var statusText = T("", 7, Green);
        var status = H(3, statusDot, statusText);
        status.Observe(e, () =>
        {
            var c = e.MidiThruEnabled ? Green : Orange;
            statusDot.Fill = B(c);
            statusText.Foreground = B(c);
            statusText.Text = e.MidiThruEnabled ? "THRU ON" : "MUTED";
        }, nameof(BitchBoyEngine.MidiThruEnabled));

        var header = ModuleChrome.Header(e, StudioModuleID.MidiRouter,
            ModuleChrome.AccentText(e, Icons.For("point.3.connected.trianglepath.dotted"), 10), "MIDI ROUTER // OUT", status);
        var tabs = Dyn(e, () => H(3,
            ModuleChrome.Tab(e, "ROUTER", e.LeftWingTab == "ROUTER", () => e.LeftWingTab = "ROUTER"),
            ModuleChrome.Tab(e, "SCOPE", e.LeftWingTab == "TELEMETRY", () => e.LeftWingTab = "TELEMETRY"),
            ModuleChrome.Tab(e, "MACROS", e.LeftWingTab == "MACROS", () => e.LeftWingTab = "MACROS")),
            nameof(BitchBoyEngine.LeftWingTab), nameof(BitchBoyEngine.CurrentTheme));
        var body = Dyn(e, () => e.LeftWingTab switch
        {
            "TELEMETRY" => Scope(e),
            "MACROS" => Macros(e),
            _ => Router(e),
        }, nameof(BitchBoyEngine.LeftWingTab), nameof(BitchBoyEngine.CurrentTheme));
        return ModuleChrome.Frame(header, tabs, body, spotlight, e.Palette.Outline.Op(0.75));
    }

    // ------------------------------------------------------------------------
    // ROUTER
    // ------------------------------------------------------------------------
    private static UIElement Router(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;

        var vToggle = new SwitchToggle { Accent = Green, IsOn = e.VirtualOutEnabled };
        vToggle.Toggled += v => e.VirtualOutEnabled = v;
        vToggle.Observe(e, () => vToggle.IsOn = e.VirtualOutEnabled, nameof(BitchBoyEngine.VirtualOutEnabled));
        var vState = T("", 6.5, Green);
        vState.Observe(e, () =>
        {
            var found = e.VirtualPortsFound;
            vState.Text = found ? "loopMIDI PORT FOUND" : "loopMIDI PORT MISSING (create 'BitchBoy Virtual Out' in loopMIDI)";
            vState.Foreground = B(found ? Green : Orange);
        }, nameof(BitchBoyEngine.VirtualPortsFound));
        var virtualCard = Box(V(3,
            H(6, Icon("cable.connector", 10, accent),
                V(1, T("BITCHBOY VIRTUAL MIDI OUT", 7.5, White), T("Port Name: 'BitchBoy Virtual Out'", 6.5, W(0.6), FontWeights.Bold)),
                Spacer(), vToggle),
            vState.Wrap()), Black.Op(0.35), 6, 6, 5);

        var targets = Dyn(e, () =>
        {
            var list = V(3);
            if (e.AvailableOutputTargets.Count == 0)
                list.Add(T("No external MIDI ports found. Add a loopMIDI port or plug in a synth.", 6.5, W(0.5)).Wrap());
            foreach (var t in e.AvailableOutputTargets)
            {
                var on = e.SelectedOutputNames.Contains(t.Name);
                list.Add(Btn(H(6,
                    Icon(on ? "checkmark.square.fill" : "square", 10, on ? accent : W(0.45)),
                    V(1, T(t.Name, 7.2, White),
                        T(t.IsHardwareBitchBoy ? "Hardware Controller Port" : "External App / Synth / Bus", 6, t.IsHardwareBitchBoy ? Orange : W(0.5), FontWeights.Bold)),
                    Spacer()),
                    () => { e.ToggleOutputTarget(t.Name); SoundEffect.KnobTick.Play(e.SoundPack); },
                    on ? accent.Op(0.12) : Black.Op(0.25), 4, 6, 4, on ? accent.Op(0.4) : White.Op(0.06), 1.0));
            }
            return list;
        }, nameof(BitchBoyEngine.AvailableOutputTargets), nameof(BitchBoyEngine.SelectedOutputNames));

        var targetCard = Box(V(4,
            H(4, ModuleChrome.HeaderText(e, "SEND TO PROGRAMS / PORTS:", 7), Spacer(),
                TextBtn("ALL APPS", 6.5, accent, White.Op(0.1), e.SelectAllExternalOutputs),
                Btn(Icon("arrow.clockwise", 8, accent), e.ScanForBitchBoy, White.Op(0.1)).Tip("Rescan MIDI ports")),
            targets), Black.Op(0.35), 6, 6, 5);

        // Transformer
        var chText = T("", 7, White);
        var chPicker = MenuBtn(H(3, chText, Icon("chevron.down", 6.5, W(0.7))), () =>
            Enumerable.Range(0, 17).Select(c => new MenuEntry(c == 0 ? "ORIG (PASS-THRU)" : $"FORCE CH {c}",
                () => e.RemapChannel = c, e.RemapChannel == c)), Black.Op(0.5), 4, 5, 3);
        chPicker.Observe(e, () => chText.Text = e.RemapChannel == 0 ? "ORIG" : $"CH {e.RemapChannel}", nameof(BitchBoyEngine.RemapChannel));

        void Transpose(int d) => e.TransposeSemitones = Math.Clamp(e.TransposeSemitones + d, -24, 24);
        var trText = T("", 7.5, accent);
        trText.Observe(e, () => trText.Text = (e.TransposeSemitones > 0 ? "+" : "") + e.TransposeSemitones, nameof(BitchBoyEngine.TransposeSemitones));
        var transpose = H(2,
            TextBtn("-12", 6.5, White, White.Op(0.08), () => Transpose(-12)),
            TextBtn("−", 7, White, White.Op(0.08), () => Transpose(-1)),
            Box(trText, Black.Op(0.5), 3, 5, 2).Size(30),
            TextBtn("+", 7, White, White.Op(0.08), () => Transpose(1)),
            TextBtn("+12", 6.5, White, White.Op(0.08), () => Transpose(12)));
        trText.HorizontalAlignment = HorizontalAlignment.Center;

        var curves = Dyn(e, () =>
        {
            var row = H(2);
            foreach (var c in Enum.GetValues<MIDIVelocityCurve>())
            {
                var on = e.VelocityCurve == c;
                row.Add(TextBtn(c.RawValue(), 6.3, on ? Black : W(0.75), on ? accent : White.Op(0.06), () => e.VelocityCurve = c, 3, 4, 3).Fill());
            }
            return row;
        }, nameof(BitchBoyEngine.VelocityCurve));

        var transformer = Box(V(5,
            ModuleChrome.HeaderText(e, "MIDI TRANSFORMER:", 7),
            H(6, Labeled("OUT CHANNEL:", chPicker), Spacer(), Labeled("TRANSPOSE:", transpose)),
            Labeled("CURVE:", curves)), Black.Op(0.35), 6, 6, 5);

        UIElement Pill(string title, Func<bool> get, Action<bool> set, string prop)
        {
            var b = TextBtn(title, 6.5, White, White.Op(0.06), () => set(!get()), 10, 6, 3);
            var t = (TextBlock)b.Child;
            b.Observe(e, () =>
            {
                var on = get();
                b.Background = B(on ? accent.Op(0.22) : White.Op(0.06));
                b.BorderBrush = B(on ? accent : White.Op(0.1));
                b.BorderThickness = new Thickness(1);
                t.Foreground = B(on ? accent : W(0.5));
            }, prop);
            return b.Fill();
        }
        var filters = H(3,
            Pill("KEYS", () => e.ForwardNotes, v => e.ForwardNotes = v, nameof(BitchBoyEngine.ForwardNotes)),
            Pill("FADERS/CC", () => e.ForwardCCs, v => e.ForwardCCs = v, nameof(BitchBoyEngine.ForwardCCs)),
            Pill("UI SIM", () => e.ForwardUIInteractions, v => e.ForwardUIInteractions = v, nameof(BitchBoyEngine.ForwardUIInteractions)));

        var thru = Ui.CheckRow(() => e.MidiThruEnabled, v => e.MidiThruEnabled = v, T("MIDI THRU (ROUTE HARDWARE → PORTS)", 7, White),
            Green, e, nameof(BitchBoyEngine.MidiThruEnabled));

        var actions = H(4,
            IconTextBtn("pianokeys", "TEST CHORD", 7, Black, accent, () => { e.SendTestNoteToOutputs(); SoundEffect.KeyTap.Play(e.SoundPack); }, 4, 6, 5).Fill(),
            IconTextBtn("exclamationmark.octagon.fill", "PANIC OFF", 7, White, Red.Op(0.8), e.SendPanicAllNotesOffToOutputs, 4, 6, 5).Fill());

        var pkts = T("", 6.5, W(0.6));
        var last = T("", 7, Mint);
        var stream = Box(V(2, H(4, T("TX OUT STREAM", 6.5, W(0.6), FontWeights.Bold), Spacer(), pkts), last),
            Black.Op(0.6), 4, 6, 4, Mint.Op(0.25));
        stream.Observe(e, () => { pkts.Text = $"PKTS: {e.TxPacketCount}"; last.Text = e.LastRoutedMessage; },
            nameof(BitchBoyEngine.TxPacketCount), nameof(BitchBoyEngine.LastRoutedMessage));

        var hint = T("Windows has no built-in virtual MIDI bus: install loopMIDI (tobias-erichsen.de) and create ports named " +
                     "'BitchBoy Virtual Out' and 'BitchBoy Virtual In'.", 6.2, W(0.5), FontWeights.Bold).Wrap();

        return V(6, virtualCard, targetCard, transformer, filters, thru, actions, stream, hint);
    }

    // ------------------------------------------------------------------------
    // SCOPE (telemetry)
    // ------------------------------------------------------------------------
    private static UIElement Scope(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;

        var vfRow = H(4);
        for (var i = 49; i <= 56; i++)
        {
            var c = e.Controls[i];
            var track = new Grid { Width = 20, Height = 42, Background = B(Black.Op(0.55)) };
            var fill = new Border { Background = Gradient(accent, Mint, false), VerticalAlignment = VerticalAlignment.Bottom };
            track.Children.Add(fill);
            fill.Observe(c, () => fill.Height = 42 * c.LiveValue / 127.0, nameof(ControlItem.LiveValue));
            vfRow.Add(V(2, track, T($"F{i - 48}", 6, W(0.6)).Align(HorizontalAlignment.Center)));
        }

        var rows = V(3);
        for (var i = 57; i <= 68; i++)
        {
            var c = e.Controls[i];
            var bar = new Grid { Height = 6, Background = B(Black.Op(0.55)) };
            var fill = new Border { Background = B(Amber), HorizontalAlignment = HorizontalAlignment.Left };
            bar.Children.Add(fill);
            var val = T("", 6.5, Amber);
            bar.Observe(c, () =>
            {
                fill.Width = Math.Max(0, bar.ActualWidth) * c.LiveValue / 127.0;
                val.Text = c.LiveValue.ToString("000");
            }, nameof(ControlItem.LiveValue));
            bar.SizeChanged += (_, _) => fill.Width = bar.ActualWidth * c.LiveValue / 127.0;
            rows.Add(H(5, T(c.Label, 6.5, W(0.7)).Size(62), bar.Fill(), val));
        }

        var monitor = Dyn(e, () =>
        {
            var v = V(1);
            var hist = e.RouterPacketHistory.TakeLast(14).Reverse().ToList();
            if (hist.Count == 0) v.Add(T("NO PACKETS YET // PRESS A KEY", 6.5, W(0.45)));
            foreach (var h in hist) v.Add(T(h, 6.5, Mint));
            return v;
        }, nameof(BitchBoyEngine.RouterPacketHistory));

        return V(6,
            Box(V(4, ModuleChrome.HeaderText(e, "VERTICAL FADERS (CC 1–8):", 7), vfRow), Black.Op(0.35), 6, 6, 5),
            Box(V(4, ModuleChrome.HeaderText(e, "H-FADERS / MASTER / KNOBS:", 7), rows), Black.Op(0.35), 6, 6, 5),
            Box(V(4, ModuleChrome.HeaderText(e, "HEX PACKET MONITOR:", 7), monitor), Black.Op(0.6), 6, 6, 5, Mint.Op(0.2)));
    }

    // ------------------------------------------------------------------------
    // MACROS
    // ------------------------------------------------------------------------
    private static UIElement Macros(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;
        var bpm = T("", 14, accent, FontWeights.Black);
        bpm.Observe(e, () => bpm.Text = $"{e.TapTempoBPM:0} BPM", nameof(BitchBoyEngine.TapTempoBPM));
        var tap = Box(H(8,
            V(1, T("TAP TEMPO", 7, W(0.6), FontWeights.Bold), bpm),
            Spacer(),
            TextBtn("TAP", 9, Black, accent, () => { e.RegisterTapTempo(); SoundEffect.KeyTap.Play(e.SoundPack); }, 6, 16, 8)),
            Black.Op(0.35), 6, 8, 6);

        void Faders(int value) { for (var i = 49; i <= 60; i++) e.SetControlLiveValue(i, value); }

        var pads = new (string icon, string title, Color color, Action run)[]
        {
            ("arrow.down.to.line", "ZERO ALL FADERS", Cyan, () => Faders(0)),
            ("arrow.up.to.line", "MAX ALL FADERS", Orange, () => Faders(127)),
            ("dial.medium.fill", "CENTER 8 KNOBS", Purple, () => { for (var i = 61; i <= 68; i++) e.SetControlLiveValue(i, 64); }),
            ("sparkles", "PARTY SPARK LEDS", Yellow, () =>
            {
                var rng = new Random();
                for (var i = 0; i < 49; i++)
                {
                    e.Controls[i].IdleVelocity = HardwareColor.Rainbow[rng.Next(HardwareColor.Rainbow.Length)];
                    e.SendNoteOn(e.Controls[i].Number, e.Controls[i].IdleVelocity);
                }
                e.SaveCurrentSlot();
            }),
            ("lightbulb.fill", "ALL 49 LEDS WHITE", White, () => e.SetZoneLEDs(0, 48, 3)),
            ("moon.fill", "BLACKOUT ALL 49", W(0.6), () => e.SetZoneLEDs(0, 48, 0)),
            ("music.note.list", "SEND C-MAJ9 CHORD", Mint, e.SendTestNoteToOutputs),
            ("speaker.slash.fill", "ALL NOTES OFF", Red, e.SendPanicAllNotesOffToOutputs),
        };

        var grid = Grid(2, 5, pads.Select(p => (UIElement)Btn(V(4,
                Icon(p.icon, 14, p.color).Align(HorizontalAlignment.Center),
                T(p.title, 6.8, White).Align(HorizontalAlignment.Center)),
            () => { p.run(); SoundEffect.KeyTap.Play(e.SoundPack); }, p.color.Op(0.12), 6, 6, 9, p.color.Op(0.35))));

        return V(6, tap, ModuleChrome.HeaderText(e, "ONE-TAP MACRO PADS:", 7), grid);
    }
}
