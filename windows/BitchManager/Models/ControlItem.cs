using System.Text.Json.Serialization;
using BitchManager.Core;

namespace BitchManager.Models;

public sealed class QwertyStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = "";
    public bool Cmd { get; set; }
    public bool Shift { get; set; }
    public bool Alt { get; set; }
    public bool Ctrl { get; set; }
    public int DelayMs { get; set; } = 10;
}

/// <summary>
/// One of the 69 physical controls. Property names match the Swift Codable keys so preset JSON
/// is interchangeable with the Mac app.
/// </summary>
public sealed class ControlItem : ObservableObject
{
    private string _label = "";
    private string _customName = "";
    private ControlKind _kind;
    private int _number;
    private string _zone = "";
    private int _idleVelocity;
    private int _activeVelocity = 3;
    private int _currentLED;
    private int _liveValue;
    private bool _isPressed;
    private bool _toggleLatched;
    private FeedbackMode _feedbackMode = FeedbackMode.Momentary;
    private int _feedbackCustomVelocity = 78;
    private bool _feedbackInvert;
    private ActionType _actionType = ActionType.Passthrough;
    private List<QwertyStep> _qwertySteps = [];
    private string _oscHost = "127.0.0.1";
    private int _oscPort = 8000;
    private string _oscAddress = "/bitchboy/trigger";
    private string _oscArgType = "NONE";
    private string _oscArgValue = "1.0";
    private int _mscCommand = 1;
    private string _mscCueNumber = "";
    private string _appleScript = "";
    private bool _oscEnabled;
    private double _oscMinVal;
    private double _oscMaxVal = 1.0;
    private bool _oscSendRelease;
    private string _oscReleaseVal = "0.0";

    [JsonIgnore] public string Id => $"{_kind.RawValue()}_{_number}";
    [JsonIgnore] public bool HasLED => _kind == ControlKind.Key;
    [JsonIgnore] public string DisplayName => string.IsNullOrEmpty(_customName) ? _label : _customName;

    public string Label { get => _label; set => Set(ref _label, value); }
    public string CustomName { get => _customName; set { if (Set(ref _customName, value)) Raise(nameof(DisplayName)); } }
    public ControlKind Kind { get => _kind; set => Set(ref _kind, value); }
    public int Number { get => _number; set => Set(ref _number, value); }
    public string Zone { get => _zone; set => Set(ref _zone, value); }
    public int IdleVelocity { get => _idleVelocity; set => Set(ref _idleVelocity, value); }
    public int ActiveVelocity { get => _activeVelocity; set => Set(ref _activeVelocity, value); }
    public int CurrentLED { get => _currentLED; set => Set(ref _currentLED, value); }
    public int LiveValue { get => _liveValue; set => Set(ref _liveValue, value); }
    public bool IsPressed { get => _isPressed; set => Set(ref _isPressed, value); }
    public bool ToggleLatched { get => _toggleLatched; set => Set(ref _toggleLatched, value); }
    public FeedbackMode FeedbackMode { get => _feedbackMode; set => Set(ref _feedbackMode, value); }
    public int FeedbackCustomVelocity { get => _feedbackCustomVelocity; set => Set(ref _feedbackCustomVelocity, value); }
    public bool FeedbackInvert { get => _feedbackInvert; set => Set(ref _feedbackInvert, value); }
    public ActionType ActionType { get => _actionType; set => Set(ref _actionType, value); }
    public List<QwertyStep> QwertySteps { get => _qwertySteps; set => Set(ref _qwertySteps, value ?? []); }
    public string OscHost { get => _oscHost; set => Set(ref _oscHost, value); }
    public int OscPort { get => _oscPort; set => Set(ref _oscPort, value); }
    public string OscAddress { get => _oscAddress; set => Set(ref _oscAddress, value); }
    public string OscArgType { get => _oscArgType; set => Set(ref _oscArgType, value); }
    public string OscArgValue { get => _oscArgValue; set => Set(ref _oscArgValue, value); }
    public int MscCommand { get => _mscCommand; set => Set(ref _mscCommand, value); }
    public string MscCueNumber { get => _mscCueNumber; set => Set(ref _mscCueNumber, value); }
    public string AppleScript { get => _appleScript; set => Set(ref _appleScript, value); }
    public bool OscEnabled { get => _oscEnabled; set => Set(ref _oscEnabled, value); }
    public double OscMinVal { get => _oscMinVal; set => Set(ref _oscMinVal, value); }
    public double OscMaxVal { get => _oscMaxVal; set => Set(ref _oscMaxVal, value); }
    public bool OscSendRelease { get => _oscSendRelease; set => Set(ref _oscSendRelease, value); }
    public string OscReleaseVal { get => _oscReleaseVal; set => Set(ref _oscReleaseVal, value); }

