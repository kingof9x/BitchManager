using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitchManager.Models;

/// <summary>
/// The Swift string raw value for an enum case. Used for display text and for JSON so presets
/// exported from the Mac app import cleanly on Windows (and the other way round).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class RawAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}

public static class Raw
{
    private static readonly Dictionary<Type, (Dictionary<Enum, string> toRaw, Dictionary<string, Enum> fromRaw)> Cache = new();

    private static (Dictionary<Enum, string> toRaw, Dictionary<string, Enum> fromRaw) Maps(Type t)
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(t, out var m)) return m;
            var to = new Dictionary<Enum, string>();
            var from = new Dictionary<string, Enum>();
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var val = (Enum)f.GetValue(null)!;
                var raw = f.GetCustomAttribute<RawAttribute>()?.Value ?? f.Name;
                to[val] = raw;
                from[raw] = val;
            }
            m = (to, from);
            Cache[t] = m;
            return m;
        }
    }

    public static string Of(Enum value) => Maps(value.GetType()).toRaw.TryGetValue(value, out var s) ? s : value.ToString();

    public static bool TryParse<T>(string raw, out T value) where T : struct, Enum
    {
        if (Maps(typeof(T)).fromRaw.TryGetValue(raw, out var e)) { value = (T)e; return true; }
        value = default;
        return false;
    }

    public static T[] All<T>() where T : struct, Enum => Enum.GetValues<T>();
}

public static class EnumExtensions
{
    public static string RawValue(this Enum value) => Raw.Of(value);
}

/// <summary>Serializes enums by their Swift raw value; unknown values fall back to the default case.</summary>
public sealed class RawEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(RawEnumConverter<>).MakeGenericType(typeToConvert))!;

    private sealed class RawEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && Raw.TryParse<T>(reader.GetString() ?? "", out var v)) return v;
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var n))
            {
                var e = (T)Enum.ToObject(typeof(T), n);
                if (Enum.IsDefined(e)) return e;
            }
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(Raw.Of(value));
    }
}

public enum ControlKind
{
    [Raw("key")] Key,       // 49 mechanical backlit keys (hardware LEDs)
    [Raw("vfader")] VFader, // 8 vertical faders (CC 0..7)
    [Raw("hfader")] HFader, // 4 horizontal faders (CC 8..11)
    [Raw("knob")] Knob,     // 8 rotary knobs (CC 20..27)
}

public enum FeedbackMode
{
    [Raw("MOMENTARY")] Momentary,
    [Raw("TOGGLE")] Toggle,
    [Raw("STATIC")] StaticLED,
    [Raw("CUSTOM COLOR")] CustomVelocity,
    [Raw("ECHO VELOCITY")] EchoInbound,
    [Raw("DISABLED")] Disabled,
}

public enum ActionType
{
    [Raw("PASSTHROUGH")] Passthrough,
    [Raw("QWERTY_MACRO")] Qwerty,
    [Raw("OSC_UDP")] Osc,
    // Kept as "APPLESCRIPT" for preset compatibility; on Windows the script text runs in PowerShell.
    [Raw("APPLESCRIPT")] AppleScript,
    [Raw("MSC_BROADCAST")] Msc,
    [Raw("ARTNET_DMX")] ArtNet,
    [Raw("SACN_E131")] Sacn,
}

public enum LEDFXMode
{
    [Raw("STATIC (PRESET)")] StaticFX,
    [Raw("RAINBOW WAVE")] RainbowContinuous,
    [Raw("CYLON SCANNER")] KnightRider,
    [Raw("DIGITAL MATRIX")] MatrixRain,
    [Raw("CYBERPUNK PULSE")] CyberPulse,
    [Raw("LIVE FADER VU")] VuMeter,
    [Raw("FIRE & EMBERS")] FireEmbers,
    [Raw("CENTER RIPPLE")] WaterRipple,
    [Raw("TWINKLE STARS")] Starfield,
    [Raw("STROBE FLASH")] AudioStrobe,
    [Raw("HEARTBEAT")] Heartbeat,
    [Raw("PLASMA FIELD")] PlasmaWave,
    [Raw("SPIRAL VORTEX")] SpiralVortex,
    [Raw("EQ DANCE")] EqDance,
    [Raw("COLOR RUNNER")] ColorChase,
}

public enum StudioModuleID
{
    [Raw("MIDI ROUTER // OUT")] MidiRouter,
    [Raw("MEMORY SLOTS // BANK")] MemorySlots,
    [Raw("CUSTOM FX STUDIO")] CustomFXStudio,
    [Raw("REACTIVE KEY FX // LIVE")] ReactiveKeyFX,
    [Raw("SHOW CONTROL // OSC STUDIO")] ShowControl,
}

public static class StudioModuleInfo
{
    public static string ShortName(this StudioModuleID m) => m switch
    {
        StudioModuleID.MidiRouter => "MIDI ROUTER",
        StudioModuleID.MemorySlots => "MEMORY SLOTS",
        StudioModuleID.CustomFXStudio => "CUSTOM FX",
        StudioModuleID.ReactiveKeyFX => "REACTIVE FX",
        _ => "SHOW CTRL",
    };

    public static double DefaultSlotHeight(this StudioModuleID m) => m switch
    {
        StudioModuleID.MidiRouter => 340,
        StudioModuleID.MemorySlots => 240,
        StudioModuleID.CustomFXStudio => 340,
        StudioModuleID.ReactiveKeyFX => 220,
        _ => 340,
    };
}

