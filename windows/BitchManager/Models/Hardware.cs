using System.Windows.Media;

namespace BitchManager.Models;

/// <summary>
/// Authentic BitchBoy LED velocities. Red 72, Green 87, Blue 41, Yellow 74, Cyan 78, Orange 61,
/// Purple 67, Brown 83, Off 0, White 3, Pink 95, Amber 96, Turquoise 33.
/// </summary>
public static class HardwareColor
{
    public static Color Rgb(double r, double g, double b, double a = 1.0)
        => Color.FromArgb((byte)Math.Round(a * 255), (byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));

    public static Color White(double w, double a = 1.0) => Rgb(w, w, w, a);

    public static readonly Color Off = White(0.14);
    public static readonly Color WhiteLed = Rgb(0.98, 0.98, 1.00);
    public static readonly Color Turquoise = Rgb(0.16, 0.90, 0.78);
    public static readonly Color Blue = Rgb(0.18, 0.46, 1.00);
    public static readonly Color Orange = Rgb(1.00, 0.52, 0.08);
    public static readonly Color Purple = Rgb(0.68, 0.22, 0.96);
    public static readonly Color Red = Rgb(1.00, 0.16, 0.16);
    public static readonly Color Yellow = Rgb(1.00, 0.92, 0.10);
    public static readonly Color Cyan = Rgb(0.10, 0.94, 0.98);
    public static readonly Color Brown = Rgb(0.62, 0.38, 0.18);
    public static readonly Color Green = Rgb(0.16, 0.92, 0.32);
    public static readonly Color Pink = Rgb(1.00, 0.30, 0.72);
    public static readonly Color Amber = Rgb(1.00, 0.74, 0.12);

    public sealed record Item(string Name, string ShortName, int Velocity, Color Color);

    public static readonly Item[] AllItems =
    [
        new("OFF", "OFF", 0, Off),
        new("WHITE", "WHT", 3, WhiteLed),
        new("TURQUOISE", "TRQ", 33, Turquoise),
        new("BLUE", "BLU", 41, Blue),
        new("ORANGE", "ORG", 61, Orange),
        new("PURPLE", "PRP", 67, Purple),
        new("RED", "RED", 72, Red),
        new("YELLOW", "YLW", 74, Yellow),
        new("CYAN", "CYN", 78, Cyan),
        new("BROWN", "BRN", 83, Brown),
        new("GREEN", "GRN", 87, Green),
        new("PINK", "PNK", 95, Pink),
        new("AMBER", "AMB", 96, Amber),
    ];

    public static readonly int[] KnownVelocities = [0, 3, 33, 41, 61, 67, 72, 74, 78, 83, 87, 95, 96];

    /// <summary>The 11-step rainbow used by sweeps, sparks and reactive FX.</summary>
    public static readonly int[] Rainbow = [72, 61, 96, 74, 87, 33, 78, 41, 67, 95, 3];

    public static int ClosestHardwareVelocity(int vel)
    {
        if (vel == 0) return 0;
        var best = 0;
        var bestDist = int.MaxValue;
        foreach (var k in KnownVelocities)
        {
            var d = Math.Abs(k - vel);
            if (d < bestDist) { bestDist = d; best = k; }
        }
        return best;
    }

    private static Item ItemFor(int vel)
    {
        var nearest = ClosestHardwareVelocity(vel);
        foreach (var i in AllItems) if (i.Velocity == nearest) return i;
        return AllItems[0];
    }

    public static Color ColorFor(int vel) => vel == 0 ? Off : ItemFor(vel).Color;

    public static string Label(int vel) => vel == 0 ? "OFF" : ItemFor(vel).Name;

    public static string FormattedLabel(int vel) => vel == 0 ? "OFF (0)" : $"{Label(vel)} ({vel})";
}

/// <summary>Physical positions of the 49 LED keys on an 11-column, 5-row grid.</summary>
public static class KeyMap
{
    public static readonly (int Col, int Row)[] Positions = BuildPositions();

    // Precomputed 49x49 distance matrix for lag-free reactive ripple and wave rendering.
    public static readonly double[,] Distances = BuildDistances();

    private static (int, int)[] BuildPositions()
    {
        var pos = new List<(int, int)>();
        for (var r = 0; r < 4; r++) for (var c = 0; c < 8; c++) pos.Add((c, r)); // 0..31: 8x4 grid
        for (var c = 0; c < 8; c++) pos.Add((c, 4));                              // 32..39: 1x8 strip
        for (var r = 0; r < 3; r++) for (var c = 0; c < 3; c++) pos.Add((8 + c, r)); // 40..48: 3x3 keypad
        return pos.ToArray();
    }

    private static double[,] BuildDistances()
    {
        var m = new double[49, 49];
        for (var i = 0; i < 49; i++)
        for (var j = 0; j < 49; j++)
        {
            double dx = Positions[i].Col - Positions[j].Col;
            double dy = Positions[i].Row - Positions[j].Row;
            m[i, j] = Math.Sqrt(dx * dx + dy * dy);
        }
        return m;
    }

    public static int IndexAt(int col, int row)
    {
        for (var i = 0; i < Positions.Length; i++)
            if (Positions[i].Col == col && Positions[i].Row == row) return i;
        return -1;
    }
}
