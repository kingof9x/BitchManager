using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BitchManager.Core;

namespace BitchManager.UI;

/// <summary>
/// A tiny SwiftUI-flavoured toolkit for building the WPF UI in code: stacks with spacers,
/// text, pill buttons, menus, fields and re-rendering hosts.
/// </summary>
public static class Ui
{
    public static readonly FontFamily Mono = new("Cascadia Mono, Consolas, Segoe UI Symbol, Segoe UI Emoji");
    public static readonly FontFamily Symbols = new("Segoe UI Symbol, Segoe UI Emoji, Segoe MDL2 Assets");

    public static Color C(double r, double g, double b, double a = 1) => Models.HardwareColor.Rgb(r, g, b, a);
    public static Color W(double w, double a = 1) => Models.HardwareColor.White(w, a);
    public static Color Op(this Color c, double opacity) => Color.FromArgb((byte)Math.Round(c.A * Math.Clamp(opacity, 0, 1)), c.R, c.G, c.B);

    public static readonly Color Black = Colors.Black;
    public static readonly Color White = Colors.White;
    public static readonly Color Clear = Colors.Transparent;
    public static readonly Color Green = C(0.2, 0.84, 0.29);
    public static readonly Color Red = C(1.0, 0.23, 0.19);
    public static readonly Color Orange = C(1.0, 0.58, 0.0);
    public static readonly Color Yellow = C(1.0, 0.8, 0.0);
    public static readonly Color Cyan = C(0.35, 0.78, 0.98);
    public static readonly Color Purple = C(0.69, 0.32, 0.87);
    public static readonly Color Gold = C(0.96, 0.76, 0.18);
    public static readonly Color Amber = C(0.96, 0.78, 0.35);
    public static readonly Color Mint = C(0.22, 0.96, 0.76);

    private static readonly Dictionary<Color, SolidColorBrush> BrushCache = new();

    public static SolidColorBrush B(Color c)
    {
        if (BrushCache.TryGetValue(c, out var b)) return b;
        b = new SolidColorBrush(c);
        b.Freeze();
        if (BrushCache.Count < 4096) BrushCache[c] = b;
        return b;
    }

    public static Brush Gradient(Color a, Color b, bool diagonal = true)
    {
        var g = new LinearGradientBrush(a, b, diagonal ? new Point(0, 0) : new Point(0.5, 0), diagonal ? new Point(1, 1) : new Point(0.5, 1));
        g.Freeze();
        return g;
    }

    // ------------------------------------------------------------------------
    // Text & icons
    // ------------------------------------------------------------------------
    public static TextBlock T(string text, double size, Color fg, FontWeight? weight = null) => new()
    {
        Text = text,
        FontFamily = Mono,
        FontSize = size * 1.0,
        FontWeight = weight ?? FontWeights.Heavy,
        Foreground = B(fg),
        TextTrimming = TextTrimming.CharacterEllipsis,
        VerticalAlignment = VerticalAlignment.Center,
    };

    public static TextBlock Wrap(this TextBlock t)
    {
        t.TextWrapping = TextWrapping.Wrap;
        t.TextTrimming = TextTrimming.None;
        return t;
    }

    public static TextBlock Icon(string sfSymbol, double size, Color fg) => new()
    {
        Text = Icons.For(sfSymbol),
        FontFamily = Symbols,
        FontSize = size,
        Foreground = B(fg),
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center,
    };

    public static Ellipse Dot(double size, Color fill) => new() { Width = size, Height = size, Fill = B(fill), VerticalAlignment = VerticalAlignment.Center };

    // ------------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------------
    public static Stack H(double spacing, params UIElement?[] kids) => new Stack(Orientation.Horizontal, spacing, kids);
    public static Stack V(double spacing, params UIElement?[] kids) => new Stack(Orientation.Vertical, spacing, kids);
    public static Spacer Spacer(double min = 0) => new() { Min = min };

