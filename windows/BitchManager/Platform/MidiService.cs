using NAudio.Midi;

namespace BitchManager.Platform;

/// <summary>
/// Windows MIDI I/O (WinMM via NAudio). Stands in for the CoreMIDI client in the Mac app.
///
/// Differences from macOS: Windows has no built-in way for an app to publish its own virtual
/// ports, so "BitchBoy Virtual Out" / "BitchBoy Virtual In" are loopback ports the user creates
/// (loopMIDI, or Windows MIDI Services loopback endpoints). When a port with that name exists we
/// use it exactly like the Mac virtual endpoints.
/// </summary>
public sealed class MidiService : IDisposable
{
    public const string VirtualOutName = "BitchBoy Virtual Out";
    public const string VirtualInName = "BitchBoy Virtual In";

    public sealed record Port(int Index, string Name);

    private MidiIn? _hwIn;
    private MidiOut? _hwOut;
    private MidiIn? _virtualIn;
    private MidiOut? _virtualOut;
    private readonly Dictionary<string, MidiOut> _targetOuts = new();
    private readonly object _lock = new();

    public string? HardwareInName { get; private set; }
    public string? HardwareOutName { get; private set; }
    public string? VirtualInPortName { get; private set; }
    public string? VirtualOutPortName { get; private set; }

    /// <summary>Short message from the physical BitchBoy (raised on the MIDI driver thread).</summary>
    public event Action<byte, byte, byte>? HardwareMessage;
    public event Action<byte[]>? HardwareSysEx;
    /// <summary>Short message arriving on "BitchBoy Virtual In" from a DAW / QLab.</summary>
    public event Action<byte, byte, byte>? ExternalMessage;
    public event Action<byte[]>? ExternalSysEx;
    public event Action<string>? Error;

    public static List<Port> Inputs()
    {
        var list = new List<Port>();
        try
        {
            for (var i = 0; i < MidiIn.NumberOfDevices; i++) list.Add(new Port(i, MidiIn.DeviceInfo(i).ProductName));
        }
        catch { /* enumeration can fail transiently while devices are being plugged in */ }
        return Uniquify(list);
    }

    public static List<Port> Outputs()
    {
        var list = new List<Port>();
        try
        {
            for (var i = 0; i < MidiOut.NumberOfDevices; i++) list.Add(new Port(i, MidiOut.DeviceInfo(i).ProductName));
        }
        catch { }
        return Uniquify(list);
    }

    private static List<Port> Uniquify(List<Port> ports)
    {
        var seen = new Dictionary<string, int>();
        var result = new List<Port>(ports.Count);
        foreach (var p in ports)
        {
            seen[p.Name] = seen.TryGetValue(p.Name, out var n) ? n + 1 : 1;
            result.Add(seen[p.Name] == 1 ? p : p with { Name = $"{p.Name} ({seen[p.Name]})" });
        }
        return result;
    }

    public static bool IsVirtualOut(string name) => name.Contains("bitchboy virtual out", StringComparison.OrdinalIgnoreCase);
    public static bool IsVirtualIn(string name) => name.Contains("bitchboy virtual in", StringComparison.OrdinalIgnoreCase);

    /// <summary>Same matching rules as the Mac app: only the physical BitchBoy, never a DAW or virtual bus.</summary>
    public static bool IsRealBitchBoy(string name)
    {
        var n = name.ToLowerInvariant();
        string[] excluded =
        [
            "bitchboy virtual out", "bitchboy virtual in", "virtual out", "virtual in", "bitchmanager", "simulated",
            "qlab", "iac", "network", "session", "ableton", "logic", "mainstage", "pro tools", "reaper", "bitwig",
            "fl studio", "cubase", "garageband", "traktor", "serato", "rekordbox", "resolume", "loopmidi", "loopback",
        ];
        if (excluded.Any(n.Contains)) return false;
        return n.Contains("bitchboy") || n.Contains("bitch boy") || n.Contains("block system");
    }

    /// <summary>Opens or closes the hardware ports to match what is plugged in. Returns true if connected.</summary>
    public bool SyncHardware(Port? input, Port? output)
    {
        lock (_lock)
        {
            if (input?.Name != HardwareInName)
            {
                CloseIn(ref _hwIn);
                HardwareInName = null;
                if (input != null && TryOpenIn(input, out var mi, isHardware: true))
                {
                    _hwIn = mi;
                    HardwareInName = input.Name;
                }
            }
            if (output?.Name != HardwareOutName)
            {
                CloseOut(ref _hwOut);
                HardwareOutName = null;
                if (output != null && TryOpenOut(output, out var mo))
                {
                    _hwOut = mo;
                    HardwareOutName = output.Name;
                }
            }
            return _hwIn != null || _hwOut != null;
        }
    }

    public void SyncVirtualPorts(Port? virtualIn, Port? virtualOut)
    {
        lock (_lock)
        {
            if (virtualIn?.Name != VirtualInPortName)
            {
                CloseIn(ref _virtualIn);
                VirtualInPortName = null;
                if (virtualIn != null && TryOpenIn(virtualIn, out var mi, isHardware: false))
                {
                    _virtualIn = mi;
                    VirtualInPortName = virtualIn.Name;
                }
            }
            if (virtualOut?.Name != VirtualOutPortName)
            {
                CloseOut(ref _virtualOut);
                VirtualOutPortName = null;
                if (virtualOut != null && TryOpenOut(virtualOut, out var mo))
                {
                    _virtualOut = mo;
                    VirtualOutPortName = virtualOut.Name;
                }
            }
        }
    }

