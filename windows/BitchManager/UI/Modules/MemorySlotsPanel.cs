using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BitchManager.Core;
using BitchManager.Engine;
using BitchManager.Models;
using BitchManager.Platform;
using static BitchManager.UI.Ui;

namespace BitchManager.UI.Modules;

/// <summary>MEMORY SLOTS: the 8 preset banks with naming, icon, accent colour and clone/export tools.</summary>
public static class MemorySlotsPanel
{
    private static readonly (string Icon, string Title)[] SlotIcons =
    [
        ("music.mic", "LIVE / VOCAL"),
        ("slider.horizontal.3", "DAW MIXER"),
        ("sparkles", "LIGHT SHOW / DMX"),
        ("square.grid.3x3.fill", "STREAM DECK"),
        ("pianokeys", "SYNTH / KEYS"),
        ("waveform.path.ecg", "DRUMS / FX"),
        ("tv.fill", "VJ / RESOLUME"),
        ("cpu.fill", "HARDWARE MACRO"),
        ("bolt.fill", "HIGH ENERGY"),
        ("flame.fill", "FIRE SET"),
    ];

    private static readonly int[] Swatches = [72, 61, 96, 74, 87, 33, 78, 41, 67, 95, 83, 3];

    /// <summary>UI-only state shared by every instance of the panel (bank page, palette drawer).</summary>
    private sealed class PanelState : ObservableObject
    {
        public bool SecondBank { get => G(false); set => S(value); }
        public bool ShowCustomizer { get => G(false); set => S(value); }
    }

    private static readonly PanelState State = new();

    public static UIElement Build(BitchBoyEngine e, bool spotlight)
    {
        var header = ModuleChrome.Header(e, StudioModuleID.MemorySlots,
            ModuleChrome.AccentText(e, Icons.For("memorychip.fill"), 10), "MEMORY SLOTS // 8 BANKS",
            Dyn(e, () => T($"SLOT {BitchBoyEngine.SlotLetters[e.ActiveSlot % 8]}", 7, Leds.For(e, e.CurrentSlot.AccentVelocity)),
                nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.Slots)));

        var tabs = Dyn(State, () => H(3,
            ModuleChrome.Tab(e, "BANK A–D", !State.SecondBank, () => State.SecondBank = false),
            ModuleChrome.Tab(e, "BANK E–H", State.SecondBank, () => State.SecondBank = true)),
            nameof(PanelState.SecondBank)).Also(e, nameof(BitchBoyEngine.CurrentTheme));