public enum ModuleZone
{
    [Raw("LEFT WING")] LeftWing,
    [Raw("RIGHT WING")] RightWing,
    [Raw("BOTTOM RACK")] BottomRack,
    [Raw("FLOATING")] Floating,
}

public enum SoundPack
{
    [Raw("8-BIT ARCADE")] Arcade8Bit,
    [Raw("CHERRY MX BLUE")] MechanicalClick,
    [Raw("SCI-FI SYNTH")] SciFiBeeps,
    [Raw("CUSTOM SOUNDS")] CustomUser,
    [Raw("MUTE SOUNDS")] Mute,
}

public enum SoundEffect
{
    [Raw("keyTap")] KeyTap,
    [Raw("faderSlide")] FaderSlide,
    [Raw("knobTick")] KnobTick,
    [Raw("mascotSqueak")] MascotSqueak,
    [Raw("presetSwitch")] PresetSwitch,
    [Raw("hardwareConnected")] HardwareConnected,
    [Raw("hardwareDisconnected")] HardwareDisconnected,
}

public enum LEDPaletteMode
{
    [Raw("VELOCITY MULTI-COLOR")] MultiVelocity,
    [Raw("CYBER CYAN")] CyberCyan,
    [Raw("MATRIX GREEN")] MatrixGreen,
    [Raw("SOLAR AMBER")] SolarAmber,
    [Raw("SYNTH MAGENTA")] SynthMagenta,
    [Raw("ARCTIC WHITE")] ArcticWhite,
}

public enum MSCCommand : byte
{
    [Raw("GO")] Go = 0x01,
    [Raw("STOP")] Stop = 0x02,
    [Raw("RESUME")] Resume = 0x03,
    [Raw("TIMED GO")] TimedGo = 0x04,
    [Raw("LOAD")] Load = 0x05,
    [Raw("SET")] Set = 0x06,
    [Raw("FIRE MACRO")] Fire = 0x07,
    [Raw("ALL OFF / PANIC")] AllOff = 0x08,
    [Raw("RESTORE")] Restore = 0x09,
    [Raw("RESET")] Reset = 0x0A,
    [Raw("GO OFF")] GoOff = 0x0B,
    [Raw("GO JAM")] GoJam = 0x11,
    [Raw("STANDBY +")] StandbyPlus = 0x12,
    [Raw("STANDBY -")] StandbyMinus = 0x13,
}

public enum MSCCommandFormat : byte
{
    [Raw("Lighting (General)")] LightingGeneral = 0x01,
    [Raw("Moving Lights")] MovingLights = 0x02,
    [Raw("Color Changers")] ColorChangers = 0x03,
    [Raw("Sound (General)")] SoundGeneral = 0x10,
    [Raw("Sound Playback")] SoundPlayback = 0x11,
    [Raw("Sound FX")] SoundEffects = 0x12,
    [Raw("Machinery")] Machinery = 0x20,
    [Raw("Video (General)")] VideoGeneral = 0x30,
    [Raw("Projection")] Projection = 0x40,
    [Raw("Process Control")] ProcessControl = 0x50,
    [Raw("Pyrotechnics")] Pyro = 0x60,
    [Raw("All Types (Universal Broadcast)")] AllTypes = 0x7F,
}

public enum MIDIVelocityCurve
{
    [Raw("LINEAR")] Linear,
    [Raw("SOFT")] Soft,
    [Raw("HARD")] Hard,
    [Raw("MAX 127")] Fixed127,
}

public static class VelocityCurveExtensions
{
    public static int Transform(this MIDIVelocityCurve curve, int v)
    {
        if (v <= 0) return 0;
        var norm = v / 127.0;
        return curve switch
        {
            MIDIVelocityCurve.Soft => Math.Clamp((int)Math.Round(Math.Pow(norm, 1.65) * 127.0), 0, 127),
            MIDIVelocityCurve.Hard => Math.Clamp((int)Math.Round(Math.Pow(norm, 0.55) * 127.0), 0, 127),
            MIDIVelocityCurve.Fixed127 => 127,
            _ => v,
        };
    }
}

public enum CustomFXLoopMode
{
    [Raw("LOOP")] Loop,
    [Raw("BOUNCE")] PingPong,
    [Raw("1-SHOT")] OneShot,
}

public enum CustomWaveShape
{
    [Raw("SINE WAVE")] Sine,
    [Raw("SAWTOOTH")] Sawtooth,
    [Raw("TRIANGLE")] Triangle,
    [Raw("SQUARE PULSE")] Square,
    [Raw("RADIAL RING")] Radial,
    [Raw("SPIRAL VORTEX")] Spiral,
    [Raw("NOISE SPARK")] Sparkle,
}

public enum CustomWaveDirection
{
    [Raw("LEFT → RIGHT")] LeftToRight,
    [Raw("TOP → BOTTOM")] TopToBottom,
    [Raw("DIAGONAL 45°")] Diagonal,
    [Raw("CENTER → OUT")] CenterOut,
    [Raw("OUTER → CENTER")] OuterIn,
}

public enum ReactiveKeyFXMode
{
    [Raw("RIPPLE RING")] Ripple,
    [Raw("LASER CROSS")] CrossLaser,
    [Raw("SUPERNOVA")] Supernova,
    [Raw("HEAT TRAIL")] HeatTrail,
    [Raw("METEOR DROP")] MeteorRain,
    [Raw("ARC LIGHTNING")] ArcLightning,
    [Raw("OFF")] Off,
}
