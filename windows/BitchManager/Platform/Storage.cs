using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using BitchManager.Models;

namespace BitchManager.Platform;

/// <summary>Everything the Mac app keeps in UserDefaults, as one settings file.</summary>
public sealed class AppSettings
{
    public ChassisTheme Theme { get; set; } = ChassisTheme.PetrolTeal;
    public Guid? ActiveCustomThemeId { get; set; }
    public SoundPack SoundPack { get; set; } = SoundPack.Arcade8Bit;
    public bool AlwaysOnTop { get; set; }
    public double UiScale { get; set; } = 1.0;
    public bool ConnectFXEnabled { get; set; } = true;
    public string ConnectFXName { get; set; } = "BITCHBOY CONNECT IGNITION";
    public List<StudioModuleID>? LeftWingModules { get; set; }
    public List<StudioModuleID>? RightWingModules { get; set; }
    public List<StudioModuleID>? BottomRackModules { get; set; }
    public Dictionary<string, double> ModuleHeights { get; set; } = new();
    public Dictionary<string, double> ModuleWidths { get; set; } = new();
}

/// <summary>JSON files under %APPDATA%\BitchManager. Formats match the Mac app's Codable JSON.</summary>
public static class Storage
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = null,
        Converters = { new RawEnumConverterFactory() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
    };

    public static string Folder
    {
        get
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BitchManager");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static T? Load<T>(string name) where T : class
    {
        try
        {
            var path = Path.Combine(Folder, name);
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Save<T>(string name, T value)
    {
        try
        {
            var path = Path.Combine(Folder, name);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(value, Json));
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            // A failed save must never take down a live show.
        }
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    public static T? Deserialize<T>(string json) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(json, Json); }
        catch { return null; }
    }
}
