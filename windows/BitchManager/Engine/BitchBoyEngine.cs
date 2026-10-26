using System.Windows;
using System.Windows.Threading;
using BitchManager.Core;
using BitchManager.Models;
using BitchManager.Platform;

namespace BitchManager.Engine;

/// <summary>
/// The whole app state and behaviour, ported from BitchBoyEngine in BitchBoyApp.swift.
/// Split into partial files by area: core + MIDI (this file), show control, lighting, layout.
/// </summary>
public sealed partial class BitchBoyEngine : ObservableObject, IDisposable
{
    public static readonly string[] SlotLetters = ["A", "B", "C", "D", "E", "F", "G", "H"];

    private readonly MidiService _midi = new();
    private readonly Dispatcher _ui;
    private DispatcherTimer? _watchdog;
    private DispatcherTimer? _autosave;
    private AppSettings _settings;

    // ---- Core state ----
    /// <summary>The 69 live controls. Instances stay fixed; slot changes copy values in place.</summary>
    public List<ControlItem> Controls { get; } = BuildDefault69Map();
    public List<PresetSlot> Slots { get; private set; } = BuildDefault8MemorySlots();
    public HashSet<int> SelectedIndices { get; } = [0];
    public List<string> MidiLogs { get; } = [];

    public int SelectedIndex { get => G(0); set => S(value); }
    public int ActiveSlot { get => G(0); set => S(value); }
    public bool IsConnected { get => G(false); set => S(value); }
    public string DeviceName { get => G("Unplugged"); set => S(value); }
    public string LastStreamMessage { get => G("IDLE // READY"); set => S(value); }
    public bool VirtualPortsFound { get => G(false); set => S(value); }

    // ---- Customisation ----
    public ChassisTheme CurrentTheme { get => G(ChassisTheme.PetrolTeal); set { S(value); Raise(nameof(Palette)); } }
    public SoundPack SoundPack { get => G(SoundPack.Arcade8Bit); set => S(value); }
    public LEDFXMode CurrentFXMode { get => G(LEDFXMode.StaticFX); set => S(value); }
    public LEDPaletteMode LedPalette { get => G(LEDPaletteMode.MultiVelocity); set => S(value); }
    public bool VuMeterLEDsEnabled { get => G(false); set => S(value); }
    public double FxSpeed { get => G(1.0); set => S(value); }
    public bool IsRainbowRunning { get => G(false); set => S(value); }

    // ---- Mascot & banner ----
    public double MascotScale { get => G(1.0); set => S(value); }
    public string MascotSpeech { get => G("ALL 69 HARDWARE CONTROLS LIVE!"); set => S(value); }
    public bool MascotSpeechVisible { get => G(false); set => S(value); }
    public bool ShowConnectionBanner { get => G(false); set => S(value); }
    public string ConnectionBannerTitle { get => G("🔌 BITCHBOY CONTROLLER CONNECTED & READY"); set => S(value); }
    public string ConnectionBannerSubtitle { get => G("Hardware Recognized // Playing On-Connect FX"); set => S(value); }
    public bool ConnectEffectEnabled { get => G(true); set => S(value); }
    public string ConnectCustomFXName { get => G("BITCHBOY CONNECT IGNITION"); set => S(value); }

    public ControlItem SelectedControl => Controls[Math.Clamp(SelectedIndex, 0, Controls.Count - 1)];
    public PresetSlot CurrentSlot => Slots[Math.Clamp(ActiveSlot, 0, Slots.Count - 1)];

    public string GlobalRules
    {
        get => CurrentSlot.GlobalRules;
        set { CurrentSlot.GlobalRules = value; Raise(); ScheduleSave(); }
    }

    public string AppVersion => "1.1.0";

    public BitchBoyEngine()
    {
        _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _settings = Storage.Load<AppSettings>("settings.json") ?? new AppSettings();

        LoadSlotsFromDisk();
        LoadCustomEffectsFromDisk();
        LoadLayoutAndSettings();

        foreach (var c in Controls) c.PropertyChanged += OnControlEdited;

        _midi.HardwareMessage += (s, d1, d2) => _ui.BeginInvoke(() => HandleMidiMessage(s, d1, d2));
        _midi.HardwareSysEx += b => _ui.BeginInvoke(() => HandleIncomingSysExBytes(b));
        _midi.ExternalMessage += (s, d1, d2) => _ui.BeginInvoke(() => HandleInboundExternalMidi(s, d1, d2));
        _midi.ExternalSysEx += b => _ui.BeginInvoke(() => HandleIncomingSysExBytes(b));
        _midi.Error += m => _ui.BeginInvoke(() => Log($"⚠️ {m}"));

        ScanForBitchBoy();
        _watchdog = StartTimer(2.0, ScanForBitchBoy);
    }

