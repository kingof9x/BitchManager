using System.Globalization;
using System.Text;
using BitchManager.Models;
using BitchManager.Protocols;

namespace BitchManager.Engine;

/// <summary>MSC, OSC, Art-Net, sACN, MIDI→OSC mapping and the MIDI router.</summary>
public sealed partial class BitchBoyEngine
{
    // ---- Router state ----
    public string LeftWingTab { get => G("ROUTER"); set => S(value); }
    public bool MidiThruEnabled { get => G(true); set => S(value); }
    public bool VirtualOutEnabled { get => G(true); set => S(value); }
    public bool ForwardUIInteractions { get => G(true); set => S(value); }
    public bool ForwardNotes { get => G(true); set => S(value); }
    public bool ForwardCCs { get => G(true); set => S(value); }
    public int RemapChannel { get => G(0); set => S(value); }
    public int TransposeSemitones { get => G(0); set => S(value); }
    public MIDIVelocityCurve VelocityCurve { get => G(MIDIVelocityCurve.Linear); set => S(value); }
    public int TxPacketCount { get => G(0); set => S(value); }
    public string LastRoutedMessage { get => G("READY // NO TX YET"); set => S(value); }
    public List<string> RouterPacketHistory { get; } = [];
    public double TapTempoBPM { get => G(120.0); set => S(value); }
    private readonly List<DateTime> _lastTapTimes = [];

    // ---- MSC ----
    public bool MscBroadcastEnabled { get => G(true); set => S(value); }
    public int MscDeviceID { get => G(1); set => S(value); }
    public MSCCommandFormat MscCommandFormat { get => G(MSCCommandFormat.AllTypes); set => S(value); }
    public string MscCueNumber { get => G("1.0"); set => S(value); }
    public string MscCueList { get => G(""); set => S(value); }
    public bool MscAutoTriggerOnInbound { get => G(true); set => S(value); }
    public List<MSCMessage> MscLogHistory { get; } = [];
    private List<byte> _inboundSysEx = [];

    // ---- Custom OSC transmitter ----
    public string CustomOSCHost { get => G("127.0.0.1"); set => S(value); }
    public int CustomOSCPort { get => G(53000); set => S(value); }
    public string CustomOSCAddress { get => G("/cue/selected/start"); set => S(value); }
    public string CustomOSCArgType { get => G("NONE"); set => S(value); }
    public string CustomOSCArgValue { get => G("1.0"); set => S(value); }

    // ---- Art-Net / sACN ----
    public string ArtNetHost { get => G("127.0.0.1"); set => S(value); }
    public int ArtNetPort { get => G(6454); set => S(value); }
    public int ArtNetUniverse { get => G(0); set => S(value); }
    public byte[] ArtNetChannels { get; } = new byte[512];
    private byte _artNetSequence;
    public string SacnHost { get => G(""); set => S(value); }
    public int SacnPort { get => G(5568); set => S(value); }
    public int SacnUniverse { get => G(1); set => S(value); }
    public int SacnPriority { get => G(100); set => S(value); }
    private byte _sacnSequence;

    // ---- Feedback & rig ----
    public bool HardwareFeedbackEnabled { get => G(true); set => S(value); }
    public bool InboundMIDIEchoEnabled { get => G(true); set => S(value); }
    public string ShowControlSubTab { get => G("OSC"); set => S(value); }
    public string OscStudioMode { get => G("MAPPINGS"); set => S(value); }
    public bool ArtNetMirrorHardwareFaders { get => G(false); set => S(value); }
    public double GrandMasterLevel { get => G(1.0); set => S(value); }
    public bool IsBlackoutActive { get => G(false); set => S(value); }

    // ---- MIDI → OSC matrix ----
    public bool MidiToOSCMappingActive { get => G(true); set => S(value); }
    public string MidiToOSCFilter { get => G("ALL"); set => S(value); }
    public string MidiToOSCSearchText { get => G(""); set => S(value); }

