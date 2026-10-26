using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using BitchManager.Models;

namespace BitchManager.Platform;

/// <summary>
/// UI sound packs. The Mac app plays named macOS system sounds; here each one maps to the closest
/// stock Windows sound in C:\Windows\Media. Like NSSound, a sound that is still playing is not
/// restarted, which keeps fader drags from turning into a buzz.
/// </summary>
public static class Sounds
{
    private static readonly Dictionary<string, string> MacToWindows = new()
    {
        ["Tink"] = "Windows Navigation Start.wav",
        ["Pop"] = "Windows Menu Command.wav",
        ["Morse"] = "Windows Balloon.wav",
        ["Purr"] = "Windows Background.wav",
        ["Glass"] = "Windows Ding.wav",
        ["Hero"] = "tada.wav",
        ["Basso"] = "Windows Hardware Remove.wav",
        ["Blow"] = "Windows Pop-up Blocked.wav",
        ["Ping"] = "Windows Notify System Generic.wav",
        ["Sosumi"] = "Windows Critical Stop.wav",
        ["Bottle"] = "Speech On.wav",
        ["Submarine"] = "chimes.wav",
        ["Insert"] = "Windows Hardware Insert.wav",
    };

    private static readonly Dictionary<string, (MediaPlayer Player, DateTime Started)> Players = new();

    public static void Play(this SoundEffect effect, SoundPack pack)
    {
        if (pack == SoundPack.Mute) return;
        var (first, second) = Names(effect, pack);
        PlayNamed(first);
        if (second != null)
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
            t.Tick += (_, _) => { t.Stop(); PlayNamed(second); };
            t.Start();
        }
    }

    private static (string, string?) Names(SoundEffect e, SoundPack pack) => pack switch
    {
        SoundPack.MechanicalClick => e switch
        {
            SoundEffect.KeyTap => ("Pop", null),
            SoundEffect.FaderSlide => ("Tink", null),
            SoundEffect.KnobTick => ("Tink", null),
            SoundEffect.MascotSqueak => ("Blow", null),
            SoundEffect.PresetSwitch => ("Ping", null),
            SoundEffect.HardwareConnected => ("Ping", "Glass"),
            _ => ("Sosumi", null),
        },
        SoundPack.SciFiBeeps => e switch
        {
            SoundEffect.KeyTap => ("Morse", null),
            SoundEffect.FaderSlide => ("Purr", null),
            SoundEffect.KnobTick => ("Bottle", null),
            SoundEffect.MascotSqueak => ("Hero", null),
            SoundEffect.PresetSwitch => ("Submarine", null),
            SoundEffect.HardwareConnected => ("Submarine", "Hero"),
            _ => ("Blow", null),
        },
        _ => e switch // 8-BIT ARCADE (and the fallback for CUSTOM SOUNDS)
        {
            SoundEffect.KeyTap => ("Tink", null),
            SoundEffect.FaderSlide => ("Pop", null),
            SoundEffect.KnobTick => ("Morse", null),
            SoundEffect.MascotSqueak => ("Purr", null),
            SoundEffect.PresetSwitch => ("Glass", null),
            SoundEffect.HardwareConnected => ("Insert", "Hero"),
            _ => ("Basso", null),
        },
    };

    private static void PlayNamed(string name)
    {
        try
        {
            if (!MacToWindows.TryGetValue(name, out var file)) return;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", file);
            if (!File.Exists(path)) return;

            if (Players.TryGetValue(name, out var entry))
            {
                var duration = entry.Player.NaturalDuration.HasTimeSpan ? entry.Player.NaturalDuration.TimeSpan : TimeSpan.FromMilliseconds(250);
                if (DateTime.UtcNow - entry.Started < duration) return; // still playing
                entry.Player.Position = TimeSpan.Zero;
                entry.Player.Play();
                Players[name] = (entry.Player, DateTime.UtcNow);
                return;
            }

            var player = new MediaPlayer { Volume = 0.6 };
            player.Open(new Uri(path));
            player.Play();
            Players[name] = (player, DateTime.UtcNow);
        }
        catch
        {
            // Sound is decoration; never let it break the console.
        }
    }
}
