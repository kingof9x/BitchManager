using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.UI.Modules;
using static BitchManager.UI.Ui;

namespace BitchManager.UI;

/// <summary>Module cards: header controls, resize bar, rearrange mode, spotlight and floating bodies.</summary>
public static class ModuleChrome
{
    private const string DragFormat = "BitchManager.Module";

    public static string IconName(StudioModuleID m) => m switch
    {
        StudioModuleID.MidiRouter => "point.3.connected.trianglepath.dotted",
        StudioModuleID.MemorySlots => "memorychip.fill",
        StudioModuleID.CustomFXStudio => "wand.and.stars",
        StudioModuleID.ReactiveKeyFX => "waveform.circle.fill",
        _ => "dot.radiowaves.left.and.right",
    };

    public static Size IdealWindowSize(StudioModuleID m) => m switch
    {
        StudioModuleID.MidiRouter => new Size(375, 490),
        StudioModuleID.MemorySlots => new Size(375, 285),
        StudioModuleID.CustomFXStudio => new Size(375, 520),
        StudioModuleID.ReactiveKeyFX => new Size(375, 265),
        _ => new Size(420, 560),
    };

    public static UIElement Content(BitchBoyEngine e, StudioModuleID m, bool spotlight) => m switch
    {
        StudioModuleID.MidiRouter => MidiRouterPanel.Build(e, spotlight),
        StudioModuleID.MemorySlots => MemorySlotsPanel.Build(e, spotlight),
        StudioModuleID.CustomFXStudio => CustomFXPanel.Build(e, spotlight),
        StudioModuleID.ReactiveKeyFX => ReactiveFXPanel.Build(e, spotlight),
        _ => ShowControlPanel.Build(e, spotlight),
    };

