using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using static BitchManager.UI.Ui;

namespace BitchManager.UI;

public static class Leds
{
    /// <summary>The on-screen colour for a velocity, honouring the LED palette mode.</summary>
    public static Color For(BitchBoyEngine e, int vel)
    {
        if (vel == 0) return HardwareColor.Off;
        return e.LedPalette switch
        {
            LEDPaletteMode.CyberCyan => HardwareColor.Cyan,
            LEDPaletteMode.MatrixGreen => HardwareColor.Green,
            LEDPaletteMode.SolarAmber => HardwareColor.Amber,
            LEDPaletteMode.SynthMagenta => HardwareColor.Pink,
            LEDPaletteMode.ArcticWhite => HardwareColor.WhiteLed,
            _ => HardwareColor.ColorFor(vel),
        };
    }

    public static string ActionIcon(ActionType t) => t switch
    {
        ActionType.Qwerty => "command",
        ActionType.Osc => "antenna.radiowaves.left.and.right",
        ActionType.AppleScript => "applescript",
        ActionType.Msc => "dot.radiowaves.left.and.right",
        ActionType.ArtNet => "lightbulb.fill",
        ActionType.Sacn => "sparkles",
        _ => "plus",
    };
}

internal static class Draw
{
    public static FormattedText Text(Visual v, string s, double size, Color c, FontWeight? weight = null)
        => new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
               new Typeface(Ui.Mono, FontStyles.Normal, weight ?? FontWeights.Bold, FontStretches.Normal),
               size, Ui.B(c), VisualTreeHelper.GetDpi(v).PixelsPerDip);

    public static FormattedText Glyph(Visual v, string s, double size, Color c)
        => new(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
               new Typeface(Ui.Symbols, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
               size, Ui.B(c), VisualTreeHelper.GetDpi(v).PixelsPerDip);

    public static void Centered(DrawingContext dc, FormattedText t, double cx, double y)
        => dc.DrawText(t, new Point(cx - t.Width / 2, y));
}

/// <summary>Port of HardwareKeyCap: a 44x40 backlit mechanical key.</summary>
public sealed class KeyCap : FrameworkElement
{
    private static readonly Color Selected = C(0.22, 0.96, 0.76);
    private readonly BitchBoyEngine _e;
    private readonly int _index;
    private readonly ControlItem _item;
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly DropShadowEffect _glow = new() { ShadowDepth = 0, BlurRadius = 6, Opacity = 1 };
    private bool _hover;

    public KeyCap(BitchBoyEngine e, int index)
    {
        _e = e;
        _index = index;
        _item = e.Controls[index];
        Width = 44;
        Height = 40;
        Cursor = Cursors.Hand;
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = _scale;

        this.Observe(_item, Refresh, nameof(ControlItem.CurrentLED), nameof(ControlItem.CustomName), nameof(ControlItem.Label),
            nameof(ControlItem.ActionType), nameof(ControlItem.IsPressed), nameof(ControlItem.Number));
        this.Observe(e, Refresh, nameof(BitchBoyEngine.SelectedIndex), nameof(BitchBoyEngine.SelectedIndices),
            nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme), nameof(BitchBoyEngine.LedPalette));

        MouseEnter += (_, _) => { _hover = true; Refresh(); };
        MouseLeave += (_, _) => { _hover = false; Refresh(); };
        MouseLeftButtonDown += (_, ev) => { CaptureMouse(); ev.Handled = true; };
        MouseLeftButtonUp += (_, ev) =>
        {
            if (!IsMouseCaptured) return;
            ReleaseMouseCapture();
            if (IsMouseOver) _e.HandleKeyPressWithModifiers(_index, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
            ev.Handled = true;
        };
    }

    private bool IsSelected => _e.SelectedIndex == _index || _e.SelectedIndices.Contains(_index);

