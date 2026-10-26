using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.Platform;
using static BitchManager.UI.Ui;

namespace BitchManager.UI.Modules;

/// <summary>SHOW CONTROL // OSC STUDIO: the MIDI→OSC matrix, manual OSC tester and feedback settings.</summary>
public static class ShowControlPanel
{
    public static UIElement Build(BitchBoyEngine e, bool spotlight)
    {
        var header = ModuleChrome.Header(e, StudioModuleID.ShowControl,
            ModuleChrome.AccentText(e, Icons.For("dot.radiowaves.left.and.right"), 10),
            "SHOW CONTROL // OSC STUDIO", H(3, Dot(6, Green), T("LIVE", 7, Green)));
        var tabs = Dyn(e, () => H(3,
            ModuleChrome.Tab(e, "OSC STUDIO", e.ShowControlSubTab != "FEEDBACK", () => e.ShowControlSubTab = "OSC"),
            ModuleChrome.Tab(e, "FEEDBACK", e.ShowControlSubTab == "FEEDBACK", () => e.ShowControlSubTab = "FEEDBACK")),
            nameof(BitchBoyEngine.ShowControlSubTab));
        var body = Dyn(e, () => e.ShowControlSubTab == "FEEDBACK" ? Feedback(e) : UnifiedOsc(e), nameof(BitchBoyEngine.ShowControlSubTab));
        return ModuleChrome.Frame(header, tabs, body, spotlight, e.Palette.Outline.Op(0.75));
    }

    private static UIElement UnifiedOsc(BitchBoyEngine e)
    {
        Clickable ModeBtn(string icon, string text, string mode)
        {
            var active = e.OscStudioMode == mode;
            var accent = e.Palette.Accent;
            return Btn(H(5, Icon(icon, 9, active ? Black : White), T(text, 8, active ? Black : White)),
                () => { e.OscStudioMode = mode; SoundEffect.KnobTick.Play(e.SoundPack); },
                active ? accent : White.Op(0.06), 6, 8, 5, active ? accent : White.Op(0.12));
        }

        return Dyn(e, () => V(7,
            H(4,
                ModeBtn("slider.horizontal.2.square.on.square", "KEY & FADER MAPPINGS (65)", "MAPPINGS"),
                ModeBtn("paperplane.fill", "MANUAL PACKET TESTER", "TRANSMITTER"),
                Spacer()),
            e.OscStudioMode == "TRANSMITTER" ? Transmitter(e) : Matrix(e)),
            nameof(BitchBoyEngine.OscStudioMode), nameof(BitchBoyEngine.CurrentTheme));
    }