        var body = V(7, SlotGrid(e), Editor(e));
        var frame = ModuleChrome.Frame(header, tabs, body, spotlight, e.Palette.Outline.Op(0.75));
        frame.Observe(e, () => frame.BorderBrush = B(Leds.For(e, e.CurrentSlot.AccentVelocity).Op(0.7)),
            nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.Slots));
        return frame;
    }

    private static UIElement SlotGrid(BitchBoyEngine e) => Dyn(e, () =>
    {
        var start = State.SecondBank ? 4 : 0;
        var cards = new List<UIElement>();
        for (var i = start; i < start + 4 && i < e.Slots.Count; i++)
        {
            var idx = i;
            var slot = e.Slots[i];
            var active = e.ActiveSlot == i;
            var color = Leds.For(e, slot.AccentVelocity);
            var lit = slot.Controls.Count(c => c.HasLED && c.IdleVelocity > 0);
            var macros = slot.Controls.Count(c => c.ActionType != ActionType.Passthrough);
            var name = slot.Name.Contains("//") ? slot.Name[(slot.Name.IndexOf("//") + 2)..].Trim() : slot.Name;

            var card = Btn(V(4,
                H(5,
                    Box(T(BitchBoyEngine.SlotLetters[i], 8, active ? Black : color, FontWeights.Black), active ? color : color.Op(0.15), 3, 5, 1.5),
                    Icon(slot.IconName, 10, color),
                    Spacer(),
                    Dot(6, active ? Green : W(0.3))),
                T(name, 7.5, White),
                T($"{lit} LED • {macros} ACT", 6.2, W(0.55), FontWeights.Bold)),
                () => { e.SelectSlot(idx); SoundEffect.KeyTap.Play(e.SoundPack); },
                active ? color.Op(0.14) : Black.Op(0.35), 6, 7, 6, active ? color.Op(0.8) : White.Op(0.08));
            cards.Add(card);
        }
        return Grid(2, 5, cards);
    }, nameof(BitchBoyEngine.Slots), nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.CurrentTheme), nameof(BitchBoyEngine.LedPalette))
        .Also(State, nameof(PanelState.SecondBank));

    private static UIElement Editor(BitchBoyEngine e)
    {
        // The name field lives outside the rebuilding part so typing (which raises Slots) keeps focus.
        var name = Field(() => e.CurrentSlot.Name, e.RenameCurrentSlot, 8, e, nameof(BitchBoyEngine.ActiveSlot));
        var tools = Dyn(e, () => EditorTools(e), nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.Slots), nameof(BitchBoyEngine.CurrentTheme))
            .Also(State, nameof(PanelState.ShowCustomizer));
        var iconBtn = Dyn(e, () =>
        {
            var slot = e.CurrentSlot;
            return MenuBtn(H(3, Icon(slot.IconName, 10, Leds.For(e, slot.AccentVelocity)), Icon("chevron.down", 6.5, W(0.6))),
                () => SlotIcons.Select(ic => new MenuEntry($"{Icons.For(ic.Icon)}  {ic.Title}", () => e.SetSlotIcon(ic.Icon), e.CurrentSlot.IconName == ic.Icon)),
                Black.Op(0.5), 4, 5, 3);
        }, nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.Slots));
        var paletteBtn = Dyn(e, () => Btn(Icon(State.ShowCustomizer ? "paintpalette.fill" : "paintpalette", 10,
                State.ShowCustomizer ? Leds.For(e, e.CurrentSlot.AccentVelocity) : W(0.7)),
            () => State.ShowCustomizer = !State.ShowCustomizer, Black.Op(0.5), 4, 5, 3).Tip("Slot colour"),
            nameof(BitchBoyEngine.ActiveSlot), nameof(BitchBoyEngine.Slots)).Also(State, nameof(PanelState.ShowCustomizer));
        return Box(V(5, H(4, iconBtn, name.Fill(), paletteBtn), tools), Black.Op(0.45), 8, 8, 8);
    }

    private static UIElement EditorTools(BitchBoyEngine e)
    {
        var accent = e.Palette.Accent;
        var slot = e.CurrentSlot;
        var slotColor = Leds.For(e, slot.AccentVelocity);

        UIElement? customizer = null;
        if (State.ShowCustomizer)
        {
            var row = H(3);
            foreach (var v in Swatches)
            {
                var vel = v;
                var sel = slot.AccentVelocity == vel;
                var c = Leds.For(e, vel);
                row.Add(Btn(new Border
                {
                    Width = 13, Height = 13, CornerRadius = new CornerRadius(7), Background = B(c),
                    BorderBrush = B(sel ? White : Black.Op(0.4)), BorderThickness = new Thickness(sel ? 2 : 1),
                }, () => e.SetSlotAccent(vel), Clear, 7, 0, 0));
            }
            customizer = Box(V(5,
                T("SLOT ACCENT COLOUR:", 6.5, W(0.6), FontWeights.Bold),
                row,
                IconTextBtn("lightbulb.fill", "SYNC 49 LEDS", 7, Black, slotColor, e.SyncSlotAccentToLEDs, 4, 6, 4)),
                Black.Op(0.35), 6, 6, 5);
        }

        var cloneBtn = MenuBtn(H(3, Icon("doc.on.doc.fill", 8, White), T("CLONE", 7, White)),
            () => Enumerable.Range(0, e.Slots.Count).Where(i => i != e.ActiveSlot)
                .Select(i => new MenuEntry($"→ SLOT {BitchBoyEngine.SlotLetters[i]}: {e.Slots[i].Name}", () => e.CloneCurrentSlotTo(i))),
            White.Op(0.1), 4, 6, 4);

        var actions = H(3,
            IconTextBtn("square.and.arrow.down.fill", "SAVE", 7, Black, accent, () => { e.SaveCurrentSlot(); SoundEffect.KeyTap.Play(e.SoundPack); }, 4, 6, 4).Fill(),
            cloneBtn.Fill(),
            TextBtn("EXP", 7, White, White.Op(0.1), () => e.ExportPresetToJson(), 4, 6, 4).Tip("Copy preset JSON to clipboard").Fill(),
            TextBtn("IMP", 7, White, White.Op(0.1), e.ImportPresetFromClipboard, 4, 6, 4).Tip("Import preset JSON from clipboard").Fill(),
            Btn(Icon("arrow.counterclockwise", 9, Red), e.ResetCurrentSlotToDefault, Red.Op(0.15), 4, 6, 4).Tip("Reset slot to factory map"));

        return V(5, customizer, actions);
    }
}