    private static readonly string[] ThemeProps = [nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.ActiveCustomTheme)];

    /// <summary>Text in the theme's header colour that follows theme changes.</summary>
    public static TextBlock HeaderText(BitchBoyEngine e, string text, double size)
    {
        var t = T(text, size, e.Palette.HeaderText);
        t.Observe(e, () => t.Foreground = B(e.Palette.HeaderText), ThemeProps);
        return t;
    }

    /// <summary>Text in the theme accent colour that follows theme changes.</summary>
    public static TextBlock AccentText(BitchBoyEngine e, string text, double size, FontWeight? weight = null)
    {
        var t = T(text, size, e.Palette.Accent, weight);
        t.Observe(e, () => t.Foreground = B(e.Palette.Accent), ThemeProps);
        return t;
    }

    /// <summary>The standard panel title row: icon, title, status, then the module header controls.</summary>
    public static UIElement Header(BitchBoyEngine e, StudioModuleID m, UIElement icon, string title, UIElement? status = null)
    {
        var titleRow = H(5, icon, HeaderText(e, title, 8.5), Spacer(2), status);
        var tap = new Clickable(() => e.ToggleSpotlightModule(m), 1.0) { Child = titleRow };
        var row = H(5, tap.Fill(), HeaderControls(e, m));
        return Box(row, White.Op(0.04), 6, 6, 4);
    }

    public static UIElement HeaderControls(BitchBoyEngine e, StudioModuleID m)
    {
        var accent = e.Palette.Accent;
        var row = H(3);
        row.Add(Btn(H(2, Icon("arrow.up.and.down", 6.5, accent), T(e.ModuleSizePresetLabel(m), 6, accent)),
            () => e.CycleModuleSizePreset(m), accent.Op(0.18), 3.5, 4, 2.5));
        if (e.CurrentZone(m) == ModuleZone.BottomRack && !e.DetachedModules.Contains(m))
            row.Add(Btn(H(2, Icon("arrow.left.and.right", 6.5, Gold), T(e.ModuleWidthPresetLabel(m), 6, Gold)),
                () => e.CycleModuleWidthPreset(m), Gold.Op(0.18), 3.5, 4, 2.5));
        row.Add(MenuBtn(Icon("square.split.bottomrightquarter", 7.5, W(0.85)).Size(16, 16), () => PlacementMenu(e, m), White.Op(0.1), 4, 0, 0));
        var spot = e.SpotlightModule == m;
        row.Add(Btn(Icon(spot ? "arrow.down.right.and.arrow.up.left" : "arrow.up.left.and.arrow.down.right", 7.5, spot ? Black : accent).Size(16, 16),
            () => e.ToggleSpotlightModule(m), spot ? accent : White.Op(0.1), 4, 0, 0));
        if (!e.DetachedModules.Contains(m))
            row.Add(Btn(Icon("macwindow.badge.plus", 7.5, W(0.85)).Size(16, 16), () => e.PopOutModuleToWindow(m), White.Op(0.1), 4, 0, 0)
                .Tip("Pop out into a floating window"));
        return row;
    }

    public static IEnumerable<MenuEntry> PlacementMenu(BitchBoyEngine e, StudioModuleID m)
    {
        var list = new List<MenuEntry>
        {
            MenuEntry.Label("PLACEMENT ZONE"),
            new("Left Wing", () => e.MoveModuleToZone(m, ModuleZone.LeftWing)),
            new("Right Wing", () => e.MoveModuleToZone(m, ModuleZone.RightWing)),
            new("Bottom Console Rack", () => e.MoveModuleToZone(m, ModuleZone.BottomRack)),
            MenuEntry.Sep,
            new("Pop Out into Floating Window", () => e.PopOutModuleToWindow(m)),
            new("Center in Spotlight", () => e.ToggleSpotlightModule(m)),
            MenuEntry.Sep,
            MenuEntry.Label("REORDER IN ZONE"),
            new("Move Up / Left", () => e.MoveModuleWithinZone(m, -1)),
            new("Move Down / Right", () => e.MoveModuleWithinZone(m, 1)),
            MenuEntry.Sep,
            MenuEntry.Label("HEIGHT PRESETS"),
            new("Compact (210 px)", () => e.SetModuleHeight(m, 210)),
            new("Standard (340 px)", () => e.SetModuleHeight(m, 340)),
            new("Tall (480 px)", () => e.SetModuleHeight(m, 480)),
            new("Reset to Default", () => e.ResetModuleHeight(m)),
        };
        if (e.CurrentZone(m) == ModuleZone.BottomRack)
        {
            list.Add(MenuEntry.Sep);
            list.Add(MenuEntry.Label("WIDTH PRESETS (BOTTOM RACK)"));
            list.Add(new("Compact (320 px)", () => e.SetModuleWidth(m, 320)));
            list.Add(new("Half Rack (435 px)", () => e.SetModuleWidth(m, 435)));
            list.Add(new("Wide (620 px)", () => e.SetModuleWidth(m, 620)));
            list.Add(new("Full Console (894 px)", () => e.SetModuleWidth(m, 894)));
        }
        return list;
    }

    /// <summary>A tab button as used along the top of each studio panel.</summary>
    public static Clickable Tab(BitchBoyEngine e, string title, bool active, Action onClick)
        => TextBtn(title, 7, active ? Black : W(0.78), active ? e.Palette.Accent : White.Op(0.08), onClick, 5, 4, 5).Fill();

    /// <summary>The rounded, outlined frame around a panel (none in spotlight mode).</summary>
    public static Border Frame(UIElement header, UIElement? tabs, UIElement body, bool spotlight, Color stroke, double bgOpacity = 0.42)
    {
        var dock = new DockPanel { LastChildFill = true };
        if (!spotlight)
        {
            DockPanel.SetDock(header, Dock.Top);
            dock.Children.Add(header);
        }
        if (tabs != null)
        {
            var t = new Border { Child = tabs, Margin = new Thickness(0, spotlight ? 0 : 7, 0, 7) };
            DockPanel.SetDock(t, Dock.Top);
            dock.Children.Add(t);
        }
        dock.Children.Add(Scroll(body));
        return new Border
        {
            Child = dock,
            Padding = new Thickness(spotlight ? 14 : 8),
            CornerRadius = new CornerRadius(12),
            Background = B(spotlight ? Clear : Black.Op(bgOpacity)),
            BorderBrush = B(spotlight ? Clear : stroke),
            BorderThickness = new Thickness(spotlight ? 0 : 1.2),
        };
    }

    // ------------------------------------------------------------------------
    // Container in a wing / rack
    // ------------------------------------------------------------------------
    public static FrameworkElement Container(BitchBoyEngine e, StudioModuleID m, ModuleZone zone)
    {
        var outer = V(3);
        if (e.IsRearrangingModules) outer.Add(RearrangeBar(e, m, zone));

        if (e.DetachedModules.Contains(m))
        {
            outer.Add(DetachedPlaceholder(e, m));
        }
        else
        {
            var height = e.ModuleHeight(m);
            var dock = new DockPanel { LastChildFill = true };
            var resize = ResizeBar(e, m, height, zone);
            DockPanel.SetDock(resize, Dock.Bottom);
            dock.Children.Add(resize);
            dock.Children.Add(Content(e, m, false));
            var card = new Border
            {
                Child = dock,
                Height = height,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
            };
            if (e.IsRearrangingModules)
            {
                var g = new Grid { Height = height };
                g.Children.Add(card);
                g.Children.Add(new Rectangle
                {
                    Stroke = B(Gold), StrokeThickness = 1.8, StrokeDashArray = [6 / 1.8, 4 / 1.8],
                    RadiusX = 12, RadiusY = 12, IsHitTestVisible = false,
                });
                outer.Add(g);
            }
            else outer.Add(card);
        }

        if (e.IsRearrangingModules)
        {
            var rot = new RotateTransform(0);
            outer.RenderTransformOrigin = new Point(0.5, 0.5);
            outer.RenderTransform = rot;
            var jiggle = new DoubleAnimation(-1.1, 1.1, TimeSpan.FromMilliseconds(130)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            rot.BeginAnimation(RotateTransform.AngleProperty, jiggle);

            outer.PreviewMouseMove += (_, ev) =>
            {
                if (ev.LeftButton != MouseButtonState.Pressed || ev.OriginalSource is TextBox) return;
                DragDrop.DoDragDrop(outer, new DataObject(DragFormat, m.RawValue()), DragDropEffects.Move);
            };
            outer.AllowDrop = true;
            outer.Drop += (_, ev) =>
            {
                if (TryGetDragged(ev, out var dragged)) e.SwapModules(dragged, m);
                ev.Handled = true;
            };
        }
        return outer;
    }

    private static bool TryGetDragged(DragEventArgs ev, out StudioModuleID m)
    {
        m = default;
        return ev.Data.GetData(DragFormat) is string raw && Raw.TryParse(raw, out m);
    }

    public static void MakeZoneDrop(BitchBoyEngine e, UIElement target, ModuleZone zone)
    {
        target.AllowDrop = true;
        target.Drop += (_, ev) =>
        {
            if (!ev.Handled && TryGetDragged(ev, out var dragged)) e.MoveModuleToZone(dragged, zone);
            ev.Handled = true;
        };
    }

    public static FrameworkElement DropTarget(BitchBoyEngine e, ModuleZone zone)
    {
        var content = V(4,
            Icon("plus.circle.dashed", 14, Gold.Op(0.7)),
            T($"+ PLACE MODULE IN {zone.RawValue()}", 6.5, Gold).Align(HorizontalAlignment.Center));
        var g = new Grid { MinHeight = 56 };
        g.Children.Add(new Rectangle { Fill = B(Black.Op(0.28)), Stroke = B(Gold.Op(0.65)), StrokeThickness = 1.2, StrokeDashArray = [5 / 1.2, 4 / 1.2], RadiusX = 10, RadiusY = 10 });
        content.VerticalAlignment = VerticalAlignment.Center;
        g.Children.Add(content);
        var btn = MenuBtn(g, () => Enum.GetValues<StudioModuleID>().Where(m => e.CurrentZone(m) != zone)
            .Select(m => new MenuEntry($"Move {m.RawValue()} here", () => e.MoveModuleToZone(m, zone))), Clear, 10, 0, 0);
        MakeZoneDrop(e, btn, zone);
        return btn;
    }

    private static UIElement RearrangeBar(BitchBoyEngine e, StudioModuleID m, ModuleZone zone)
    {
        Clickable ZoneBtn(string text, ModuleZone z) =>
            TextBtn(text, 5.5, zone == z ? Black : White, zone == z ? Gold : White.Op(0.12), () => e.MoveModuleToZone(m, z), 3, 3.5, 1.5);
        var row = H(3,
            Icon("hand.draw.fill", 7, C(0.96, 0.78, 0.22)),
            T("MOVE:", 5.5, C(0.96, 0.78, 0.22)),
            Spacer(2),
            ZoneBtn("⇦ LW", ModuleZone.LeftWing),
            ZoneBtn("⇨ RW", ModuleZone.RightWing),
            ZoneBtn("⇩ RACK", ModuleZone.BottomRack),
            Btn(Icon("macwindow.badge.plus", 6.5, e.Palette.Accent), () => e.PopOutModuleToWindow(m), e.Palette.Accent.Op(0.28), 3, 3.5, 1.5));
        return Box(row, Black.Op(0.78), 5, 5, 2.5, Gold, 0.8);
    }

    private static UIElement ResizeBar(BitchBoyEngine e, StudioModuleID m, double height, ModuleZone zone)
    {
        var accent = e.Palette.Accent;
        var row = H(4,
            T("↕ RESIZE", 5.5, W(0.45)),
            Spacer(),
            new Border { Width = 28, Height = 3, CornerRadius = new CornerRadius(1.5), Background = B(White.Op(0.22)) },
            Spacer());
        if (zone == ModuleZone.BottomRack)
            row.Add(TextBtn($"WIDTH: {e.ModuleWidthPresetLabel(m)}", 5.5, Gold, White.Op(0.12), () => e.CycleModuleWidthPreset(m), 2, 4, 1));
        var px = T($"{(int)height}PX", 5.5, accent.Op(0.85), FontWeights.Bold);
        row.Add(px);
        row.Add(Icon("arrow.up.left.and.arrow.down.right", 6.5, accent.Op(0.8)));
        var bar = new Border { Child = row, Padding = new Thickness(8, 3, 8, 3), Background = B(Black.Op(0.75)), Cursor = Cursors.SizeNS };

        // Drag to resize; the card is resized live and the console relayouts when the drag ends.
        double startY = 0, startH = height;
        FrameworkElement? card = null;
        bar.MouseLeftButtonDown += (_, ev) =>
        {
            if (ev.ClickCount == 2) { e.CycleModuleSizePreset(m); ev.Handled = true; return; }
            startY = ev.GetPosition(null).Y;
            startH = height;
            card = FindCard(bar);
            bar.CaptureMouse();
            ev.Handled = true;
        };
        bar.MouseMove += (_, ev) =>
        {
            if (!bar.IsMouseCaptured || card == null) return;
            var scale = card.ActualHeight > 0 && card.RenderSize.Height > 0 ? GetScale(card) : 1.0;
            var nh = Math.Clamp(startH + (ev.GetPosition(null).Y - startY) / scale, 160, 650);
            card.Height = nh;
            px.Text = $"{(int)nh}PX";
        };
        bar.MouseLeftButtonUp += (_, _) =>
        {
            if (!bar.IsMouseCaptured) return;
            bar.ReleaseMouseCapture();
            if (card != null) e.SetModuleHeight(m, card.Height);
        };
        return bar;
    }

    private static FrameworkElement? FindCard(DependencyObject d)
    {
        var p = VisualTreeHelper.GetParent(d);
        while (p != null)
        {
            if (p is Border { CornerRadius.TopLeft: 12, ClipToBounds: true } b) return b;
            p = VisualTreeHelper.GetParent(p);
        }
        return null;
    }

    private static double GetScale(Visual v)
    {
        try
        {
            var root = PresentationSource.FromVisual(v)?.RootVisual as Visual;
            if (root == null) return 1.0;
            var m = v.TransformToAncestor(root).Transform(new Point(0, 1)) - v.TransformToAncestor(root).Transform(new Point(0, 0));
            return Math.Max(0.05, m.Length);
        }
        catch
        {
            return 1.0;
        }
    }

    private static UIElement DetachedPlaceholder(BitchBoyEngine e, StudioModuleID m)
    {
        var accent = e.Palette.Accent;
        var body = V(8,
            H(5, Icon(IconName(m), 10, accent), HeaderText(e, m.RawValue(), 8.5), Spacer(),
                Box(T("POPPED OUT", 6.5, accent), accent.Op(0.22), 4, 5, 2)),
            V(6,
                Icon("macwindow.on.rectangle", 20, accent.Op(0.85)),
                T("ACTIVE IN FLOATING WINDOW", 7.5, White).Align(HorizontalAlignment.Center),
                H(6,
                    IconTextBtn("arrow.down.left.square.fill", "DOCK BACK", 6.5, Black, accent, () => e.DockModuleBackFromWindow(m), 4, 8, 4),
                    TextBtn("CENTER", 6.5, White, White.Op(0.12), () => { e.DockModuleBackFromWindow(m); e.ToggleSpotlightModule(m); }, 4, 8, 4))
                    .Align(HorizontalAlignment.Center)));
        return Box(body, Black.Op(0.45), 12, 10, 10, accent.Op(0.5));
    }

    // ------------------------------------------------------------------------
    // Floating window body
    // ------------------------------------------------------------------------
    public static FrameworkElement FloatingBody(BitchBoyEngine e, StudioModuleID m)
    {
        var accent = e.Palette.Accent;
        var pinText = T("PIN", 7.5, White);
        var pin = Btn(H(3, Icon("pin", 7.5, White), pinText), () => e.ToggleModuleWindowTopMost(m), White.Op(0.12), 4, 7, 3.5);
        pin.Observe(e, () =>
        {
            var on = e.IsModuleWindowTopMost(m);
            pinText.Text = on ? "PINNED" : "PIN";
            pinText.Foreground = B(on ? Black : White);
            pin.Background = B(on ? C(1.0, 0.35, 0.15) : White.Op(0.12));
        }, nameof(BitchBoyEngine.PinnedModuleWindows), nameof(BitchBoyEngine.IsWindowAlwaysOnTop));
        var header = new Border
        {
            Child = H(6, Icon(IconName(m), 11, accent), T($"FLOATING // {m.RawValue().ToUpperInvariant()}", 9, White), Spacer(), pin,
                IconTextBtn("arrow.down.left.square.fill", "DOCK", 7.5, Black, accent, () => e.DockModuleBackFromWindow(m), 4, 7, 3.5)),
            Padding = new Thickness(10, 6, 10, 6),
            Background = B(Black.Op(0.6)),
            BorderBrush = B(accent.Op(0.4)),
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        var dock = new DockPanel { Background = B(C(0.02, 0.05, 0.04)) };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(Content(e, m, true));
        return dock;
    }
}