    public static T Fill<T>(this T el) where T : UIElement
    {
        Stack.SetFill(el, true);
        return el;
    }

    public static T Size<T>(this T el, double w = double.NaN, double h = double.NaN) where T : FrameworkElement
    {
        if (!double.IsNaN(w)) el.Width = w;
        if (!double.IsNaN(h)) el.Height = h;
        return el;
    }

    public static T Margin<T>(this T el, double l, double t, double r, double b) where T : FrameworkElement
    {
        el.Margin = new Thickness(l, t, r, b);
        return el;
    }

    public static T Align<T>(this T el, HorizontalAlignment h, VerticalAlignment v = VerticalAlignment.Stretch) where T : FrameworkElement
    {
        el.HorizontalAlignment = h;
        el.VerticalAlignment = v;
        return el;
    }

    public static T Tip<T>(this T el, string tip) where T : FrameworkElement
    {
        el.ToolTip = tip;
        return el;
    }

    public static Border Box(UIElement? child, Color bg, double radius = 6, double padH = 6, double padV = 6,
        Color? stroke = null, double strokeWidth = 1) => new()
    {
        Child = child,
        Background = B(bg),
        CornerRadius = new CornerRadius(radius),
        Padding = new Thickness(padH, padV, padH, padV),
        BorderBrush = stroke.HasValue ? B(stroke.Value) : null,
        BorderThickness = stroke.HasValue ? new Thickness(strokeWidth) : new Thickness(0),
    };

    /// <summary>An n-column grid with equal widths, like LazyVGrid with flexible columns.</summary>
    public static UIElement Grid(int columns, double spacing, IEnumerable<UIElement> kids)
    {
        var g = new UniformGrid { Columns = columns, Margin = new Thickness(-spacing / 2) };
        foreach (var k in kids) g.Children.Add(new Border { Child = k, Margin = new Thickness(spacing / 2) });
        return g;
    }

    public static ScrollViewer Scroll(UIElement content, bool horizontal = false) => new()
    {
        Content = content,
        VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = horizontal ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled,
        Focusable = false,
        PanningMode = horizontal ? PanningMode.HorizontalOnly : PanningMode.VerticalOnly,
    };

    // ------------------------------------------------------------------------
    // Buttons
    // ------------------------------------------------------------------------
    public static Clickable Btn(UIElement content, Action onClick, Color bg, double radius = 4, double padH = 6, double padV = 3,
        Color? stroke = null, double hover = 1.05) => new(onClick, hover)
    {
        Child = content,
        Background = B(bg),
        CornerRadius = new CornerRadius(radius),
        Padding = new Thickness(padH, padV, padH, padV),
        BorderBrush = stroke.HasValue ? B(stroke.Value) : null,
        BorderThickness = stroke.HasValue ? new Thickness(1) : new Thickness(0),
    };

    public static Clickable TextBtn(string text, double size, Color fg, Color bg, Action onClick, double radius = 4,
        double padH = 6, double padV = 3, Color? stroke = null)
    {
        var t = T(text, size, fg);
        t.HorizontalAlignment = HorizontalAlignment.Center;
        return Btn(t, onClick, bg, radius, padH, padV, stroke);
    }

    public static Clickable IconTextBtn(string icon, string text, double size, Color fg, Color bg, Action onClick,
        double radius = 4, double padH = 6, double padV = 3, Color? stroke = null)
    {
        var row = H(3, Icon(icon, size + 1, fg), T(text, size, fg));
        row.HorizontalAlignment = HorizontalAlignment.Center;
        return Btn(row, onClick, bg, radius, padH, padV, stroke);
    }

    // ------------------------------------------------------------------------
    // Menus (SwiftUI Menu → WPF ContextMenu)
    // ------------------------------------------------------------------------
    public sealed record MenuEntry(string Header, Action? Click = null, bool Checked = false, bool Separator = false,
        bool Disabled = false, List<MenuEntry>? Children = null)
    {
        public static MenuEntry Sep => new("", Separator: true);
        public static MenuEntry Label(string text) => new(text, Disabled: true);
    }