    private void Refresh()
    {
        var lit = _item.CurrentLED > 0;
        var color = Leds.For(_e, _item.CurrentLED);
        var s = _item.IsPressed ? 0.92 : _hover ? 1.08 : 1.0;
        _scale.ScaleX = s;
        _scale.ScaleY = s;
        if (_hover) { _glow.Color = lit ? color : Ui.White; _glow.Opacity = lit ? 0.95 : 0.4; _glow.BlurRadius = 7; Effect = _glow; }
        else if (IsSelected) { _glow.Color = Selected; _glow.Opacity = 0.8; _glow.BlurRadius = 6; Effect = _glow; }
        else if (lit) { _glow.Color = color; _glow.Opacity = 0.6; _glow.BlurRadius = 6; Effect = _glow; }
        else Effect = null;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var lit = _item.CurrentLED > 0;
        var color = Leds.For(_e, _item.CurrentLED);
        var rect = new Rect(0.5, 0.5, Width - 1, Height - 1);
        var fill = lit ? Gradient(color.Op(0.85), color.Op(0.40)) : Gradient(C(0.18, 0.08, 0.08), C(0.12, 0.05, 0.05));
        var sel = IsSelected;
        var stroke = sel ? Selected : _hover ? Ui.White.Op(0.9) : _e.Palette.Outline.Op(0.85);
        dc.DrawRoundedRectangle(fill, new Pen(Ui.B(stroke), sel || _hover ? 2 : 1), rect, 6, 6);

        var name = string.IsNullOrEmpty(_item.CustomName) ? _item.Label : _item.CustomName;
        var title = Draw.Text(this, name, 8, lit ? Ui.White : W(0.7));
        title.MaxTextWidth = Width - 4;
        title.MaxLineCount = 1;
        title.Trimming = TextTrimming.CharacterEllipsis;
        Draw.Centered(dc, title, Width / 2, 2);

        dc.DrawEllipse(null, new Pen(Ui.B(Ui.White.Op(0.2)), 1), new Point(Width / 2, Height / 2), 7, 7);
        var icon = Draw.Glyph(this, Icons.For(Leds.ActionIcon(_item.ActionType)), 7.5, lit ? Ui.White : W(0.65));
        dc.DrawText(icon, new Point(Width / 2 - icon.Width / 2, Height / 2 - icon.Height / 2));

        var num = Draw.Text(this, $"N{_item.Number}", 7, lit ? Ui.White.Op(0.9) : W(0.55));
        Draw.Centered(dc, num, Width / 2, Height - num.Height - 1);
    }
}

/// <summary>Port of ScrewRivetView.</summary>
public sealed class ScrewRivet : FrameworkElement
{
    public ScrewRivet(double size = 11)
    {
        Width = size;
        Height = size;
        Effect = new DropShadowEffect { ShadowDepth = 0.7, BlurRadius = 2, Opacity = 0.7, Direction = 315 };
    }

    protected override void OnRender(DrawingContext dc)
    {
        var s = Width;
        var g = new RadialGradientBrush
        {
            GradientOrigin = new Point(0, 0),
            Center = new Point(0, 0),
            RadiusX = 1.0,
            RadiusY = 1.0,
            GradientStops = { new GradientStop(W(0.94), 0), new GradientStop(W(0.65), 0.5), new GradientStop(W(0.35), 1) },
        };
        dc.DrawEllipse(g, null, new Point(s / 2, s / 2), s / 2, s / 2);
        var slot = Ui.B(W(0.18));
        dc.DrawRectangle(slot, null, new Rect(s / 2 - s * 0.325, s / 2 - 0.6, s * 0.65, 1.2));
        dc.DrawRectangle(slot, null, new Rect(s / 2 - 0.6, s / 2 - s * 0.325, 1.2, s * 0.65));
    }
}

/// <summary>Base for the analog controls: select on click, drag to change value, wheel to nudge.</summary>
public abstract class AnalogControl : FrameworkElement
{
    protected readonly BitchBoyEngine E;
    protected readonly int Index;
    protected readonly ControlItem Item;

    protected AnalogControl(BitchBoyEngine e, int index)
    {
        E = e;
        Index = index;
        Item = e.Controls[index];
        Cursor = Cursors.Hand;
        this.Observe(Item, InvalidateVisual, nameof(ControlItem.LiveValue), nameof(ControlItem.Label), nameof(ControlItem.Number));
        this.Observe(e, InvalidateVisual, nameof(BitchBoyEngine.SelectedIndex));
        MouseLeftButtonDown += (_, ev) => { CaptureMouse(); E.Select(Index); OnDragStart(ev.GetPosition(this)); ev.Handled = true; };
        MouseMove += (_, ev) => { if (IsMouseCaptured) OnDrag(ev.GetPosition(this)); };
        MouseLeftButtonUp += (_, _) => { if (IsMouseCaptured) ReleaseMouseCapture(); };
        MouseWheel += (_, ev) => { E.Select(Index); E.SetControlLiveValue(Index, Item.LiveValue + Math.Sign(ev.Delta) * 2); ev.Handled = true; };
    }