    public ControlItem Clone()
    {
        var c = new ControlItem();
        c.CopyFrom(this);
        return c;
    }

    /// <summary>Copies every value in place so existing UI subscriptions stay connected.</summary>
    public void CopyFrom(ControlItem o)
    {
        Label = o.Label; CustomName = o.CustomName; Kind = o.Kind; Number = o.Number; Zone = o.Zone;
        IdleVelocity = o.IdleVelocity; ActiveVelocity = o.ActiveVelocity; CurrentLED = o.CurrentLED;
        LiveValue = o.LiveValue; IsPressed = o.IsPressed; ToggleLatched = o.ToggleLatched;
        FeedbackMode = o.FeedbackMode; FeedbackCustomVelocity = o.FeedbackCustomVelocity; FeedbackInvert = o.FeedbackInvert;
        ActionType = o.ActionType;
        QwertySteps = o.QwertySteps.Select(s => new QwertyStep { Id = s.Id, Key = s.Key, Cmd = s.Cmd, Shift = s.Shift, Alt = s.Alt, Ctrl = s.Ctrl, DelayMs = s.DelayMs }).ToList();
        OscHost = o.OscHost; OscPort = o.OscPort; OscAddress = o.OscAddress; OscArgType = o.OscArgType; OscArgValue = o.OscArgValue;
        MscCommand = o.MscCommand; MscCueNumber = o.MscCueNumber; AppleScript = o.AppleScript;
        OscEnabled = o.OscEnabled; OscMinVal = o.OscMinVal; OscMaxVal = o.OscMaxVal; OscSendRelease = o.OscSendRelease; OscReleaseVal = o.OscReleaseVal;
    }
}

public sealed class PresetSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<ControlItem> Controls { get; set; } = [];
    public string GlobalRules { get; set; } = "";
    public string IconName { get; set; } = "dial.medium.fill";
    public int AccentVelocity { get; set; } = 78;
}