    public static void ShowMenu(FrameworkElement anchor, IEnumerable<MenuEntry> entries)
    {
        var menu = BuildMenu(entries);
        menu.PlacementTarget = anchor;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private static ContextMenu BuildMenu(IEnumerable<MenuEntry> entries)
    {
        var menu = new ContextMenu
        {
            Background = B(C(0.07, 0.09, 0.09)),
            Foreground = B(White),
            BorderBrush = B(W(1, 0.18)),
            FontFamily = Mono,
            FontSize = 11,
        };
        foreach (var e in entries) menu.Items.Add(BuildItem(e));
        return menu;
    }

    private static object BuildItem(MenuEntry e)
    {
        if (e.Separator) return new Separator();
        var item = new MenuItem
        {
            Header = e.Header,
            IsCheckable = false,
            IsChecked = e.Checked,
            IsEnabled = !e.Disabled,
            Foreground = B(e.Disabled ? W(0.6) : White),
            Background = B(C(0.07, 0.09, 0.09)),
        };
        if (e.Children != null) foreach (var c in e.Children) item.Items.Add(BuildItem(c));
        if (e.Click != null) item.Click += (_, _) => e.Click();
        return item;
    }

    public static Clickable MenuBtn(UIElement content, Func<IEnumerable<MenuEntry>> entries, Color bg, double radius = 4,
        double padH = 6, double padV = 3, Color? stroke = null)
    {
        Clickable? btn = null;
        btn = Btn(content, () => ShowMenu(btn!, entries()), bg, radius, padH, padV, stroke);
        return btn;
    }

    // ------------------------------------------------------------------------
    // Inputs
    // ------------------------------------------------------------------------
    /// <summary>A dark text field. Writes go straight to the model; model changes refresh it unless it has focus.</summary>
    public static TextBox Field(Func<string> get, Action<string> set, double size = 8, INotifyPropertyChanged? source = null,
        string? prop = null, string placeholder = "")
    {
        var tb = new TextBox
        {
            Text = get(),
            FontFamily = Mono,
            FontSize = size * 1.15,
            FontWeight = FontWeights.Bold,
            Foreground = B(White),
            CaretBrush = B(White),
            SelectionBrush = B(Mint.Op(0.5)),
            Background = B(Black.Op(0.5)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 2, 4, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = string.IsNullOrEmpty(placeholder) ? null : placeholder,
        };
        var updating = false;
        tb.TextChanged += (_, _) => { if (!updating) set(tb.Text); };
        if (source != null)
        {
            tb.Observe(source, () =>
            {
                if (tb.IsKeyboardFocusWithin) return;
                var v = get();
                if (tb.Text == v) return;
                updating = true;
                tb.Text = v;
                updating = false;
            }, prop == null ? [] : [prop]);
        }
        tb.KeyDown += (_, e) => { if (e.Key == Key.Enter) Keyboard.ClearFocus(); };
        return tb;
    }

    public static TextBox IntField(Func<int> get, Action<int> set, double size = 8, INotifyPropertyChanged? source = null, string? prop = null)
        => Field(() => get().ToString(CultureInfo.InvariantCulture),
                 s => { if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) set(v); },
                 size, source, prop);

    public static TextBox DoubleField(Func<double> get, Action<double> set, double size = 8, INotifyPropertyChanged? source = null, string? prop = null)
        => Field(() => get().ToString("0.###", CultureInfo.InvariantCulture),
                 s => { if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) set(v); },
                 size, source, prop);

    /// <summary>Caption above a control, as used all over the Mac panels ("HOST IP:" over a field).</summary>
    public static Stack Labeled(string caption, UIElement control, double captionSize = 6.5)
        => V(1, T(caption, captionSize, W(0.6), FontWeights.Bold), control);

