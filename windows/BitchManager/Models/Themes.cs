using System.Globalization;
using System.Text.Json.Serialization;
using System.Windows.Media;
using static BitchManager.Models.HardwareColor;

namespace BitchManager.Models;

public enum ChassisTheme
{
    [Raw("PETROL TEAL")] PetrolTeal,
    [Raw("RETRO MATRIX")] RetroMatrix,
    [Raw("NEON CYBER")] NeonCyber,
    [Raw("ROYAL GOLD")] RoyalGold,
    [Raw("STEALTH NOIR")] StealthBlack,
    [Raw("CUSTOM THEME")] CustomUser,
}

/// <summary>The resolved colours for whichever chassis theme is active.</summary>
public sealed record ThemePalette(Color Pcb, Color[] Bezel, Color Accent, Color Outline, Color HeaderText);

public static class ThemeColors
{
    public static ThemePalette For(ChassisTheme theme, CustomChassisTheme custom) => theme switch
    {
        ChassisTheme.PetrolTeal => new(Rgb(0.04, 0.24, 0.21), [White(0.94), White(0.75), White(0.55)],
            Rgb(0.22, 0.96, 0.85), Rgb(0.96, 0.52, 0.25), Rgb(0.98, 0.90, 0.65)),
        ChassisTheme.RetroMatrix => new(Rgb(0.02, 0.16, 0.08), [Rgb(0.15, 0.55, 0.25), Rgb(0.08, 0.30, 0.12)],
            Rgb(0.18, 0.95, 0.35), Rgb(0.20, 0.85, 0.45), Rgb(0.65, 0.98, 0.65)),
        ChassisTheme.NeonCyber => new(Rgb(0.08, 0.02, 0.18), [Rgb(0.95, 0.22, 0.85), Rgb(0.35, 0.10, 0.65)],
            Rgb(1.0, 0.25, 0.88), Rgb(0.20, 0.92, 0.98), Rgb(0.95, 0.75, 0.98)),
        ChassisTheme.RoyalGold => new(Rgb(0.18, 0.12, 0.04), [Rgb(0.98, 0.85, 0.45), Rgb(0.65, 0.45, 0.15)],
            Rgb(0.98, 0.82, 0.35), Rgb(0.92, 0.75, 0.25), Rgb(0.98, 0.92, 0.70)),
        ChassisTheme.StealthBlack => new(Rgb(0.06, 0.06, 0.07), [White(0.35), White(0.15), White(0.08)],
            White(0.85), White(0.45), White(0.80)),
        _ => custom.Palette,
    };
}

public sealed class CustomChassisTheme
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string PcbHex { get; set; } = "";
    public string BezelStartHex { get; set; } = "";
    public string BezelEndHex { get; set; } = "";
    public string AccentGlowHex { get; set; } = "";
    public string OutlineHex { get; set; } = "";
    public string HeaderTextHex { get; set; } = "";

    [JsonIgnore]
    public ThemePalette Palette => new(
        Hex(PcbHex, Rgb(0.04, 0.24, 0.21)),
        [Hex(BezelStartHex, White(0.94)), Hex(BezelEndHex, White(0.55))],
        Hex(AccentGlowHex, Rgb(0.22, 0.96, 0.85)),
        Hex(OutlineHex, Rgb(0.96, 0.52, 0.25)),
        Hex(HeaderTextHex, Rgb(0.98, 0.90, 0.65)));

    public CustomChassisTheme Clone() => (CustomChassisTheme)MemberwiseClone();

    public static Color Hex(string hex, Color fallback)
    {
        var clean = new string(hex.Where(char.IsAsciiHexDigit).ToArray());
        if (!ulong.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v)) return fallback;
        return clean.Length switch
        {
            3 => Color.FromRgb((byte)((v >> 8) * 17), (byte)(((v >> 4) & 0xF) * 17), (byte)((v & 0xF) * 17)),
            6 => Color.FromRgb((byte)(v >> 16), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)),
            8 => Color.FromArgb((byte)(v >> 24), (byte)((v >> 16) & 0xFF), (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF)),
            _ => fallback,
        };
    }

    private static CustomChassisTheme Make(string name, string pcb, string b1, string b2, string accent, string outline, string header)
        => new() { Name = name, PcbHex = pcb, BezelStartHex = b1, BezelEndHex = b2, AccentGlowHex = accent, OutlineHex = outline, HeaderTextHex = header };

    public static CustomChassisTheme DefaultCustom() => StarterThemes()[0];

    public static List<CustomChassisTheme> StarterThemes() =>
    [
        Make("NEO TOKYO GLOW", "#0D0A1C", "#FF007F", "#7928CA", "#00F0FF", "#FF007F", "#E0E7FF"),
        Make("ACID LIME OVERDRIVE", "#08140B", "#39FF14", "#0A4A1C", "#39FF14", "#CCFF00", "#E5FFD0"),
        Make("CYBER INFERNO", "#1A0505", "#FF3300", "#8B0000", "#FF4500", "#FF9900", "#FFE4B5"),
        Make("DEEP VOID ICE", "#040914", "#00D2FF", "#003366", "#38E8FF", "#0088FF", "#D8F3FF"),
    ];
}
