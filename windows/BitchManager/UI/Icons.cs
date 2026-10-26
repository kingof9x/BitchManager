namespace BitchManager.UI;

/// <summary>SF Symbols used by the Mac app, mapped to Segoe UI Symbol / emoji glyphs.</summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Map = new()
    {
        ["music.mic"] = "🎤", ["slider.horizontal.3"] = "🎚", ["sparkles"] = "✨", ["square.grid.3x3.fill"] = "▦",
        ["pianokeys"] = "🎹", ["waveform.path.ecg"] = "〰", ["tv.fill"] = "📺", ["cpu.fill"] = "▣",
        ["bolt.fill"] = "⚡", ["flame.fill"] = "🔥", ["memorychip.fill"] = "▤", ["wand.and.stars"] = "✨",
        ["dot.radiowaves.left.and.right"] = "📡", ["point.3.connected.trianglepath.dotted"] = "🔀",
        ["waveform.circle.fill"] = "◉", ["play.fill"] = "▶", ["stop.fill"] = "■", ["pin"] = "📌", ["pin.fill"] = "📌",
        ["plus"] = "＋", ["command"] = "⌘", ["antenna.radiowaves.left.and.right"] = "📶", ["applescript"] = "📜",
        ["lightbulb.fill"] = "💡", ["power"] = "⏻", ["rainbow"] = "🌈", ["bolt.horizontal.fill"] = "⚡",
        ["cloud.rain.fill"] = "🌧", ["waveform.path"] = "〰", ["slider.vertical.3"] = "🎛", ["circle.dotted"] = "◌",
        ["heart.fill"] = "❤", ["wind"] = "🌀", ["circle.hexagonpath"] = "⬡", ["chart.bar.xaxis"] = "📊",
        ["arrow.triangle.2.circlepath"] = "🔁", ["scope"] = "⊕", ["sun.max.fill"] = "☀", ["cloud.bolt.rain.fill"] = "⛈",
        ["poweroff"] = "⏻", ["keyboard.fill"] = "⌨", ["dial.medium.fill"] = "◎", ["film.stack"] = "🎞",
        ["checkmark"] = "✓", ["checkmark.circle.fill"] = "✔", ["circle"] = "○", ["chevron.down"] = "▾",
        ["magnifyingglass"] = "🔍", ["xmark.circle.fill"] = "✕", ["paperplane.fill"] = "✈",
        ["paintbrush"] = "🖌", ["paintbrush.pointed.fill"] = "🖌", ["paintpalette"] = "🎨", ["paintpalette.fill"] = "🎨",
        ["doc.on.doc.fill"] = "⧉", ["arrow.up.doc.fill"] = "⤓", ["arrow.down.doc.fill"] = "⤒",
        ["arrow.counterclockwise"] = "↺", ["externaldrive.fill.badge.checkmark"] = "💾", ["music.note.list"] = "♫",
        ["exclamationmark.triangle.fill"] = "⚠", ["hand.tap.fill"] = "👆", ["macwindow.badge.plus"] = "⧉",
        ["macwindow"] = "▢", ["macwindow.on.rectangle"] = "⧉",
        ["arrow.up.left.and.arrow.down.right"] = "⤢", ["arrow.down.right.and.arrow.up.left"] = "⤡",
        ["square.split.bottomrightquarter"] = "▣", ["speaker.wave.2.fill"] = "🔊", ["speaker.slash.fill"] = "🔇",
        ["waveform.badge.mic"] = "🎙", ["square.grid.2x2.fill"] = "▦", ["gamecontroller.fill"] = "🎮",
        ["square"] = "☐", ["checkmark.square.fill"] = "☑", ["checkmark.seal.fill"] = "✔",
        ["arrow.up.and.down"] = "↕", ["arrow.left.and.right"] = "↔", ["arrow.up"] = "↑", ["arrow.down"] = "↓",
        ["rectangle.bottomthird.inset.filled"] = "▁", ["sidebar.left"] = "◧", ["sidebar.right"] = "◨",
        ["arrow.down.and.line.horizontal.and.arrow.up"] = "⇕", ["plus.circle.fill"] = "⊕", ["plus.circle.dashed"] = "⊕",
        ["hand.draw.fill"] = "✋", ["arrow.down.left.square.fill"] = "↙", ["photo.badge.checkmark"] = "🖼",
        ["slider.horizontal.below.rectangle"] = "🎛", ["bolt.circle.fill"] = "⚡", ["play.circle.fill"] = "▶",
        ["arrow.down.to.line"] = "⤓", ["arrow.up.to.line"] = "⤒", ["moon.fill"] = "☾", ["xmark.octagon.fill"] = "⛔",
        ["slider.horizontal.2.square.on.square"] = "🎚", ["folder.fill"] = "📁", ["arrow.down.circle.fill"] = "⬇",
        ["stop.circle"] = "■", ["playpause.fill"] = "⏯", ["timer"] = "⏱", ["arrow.clockwise"] = "↻",
        ["minus.magnifyingglass"] = "−", ["plus.magnifyingglass"] = "+", ["wand.and.rays"] = "✨",
    };

    public static string For(string sfSymbol) => Map.TryGetValue(sfSymbol, out var g) ? g : "•";
}