    public static Clickable CheckRow(Func<bool> get, Action<bool> set, UIElement label, Color accent,
        INotifyPropertyChanged source, string prop)
    {
        var box = Icon("square", 10, W(0.5));
        var btn = Btn(H(6, box, label), () => set(!get()), Clear, 4, 0, 0, hover: 1.0);
        btn.Observe(source, () =>
        {
            var on = get();
            box.Text = Icons.For(on ? "checkmark.square.fill" : "square");
            box.Foreground = B(on ? accent : W(0.5));
        }, prop);
        return btn;
    }

    // ------------------------------------------------------------------------
    // Re-rendering host
    // ------------------------------------------------------------------------
    /// <summary>A container whose content is rebuilt whenever one of the named properties changes.</summary>
    public static DynHost Dyn(INotifyPropertyChanged source, Func<UIElement> build, params string[] props)
    {
        var host = new DynHost(build);
        host.Observe(source, host.Rebuild, props);
        return host;
    }
}

public sealed class DynHost : Border
{
    private readonly Func<UIElement> _build;
    public DynHost(Func<UIElement> build) => _build = build;
    public void Rebuild() => Child = _build();

    public DynHost Also(INotifyPropertyChanged source, params string[] props)
    {
        this.Observe(source, Rebuild, props);
        return this;
    }
}

/// <summary>SwiftUI Spacer: takes up the free space in a <see cref="Stack"/>.</summary>
public sealed class Spacer : FrameworkElement
{
    public double Min { get; set; }
}