    /// <summary>Closes cached router outputs whose devices are gone.</summary>
    public void PruneTargets(IEnumerable<string> liveNames)
    {
        lock (_lock)
        {
            var live = new HashSet<string>(liveNames);
            foreach (var name in _targetOuts.Keys.Where(k => !live.Contains(k)).ToList())
            {
                try { _targetOuts[name].Dispose(); } catch { }
                _targetOuts.Remove(name);
            }
        }
    }

    private bool TryOpenIn(Port port, out MidiIn midiIn, bool isHardware)
    {
        midiIn = null!;
        try
        {
            var mi = new MidiIn(port.Index);
            if (isHardware)
            {
                mi.MessageReceived += (_, e) => Dispatch(e.RawMessage, HardwareMessage);
                mi.SysexMessageReceived += (_, e) => HardwareSysEx?.Invoke(e.SysexBytes);
            }
            else
            {
                mi.MessageReceived += (_, e) => Dispatch(e.RawMessage, ExternalMessage);
                mi.SysexMessageReceived += (_, e) => ExternalSysEx?.Invoke(e.SysexBytes);
            }
            mi.CreateSysexBuffers(1024, 8);
            mi.Start();
            midiIn = mi;
            return true;
        }
        catch (Exception ex)
        {
            Error?.Invoke($"Could not open MIDI input '{port.Name}': {ex.Message}");
            return false;
        }
    }

    private static void Dispatch(int raw, Action<byte, byte, byte>? handler)
    {
        var status = (byte)(raw & 0xFF);
        if (status < 0x80 || status >= 0xF0) return;
        handler?.Invoke(status, (byte)((raw >> 8) & 0x7F), (byte)((raw >> 16) & 0x7F));
    }

    private bool TryOpenOut(Port port, out MidiOut midiOut)
    {
        midiOut = null!;
        try
        {
            midiOut = new MidiOut(port.Index);
            return true;
        }
        catch (Exception ex)
        {
            Error?.Invoke($"Could not open MIDI output '{port.Name}': {ex.Message}");
            return false;
        }
    }

    private static void CloseIn(ref MidiIn? mi)
    {
        if (mi == null) return;
        try { mi.Stop(); mi.Dispose(); } catch { }
        mi = null;
    }

    private static void CloseOut(ref MidiOut? mo)
    {
        if (mo == null) return;
        try { mo.Dispose(); } catch { }
        mo = null;
    }

    private static int Pack(byte status, byte d1, byte d2) => status | (d1 << 8) | (d2 << 16);

    public bool HardwareOutOpen => _hwOut != null;
    public bool VirtualOutOpen => _virtualOut != null;

    public void SendToHardware(byte status, byte d1, byte d2)
    {
        var mo = _hwOut;
        if (mo == null) return;
        try { mo.Send(Pack(status, d1, d2)); } catch { }
    }

    public void SendSysExToHardware(byte[] bytes)
    {
        var mo = _hwOut;
        if (mo == null) return;
        try { mo.SendBuffer(bytes); } catch { }
    }

    public void SendToVirtualOut(byte status, byte d1, byte d2)
    {
        var mo = _virtualOut;
        if (mo == null) return;
        try { mo.Send(Pack(status, d1, d2)); } catch { }
    }

    public void SendSysExToVirtualOut(byte[] bytes)
    {
        var mo = _virtualOut;
        if (mo == null) return;
        try { mo.SendBuffer(bytes); } catch { }
    }

    private MidiOut? TargetOut(Port port)
    {
        lock (_lock)
        {
            if (_targetOuts.TryGetValue(port.Name, out var existing)) return existing;
            if (!TryOpenOut(port, out var mo)) return null;
            _targetOuts[port.Name] = mo;
            return mo;
        }
    }

    public void SendToTarget(Port port, byte status, byte d1, byte d2)
    {
        if (port.Name == HardwareOutName) { SendToHardware(status, d1, d2); return; }
        if (port.Name == VirtualOutPortName) { SendToVirtualOut(status, d1, d2); return; }
        var mo = TargetOut(port);
        if (mo == null) return;
        try { mo.Send(Pack(status, d1, d2)); } catch { }
    }

    public void SendSysExToTarget(Port port, byte[] bytes)
    {
        if (port.Name == HardwareOutName) { SendSysExToHardware(bytes); return; }
        if (port.Name == VirtualOutPortName) { SendSysExToVirtualOut(bytes); return; }
        var mo = TargetOut(port);
        if (mo == null) return;
        try { mo.SendBuffer(bytes); } catch { }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            CloseIn(ref _hwIn);
            CloseOut(ref _hwOut);
            CloseIn(ref _virtualIn);
            CloseOut(ref _virtualOut);
            foreach (var mo in _targetOuts.Values) { try { mo.Dispose(); } catch { } }
            _targetOuts.Clear();
        }
    }
}