    protected bool IsSelected => E.SelectedIndex == Index;
    protected virtual void OnDragStart(Point p) => OnDrag(p);
    protected abstract void OnDrag(Point p);
    protected void SetValue(double v) => E.SetControlLiveValue(Index, (int)Math.Clamp(v, 0, 127));

    protected static readonly Color LabelSel = C(0.38, 0.94, 0.78);
    protected static readonly Color CcColor = C(0.94, 0.76, 0.38);

    protected static void DrawCap(DrawingContext dc, Rect r)
    {
        var shadow = r;
        shadow.Offset(1, 1);
        dc.DrawRoundedRectangle(Ui.B(Ui.Black.Op(0.5)), null, shadow, 3, 3);
        dc.DrawRoundedRectangle(Gradient(C(0.98, 0.52, 0.44), C(0.85, 0.32, 0.28), false), new Pen(Ui.B(Ui.White.Op(0.35)), 1), r, 3, 3);
    }
}

/// <summary>Port of HardwareVerticalFader (label, 42x110 track, CC label).</summary>
public sealed class VerticalFader : AnalogControl
{
    private const double TrackTop = 14, TrackH = 110;

    public VerticalFader(BitchBoyEngine e, int index) : base(e, index)
    {
        Width = 46;
        Height = TrackTop + TrackH + 18;
    }

    protected override void OnDrag(Point p)
    {
        var travel = TrackH - 20;
        var y = Math.Clamp(p.Y - TrackTop - 9, 0, travel);
        SetValue((1.0 - y / travel) * 127.0);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (IsSelected) dc.DrawRoundedRectangle(Ui.B(Ui.White.Op(0.06)), null, new Rect(0, 0, Width, Height), 5, 5);
        dc.DrawRectangle(Ui.B(Ui.Clear), null, new Rect(0, 0, Width, Height));
        Draw.Centered(dc, Draw.Text(this, Item.Label, 8, IsSelected ? LabelSel : W(0.65)), Width / 2, 2);

        var cx = Width / 2;
        dc.DrawRoundedRectangle(Ui.B(C(0.02, 0.04, 0.04)), new Pen(Ui.B(Ui.White.Op(0.14)), 1), new Rect(cx - 3, TrackTop, 6, TrackH), 3, 3);
        var capY = TrackTop + (1.0 - Item.LiveValue / 127.0) * (TrackH - 20);
        DrawCap(dc, new Rect(cx - 11, capY, 22, 18));
        dc.DrawRectangle(Ui.B(Ui.White.Op(0.85)), null, new Rect(cx - 7, capY + 8.25, 14, 1.5));

        Draw.Centered(dc, Draw.Text(this, $"CC{Item.Number}", 7, CcColor), cx, TrackTop + TrackH + 4);
    }
}

/// <summary>Port of HardwareHorizontalFader.</summary>
public sealed class HorizontalFader : AnalogControl
{
    private const double TrackX = 36, LabelW = 36;

    public HorizontalFader(BitchBoyEngine e, int index) : base(e, index)
    {
        Height = 28;
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    private double TrackW => Math.Max(30, ActualWidth - TrackX - LabelW - 8);

    protected override void OnDrag(Point p)
    {
        var travel = TrackW - 20;
        var x = Math.Clamp(p.X - TrackX - 9, 0, travel);
        SetValue(x / travel * 127.0);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        dc.DrawRoundedRectangle(Ui.B(IsSelected ? Ui.White.Op(0.06) : Ui.Clear), null, new Rect(0, 0, w, Height), 5, 5);
        var label = Draw.Text(this, Item.Label, 8.5, IsSelected ? LabelSel : W(0.65));
        dc.DrawText(label, new Point(4, Height / 2 - label.Height / 2));

        var mid = Height / 2;
        dc.DrawRoundedRectangle(Ui.B(C(0.02, 0.04, 0.04)), new Pen(Ui.B(Ui.White.Op(0.12)), 1), new Rect(TrackX, mid - 3, TrackW, 6), 3, 3);
        var capX = TrackX + Item.LiveValue / 127.0 * (TrackW - 20);
        DrawCap(dc, new Rect(capX, mid - 11, 18, 22));
        dc.DrawRectangle(Ui.B(Ui.Black.Op(0.75)), null, new Rect(capX + 8.25, mid - 8, 1.5, 16));

        var cc = Draw.Text(this, $"CC{Item.Number}", 7.5, CcColor);
        dc.DrawText(cc, new Point(w - cc.Width - 4, mid - cc.Height / 2));
    }
}

/// <summary>Port of HardwareKnob (drag up/down to turn).</summary>
public sealed class Knob : AnalogControl
{
    private static readonly Color Sel = C(0.22, 0.96, 0.76);
    private double _startY;
    private int _startValue;