    // ------------------------------------------------------------------------
    // Manual OSC packet tester
    // ------------------------------------------------------------------------
    private static UIElement Transmitter(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;
        string[] types = ["NONE", "FLOAT", "INT", "STRING", "BOOLEAN_TRUE", "BOOLEAN_FALSE"];
        string[] labels = ["NONE (Bang)", "FLOAT (,f)", "INT (,i)", "STRING (,s)", "TRUE (,T)", "FALSE (,F)"];

        var argLabel = T("", 7.5, White);
        var argPicker = MenuBtn(H(3, argLabel, Icon("chevron.down", 7, W(0.7))), () =>
            types.Select((t, i) => new MenuEntry(labels[i], () => e.CustomOSCArgType = t, e.CustomOSCArgType == t)), Black.Op(0.5), 4, 5, 3);
        var argValue = Labeled("ARG VALUE:", Field(() => e.CustomOSCArgValue, v => e.CustomOSCArgValue = v, 8, e, nameof(BitchBoyEngine.CustomOSCArgValue), "Value")).Fill();
        argPicker.Observe(e, () =>
        {
            argLabel.Text = labels[Math.Max(0, Array.IndexOf(types, e.CustomOSCArgType))];
            argValue.Visibility = e.CustomOSCArgType != "NONE" && !e.CustomOSCArgType.StartsWith("BOOLEAN") ? Visibility.Visible : Visibility.Collapsed;
        }, nameof(BitchBoyEngine.CustomOSCArgType));

        void Preset(string addr, int port)
        {
            e.CustomOSCAddress = addr;
            e.CustomOSCPort = port;
            e.CustomOSCArgType = "NONE";
            e.SendCustomOSC(e.CustomOSCHost, port, addr, "NONE", "");
        }

        var send = Btn(H(5, Icon("paperplane.fill", 9, Black), T("TRANSMIT OSC UDP", 8, Black)).Align(HorizontalAlignment.Center),
            () =>
            {
                e.SendCustomOSC(e.CustomOSCHost, e.CustomOSCPort, e.CustomOSCAddress, e.CustomOSCArgType, e.CustomOSCArgValue);
                SoundEffect.KeyTap.Play(e.SoundPack);
            }, accent, 6, 8, 6);

        var engineToggle = Box(Ui.CheckRow(() => e.MidiToOSCMappingActive, v => e.MidiToOSCMappingActive = v,
            V(1, T("LIVE BITCHBOY OSC ENGINE (ACTIVE)", 7.2, White), T("Keys, Faders, & Encoders broadcast mapped OSC addresses via UDP", 6.2, W(0.6), FontWeights.Bold)),
            Green, e, nameof(BitchBoyEngine.MidiToOSCMappingActive)), Black.Op(0.35), 6, 6, 5);

        return Box(V(6,
            ModuleChrome.HeaderText(e, "CUSTOM OSC (OPEN SOUND CONTROL) TRANSMITTER:", 7.5),
            H(6,
                Labeled("HOST IP:", Field(() => e.CustomOSCHost, v => e.CustomOSCHost = v, 8, e, nameof(BitchBoyEngine.CustomOSCHost))).Fill(),
                Labeled("PORT:", IntField(() => e.CustomOSCPort, v => e.CustomOSCPort = v, 8, e, nameof(BitchBoyEngine.CustomOSCPort)).Size(60))),
            Labeled("OSC ADDRESS:", Field(() => e.CustomOSCAddress, v => e.CustomOSCAddress = v, 8, e, nameof(BitchBoyEngine.CustomOSCAddress))),
            H(6, Labeled("ARG TYPE:", argPicker), argValue),
            send,
            Box(V(3,
                T("QUICK APPLICATION PRESETS:", 6.8, W(0.65), FontWeights.Bold),
                H(3,
                    TextBtn("QLAB GO", 7, accent, White.Op(0.1), () => Preset("/cue/selected/start", 53000)).Fill(),
                    TextBtn("QLAB PANIC", 7, accent, White.Op(0.1), () => Preset("/panic", 53000)).Fill(),
                    TextBtn("EOS GO", 7, accent, White.Op(0.1), () => Preset("/eos/key/go_0", 8000)).Fill())),
                Black.Op(0.35), 6, 6, 5),
            engineToggle), Black.Op(0.45), 8, 8, 8);
    }

    // ------------------------------------------------------------------------
    // MIDI → OSC mapping matrix (screenshot 1)
    // ------------------------------------------------------------------------
    private static UIElement Matrix(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;
        var toggle = new SwitchToggle { Accent = accent, IsOn = e.MidiToOSCMappingActive };
        toggle.Toggled += v => e.MidiToOSCMappingActive = v;
        toggle.Observe(e, () => toggle.IsOn = e.MidiToOSCMappingActive, nameof(BitchBoyEngine.MidiToOSCMappingActive));

        var title = Box(H(6,
            Icon("slider.horizontal.2.square.on.square", 11, accent),
            V(1, ModuleChrome.HeaderText(e, "MIDI ➔ OSC MAPPING MATRIX", 8), T("Map any button or fader to send custom OSC messages", 6.5, W(0.6), FontWeights.Bold)),
            Spacer(), toggle), White.Op(0.04), 6, 6, 5);

        var templates = V(3,
            T("QUICK 1-CLICK TEMPLATES:", 6.5, W(0.55), FontWeights.Bold),
            H(3,
                TextBtn("⚡ AUTO-MAP", 7, accent, White.Op(0.1), e.ApplyOSCAutoMapTemplate).Fill(),
                TextBtn("🎭 QLAB", 7, Yellow, White.Op(0.1), e.ApplyOSCQLabTemplate).Fill(),
                TextBtn("🎬 RESOLUME", 7, Cyan, White.Op(0.1), e.ApplyOSCResolumeTemplate).Fill(),
                TextBtn("💡 ETC EOS", 7, Orange, White.Op(0.1), e.ApplyOSCEosTemplate).Fill(),
                TextBtn("🧹 CLEAR", 7, Red, Red.Op(0.2), e.ClearAllOSCMappings).Fill()));

        var globals = H(6,
            Labeled("GLOBAL TARGET IP:", Field(() => e.CustomOSCHost, v => e.CustomOSCHost = v, 7.5, e, nameof(BitchBoyEngine.CustomOSCHost)), 6).Fill(),
            Labeled("PORT:", IntField(() => e.CustomOSCPort, v => e.CustomOSCPort = v, 7.5, e, nameof(BitchBoyEngine.CustomOSCPort)).Size(52), 6),
            Labeled("SEARCH / FILTER:", Field(() => e.MidiToOSCSearchText, v => e.MidiToOSCSearchText = v, 7.5, e, nameof(BitchBoyEngine.MidiToOSCSearchText), "Search key/fader..."), 6).Fill());

        var filters = Dyn(e, () =>
        {
            Clickable F(string title, string key)
            {
                var on = e.MidiToOSCFilter == key;
                return TextBtn(title, 6.5, on ? Black : W(0.7), on ? accent : White.Op(0.06),
                    () => { e.MidiToOSCFilter = key; SoundEffect.KnobTick.Play(e.SoundPack); }, 4, 4, 3).Fill();
            }
            return H(2, F("ALL", "ALL"), F("GRID", "GRID"), F("STRIP", "STRIP"), F("PAD", "PAD"),
                F("V-FADERS", "VFADERS"), F("H-FADERS", "HFADERS"), F("MASTER/KNOBS", "MASTER_KNOBS"));
        }, nameof(BitchBoyEngine.MidiToOSCFilter));

        var list = Dyn(e, () =>
        {
            var idx = e.FilteredControlIndices();
            if (idx.Count == 0)
                return V(4, Icon("magnifyingglass", 14, W(0.4)), T("No matching controls", 7.5, W(0.5)).Align(HorizontalAlignment.Center)).Margin(0, 12, 0, 12);
            var v = V(4);
            foreach (var i in idx) v.Add(MappingCard(e, i));
            return v;
        }, nameof(BitchBoyEngine.MidiToOSCFilter), nameof(BitchBoyEngine.MidiToOSCSearchText));

        return Box(V(7, title, templates, globals, filters, list), Black.Op(0.45), 8, 8, 8);
    }