    // ------------------------------------------------------------------------
    // Defaults
    // ------------------------------------------------------------------------
    public static List<PresetSlot> BuildDefault8MemorySlots() =>
    [
        new() { Name = "SLOT A // PERFORMANCE", Controls = BuildDefault69Map(), IconName = "music.mic", AccentVelocity = 78 },
        new() { Name = "SLOT B // DAW PRODUCTION", Controls = BuildDefault69Map(), IconName = "slider.horizontal.3", AccentVelocity = 87 },
        new() { Name = "SLOT C // VISUALS & DMX", Controls = BuildDefault69Map(), IconName = "sparkles", AccentVelocity = 95 },
        new() { Name = "SLOT D // STREAM DECK", Controls = BuildDefault69Map(), IconName = "square.grid.3x3.fill", AccentVelocity = 74 },
        new() { Name = "SLOT E // SYNTH LEAD", Controls = BuildDefault69Map(), IconName = "pianokeys", AccentVelocity = 41 },
        new() { Name = "SLOT F // DRUM RACK", Controls = BuildDefault69Map(), IconName = "waveform.path.ecg", AccentVelocity = 72 },
        new() { Name = "SLOT G // VJ MATRIX", Controls = BuildDefault69Map(), IconName = "tv.fill", AccentVelocity = 67 },
        new() { Name = "SLOT H // MASTER CUSTOM", Controls = BuildDefault69Map(), IconName = "cpu.fill", AccentVelocity = 96 },
    ];