/// <summary>HStack / VStack with spacing, Spacer support and "fill" children (maxWidth: .infinity).</summary>
public sealed class Stack : Panel
{
    public static readonly DependencyProperty FillProperty =
        DependencyProperty.RegisterAttached("Fill", typeof(bool), typeof(Stack),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public static void SetFill(UIElement el, bool v) => el.SetValue(FillProperty, v);
    public static bool GetFill(UIElement el) => (bool)el.GetValue(FillProperty);

    public Orientation Orientation { get; }
    public double Spacing { get; set; }
    /// <summary>Cross-axis alignment. Null means "use each child's own alignment".</summary>
    public HorizontalAlignment? CrossH { get; set; }
    public VerticalAlignment? CrossV { get; set; }

    public Stack(Orientation o, double spacing, IEnumerable<UIElement?> kids)
    {
        Orientation = o;
        Spacing = spacing;
        if (o == Orientation.Horizontal) CrossV = VerticalAlignment.Center;
        foreach (var k in kids) if (k != null) Children.Add(k);
    }

    public Stack Add(UIElement? el)
    {
        if (el != null) Children.Add(el);
        return this;
    }

    private bool Horizontal => Orientation == Orientation.Horizontal;
    private static bool IsFlex(UIElement e) => e is Spacer || GetFill(e);
    private static double Main(Size s, bool h) => h ? s.Width : s.Height;
    private static double Cross(Size s, bool h) => h ? s.Height : s.Width;
    private static Size Make(double main, double cross, bool h) => h ? new Size(main, cross) : new Size(cross, main);

    protected override Size MeasureOverride(Size available)
    {
        var h = Horizontal;
        var availMain = Main(available, h);
        var availCross = Cross(available, h);
        double fixedMain = 0, cross = 0, minFlex = 0;
        int visible = 0, flex = 0;

        foreach (UIElement c in InternalChildren)
        {
            if (c.Visibility == Visibility.Collapsed) continue;
            visible++;
            if (IsFlex(c)) { flex++; if (c is Spacer sp) minFlex += sp.Min; continue; }
            c.Measure(Make(double.PositiveInfinity, availCross, h));
            fixedMain += Main(c.DesiredSize, h);
            cross = Math.Max(cross, Cross(c.DesiredSize, h));
        }

        var spacing = Math.Max(0, visible - 1) * Spacing;
        var share = flex > 0 && !double.IsInfinity(availMain) ? Math.Max(0, (availMain - fixedMain - spacing) / flex) : 0;
        double flexMain = 0;
        foreach (UIElement c in InternalChildren)
        {
            if (c.Visibility == Visibility.Collapsed || !IsFlex(c)) continue;
            if (c is Spacer s) { c.Measure(new Size(0, 0)); flexMain += Math.Max(s.Min, share); continue; }
            c.Measure(Make(double.IsInfinity(availMain) ? double.PositiveInfinity : share, availCross, h));
            flexMain += double.IsInfinity(availMain) ? Main(c.DesiredSize, h) : share;
            cross = Math.Max(cross, Cross(c.DesiredSize, h));
        }

        var main = fixedMain + spacing + Math.Max(flexMain, minFlex);
        if (flex > 0 && !double.IsInfinity(availMain)) main = Math.Max(main, availMain);
        return Make(main, cross, h);
    }

    protected override Size ArrangeOverride(Size final)
    {
        var h = Horizontal;
        var finalMain = Main(final, h);
        var finalCross = Cross(final, h);
        double fixedMain = 0;
        int visible = 0, flex = 0;
        foreach (UIElement c in InternalChildren)
        {
            if (c.Visibility == Visibility.Collapsed) continue;
            visible++;
            if (IsFlex(c)) flex++;
            else fixedMain += Main(c.DesiredSize, h);
        }
        var spacing = Math.Max(0, visible - 1) * Spacing;
        var share = flex > 0 ? Math.Max(0, (finalMain - fixedMain - spacing) / flex) : 0;

        double pos = 0;
        foreach (UIElement c in InternalChildren)
        {
            if (c.Visibility == Visibility.Collapsed) continue;
            var len = IsFlex(c) ? (c is Spacer s ? Math.Max(s.Min, share) : share) : Main(c.DesiredSize, h);
            var desiredCross = Cross(c.DesiredSize, h);
            double crossPos = 0, crossLen = finalCross;
            if (h)
            {
                var va = CrossV ?? (c as FrameworkElement)?.VerticalAlignment ?? VerticalAlignment.Stretch;
                if (va != VerticalAlignment.Stretch) crossLen = Math.Min(desiredCross, finalCross);
                crossPos = va switch
                {
                    VerticalAlignment.Center => (finalCross - crossLen) / 2,
                    VerticalAlignment.Bottom => finalCross - crossLen,
                    _ => 0,
                };
                c.Arrange(new Rect(pos, crossPos, len, crossLen));
            }
            else
            {
                var ha = CrossH ?? (c as FrameworkElement)?.HorizontalAlignment ?? HorizontalAlignment.Stretch;
                if (ha != HorizontalAlignment.Stretch) crossLen = Math.Min(desiredCross, finalCross);
                crossPos = ha switch
                {
                    HorizontalAlignment.Center => (finalCross - crossLen) / 2,
                    HorizontalAlignment.Right => finalCross - crossLen,
                    _ => 0,
                };
                c.Arrange(new Rect(crossPos, pos, crossLen, len));
            }
            pos += len + Spacing;
        }
        return final;
    }
}

/// <summary>A Border that behaves like a plain SwiftUI Button, with the hover-grow effect.</summary>
public class Clickable : Border
{
    private readonly Action _onClick;
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly double _hover;

    public Clickable(Action onClick, double hover = 1.05)
    {
        _onClick = onClick;
        _hover = hover;
        Cursor = Cursors.Hand;
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = _scale;
        Background = Brushes.Transparent;
        Focusable = false;
        MouseEnter += (_, _) => SetScale(_hover);
        MouseLeave += (_, _) => SetScale(1.0);
        MouseLeftButtonDown += (_, e) =>
        {
            CaptureMouse();
            SetScale(0.96);
            e.Handled = true;
        };
        MouseLeftButtonUp += (_, e) =>
        {
            if (!IsMouseCaptured) return;
            ReleaseMouseCapture();
            SetScale(IsMouseOver ? _hover : 1.0);
            var p = e.GetPosition(this);
            if (p.X >= 0 && p.Y >= 0 && p.X <= ActualWidth && p.Y <= ActualHeight) _onClick();
            e.Handled = true;
        };
    }