    private static UIElement MappingCard(BitchBoyEngine e, int index)
    {
        var item = e.Controls[index];
        var accent = e.Palette.Accent;
        var isKey = item.Kind == ControlKind.Key;
        var isFader = item.Kind is ControlKind.VFader or ControlKind.HFader;

        var check = Icon("square", 10, W(0.4));
        var kindText = T(isKey ? $"KEY #{item.Number}" : isFader ? $"FADER CC{item.Number}" : $"KNOB CC{item.Number}", 7, W(0.6));
        var kindChip = Box(kindText, White.Op(0.06), 3, 4, 1.5);
        var checkBtn = Btn(H(5, check, kindChip), () => { item.OscEnabled = !item.OscEnabled; SoundEffect.KnobTick.Play(e.SoundPack); }, Clear, 3, 0, 0, hover: 1.0);

        UIElement live;
        if (isKey)
        {
            var pressed = Box(T("PRESSED", 6, Black), Green, 3, 3, 1);
            pressed.Observe(item, () => pressed.Visibility = item.IsPressed ? Visibility.Visible : Visibility.Collapsed, nameof(ControlItem.IsPressed));
            live = pressed;
        }
        else
        {
            var val = T("", 6.5, accent);
            val.Observe(item, () => val.Text = item.LiveValue.ToString(), nameof(ControlItem.LiveValue));
            live = val;
        }

        var test = Btn(H(2, Icon("paperplane.fill", 6.5, accent), T("TEST", 6.5, accent)),
            () => { e.SendOSCForControl(index, true); SoundEffect.KeyTap.Play(e.SoundPack); }, White.Op(0.12), 4, 5, 2);

        var row1 = H(5, checkBtn, T(item.Label, 7.5, White), live, Spacer(), test);

        var row2 = H(5,
            Labeled("CUSTOM ALIAS:", Field(() => item.CustomName, v => item.CustomName = v, 7, item, nameof(ControlItem.CustomName), item.Label).Size(80), 5.5),
            Labeled("OSC ADDRESS PATH:", Field(() => item.OscAddress, v => item.OscAddress = v, 7, item, nameof(ControlItem.OscAddress),
                isKey ? $"/bitchboy/key/{index + 1}" : $"/bitchboy/fader/{Math.Max(1, index - 48)}"), 5.5).Fill());

        var typeText = T("", 7, White);
        var typeOptions = isKey
            ? new[] { ("FLOAT", "FLOAT"), ("INT", "INT"), ("BANG / NONE", "NONE"), ("STRING", "STRING"), ("BOOL", "BOOLEAN") }
            : new[] { ("FLOAT", "FLOAT"), ("INT", "INT"), ("BANG / NONE", "NONE"), ("STRING", "STRING") };
        var typePicker = MenuBtn(H(3, typeText, Spacer(), Icon("chevron.down", 6.5, W(0.7))), () =>
            typeOptions.Select(o => new MenuEntry(o.Item1, () => item.OscArgType = o.Item2, item.OscArgType == o.Item2)), Black.Op(0.4), 4, 4, 2);
        typePicker.Width = 85;
        typePicker.Observe(item, () => typeText.Text = typeOptions.FirstOrDefault(o => o.Item2 == item.OscArgType).Item1 ?? item.OscArgType, nameof(ControlItem.OscArgType));

        var row3 = H(5, Labeled("ARG TYPE:", typePicker, 5.5));
        if (isKey)
        {
            var relIcon = Icon("circle", 8, W(0.4));
            var relField = Field(() => item.OscReleaseVal, v => item.OscReleaseVal = v, 7, item, nameof(ControlItem.OscReleaseVal), "0.0").Size(32);
            var relBtn = Btn(relIcon, () => item.OscSendRelease = !item.OscSendRelease, Clear, 3, 0, 0);
            relBtn.Observe(item, () =>
            {
                relIcon.Text = Icons.For(item.OscSendRelease ? "checkmark.circle.fill" : "circle");
                relIcon.Foreground = B(item.OscSendRelease ? Green : W(0.4));
                relField.Visibility = item.OscSendRelease ? Visibility.Visible : Visibility.Collapsed;
            }, nameof(ControlItem.OscSendRelease));
            row3.Add(Labeled("ON RELEASE:", H(3, relBtn, relField), 5.5));
        }
        else
        {
            row3.Add(Labeled("MIN VAL:", DoubleField(() => item.OscMinVal, v => item.OscMinVal = v, 7, item, nameof(ControlItem.OscMinVal)).Size(44), 5.5));
            row3.Add(Labeled("MAX VAL:", DoubleField(() => item.OscMaxVal, v => item.OscMaxVal = v, 7, item, nameof(ControlItem.OscMaxVal)).Size(44), 5.5));
        }
        row3.Add(Spacer());

        var card = Box(V(4, row1, row2, row3), Black.Op(0.2), 6, 6, 5, White.Op(0.08));
        card.Observe(item, () =>
        {
            var on = item.OscEnabled;
            check.Text = Icons.For(on ? "checkmark.square.fill" : "square");
            check.Foreground = B(on ? accent : W(0.4));
            kindChip.Background = B(on ? accent.Op(0.18) : White.Op(0.06));
            kindText.Foreground = B(on ? accent : W(0.6));
            card.Background = B(on ? White.Op(0.04) : Black.Op(0.2));
            card.BorderBrush = B(on ? accent.Op(0.35) : White.Op(0.08));
        }, nameof(ControlItem.OscEnabled));
        return card;
    }

