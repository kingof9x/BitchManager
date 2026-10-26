using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitchManager.Platform;

/// <summary>
/// QWERTY macro and script actions. The Mac app drives System Events through osascript; on
/// Windows keystrokes go through SendInput and the "AppleScript" action runs PowerShell.
/// The Mac "cmd" modifier maps to Ctrl, which is what Mac shortcuts use on Windows.
/// </summary>
public static class KeyboardMacro
{
    private const ushort VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12;

    private static readonly Dictionary<string, ushort> Named = new()
    {
        ["space"] = 0x20, ["return"] = 0x0D, ["enter"] = 0x0D, ["escape"] = 0x1B, ["esc"] = 0x1B,
        ["tab"] = 0x09, ["delete"] = 0x08, ["up"] = 0x26, ["down"] = 0x28, ["left"] = 0x25, ["right"] = 0x27,
        ["f1"] = 0x70, ["f2"] = 0x71, ["f3"] = 0x72, ["f4"] = 0x73, ["f5"] = 0x74, ["f6"] = 0x75,
        ["f7"] = 0x76, ["f8"] = 0x77, ["f9"] = 0x78, ["f10"] = 0x79, ["f11"] = 0x7A, ["f12"] = 0x7B,
    };

    /// <summary>Fires one key (or types a short string) with the given modifiers held.</summary>
    public static void Fire(string key, bool cmd, bool shift, bool alt, bool ctrl)
    {
        var k = key.Trim().ToLowerInvariant();
        if (k.Length == 0) return;

        var mods = new List<ushort>();
        if (cmd || ctrl) mods.Add(VK_CONTROL);
        if (shift) mods.Add(VK_SHIFT);
        if (alt) mods.Add(VK_MENU);

        var inputs = new List<INPUT>();
        foreach (var m in mods) inputs.Add(Vk(m, false));

        if (Named.TryGetValue(k, out var vk))
        {
            inputs.Add(Vk(vk, false));
            inputs.Add(Vk(vk, true));
        }
        else if (mods.Count > 0 && k.Length == 1 && TryVkFor(k[0], out var charVk))
        {
            // Shortcuts like Ctrl+S need the virtual key, not a unicode character.
            inputs.Add(Vk(charVk, false));
            inputs.Add(Vk(charVk, true));
        }
        else
        {
            foreach (var ch in k)
            {
                inputs.Add(Unicode(ch, false));
                inputs.Add(Unicode(ch, true));
            }
        }

        for (var i = mods.Count - 1; i >= 0; i--) inputs.Add(Vk(mods[i], true));
        try { SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>()); } catch { }
    }

    /// <summary>Runs the action's script text with PowerShell (the Windows stand-in for osascript).</summary>
    public static void RunScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script)) return;
        try
        {
            var psi = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-ExecutionPolicy");
            psi.ArgumentList.Add("Bypass");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(script);
            Process.Start(psi);
        }
        catch { }
    }

    private static bool TryVkFor(char c, out ushort vk)
    {
        vk = 0;
        try
        {
            var r = VkKeyScan(c);
            if (r == -1) return false;
            vk = (ushort)(r & 0xFF);
            return true;
        }
        catch { return false; }
    }

    private static INPUT Vk(ushort vk, bool up) => new()
    {
        type = 1,
        U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = up ? 0x0002u : 0u } },
    };

    private static INPUT Unicode(char c, bool up) => new()
    {
        type = 1,
        U = new InputUnion { ki = new KEYBDINPUT { wScan = c, dwFlags = 0x0004u | (up ? 0x0002u : 0u) } },
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScan(char ch);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }
}