    private void SetScale(double s)
    {
        _scale.ScaleX = s;
        _scale.ScaleY = s;
    }
}

/// <summary>A dark horizontal slider (SwiftUI Slider with an accent colour).</summary>
public sealed class MiniSlider : FrameworkElement
{
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 1;
    public double Step { get; set; }
    public Color Accent { get; set; } = Ui.Mint;
    private double _value;
    public event Action<double>? ValueChanged;
    /// <summary>Raised once when a drag ends (SwiftUI's onEditingChanged(false)).</summary>
    public event Action? DragEnded;

    public MiniSlider()
    {
        Height = 16;
        MinWidth = 40;
        Cursor = Cursors.Hand;
        MouseLeftButtonDown += (_, e) => { CaptureMouse(); FromMouse(e.GetPosition(this).X); e.Handled = true; };
        MouseMove += (_, e) => { if (IsMouseCaptured) FromMouse(e.GetPosition(this).X); };
        MouseLeftButtonUp += (_, _) => { if (IsMouseCaptured) { ReleaseMouseCapture(); DragEnded?.Invoke(); } };
        MouseWheel += (_, e) =>
        {
            var step = Step > 0 ? Step : (Maximum - Minimum) / 100;
            SetFromUser(_value + Math.Sign(e.Delta) * step);
            e.Handled = true;
        };
    }

    public double Value
    {
        get => _value;
        set { _value = Math.Clamp(value, Minimum, Maximum); InvalidateVisual(); }
    }

    private void FromMouse(double x)
    {
        var w = Math.Max(1, ActualWidth - 10);
        SetFromUser(Minimum + Math.Clamp((x - 5) / w, 0, 1) * (Maximum - Minimum));
    }

    private void SetFromUser(double v)
    {
        if (Step > 0) v = Minimum + Math.Round((v - Minimum) / Step) * Step;
        v = Math.Clamp(v, Minimum, Maximum);
        if (Math.Abs(v - _value) < 1e-9) return;
        Value = v;
        ValueChanged?.Invoke(v);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var mid = ActualHeight / 2;
        var frac = Maximum > Minimum ? (_value - Minimum) / (Maximum - Minimum) : 0;
        var x = 5 + frac * Math.Max(0, w - 10);
        dc.DrawRoundedRectangle(Ui.B(Ui.W(1, 0.15)), null, new Rect(0, mid - 2, w, 4), 2, 2);
        dc.DrawRoundedRectangle(Ui.B(Accent), null, new Rect(0, mid - 2, x, 4), 2, 2);
        dc.DrawEllipse(Ui.B(Ui.W(0.95)), new Pen(Ui.B(Ui.Black.Op(0.4)), 0.5), new Point(x, mid), 6, 6);
    }
}

/// <summary>SwiftUI switch-style Toggle.</summary>
public sealed class SwitchToggle : FrameworkElement
{
    private bool _isOn;
    public Color Accent { get; set; } = Ui.Mint;
    public event Action<bool>? Toggled;

    public SwitchToggle()
    {
        Width = 30;
        Height = 16;
        Cursor = Cursors.Hand;
        MouseLeftButtonUp += (_, _) => { IsOn = !IsOn; Toggled?.Invoke(IsOn); };
    }

    public bool IsOn
    {
        get => _isOn;
        set { _isOn = value; InvalidateVisual(); }
    }

    protected override void OnRender(DrawingContext dc)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        dc.DrawRoundedRectangle(Ui.B(_isOn ? Accent : Ui.W(0.3)), null, new Rect(0, 0, w, h), h / 2, h / 2);
        var r = h / 2 - 2;
        dc.DrawEllipse(Ui.B(Ui.White), null, new Point(_isOn ? w - h / 2 : h / 2, h / 2), r, r);
    }
}