    // ------------------------------------------------------------------------
    // Feedback
    // ------------------------------------------------------------------------
    private static UIElement Feedback(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;
        UIElement Check(Func<bool> get, Action<bool> set, string title, string sub, string prop) =>
            Box(Ui.CheckRow(get, set, V(1, T(title, 7.5, White), T(sub, 6.8, W(0.6), FontWeights.Bold).Wrap()), accent, e, prop), Black.Op(0.35), 6, 6, 5);

        var selected = T("", 7, accent);
        var modeText = T("", 7.5, White);
        var modePicker = MenuBtn(H(3, modeText, Spacer(), Icon("chevron.down", 7, W(0.7))), () =>
            Enum.GetValues<FeedbackMode>().Select(m => new MenuEntry(m.RawValue(), () =>
            {
                e.ForEachSelected(c => c.FeedbackMode = m);
                e.SaveCurrentSlot();
            }, e.SelectedControl.FeedbackMode == m)), Black.Op(0.5), 4, 5, 3).Fill();
        var modeRow = H(4, T("MODE:", 6.8, W(0.6), FontWeights.Bold), modePicker);
        modeRow.Observe(e, () =>
        {
            selected.Text = $"Selected Control: {e.SelectedControl.Label}";
            modeText.Text = e.SelectedControl.FeedbackMode.RawValue();
        }, nameof(BitchBoyEngine.SelectedIndex));
        foreach (var c in e.Controls)
            modeRow.Observe(c, () => modeText.Text = e.SelectedControl.FeedbackMode.RawValue(), nameof(ControlItem.FeedbackMode));

        return Box(V(6,
            ModuleChrome.HeaderText(e, "CUSTOMIZABLE HARDWARE MIDI FEEDBACK:", 7.5),
            Check(() => e.HardwareFeedbackEnabled, v => e.HardwareFeedbackEnabled = v, "LOCAL HARDWARE LED FEEDBACK",
                "When ON, pressing keys immediately updates BitchBoy LEDs locally based on feedback mode.", nameof(BitchBoyEngine.HardwareFeedbackEnabled)),
            Check(() => e.InboundMIDIEchoEnabled, v => e.InboundMIDIEchoEnabled = v, "DAW / INBOUND NOTE HARDWARE ECHO",
                "When ON, incoming MIDI notes from QLab or DAWs light up the hardware key LEDs.", nameof(BitchBoyEngine.InboundMIDIEchoEnabled)),
            selected,
            modeRow), Black.Op(0.45), 8, 8, 8);
    }
}