    // ------------------------------------------------------------------------
    // MSC
    // ------------------------------------------------------------------------
    public void SendMSCBroadcast(MSCCommand command, string cue = "", string cueList = "", int deviceID = 1,
        MSCCommandFormat format = MSCCommandFormat.AllTypes)
    {
        var bytes = new List<byte> { 0xF0, 0x7F, (byte)deviceID, 0x02, (byte)format, (byte)command };
        if (cue.Length > 0) bytes.AddRange(Encoding.UTF8.GetBytes(cue));
        if (cueList.Length > 0)
        {
            bytes.Add(0x00);
            bytes.AddRange(Encoding.UTF8.GetBytes(cueList));
        }
        bytes.Add(0xF7);
        var raw = bytes.ToArray();

        if (IsConnected) _midi.SendSysExToHardware(raw);
        if (VirtualOutEnabled) _midi.SendSysExToVirtualOut(raw);
        foreach (var t in AvailableOutputTargets.Where(t => SelectedOutputNames.Contains(t.Name) && t.Name != _midi.HardwareOutName))
            _midi.SendSysExToTarget(new(t.DeviceIndex, t.Name), raw);

        var msg = new MSCMessage
        {
            IsOutbound = true, DeviceID = deviceID, CommandFormat = format, Command = command,
            CueNumber = cue, CueList = cueList, RawBytes = raw,
        };
        AddMscLog(msg);
        Log($"📡 TX MSC: {msg.Summary}");
        Play(SoundEffect.PresetSwitch);
    }

    private void AddMscLog(MSCMessage msg)
    {
        MscLogHistory.Insert(0, msg);
        if (MscLogHistory.Count > 50) MscLogHistory.RemoveAt(MscLogHistory.Count - 1);
        Raise(nameof(MscLogHistory));
    }

    public void ClearMSCLogHistory()
    {
        MscLogHistory.Clear();
        Raise(nameof(MscLogHistory));
    }