    public static List<ControlItem> BuildDefault69Map()
    {
        var items = new List<ControlItem>(69);
        int[][] gridNotes =
        [
            [64, 65, 66, 67, 96, 97, 98, 99],
            [60, 61, 62, 63, 92, 93, 94, 95],
            [56, 57, 58, 59, 88, 89, 90, 91],
            [52, 53, 54, 55, 84, 85, 86, 87],
        ];
        ControlItem Key(string lbl, int note, string zone) => new()
        {
            Label = lbl, CustomName = lbl, Kind = ControlKind.Key, Number = note, Zone = zone,
            IdleVelocity = 0, ActiveVelocity = 3, CurrentLED = 0,
        };
        ControlItem Analog(string lbl, ControlKind kind, int cc, string zone, int live) => new()
        {
            Label = lbl, CustomName = lbl, Kind = kind, Number = cc, Zone = zone,
            IdleVelocity = 0, ActiveVelocity = 0, LiveValue = live,
        };

        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 8; c++)
                items.Add(Key($"R{r + 1}C{c + 1}", gridNotes[r][c], $"8X4 GRID - ROW {r + 1}, COL {c + 1}"));
        int[] stripNotes = [48, 49, 50, 51, 80, 81, 82, 83];
        for (var i = 0; i < 8; i++) items.Add(Key($"S{i + 1}", stripNotes[i], $"1X8 KEY STRIP - KEY {i + 1}"));
        int[] padNotes = [44, 45, 46, 40, 41, 42, 36, 37, 38];
        for (var i = 0; i < 9; i++) items.Add(Key($"P{i + 1}", padNotes[i], $"3X3 KEYPAD - KEY {i + 1}"));
        for (var i = 0; i < 8; i++) items.Add(Analog($"VF{i + 1}", ControlKind.VFader, i, $"VERTICAL FADER {i + 1} (ANALOG CC)", i * 16));
        for (var i = 0; i < 4; i++) items.Add(Analog($"HF{i + 1}", ControlKind.HFader, 8 + i, $"HORIZONTAL FADER {i + 1} (ANALOG CC)", 32 + i * 24));
        for (var i = 0; i < 8; i++) items.Add(Analog($"K{i + 1}", ControlKind.Knob, 20 + i, $"ROTARY KNOB {i + 1} (ANALOG CC)", 40 + i * 10));
        return items;
    }

    // ------------------------------------------------------------------------
    // Timers (DispatcherTimer stands in for Combine Timer.publish / asyncAfter)
    // ------------------------------------------------------------------------
    private DispatcherTimer StartTimer(double seconds, Action tick)
    {
        var t = new DispatcherTimer(DispatcherPriority.Normal, _ui) { Interval = TimeSpan.FromSeconds(seconds) };
        t.Tick += (_, _) => tick();
        t.Start();
        return t;
    }

    private void After(double seconds, Action action)
    {
        var t = new DispatcherTimer(DispatcherPriority.Normal, _ui) { Interval = TimeSpan.FromSeconds(seconds) };
        t.Tick += (_, _) => { t.Stop(); action(); };
        t.Start();
    }

    private void Play(SoundEffect e) => e.Play(SoundPack);

    // ------------------------------------------------------------------------
    // Hardware detection (2 s watchdog, like the Mac app)
    // ------------------------------------------------------------------------
    public List<MidiOutputTarget> AvailableOutputTargets { get; private set; } = [];
    public HashSet<string> SelectedOutputNames { get; } = [];
    private bool _targetsInitialised;

    public void ScanForBitchBoy()
    {
        var inputs = MidiService.Inputs();
        var outputs = MidiService.Outputs();

        var hwIn = inputs.FirstOrDefault(p => MidiService.IsRealBitchBoy(p.Name));
        var hwOut = outputs.FirstOrDefault(p => MidiService.IsRealBitchBoy(p.Name));
        var wasConnected = IsConnected;
        var prevName = DeviceName;
        var found = _midi.SyncHardware(hwIn, hwOut);

        _midi.SyncVirtualPorts(inputs.FirstOrDefault(p => MidiService.IsVirtualIn(p.Name)),
                               outputs.FirstOrDefault(p => MidiService.IsVirtualOut(p.Name)));
        VirtualPortsFound = _midi.VirtualOutOpen;

        var targets = outputs
            .Where(p => !MidiService.IsVirtualOut(p.Name) && !MidiService.IsVirtualIn(p.Name))
            .Select(p => new MidiOutputTarget(p.Name, p.Index, MidiService.IsRealBitchBoy(p.Name)))
            .ToList();
        if (!targets.SequenceEqual(AvailableOutputTargets))
        {
            AvailableOutputTargets = targets;
            _midi.PruneTargets(targets.Select(t => t.Name));
            if (!_targetsInitialised)
                foreach (var t in targets.Where(t => !t.IsHardwareBitchBoy)) SelectedOutputNames.Add(t.Name);
            Raise(nameof(AvailableOutputTargets));
            Raise(nameof(SelectedOutputNames));
        }
        _targetsInitialised = true;

        DeviceName = found ? (hwOut?.Name ?? hwIn?.Name ?? "BitchBoy") : "Unplugged";
        IsConnected = found;

        if (IsConnected && (!wasConnected || prevName != DeviceName))
        {
            Log($"🔌 BitchBoy Hardware Recognized: {DeviceName}");
            TriggerBitchBoyConnectedNotificationFX(false);
        }
        else if (!IsConnected && wasConnected)
        {
            ShowConnectionBanner = false;
            Play(SoundEffect.HardwareDisconnected);
            Log("⚠️ BitchBoy Hardware Disconnected");
        }
    }

    public void TriggerBitchBoyConnectedNotificationFX(bool isManualTest)
    {
        if (!IsConnected && !isManualTest) return;
        Play(SoundEffect.HardwareConnected);

        var titleName = IsConnected ? DeviceName.ToUpperInvariant() : "BITCHBOY (PREVIEW)";
        ConnectionBannerTitle = $"🔌 {titleName} CONNECTED & READY";
        ConnectionBannerSubtitle = ConnectEffectEnabled
            ? $"PLAYING ON-CONNECT FX: {ConnectCustomFXName}"
            : "69-CONTROL HARDWARE MATRIX SYNCHRONIZED";
        ShowConnectionBanner = true;
        MascotSpeech = "🔌 BITCHBOY CONTROLLER ONLINE & READY!";
        MascotSpeechVisible = true;
        MascotScale = 1.18;
        After(0.3, () => MascotScale = 1.0);
        After(4.2, () => { ShowConnectionBanner = false; MascotSpeechVisible = false; });

        if (!ConnectEffectEnabled)
        {
            if (CurrentFXMode == LEDFXMode.StaticFX) RestorePresetLEDs();
            else ApplyFXMode(CurrentFXMode);
            return;
        }

        var chosen = (CustomEffects.FirstOrDefault(e => e.Name == ConnectCustomFXName) ?? CustomLightEffect.BuildDefaultConnectEffect()).Clone();
        if (!chosen.IsProcedural) chosen.LoopMode = CustomFXLoopMode.OneShot;
        PlayCustomEffect(chosen);
        Log($"✨ Playing BitchBoy Connection FX: '{chosen.Name}'");
        if (chosen.IsProcedural)
            After(2.4, () => { if (IsPlayingCustomFX && ActiveCustomFXName == chosen.Name) StopCustomEffect(); });
    }

    public void SetAsOnConnectFX(CustomLightEffect fx)
    {
        ConnectCustomFXName = fx.Name;
        ConnectEffectEnabled = true;
        SaveSettings();
        Play(SoundEffect.PresetSwitch);
        Log($"🔌 Set '{fx.Name}' as the BitchBoy On-Connect Notification FX!");
    }

    public void ToggleConnectEffectEnabled()
    {
        ConnectEffectEnabled = !ConnectEffectEnabled;
        SaveSettings();
        Play(SoundEffect.KnobTick);
        Log(ConnectEffectEnabled ? $"🔌 On-Connect FX: ENABLED ({ConnectCustomFXName})" : "🔌 On-Connect FX: DISABLED");
    }

    // ------------------------------------------------------------------------
    // Transmit to the BitchBoy (LEDs)
    // ------------------------------------------------------------------------
    public void SendNoteOn(int note, int velocity)
    {
        if (!IsConnected) return;
        _midi.SendToHardware(0x90, (byte)(note & 0x7F), (byte)(velocity & 0x7F));
        if (velocity == 0) _midi.SendToHardware(0x80, (byte)(note & 0x7F), 0);
    }

    public void SendCC(int cc, int value)
    {
        if (!IsConnected) return;
        _midi.SendToHardware(0xB0, (byte)(cc & 0x7F), (byte)(value & 0x7F));
    }

    // ------------------------------------------------------------------------
    // Inbound MIDI from the physical BitchBoy
    // ------------------------------------------------------------------------
    private int FindControl(int msgType, int data1)
    {
        var isCC = msgType == 0xB0;
        return Controls.FindIndex(c => isCC
            ? c.Kind != ControlKind.Key && c.Number == data1
            : c.Kind == ControlKind.Key && c.Number == data1);
    }

    private void HandleMidiMessage(byte status, byte data1, byte data2)
    {
        RouteIncomingMidiToTargets(status, data1, data2, true);

        var msgType = status & 0xF0;
        var isNoteOn = msgType == 0x90 && data2 > 0;
        var isNoteOff = msgType == 0x80 || (msgType == 0x90 && data2 == 0);
        var isCC = msgType == 0xB0;
        if (!isNoteOn && !isNoteOff && !isCC) return;

        var idx = FindControl(msgType, data1);
        if (idx < 0) return;
        SelectedIndex = idx;
        var c = Controls[idx];

        if (isNoteOn)
        {
            c.IsPressed = true;
            LastStreamMessage = $"[{c.Label}] ON #{data1} V:{data2}";
            Play(SoundEffect.KeyTap);
            if (HardwareFeedbackEnabled)
            {
                switch (c.FeedbackMode)
                {
                    case FeedbackMode.Momentary: c.CurrentLED = c.ActiveVelocity; break;
                    case FeedbackMode.Toggle:
                        c.ToggleLatched = !c.ToggleLatched;
                        c.CurrentLED = c.ToggleLatched ? c.ActiveVelocity : c.IdleVelocity;
                        break;
                    case FeedbackMode.StaticLED: c.CurrentLED = c.IdleVelocity; break;
                    case FeedbackMode.CustomVelocity: c.CurrentLED = c.FeedbackCustomVelocity; break;
                    case FeedbackMode.EchoInbound: c.CurrentLED = HardwareColor.ClosestHardwareVelocity(data2); break;
                }
                if (c.FeedbackMode != FeedbackMode.Disabled) SendNoteOn(data1, c.CurrentLED);
            }
            ExecuteAction(c);
            if (idx < 49) TriggerReactiveKeyFX(idx);
            SendOSCForControl(idx, true, data2);
        }
        else if (isNoteOff)
        {
            c.IsPressed = false;
            LastStreamMessage = $"[{c.Label}] OFF #{data1} V:0";
            SendOSCForControl(idx, false, 0);
            if (HardwareFeedbackEnabled && ActiveReactivePulses.Count == 0)
            {
                var restore = c.FeedbackMode switch
                {
                    FeedbackMode.Toggle => c.ToggleLatched ? c.ActiveVelocity : c.IdleVelocity,
                    FeedbackMode.Disabled => c.CurrentLED,
                    _ => c.IdleVelocity,
                };
                c.CurrentLED = restore;
                if (c.FeedbackMode != FeedbackMode.Disabled)
                    After(0.015, () => { if (ActiveReactivePulses.Count == 0) SendNoteOn(data1, restore); });
            }
        }
        else
        {
            c.LiveValue = data2;
            LastStreamMessage = $"[{c.Label}] CC #{data1} VAL:{data2}";
            if (CurrentFXMode == LEDFXMode.VuMeter && idx is >= 49 and < 57) UpdateVUMeterFaders();
            if (ArtNetMirrorHardwareFaders && idx is >= 49 and < 57)
            {
                ArtNetChannels[idx - 49] = (byte)Math.Clamp(data2 / 127.0 * 255.0 * GrandMasterLevel, 0, 255);
                SendArtNetDMX(ArtNetHost, ArtNetUniverse, ArtNetChannels);
            }
            SendOSCForControl(idx, true, data2);
        }
    }

    // ------------------------------------------------------------------------
    // Inbound from DAWs / QLab on "BitchBoy Virtual In" (feedback echo)
    // ------------------------------------------------------------------------
    private void HandleInboundExternalMidi(byte status, byte data1, byte data2)
    {
        var msgType = status & 0xF0;
        var isNoteOn = msgType == 0x90 && data2 > 0;
        var isNoteOff = msgType == 0x80 || (msgType == 0x90 && data2 == 0);
        var isCC = msgType == 0xB0;
        if (!isNoteOn && !isNoteOff && !isCC) return;
        var idx = FindControl(msgType, data1);
        if (idx < 0) return;
        var c = Controls[idx];

        if (isNoteOn)
        {
            c.IsPressed = true;
            LastStreamMessage = $"[EXT RX // {c.Label}] NOTE #{data1} VEL:{data2}";
            if (InboundMIDIEchoEnabled && idx < 49)
            {
                var echo = HardwareColor.ClosestHardwareVelocity(data2);
                c.CurrentLED = echo;
                SendNoteOn(data1, echo);
                TriggerReactiveKeyFX(idx);
            }
        }
        else if (isNoteOff)
        {
            c.IsPressed = false;
            if (InboundMIDIEchoEnabled && idx < 49 && ActiveReactivePulses.Count == 0)
            {
                c.CurrentLED = c.IdleVelocity;
                SendNoteOn(data1, c.IdleVelocity);
            }
        }
        else
        {
            c.LiveValue = data2;
            LastStreamMessage = $"[EXT RX // {c.Label}] CC #{data1} VAL:{data2}";
        }
    }

    // ------------------------------------------------------------------------
    // On-screen interaction
    // ------------------------------------------------------------------------
    public void HandleKeyPressWithModifiers(int index, bool isShiftDown)
    {
        if (PaintOnMainChassis && index < 49)
        {
            PaintCustomFXKey(index, CustomBrushVelocity);
            SelectedIndex = index;
            return;
        }
        if (isShiftDown)
        {
            if (!SelectedIndices.Remove(index)) SelectedIndices.Add(index);
        }
        else
        {
            SelectedIndices.Clear();
            SelectedIndices.Add(index);
        }
        SelectedIndex = index;
        Raise(nameof(SelectedIndices));
        SimulateKeyPress(index);
    }

    /// <summary>Selects a control in the inspector without pressing it (faders, knobs).</summary>
    public void Select(int index, bool additive = false)
    {
        if (!additive) SelectedIndices.Clear();
        SelectedIndices.Add(index);
        SelectedIndex = index;
        Raise(nameof(SelectedIndices));
    }

    public async void SimulateKeyPress(int index)
    {
        if (index < 0 || index >= Controls.Count) return;
        SelectedIndex = index;
        if (SelectedIndices.Add(index)) Raise(nameof(SelectedIndices));

        var c = Controls[index];
        c.IsPressed = true;
        var note = c.Number;
        var activeVel = c.ActiveVelocity;

        Play(SoundEffect.KeyTap);
        LastStreamMessage = $"[{c.Label}] PRESS #{note} V:{activeVel}";
        Log($"🎹 Tap: {c.Label} (Note #{note} Vel: {activeVel})");

        if (HardwareFeedbackEnabled)
        {
            switch (c.FeedbackMode)
            {
                case FeedbackMode.Momentary:
                case FeedbackMode.EchoInbound:
                    c.CurrentLED = activeVel; break;
                case FeedbackMode.Toggle:
                    c.ToggleLatched = !c.ToggleLatched;
                    c.CurrentLED = c.ToggleLatched ? activeVel : c.IdleVelocity;
                    break;
                case FeedbackMode.StaticLED: c.CurrentLED = c.IdleVelocity; break;
                case FeedbackMode.CustomVelocity: c.CurrentLED = c.FeedbackCustomVelocity; break;
            }
            if (c.FeedbackMode != FeedbackMode.Disabled) SendNoteOn(note, c.CurrentLED);
        }

        ExecuteAction(c);
        if (index < 49) TriggerReactiveKeyFX(index);
        SendOSCForControl(index, true);
        if (ForwardUIInteractions) RouteIncomingMidiToTargets(0x90, (byte)note, (byte)activeVel, false);

        await Task.Delay(120);
        c.IsPressed = false;
        SendOSCForControl(index, false);
        if (c.FeedbackMode is FeedbackMode.Momentary or FeedbackMode.CustomVelocity && ActiveReactivePulses.Count == 0)
        {
            c.CurrentLED = c.IdleVelocity;
            if (HardwareFeedbackEnabled) SendNoteOn(note, c.IdleVelocity);
            LastStreamMessage = $"[{c.Label}] RELEASE #{note} V:{c.IdleVelocity}";
        }
        if (ForwardUIInteractions) RouteIncomingMidiToTargets(0x80, (byte)note, 0, false);
    }

    public void SetControlLiveValue(int index, int value)
    {
        if (index < 0 || index >= Controls.Count) return;
        SelectedIndex = index;
        var c = Controls[index];
        value = Math.Clamp(value, 0, 127);
        if (c.LiveValue == value) return;
        c.LiveValue = value;
        SendCC(c.Number, value);
        if (ForwardUIInteractions) RouteIncomingMidiToTargets(0xB0, (byte)c.Number, (byte)value, false);
        SendOSCForControl(index, true, value);
        LastStreamMessage = $"[{c.Label}] CC #{c.Number} VAL: {value}";
        Play(SoundEffect.FaderSlide);
        if (CurrentFXMode == LEDFXMode.VuMeter && index is >= 49 and < 57) UpdateVUMeterFaders();
    }

    public void ExecuteAction(ControlItem item)
    {
        switch (item.ActionType)
        {
            case ActionType.Qwerty:
                var total = 0;
                foreach (var step in item.QwertySteps)
                {
                    total += step.DelayMs;
                    var s = step;
                    if (total > 0) After(total / 1000.0, () => FireQwerty(s));
                    else FireQwerty(s);
                }
                break;
            case ActionType.AppleScript:
                KeyboardMacro.RunScript(item.AppleScript);
                break;
            case ActionType.Msc:
                var cmd = Enum.IsDefined(typeof(MSCCommand), (byte)item.MscCommand) ? (MSCCommand)(byte)item.MscCommand : MSCCommand.Go;
                SendMSCBroadcast(cmd, item.MscCueNumber, "", MscDeviceID, MscCommandFormat);
                break;
            case ActionType.ArtNet: SendArtNetFullRig(); break;
            case ActionType.Sacn: SendSacnFullRig(); break;
        }
    }

    private void FireQwerty(QwertyStep s)
    {
        KeyboardMacro.Fire(s.Key, s.Cmd, s.Shift, s.Alt, s.Ctrl);
        Log($"⌨️ Fired QWERTY: {s.Key.Trim().ToUpperInvariant()}");
    }

    // ------------------------------------------------------------------------
    // Mascot
    // ------------------------------------------------------------------------
    private static readonly string[] MascotQuotes =
    [
        "\"DON'T SQUEEZE THE BITCHBOY!\"",
        "\"YOU BITCH!\"",
        "\"MIDI PIPES ARE PURRING SMOOTH!\"",
        "\"CRANK THAT FADER TO 127!\"",
        "\"PRESS SHIFT FOR MULTI-SELECT!\"",
        "\"BEEPS, BOOPS, AND HEAVY BASS!\"",
        "\"TAYLOR FOSSEN APPROVED! 👑\"",
        "\"STANDING BY\"",
        "\"FART\"",
        "\"MY FAVORITE WORD IS BITCH\"",
        "\"NEON GLOW AT MAXIMUM WATTAGE!\"",
    ];

    public void TriggerMascotTap()
    {
        MascotScale = 1.5;
        Play(SoundEffect.MascotSqueak);
        MascotSpeech = MascotQuotes[Random.Shared.Next(MascotQuotes.Length)];
        MascotSpeechVisible = true;
        After(0.25, () => MascotScale = 1.0);
        var shown = MascotSpeech;
        After(6.35, () => { if (MascotSpeech == shown) MascotSpeechVisible = false; });
    }

    // ------------------------------------------------------------------------
    // Memory slots
    // ------------------------------------------------------------------------
    public void SelectSlot(int idx)
    {
        if (idx < 0 || idx >= Slots.Count) return;
        SaveCurrentSlot();
        ActiveSlot = idx;
        LoadControlsFrom(Slots[idx]);
        Raise(nameof(GlobalRules));
        Raise(nameof(CurrentSlot));
        Play(SoundEffect.PresetSwitch);
        Log($"Loaded {Slots[idx].Name}");
        RunRainbowWaveRefresh();
    }

    private void LoadControlsFrom(PresetSlot slot)
    {
        _suppressAutosave = true;
        for (var i = 0; i < Controls.Count && i < slot.Controls.Count; i++) Controls[i].CopyFrom(slot.Controls[i]);
        _suppressAutosave = false;
    }

    public void SaveCurrentSlot()
    {
        var slot = CurrentSlot;
        slot.Controls = Controls.Select(c => c.Clone()).ToList();
        Storage.Save("slots.json", Slots);
    }

    public void CloneCurrentSlot() => CloneCurrentSlotTo((ActiveSlot + 1) % Math.Max(1, Slots.Count));

    public void CloneCurrentSlotTo(int target)
    {
        if (target < 0 || target >= Slots.Count) return;
        SaveCurrentSlot();
        var current = CurrentSlot;
        var letter = SlotLetters[target % 8];
        var t = Slots[target];
        t.Controls = current.Controls.Select(c => c.Clone()).ToList();
        t.GlobalRules = current.GlobalRules;
        t.IconName = current.IconName;
        t.AccentVelocity = current.AccentVelocity;
        t.Name = $"SLOT {letter} // CLONE";
        SaveCurrentSlot();
        Raise(nameof(Slots));
        Play(SoundEffect.PresetSwitch);
        Log($"📋 Cloned {current.Name} into Slot {letter}");
    }

    public void ResetCurrentSlotToDefault()
    {
        var fallback = BuildDefault8MemorySlots()[ActiveSlot % 8];
        var slot = CurrentSlot;
        slot.Controls = BuildDefault69Map();
        slot.GlobalRules = "";
        slot.Name = fallback.Name;
        slot.IconName = fallback.IconName;
        slot.AccentVelocity = fallback.AccentVelocity;
        LoadControlsFrom(slot);
        SaveCurrentSlot();
        RestorePresetLEDs();
        Raise(nameof(Slots));
        Raise(nameof(GlobalRules));
        Play(SoundEffect.PresetSwitch);
        Log($"🔄 Reset {slot.Name} to factory defaults");
    }

    public void RenameCurrentSlot(string name)
    {
        CurrentSlot.Name = name;
        Raise(nameof(Slots));
        ScheduleSave();
    }

    public void SetSlotAccent(int velocity)
    {
        CurrentSlot.AccentVelocity = velocity;
        Raise(nameof(Slots));
        ScheduleSave();
    }

    public void SetSlotIcon(string icon)
    {
        CurrentSlot.IconName = icon;
        Raise(nameof(Slots));
        ScheduleSave();
    }

    public void SyncSlotAccentToLEDs()
    {
        for (var i = 0; i < 49; i++) Controls[i].IdleVelocity = CurrentSlot.AccentVelocity;
        SaveCurrentSlot();
        RestorePresetLEDs();
        Raise(nameof(Slots));
    }

    public string ExportPresetToJson()
    {
        SaveCurrentSlot();
        var json = Storage.Serialize(CurrentSlot);
        try { Clipboard.SetText(json); } catch { }
        Log($"⤓ Exported {CurrentSlot.Name} to Clipboard JSON");
        return json;
    }

    public void ImportPresetFromClipboard()
    {
        string text;
        try { text = Clipboard.GetText(); } catch { text = ""; }
        var decoded = Storage.Deserialize<PresetSlot>(text);
        if (decoded == null || decoded.Controls.Count != 69)
        {
            Log("❌ Invalid Preset JSON in Clipboard");
            return;
        }
        Slots[ActiveSlot] = decoded;
        LoadControlsFrom(decoded);
        SaveCurrentSlot();
        RestorePresetLEDs();
        Raise(nameof(Slots));
        Raise(nameof(GlobalRules));
        Log($"⤒ Imported Preset into Slot {ActiveSlot}");
    }

    private void LoadSlotsFromDisk()
    {
        var defaults = BuildDefault8MemorySlots();
        var decoded = Storage.Load<List<PresetSlot>>("slots.json");
        if (decoded is { Count: 4 or 8 } && decoded[0].Controls.Count == 69 &&
            decoded[0].Controls[0].Number == 64 && decoded[0].Controls[4].Number == 96 &&
            decoded[0].Controls[32].Number == 48 && decoded[0].Controls[36].Number == 80 &&
            decoded[0].Controls[40].Number == 44)
        {
            if (decoded.Count == 4) decoded.AddRange(defaults.Skip(4));
            Slots = decoded;
        }
        else
        {
            Slots = defaults;
        }
        LoadControlsFrom(Slots[0]);
    }

    // Debounced autosave for inspector edits (the Mac app saves after most edits).
    private bool _suppressAutosave;
    private static readonly HashSet<string> TransientProps =
        [nameof(ControlItem.CurrentLED), nameof(ControlItem.IsPressed), nameof(ControlItem.LiveValue), nameof(ControlItem.ToggleLatched)];

    private void OnControlEdited(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressAutosave || e.PropertyName == null || TransientProps.Contains(e.PropertyName)) return;
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        _autosave ??= new DispatcherTimer(DispatcherPriority.Background, _ui) { Interval = TimeSpan.FromSeconds(1.0) };
        _autosave.Stop();
        _autosave.Tick -= AutosaveTick;
        _autosave.Tick += AutosaveTick;
        _autosave.Start();
    }

    private void AutosaveTick(object? sender, EventArgs e)
    {
        _autosave?.Stop();
        SaveCurrentSlot();
    }

    public void SetZoneLEDs(int start, int end, int velocity)
    {
        for (var i = start; i < end; i++)
        {
            var c = Controls[i];
            if (!c.HasLED) continue;
            c.IdleVelocity = velocity;
            c.CurrentLED = velocity;
            SendNoteOn(c.Number, velocity);
        }
        SaveCurrentSlot();
        Play(SoundEffect.KeyTap);
        Log($"⚡️ Applied Velocity {velocity} to Range [{start}..{end - 1}]");
    }

    /// <summary>Applies a value to every selected control (inspector multi-edit).</summary>
    public void ForEachSelected(Action<ControlItem> edit)
    {
        foreach (var i in SelectedIndices.Where(i => i >= 0 && i < Controls.Count)) edit(Controls[i]);
        if (!SelectedIndices.Contains(SelectedIndex)) edit(SelectedControl);
    }

    public void Log(string msg)
    {
        MidiLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
        if (MidiLogs.Count > 40) MidiLogs.RemoveAt(MidiLogs.Count - 1);
        Raise(nameof(MidiLogs));
    }

    public void Dispose()
    {
        _watchdog?.Stop();
        StopAllTimers();
        SaveCurrentSlot();
        SaveSettings();
        _midi.Dispose();
    }
}