public sealed class CustomLightEffect
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public bool IsProcedural { get; set; }
    public List<int[]> Frames { get; set; } = []; // each frame is 49 velocities (0..127)
    public double Fps { get; set; } = 10.0;
    public CustomFXLoopMode LoopMode { get; set; } = CustomFXLoopMode.Loop;

    // Procedural wave synth parameters
    public CustomWaveShape WaveShape { get; set; } = CustomWaveShape.Sine;
    public CustomWaveDirection WaveDirection { get; set; } = CustomWaveDirection.LeftToRight;
    public double WaveFrequency { get; set; } = 1.0;
    public int MinVelocity { get; set; }
    public int MaxVelocity { get; set; } = 78;

    public CustomLightEffect Clone()
    {
        var c = (CustomLightEffect)MemberwiseClone();
        c.Frames = Frames.Select(f => (int[])f.Clone()).ToList();
        return c;
    }

    public static int[] EmptyFrame(int fill = 0) => Enumerable.Repeat(fill, 49).ToArray();

    public static CustomLightEffect BuildDefaultConnectEffect()
    {
        // 10-frame one-shot connection celebration sweep and flash
        var frames = new List<int[]>();
        int[] sweep = [78, 87, 74, 95, 41, 67, 3];
        for (var step = 0; step < 6; step++)
        {
            var f = EmptyFrame();
            var left = step * 2;
            var right = 10 - step * 2;
            for (var k = 0; k < 49; k++)
            {
                var p = KeyMap.Positions[k];
                if (Math.Abs(p.Col - left) <= 1 || Math.Abs(p.Col - right) <= 1)
                    f[k] = sweep[(step + p.Row) % sweep.Length];
            }
            frames.Add(f);
        }
        frames.Add(EmptyFrame(87));
        frames.Add(EmptyFrame(0));
        var burst = EmptyFrame(78);
        for (var k = 40; k < 49; k++) burst[k] = 96;
        frames.Add(burst);
        frames.Add(EmptyFrame(3));

        return new CustomLightEffect { Name = "BITCHBOY CONNECT IGNITION", Frames = frames, Fps = 14.0, LoopMode = CustomFXLoopMode.OneShot };
    }

    public static List<CustomLightEffect> BuildStarterEffects()
    {
        var connect = BuildDefaultConnectEffect();

        // 1. Neon heart pulse
        var h1 = EmptyFrame();
        var h2 = EmptyFrame();
        (int, int)[] heart =
        [
            (2,0), (3,0), (5,0), (6,0),
            (1,1), (2,1), (3,1), (4,1), (5,1), (6,1), (7,1),
            (2,2), (3,2), (4,2), (5,2), (6,2),
            (3,3), (4,3), (5,3),
            (4,4),
        ];
        for (var k = 0; k < 49; k++)
        {
            var p = KeyMap.Positions[k];
            if (heart.Contains((p.Col, p.Row))) { h1[k] = 95; h2[k] = 72; }
        }
        var heartFx = new CustomLightEffect { Name = "NEON HEART PULSE", Frames = [h1, h2, (int[])h1.Clone(), EmptyFrame()], Fps = 5.0, LoopMode = CustomFXLoopMode.Loop };

        // 2. Cyber cross chase
        var xFrames = new List<int[]>();
        int[] pal = [78, 41, 67, 95, 74, 87];
        for (var step = 0; step < 6; step++)
        {
            var f = EmptyFrame();
            for (var k = 0; k < 49; k++)
            {
                var p = KeyMap.Positions[k];
                if ((p.Col + p.Row + step) % 3 == 0 || (p.Col - p.Row + 10 + step) % 3 == 0)
                    f[k] = pal[(step + p.Col) % pal.Length];
            }
            xFrames.Add(f);
        }
        var xFx = new CustomLightEffect { Name = "CYBER CROSS CHASE", Frames = xFrames, Fps = 8.0, LoopMode = CustomFXLoopMode.PingPong };

        // 3. 11-column laser sweep
        var barFrames = new List<int[]>();
        int[] barColors = [72, 61, 96, 74, 87, 33, 78, 41, 67, 95, 3];
        for (var c = 0; c < 11; c++)
        {
            var f = EmptyFrame();
            for (var k = 0; k < 49; k++)
            {
                var col = KeyMap.Positions[k].Col;
                if (col == c) f[k] = barColors[c];
                else if (Math.Abs(col - c) == 1) f[k] = 61;
            }
            barFrames.Add(f);
        }
        var barFx = new CustomLightEffect { Name = "11-COL LASER SWEEP", Frames = barFrames, Fps = 14.0, LoopMode = CustomFXLoopMode.PingPong };

        // 4. Procedural radial synth wave
        var synth = new CustomLightEffect
        {
            Name = "RADIAL HYPERSPACE", IsProcedural = true, Frames = [EmptyFrame(78)], Fps = 15.0, LoopMode = CustomFXLoopMode.Loop,
            WaveShape = CustomWaveShape.Radial, WaveDirection = CustomWaveDirection.CenterOut, WaveFrequency = 1.4, MinVelocity = 0, MaxVelocity = 78,
        };

        return [connect, heartFx, xFx, barFx, synth];
    }
}

public sealed class MSCMessage
{
    public DateTime Timestamp { get; } = DateTime.Now;
    public bool IsOutbound { get; init; }
    public int DeviceID { get; init; }
    public MSCCommandFormat CommandFormat { get; init; }
    public MSCCommand Command { get; init; }
    public string CueNumber { get; init; } = "";
    public string CueList { get; init; } = "";
    public byte[] RawBytes { get; init; } = [];

    public string Summary
    {
        get
        {
            var dir = IsOutbound ? "TX ➡️" : "RX ⬅️";
            var cue = CueNumber.Length == 0 ? "" : $" Cue: {CueNumber}";
            var list = CueList.Length == 0 ? "" : $" [List: {CueList}]";
            var dev = DeviceID == 127 ? "ALL" : DeviceID.ToString();
            return $"{dir} {Command.RawValue()}{cue}{list} (Dev: {dev}, {CommandFormat.RawValue()})";
        }
    }

    public string TimeString => Timestamp.ToString("HH:mm:ss.ff");
}

public sealed class ReactivePulseEvent
{
    public int OriginKeyIndex { get; init; }
    public double OriginCol { get; init; }
    public double OriginRow { get; init; }
    public ReactiveKeyFXMode Mode { get; init; }
    public int BaseVelocity { get; init; }
    public double Age { get; set; }
    public int[] LightningTargets { get; init; } = [];
}

public sealed record MidiOutputTarget(string Name, int DeviceIndex, bool IsHardwareBitchBoy);