    private void HandleIncomingSysExBytes(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            if (b == 0xF0) _inboundSysEx = [0xF0];
            else if (b == 0xF7)
            {
                if (_inboundSysEx.Count > 0 && _inboundSysEx[0] == 0xF0)
                {
                    _inboundSysEx.Add(0xF7);
                    ProcessCompletedSysEx(_inboundSysEx.ToArray());
                    _inboundSysEx.Clear();
                }
            }
            else if (_inboundSysEx.Count > 0)
            {
                _inboundSysEx.Add(b);
                if (_inboundSysEx.Count > 512) _inboundSysEx.Clear();
            }
        }
    }

    private void ProcessCompletedSysEx(byte[] bytes)
    {
        if (bytes.Length < 7 || bytes[0] != 0xF0 || bytes[1] != 0x7F || bytes[3] != 0x02 || bytes[^1] != 0xF7) return;
        var command = Enum.IsDefined(typeof(MSCCommand), bytes[5]) ? (MSCCommand)bytes[5] : MSCCommand.Go;
        var format = Enum.IsDefined(typeof(MSCCommandFormat), bytes[4]) ? (MSCCommandFormat)bytes[4] : MSCCommandFormat.AllTypes;

        string cue = "", list = "";
        if (bytes.Length > 7)
        {
            var data = bytes[6..^1];
            var split = Array.IndexOf(data, (byte)0);
            cue = Encoding.UTF8.GetString(split < 0 ? data : data[..split]);
            if (split >= 0)
            {
                var rest = data[(split + 1)..];
                var end = Array.IndexOf(rest, (byte)0);
                list = Encoding.UTF8.GetString(end < 0 ? rest : rest[..end]);
            }
        }

        var msg = new MSCMessage
        {
            IsOutbound = false, DeviceID = bytes[2], CommandFormat = format, Command = command,
            CueNumber = cue, CueList = list, RawBytes = bytes,
        };
        AddMscLog(msg);
        Log($"📥 RX MSC: {msg.Summary}");
        Play(SoundEffect.KeyTap);
        if (MscAutoTriggerOnInbound) HandleMSCAutoTrigger(command, cue);
    }

    private void HandleMSCAutoTrigger(MSCCommand command, string cue)
    {
        switch (command)
        {
            case MSCCommand.Go or MSCCommand.Resume or MSCCommand.TimedGo:
                SelectSlot((ActiveSlot + 1) % Slots.Count);
                TriggerRandomDemoReactiveFX();
                Log($"🎬 MSC Auto-Trigger: GO -> Preset Slot {CurrentSlot.Name}");
                break;
            case MSCCommand.Stop or MSCCommand.AllOff or MSCCommand.Reset or MSCCommand.GoOff:
                SetZoneLEDs(0, 49, 0);
                CurrentFXMode = LEDFXMode.StaticFX;
                StopFxTimer();
                ActiveReactivePulses.Clear();
                Log("🛑 MSC Auto-Trigger: STOP / PANIC Blackout");
                break;
            case MSCCommand.Load:
                if (int.TryParse(cue, out var n) && n >= 1 && n <= Slots.Count)
                {
                    SelectSlot(n - 1);
                    Log($"📥 MSC Auto-Trigger: LOAD -> Preset Slot {n}");
                }
                break;
            case MSCCommand.Fire:
                TriggerBitchBoyConnectedNotificationFX(true);
                Log("🔥 MSC Auto-Trigger: FIRE MACRO Ignition");
                break;
        }
    }

    // ------------------------------------------------------------------------
    // Art-Net 4 / sACN E1.31
    // ------------------------------------------------------------------------
    public async void SendArtNetDMX(string host, int universe, byte[] channels)
    {
        var packet = ArtNetPacketBuilder.BuildDmxPacket(universe, channels, _artNetSequence);
        _artNetSequence = unchecked((byte)(_artNetSequence + 1));
        var port = ArtNetPort;
        var err = await Udp.SendAsync(host, port, packet);
        Log(err != null ? $"❌ Art-Net send error: {err}" : $"💡 Art-Net DMX -> {host}:{port} Univ:{universe} (512 Ch)");
    }

    public void SendArtNetFullRig() => SendArtNetDMX(ArtNetHost, ArtNetUniverse, Enumerable.Repeat((byte)255, 512).ToArray());
    public void SendArtNetBlackout() => SendArtNetDMX(ArtNetHost, ArtNetUniverse, new byte[512]);

    public void SendArtNetRGBChase()
    {
        var chs = new byte[512];
        for (var i = 0; i < 16; i++) { chs[i * 3] = 255; chs[i * 3 + 1] = 128; chs[i * 3 + 2] = 0; }
        SendArtNetDMX(ArtNetHost, ArtNetUniverse, chs);
    }

    public async void SendSacnDMX(string host, int universe, int priority, byte[] channels)
    {
        var packet = SacnPacketBuilder.BuildDmxPacket(universe, (byte)Math.Clamp(priority, 0, 200), channels, _sacnSequence);
        _sacnSequence = unchecked((byte)(_sacnSequence + 1));
        var trimmed = host.Trim();
        var target = trimmed.Length == 0 ? $"239.255.{(universe >> 8) & 0xFF}.{universe & 0xFF}" : trimmed;
        var port = SacnPort;
        var err = await Udp.SendAsync(target, port, packet);
        Log(err != null ? $"❌ sACN send error: {err}" : $"🌟 sACN E1.31 -> {target}:{port} Univ:{universe} Pri:{priority} (512 Ch)");
    }

    public void SendSacnFullRig() => SendSacnDMX(SacnHost, SacnUniverse, SacnPriority, Enumerable.Repeat((byte)255, 512).ToArray());
    public void SendSacnBlackout() => SendSacnDMX(SacnHost, SacnUniverse, SacnPriority, new byte[512]);

    // ------------------------------------------------------------------------
    // OSC
    // ------------------------------------------------------------------------
    public async void SendCustomOSC(string host, int port, string address, string argType, string argValue)
    {
        var packet = OscPacketBuilder.BuildMessage(address, argType, argValue);
        var err = await Udp.SendAsync(host, port, packet);
        if (err != null) Log($"❌ OSC send error: {err}");
        else Log($"📡 OSC UDP -> {host}:{port} {address}{(argType == "NONE" ? "" : $" [{argType}: {argValue}]")}");
    }

    public List<int> FilteredControlIndices()
    {
        IEnumerable<int> idx = Enumerable.Range(0, Controls.Count);
        idx = MidiToOSCFilter switch
        {
            "GRID" => idx.Where(i => i < 32),
            "STRIP" => idx.Where(i => i is >= 32 and < 40),
            "PAD" => idx.Where(i => i is >= 40 and < 49),
            "VFADERS" => idx.Where(i => i is >= 49 and < 57),
            "HFADERS" => idx.Where(i => i is >= 57 and < 61),
            "MASTER_KNOBS" => idx.Where(i => i >= 61),
            _ => idx,
        };
        var q = MidiToOSCSearchText.Trim().ToLowerInvariant();
        if (q.Length > 0)
        {
            idx = idx.Where(i =>
            {
                var c = Controls[i];
                return c.Label.ToLowerInvariant().Contains(q) || c.CustomName.ToLowerInvariant().Contains(q) ||
                       c.Zone.ToLowerInvariant().Contains(q) || c.OscAddress.ToLowerInvariant().Contains(q) ||
                       c.Number.ToString().Contains(q);
            });
        }
        return idx.ToList();
    }

    public void SendOSCForControl(int index, bool isPress = true, int? liveVal = null)
    {
        if (!MidiToOSCMappingActive || index < 0 || index >= Controls.Count) return;
        var c = Controls[index];
        if (!c.OscEnabled && c.ActionType != ActionType.Osc) return;

        var host = string.IsNullOrWhiteSpace(c.OscHost) ? CustomOSCHost : c.OscHost.Trim();
        var port = c.OscPort == 0 ? CustomOSCPort : c.OscPort;
        var addr = c.OscAddress.Trim();
        var analog = c.Kind != ControlKind.Key;
        if (addr.Length == 0) addr = analog ? $"/bitchboy/fader/{Math.Max(1, index - 48)}" : $"/bitchboy/key/{index + 1}";
        if (!addr.StartsWith('/')) addr = "/" + addr;

        if (isPress)
        {
            if (analog)
            {
                var norm = (liveVal ?? c.LiveValue) / 127.0;
                var scaled = c.OscMinVal + norm * (c.OscMaxVal - c.OscMinVal);
                switch (c.OscArgType)
                {
                    case "INT": SendCustomOSC(host, port, addr, "INT", ((int)Math.Round(scaled)).ToString(CultureInfo.InvariantCulture)); break;
                    case "NONE": SendCustomOSC(host, port, addr, "NONE", ""); break;
                    case "STRING": SendCustomOSC(host, port, addr, "STRING", c.OscArgValue); break;
                    default: SendCustomOSC(host, port, addr, "FLOAT", scaled.ToString("F3", CultureInfo.InvariantCulture)); break;
                }
            }
            else
            {
                switch (c.OscArgType)
                {
                    case "BOOLEAN": SendCustomOSC(host, port, addr, "BOOLEAN_TRUE", "1"); break;
                    case "INT":
                        var iv = string.IsNullOrWhiteSpace(c.OscArgValue) ? (liveVal ?? c.ActiveVelocity).ToString() : c.OscArgValue;
                        SendCustomOSC(host, port, addr, "INT", iv);
                        break;
                    case "FLOAT": SendCustomOSC(host, port, addr, "FLOAT", string.IsNullOrWhiteSpace(c.OscArgValue) ? "1.0" : c.OscArgValue); break;
                    case "STRING": SendCustomOSC(host, port, addr, "STRING", c.OscArgValue); break;
                    default: SendCustomOSC(host, port, addr, "NONE", ""); break;
                }
            }
        }
        else if (c.OscSendRelease)
        {
            var rel = c.OscReleaseVal.Trim();
            switch (c.OscArgType)
            {
                case "BOOLEAN": SendCustomOSC(host, port, addr, "BOOLEAN_FALSE", "0"); break;
                case "INT": SendCustomOSC(host, port, addr, "INT", rel.Length == 0 ? "0" : rel); break;
                case "FLOAT": SendCustomOSC(host, port, addr, "FLOAT", rel.Length == 0 ? "0.0" : rel); break;
            }
        }
    }

    private void ForEachControl(Action<int, ControlItem> edit)
    {
        for (var i = 0; i < Controls.Count; i++) edit(i, Controls[i]);
    }

    public void ApplyOSCAutoMapTemplate()
    {
        ForEachControl((i, c) =>
        {
            c.OscEnabled = true; c.OscHost = CustomOSCHost; c.OscPort = CustomOSCPort;
            if (i < 49)
            {
                c.OscAddress = $"/bitchboy/key/{i + 1}"; c.OscArgType = "FLOAT"; c.OscArgValue = "1.0";
                c.OscSendRelease = true; c.OscReleaseVal = "0.0";
            }
            else
            {
                c.OscAddress = i < 57 ? $"/bitchboy/fader/{i - 48}" : i < 61 ? $"/bitchboy/fader/h/{i - 56}" : $"/bitchboy/knob/{i - 60}";
                c.OscArgType = "FLOAT"; c.OscMinVal = 0; c.OscMaxVal = 1;
            }
        });
        Log("⚡ OSC Auto-Map Template Applied: All 65 controls configured with standard /bitchboy endpoints");
        Play(SoundEffect.PresetSwitch);
    }

    public void ApplyOSCQLabTemplate()
    {
        CustomOSCPort = 53000;
        string[] transport = ["QLab GO|/go", "Panic All|/panic", "Hard Stop|/hardStop", "Pause|/pause", "Resume|/resume", "Prev Cue|/previous", "Next Cue|/next", "Reset|/reset"];
        ForEachControl((i, c) =>
        {
            c.OscEnabled = true; c.OscHost = CustomOSCHost; c.OscPort = 53000;
            if (i < 32) { c.CustomName = $"Cue {i + 1}"; c.OscAddress = $"/cue/{i + 1}/start"; c.OscArgType = "NONE"; c.OscSendRelease = false; }
            else if (i < 40) { var p = transport[i - 32].Split('|'); c.CustomName = p[0]; c.OscAddress = p[1]; c.OscArgType = "NONE"; }
            else if (i < 49) { c.CustomName = $"Cue Pad {i - 39}"; c.OscAddress = $"/cue/{i - 39}/start"; c.OscArgType = "NONE"; }
            else if (i < 57) { c.CustomName = $"Cue {i - 48} Vol"; c.OscAddress = $"/cue/{i - 48}/sliderLevel/1"; c.OscArgType = "FLOAT"; c.OscMinVal = -60; c.OscMaxVal = 12; }
            else if (i < 61) { c.CustomName = $"Aux {i - 56}"; c.OscAddress = $"/cue/selected/level/{i - 56}"; c.OscArgType = "FLOAT"; c.OscMinVal = 0; c.OscMaxVal = 1; }
            else { c.CustomName = "Master Level"; c.OscAddress = "/masterVolume"; c.OscArgType = "FLOAT"; c.OscMinVal = 0; c.OscMaxVal = 1; }
        });
        Log("🎭 QLab OSC Template Applied: Port 53000, 32 Cues, Transport Keys & 8 Volume Faders");
        Play(SoundEffect.PresetSwitch);
    }

    public void ApplyOSCResolumeTemplate()
    {
        CustomOSCPort = 7000;
        ForEachControl((i, c) =>
        {
            c.OscEnabled = true; c.OscHost = CustomOSCHost; c.OscPort = 7000;
            if (i < 32)
            {
                var layer = i % 4 + 1; var clip = i / 4 + 1;
                c.CustomName = $"L{layer} C{clip}"; c.OscAddress = $"/composition/layers/{layer}/clips/{clip}/connect"; c.OscArgType = "INT"; c.OscArgValue = "1";
            }
            else if (i is >= 49 and < 57) { c.CustomName = $"Layer {i - 48} Opacity"; c.OscAddress = $"/composition/layers/{i - 48}/video/opacity"; c.OscArgType = "FLOAT"; c.OscMinVal = 0; c.OscMaxVal = 1; }
            else if (i >= 61) { c.CustomName = "Master Opacity"; c.OscAddress = "/composition/master"; c.OscArgType = "FLOAT"; c.OscMinVal = 0; c.OscMaxVal = 1; }
        });
        Log("🎬 Resolume OSC Template Applied: Port 7000, Clip Grid Matrix & Layer Opacity Faders");
        Play(SoundEffect.PresetSwitch);
    }

    public void ApplyOSCEosTemplate()
    {
        CustomOSCPort = 8000;
        ForEachControl((i, c) =>
        {
            c.OscEnabled = true; c.OscHost = CustomOSCHost; c.OscPort = 8000;
            if (i < 32) { c.CustomName = $"EOS Key {i + 1}"; c.OscAddress = $"/eos/key/{i + 1}"; c.OscArgType = "NONE"; }
            else if (i == 32) { c.CustomName = "EOS GO"; c.OscAddress = "/eos/key/go_0"; c.OscArgType = "NONE"; }
            else if (i == 33) { c.CustomName = "EOS Stop"; c.OscAddress = "/eos/key/stop"; c.OscArgType = "NONE"; }
            else if (i is >= 49 and < 57) { c.CustomName = $"Submaster {i - 48}"; c.OscAddress = $"/eos/fader/1/{i - 48}"; c.OscArgType = "FLOAT"; c.OscMinVal = 0; c.OscMaxVal = 1; }
        });
        Log("💡 ETC EOS OSC Template Applied: Port 8000, Go/Stop & Submaster Faders");
        Play(SoundEffect.PresetSwitch);
    }

    public void ClearAllOSCMappings()
    {
        ForEachControl((_, c) => { c.OscEnabled = false; c.OscAddress = ""; c.CustomName = ""; });
        Log("🧹 All MIDI to OSC Mappings Cleared");
        Play(SoundEffect.KeyTap);
    }

    public void ToggleAllOSCMappings(bool enabled)
    {
        ForEachControl((_, c) => c.OscEnabled = enabled);
        Log($"🔄 All MIDI to OSC Mappings: {(enabled ? "ENABLED" : "DISABLED")}");
        Play(SoundEffect.KnobTick);
    }

    // ------------------------------------------------------------------------
    // Blackout / master
    // ------------------------------------------------------------------------
    public void PanicBlackoutAll()
    {
        IsBlackoutActive = true;
        SendArtNetBlackout();
        SendSacnBlackout();
        SendMSCBroadcast(MSCCommand.AllOff, "", "", 127);
        SendCustomOSC(CustomOSCHost, CustomOSCPort, "/panic", "NONE", "");
        SendPanicAllNotesOffToOutputs();
        SetZoneLEDs(0, 49, 0);
        Play(SoundEffect.HardwareDisconnected);
        Log("🚨 PANIC BLACKOUT EXECUTED: All Lighting, Audio, MSC & MIDI Silenced");
    }

    public void ToggleBlackout()
    {
        if (!IsBlackoutActive)
        {
            PanicBlackoutAll();
        }
        else
        {
            IsBlackoutActive = false;
            SendArtNetFullRig();
            SendSacnFullRig();
            Play(SoundEffect.PresetSwitch);
            Log("💡 Blackout Disengaged — Restored Rig Levels");
        }
    }

    public void FlashRigMomentary()
    {
        Play(SoundEffect.PresetSwitch);
        var saved = GrandMasterLevel;
        GrandMasterLevel = 1.0;
        SendArtNetFullRig();
        SendSacnFullRig();
        SetZoneLEDs(0, 49, 3);
        After(0.3, () =>
        {
            GrandMasterLevel = saved;
            if (IsBlackoutActive)
            {
                SendArtNetBlackout();
                SendSacnBlackout();
                SetZoneLEDs(0, 49, 0);
            }
            else SendArtNetFullRig();
        });
    }

    public void SetGrandMaster(double level)
    {
        GrandMasterLevel = Math.Clamp(level, 0, 1);
        for (var i = 0; i < 8; i++)
            ArtNetChannels[i] = (byte)Math.Clamp(Controls[49 + i].LiveValue * 2.0 * GrandMasterLevel, 0, 255);
        SendArtNetDMX(ArtNetHost, ArtNetUniverse, ArtNetChannels);
    }

    // ------------------------------------------------------------------------
    // MIDI router
    // ------------------------------------------------------------------------
    public void RouteIncomingMidiToTargets(byte status, byte data1, byte data2, bool isFromHardware)
    {
        if (!MidiThruEnabled) return;
        var msgType = status & 0xF0;
        var isNote = msgType is 0x90 or 0x80;
        var isCC = msgType == 0xB0;
        if (isNote && !ForwardNotes) return;
        if (isCC && !ForwardCCs) return;

        var outChan = RemapChannel == 0 ? status & 0x0F : (RemapChannel - 1) & 0x0F;
        var outStatus = (byte)(msgType | outChan);
        byte out1 = data1, out2 = data2;
        if (isNote)
        {
            out1 = (byte)Math.Clamp(data1 + TransposeSemitones, 0, 127);
            if (msgType == 0x90 && data2 > 0) out2 = (byte)VelocityCurve.Transform(data2);
        }

        if (VirtualOutEnabled) _midi.SendToVirtualOut(outStatus, out1, out2);
        foreach (var t in AvailableOutputTargets)
        {
            if (!SelectedOutputNames.Contains(t.Name)) continue;
            if (isFromHardware && t.Name == _midi.HardwareOutName) continue;
            _midi.SendToTarget(new(t.DeviceIndex, t.Name), outStatus, out1, out2);
        }

        TxPacketCount++;
        var hex = $"{outStatus:X2} {out1:X2} {out2:X2}";
        var ch = outChan + 1;
        var desc = isCC ? $"CH{ch} CC#{out1} = {out2}"
            : msgType == 0x90 && out2 > 0 ? $"CH{ch} NOTE ON #{out1} V:{out2}"
            : $"CH{ch} NOTE OFF #{out1}";
        LastRoutedMessage = $"{desc} [{hex}]";
        RouterPacketHistory.Insert(0, $"[{hex}] {desc}");
        if (RouterPacketHistory.Count > 16) RouterPacketHistory.RemoveAt(RouterPacketHistory.Count - 1);
        Raise(nameof(RouterPacketHistory));
    }

    public void ToggleOutputTarget(string name)
    {
        if (!SelectedOutputNames.Remove(name)) SelectedOutputNames.Add(name);
        Raise(nameof(SelectedOutputNames));
    }

    public void SelectAllExternalOutputs()
    {
        foreach (var t in AvailableOutputTargets.Where(t => !t.IsHardwareBitchBoy)) SelectedOutputNames.Add(t.Name);
        Raise(nameof(SelectedOutputNames));
        Log($"🔌 Selected all {SelectedOutputNames.Count} external MIDI outputs");
    }

    public void SendTestNoteToOutputs()
    {
        byte[] chord = [60, 64, 67, 72];
        foreach (var n in chord) RouteIncomingMidiToTargets(0x90, n, 110, false);
        Play(SoundEffect.PresetSwitch);
        Log("🎵 Sent Test C-Major Chord to routed MIDI outputs");
        After(0.35, () => { foreach (var n in chord) RouteIncomingMidiToTargets(0x80, n, 0, false); });
    }

    public void SendPanicAllNotesOffToOutputs()
    {
        for (var ch = 0; ch < 16; ch++) RouteIncomingMidiToTargets((byte)(0xB0 | ch), 123, 0, false);
        Log("🚨 Sent All-Notes-Off (CC 123) across Channels 1–16");
    }

    public void RegisterTapTempo()
    {
        var now = DateTime.UtcNow;
        _lastTapTimes.Add(now);
        _lastTapTimes.RemoveAll(t => (now - t).TotalSeconds >= 4.0);
        if (_lastTapTimes.Count >= 2)
        {
            var avg = Enumerable.Range(1, _lastTapTimes.Count - 1)
                .Average(i => (_lastTapTimes[i] - _lastTapTimes[i - 1]).TotalSeconds);
            if (avg > 0.15)
            {
                TapTempoBPM = Math.Clamp(Math.Round(60.0 / avg), 40, 240);
                FxSpeed = Math.Clamp(Math.Round(TapTempoBPM / 120.0 * 10.0) / 10.0, 0.4, 2.5);
                Log($"⏱️ Tap Tempo: {(int)TapTempoBPM} BPM (FX Speed {FxSpeed:F1}x)");
            }
        }
        Play(SoundEffect.KnobTick);
    }
}