    public Knob(BitchBoyEngine e, int index) : base(e, index)
    {
        Width = 62;
        Height = 50;
    }

    protected override void OnDragStart(Point p)
    {
        _startY = p.Y;
        _startValue = Item.LiveValue;
    }

    protected override void OnDrag(Point p) => SetValue(_startValue + (_startY - p.Y) / 150.0 * 127.0);

    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Ui.B(Ui.Clear), null, new Rect(0, 0, Width, Height));
        var c = new Point(Width / 2, 18);
        var face = new RadialGradientBrush(W(0.22), W(0.10));
        if (IsSelected) dc.DrawEllipse(Ui.B(Sel.Op(0.25)), null, c, 19, 19);
        dc.DrawEllipse(face, new Pen(Ui.B(IsSelected ? Sel : Ui.White.Op(0.2)), IsSelected ? 1.8 : 1), c, 16, 16);

        var angle = Item.LiveValue / 127.0 * 270.0 - 135.0;
        dc.PushTransform(new RotateTransform(angle, c.X, c.Y));
        dc.DrawRectangle(Ui.B(W(0.98)), null, new Rect(c.X - 1, c.Y - 12, 2, 10));
        dc.Pop();

        var label = Draw.Text(this, Item.Label, 7.5, IsSelected ? Sel : C(0.45, 0.88, 0.75));
        var cc = Draw.Text(this, $"CC{Item.Number}", 7, C(0.96, 0.78, 0.35));
        var total = label.Width + 3 + cc.Width;
        dc.DrawText(label, new Point(Width / 2 - total / 2, 38));
        dc.DrawText(cc, new Point(Width / 2 - total / 2 + label.Width + 3, 38.5));
    }
}

/// <summary>The glowing white line-art images (logo and mascot) with hover/press feedback.</summary>
public sealed class GlowImage : Clickable
{
    private readonly DropShadowEffect _glow = new() { ShadowDepth = 0, BlurRadius = 10, Opacity = 0.85 };

    public GlowImage(string resource, double width, double height, Action onTap, BitchBoyEngine e) : base(onTap, 1.06)
    {
        Background = Brushes.Transparent;
        var img = new Image
        {
            Source = LoadResource(resource),
            Stretch = Stretch.Uniform,
            Width = width,
            Height = height,
            Effect = _glow,
        };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
        Child = img;
        this.Observe(e, () => _glow.Color = e.Palette.Accent, nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme));
        MouseEnter += (_, _) => { _glow.Opacity = 1.0; _glow.BlurRadius = 16; };
        MouseLeave += (_, _) => { _glow.Opacity = 0.85; _glow.BlurRadius = 10; };
    }

    public static BitmapImage? LoadResource(string name)
    {
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri($"pack://application:,,,/Assets/{name}", UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>A mini canvas key in the FX sequencer (paints the current frame).</summary>
public sealed class MiniCanvasKey : FrameworkElement
{
    private readonly BitchBoyEngine _e;
    private readonly int _k;

    public MiniCanvasKey(BitchBoyEngine e, int keyIndex)
    {
        _e = e;
        _k = keyIndex;
        Width = 17;
        Height = 15;
        Cursor = Cursors.Hand;
        this.Observe(e, InvalidateVisual, nameof(BitchBoyEngine.EditingCustomFX), nameof(BitchBoyEngine.EditingFrameIndex), nameof(BitchBoyEngine.LedPalette));
        MouseLeftButtonDown += (_, ev) => { _e.PaintCustomFXKey(_k, _e.CustomBrushVelocity); ev.Handled = true; };
    }

    protected override void OnRender(DrawingContext dc)
    {
        var fx = _e.EditingCustomFX;
        var fi = Math.Clamp(_e.EditingFrameIndex, 0, Math.Max(0, fx.Frames.Count - 1));
        var vel = fx.Frames.Count > 0 && _k < fx.Frames[fi].Length ? fx.Frames[fi][_k] : 0;
        dc.DrawRoundedRectangle(Ui.B(vel == 0 ? W(0.14) : Leds.For(_e, vel)),
            new Pen(Ui.B(Ui.White.Op(vel > 0 ? 0.6 : 0.15)), 0.8), new Rect(0.4, 0.4, Width - 0.8, Height - 0.8), 3, 3);
    }
}
