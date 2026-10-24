import SwiftUI
import AppKit
import CoreMIDI
import Network
import Combine
import UniformTypeIdentifiers

// ============================================================================
// 1. DATA MODELS & EXACT 69-CONTROL HARDWARE SPECIFICATION
// ============================================================================

enum ControlKind: String, Codable {
    case key      // 49 Mechanical Backlit Keys (Hardware LEDs)
    case vfader   // 8 Vertical Faders in Bottom Tier (CC 0..7)
    case hfader   // 4 Horizontal Faders in Right Tier (CC 8..11)
    case knob     // 8 Rotary Knobs in Right Tier (CC 20..27)
}

enum FeedbackMode: String, Codable, CaseIterable {
    case momentary = "MOMENTARY"
    case toggle = "TOGGLE"
    case staticLED = "STATIC"
}

enum ActionType: String, Codable, CaseIterable {
    case passthrough = "PASSTHROUGH"
    case qwerty = "QWERTY_MACRO"
    case osc = "OSC_UDP"
    case applescript = "APPLESCRIPT"
}

enum LEDFXMode: String, CaseIterable, Identifiable {
    case staticFX = "STATIC (PRESET)"
    case rainbowContinuous = "RAINBOW WAVE"
    case knightRider = "CYLON SCANNER"
    case matrixRain = "DIGITAL MATRIX"
    case cyberPulse = "CYBERPUNK PULSE"
    case vuMeter = "LIVE FADER VU"
    case fireEmbers = "FIRE & EMBERS"
    case waterRipple = "CENTER RIPPLE"
    case starfield = "TWINKLE STARS"
    case audioStrobe = "STROBE FLASH"
    case heartbeat = "HEARTBEAT"
    case plasmaWave = "PLASMA FIELD"
    case spiralVortex = "SPIRAL VORTEX"
    case eqDance = "EQ DANCE"
    case colorChase = "COLOR RUNNER"

    var id: String { rawValue }

    var icon: String {
        switch self {
        case .staticFX: return "power"
        case .rainbowContinuous: return "rainbow"
        case .knightRider: return "bolt.horizontal.fill"
        case .matrixRain: return "cloud.rain.fill"
        case .cyberPulse: return "waveform.path"
        case .vuMeter: return "slider.vertical.3"
        case .fireEmbers: return "flame.fill"
        case .waterRipple: return "circle.dotted"
        case .starfield: return "sparkles"
        case .audioStrobe: return "bolt.fill"
        case .heartbeat: return "heart.fill"
        case .plasmaWave: return "wind"
        case .spiralVortex: return "circle.hexagonpath"
        case .eqDance: return "chart.bar.xaxis"
        case .colorChase: return "arrow.triangle.2.circlepath"
        }
    }
}

struct BitchBoyKeyMap {
    static let keyPositions: [(col: Int, row: Int)] = {
        var pos: [(col: Int, row: Int)] = []
        // 0..31: 8x4 Grid (Rows 0..<4, Cols 0..<8)
        for r in 0..<4 {
            for c in 0..<8 {
                pos.append((col: c, row: r))
            }
        }
        // 32..39: 1x8 Strip (Row 4, Cols 0..<8)
        for c in 0..<8 {
            pos.append((col: c, row: 4))
        }
        // 40..48: 3x3 Keypad (Rows 0..<3, Cols 8..<11)
        for r in 0..<3 {
            for c in 0..<3 {
                pos.append((col: 8 + c, row: r))
            }
        }
        return pos
    }()
}

enum SoundPack: String, CaseIterable, Identifiable {
    case arcade8Bit = "8-BIT ARCADE"
    case mechanicalClick = "CHERRY MX BLUE"
    case sciFiBeeps = "SCI-FI SYNTH"
    case mute = "MUTE SOUNDS"

    var id: String { rawValue }
}

enum SoundEffect {
    case keyTap
    case faderSlide
    case knobTick
    case mascotSqueak
    case presetSwitch

    func play(with pack: SoundPack) {
        guard pack != .mute else { return }
        switch pack {
        case .arcade8Bit:
            switch self {
            case .keyTap: NSSound(named: "Tink")?.play()
            case .faderSlide: NSSound(named: "Pop")?.play()
            case .knobTick: NSSound(named: "Morse")?.play()
            case .mascotSqueak: NSSound(named: "Purr")?.play()
            case .presetSwitch: NSSound(named: "Glass")?.play()
            }
        case .mechanicalClick:
            switch self {
            case .keyTap: NSSound(named: "Pop")?.play()
            case .faderSlide: NSSound(named: "Tink")?.play()
            case .knobTick: NSSound(named: "Tink")?.play()
            case .mascotSqueak: NSSound(named: "Blow")?.play()
            case .presetSwitch: NSSound(named: "Ping")?.play()
            }
        case .sciFiBeeps:
            switch self {
            case .keyTap: NSSound(named: "Morse")?.play()
            case .faderSlide: NSSound(named: "Purr")?.play()
            case .knobTick: NSSound(named: "Bottle")?.play()
            case .mascotSqueak: NSSound(named: "Hero")?.play()
            case .presetSwitch: NSSound(named: "Submarine")?.play()
            }
        case .mute:
            break
        }
    }
}

enum ChassisTheme: String, CaseIterable, Identifiable {
    case petrolTeal = "PETROL TEAL"
    case retroMatrix = "RETRO MATRIX"
    case neonCyber = "NEON CYBER"
    case royalGold = "ROYAL GOLD"
    case stealthBlack = "STEALTH NOIR"

    var id: String { rawValue }

    var pcbBackground: Color {
        switch self {
        case .petrolTeal: return Color(red: 0.04, green: 0.24, blue: 0.21)
        case .retroMatrix: return Color(red: 0.02, green: 0.16, blue: 0.08)
        case .neonCyber: return Color(red: 0.08, green: 0.02, blue: 0.18)
        case .royalGold: return Color(red: 0.18, green: 0.12, blue: 0.04)
        case .stealthBlack: return Color(red: 0.06, green: 0.06, blue: 0.07)
        }
    }

    var outerBezelColors: [Color] {
        switch self {
        case .petrolTeal:
            return [Color(white: 0.94), Color(white: 0.75), Color(white: 0.55)]
        case .retroMatrix:
            return [Color(red: 0.15, green: 0.55, blue: 0.25), Color(red: 0.08, green: 0.30, blue: 0.12)]
        case .neonCyber:
            return [Color(red: 0.95, green: 0.22, blue: 0.85), Color(red: 0.35, green: 0.10, blue: 0.65)]
        case .royalGold:
            return [Color(red: 0.98, green: 0.85, blue: 0.45), Color(red: 0.65, green: 0.45, blue: 0.15)]
        case .stealthBlack:
            return [Color(white: 0.35), Color(white: 0.15), Color(white: 0.08)]
        }
    }

    var accentPrimary: Color {
        switch self {
        case .petrolTeal: return Color(red: 0.22, green: 0.96, blue: 0.85)
        case .retroMatrix: return Color(red: 0.18, green: 0.95, blue: 0.35)
        case .neonCyber: return Color(red: 1.0, green: 0.25, blue: 0.88)
        case .royalGold: return Color(red: 0.98, green: 0.82, blue: 0.35)
        case .stealthBlack: return Color(white: 0.85)
        }
    }

    var sectionOutline: Color {
        switch self {
        case .petrolTeal: return Color(red: 0.96, green: 0.52, blue: 0.25)
        case .retroMatrix: return Color(red: 0.20, green: 0.85, blue: 0.45)
        case .neonCyber: return Color(red: 0.20, green: 0.92, blue: 0.98)
        case .royalGold: return Color(red: 0.92, green: 0.75, blue: 0.25)
        case .stealthBlack: return Color(white: 0.45)
        }
    }

    var headerText: Color {
        switch self {
        case .petrolTeal: return Color(red: 0.98, green: 0.90, blue: 0.65)
        case .retroMatrix: return Color(red: 0.65, green: 0.98, blue: 0.65)
        case .neonCyber: return Color(red: 0.95, green: 0.75, blue: 0.98)
        case .royalGold: return Color(red: 0.98, green: 0.92, blue: 0.70)
        case .stealthBlack: return Color(white: 0.80)
        }
    }
}

enum LEDPaletteMode: String, CaseIterable, Identifiable {
    case multiVelocity = "VELOCITY MULTI-COLOR"
    case cyberCyan = "CYBER CYAN"
    case matrixGreen = "MATRIX GREEN"
    case solarAmber = "SOLAR AMBER"
    case synthMagenta = "SYNTH MAGENTA"
    case arcticWhite = "ARCTIC WHITE"

    var id: String { rawValue }
}

struct QwertyStep: Codable, Identifiable {
    var id = UUID()
    var key: String
    var cmd: Bool = false
    var shift: Bool = false
    var alt: Bool = false
    var ctrl: Bool = false
    var delayMs: Int = 10
}

struct ControlItem: Identifiable, Codable {
    var id: String { "\(kind.rawValue)_\(number)" }
    var label: String
    var customName: String = ""
    var kind: ControlKind
    var number: UInt8          // MIDI Note or CC Number
    var zone: String
    var idleVelocity: UInt8 = 20
    var activeVelocity: UInt8 = 127
    var currentLED: UInt8 = 20
    var liveValue: UInt8 = 0
    var isPressed: Bool = false
    var toggleLatched: Bool = false
    var feedbackMode: FeedbackMode = .momentary
    var actionType: ActionType = .passthrough
    var qwertySteps: [QwertyStep] = []
    var oscHost: String = "127.0.0.1"
    var oscPort: UInt16 = 8000
    var oscAddress: String = "/bitchboy/trigger"
    var appleScript: String = ""

    var hasLED: Bool { kind == .key }
}

// --- MIDI ROUTER & TRANSFORMER MODELS ---
struct MIDIOutputTarget: Identifiable, Hashable {
    var id: String { name }
    let name: String
    let endpoint: MIDIEndpointRef
    let isHardwareBitchBoy: Bool
}

enum MIDIVelocityCurve: String, CaseIterable, Codable, Identifiable {
    case linear = "LINEAR"
    case soft = "SOFT"
    case hard = "HARD"
    case fixed127 = "MAX 127"

    var id: String { rawValue }

    func transform(_ v: UInt8) -> UInt8 {
        guard v > 0 else { return 0 }
        let norm = Double(v) / 127.0
        switch self {
        case .linear:
            return v
        case .soft:
            return UInt8(clamping: Int(round(pow(norm, 1.65) * 127.0)))
        case .hard:
            return UInt8(clamping: Int(round(pow(norm, 0.55) * 127.0)))
        case .fixed127:
            return 127
        }
    }
}

// --- GROUND-UP CUSTOM LIGHTING FX STUDIO MODELS ---
enum CustomFXLoopMode: String, CaseIterable, Codable, Identifiable {
    case loop = "LOOP"
    case pingPong = "BOUNCE"
    case oneShot = "1-SHOT"
    var id: String { rawValue }
}

enum CustomWaveShape: String, CaseIterable, Codable, Identifiable {
    case sine = "SINE WAVE"
    case sawtooth = "SAWTOOTH"
    case triangle = "TRIANGLE"
    case square = "SQUARE PULSE"
    case radial = "RADIAL RING"
    case spiral = "SPIRAL VORTEX"
    case sparkle = "NOISE SPARK"
    var id: String { rawValue }
}

enum CustomWaveDirection: String, CaseIterable, Codable, Identifiable {
    case leftToRight = "LEFT → RIGHT"
    case topToBottom = "TOP → BOTTOM"
    case diagonal = "DIAGONAL 45°"
    case centerOut = "CENTER → OUT"
    case outerIn = "OUTER → CENTER"
    var id: String { rawValue }
}

struct CustomLightEffect: Codable, Identifiable {
    var id = UUID()
    var name: String
    var isProcedural: Bool = false
    var frames: [[UInt8]] // Each frame is 49 UInt8 velocities (0..127)
    var fps: Double = 10.0
    var loopMode: CustomFXLoopMode = .loop

    // Procedural Wave Synth Parameters
    var waveShape: CustomWaveShape = .sine
    var waveDirection: CustomWaveDirection = .leftToRight
    var waveFrequency: Double = 1.0
    var minVelocity: UInt8 = 0
    var maxVelocity: UInt8 = 122

    static func emptyFrame(fill: UInt8 = 0) -> [UInt8] {
        Array(repeating: fill, count: 49)
    }

    static func buildStarterEffects() -> [CustomLightEffect] {
        // 1. Neon Heart Icon Pulse (4 frames)
        var heartF1 = emptyFrame(fill: 0)
        var heartF2 = emptyFrame(fill: 0)
        let heartCoords: [(Int, Int)] = [
            (2,0), (3,0), (5,0), (6,0),
            (1,1), (2,1), (3,1), (4,1), (5,1), (6,1), (7,1),
            (2,2), (3,2), (4,2), (5,2), (6,2),
            (3,3), (4,3), (5,3),
            (4,4)
        ]
        for (k, pos) in BitchBoyKeyMap.keyPositions.enumerated() {
            if heartCoords.contains(where: { $0.0 == pos.col && $0.1 == pos.row }) {
                heartF1[k] = 112 // Pink
                heartF2[k] = 5   // Red
            }
        }
        let heartEffect = CustomLightEffect(
            name: "NEON HEART PULSE",
            isProcedural: false,
            frames: [heartF1, heartF2, heartF1, emptyFrame(fill: 14)],
            fps: 5.0,
            loopMode: .loop
        )

        // 2. X-Matrix Cross Flash (6 frames)
        var xFrames: [[UInt8]] = []
        let pal: [UInt8] = [68, 84, 98, 112, 24, 52]
        for step in 0..<6 {
            var f = emptyFrame(fill: 0)
            for k in 0..<49 {
                let p = BitchBoyKeyMap.keyPositions[k]
                if (p.col + p.row + step) % 3 == 0 || (p.col - p.row + 10 + step) % 3 == 0 {
                    f[k] = pal[(step + p.col) % pal.count]
                }
            }
            xFrames.append(f)
        }
        let xEffect = CustomLightEffect(
            name: "CYBER CROSS CHASE",
            isProcedural: false,
            frames: xFrames,
            fps: 8.0,
            loopMode: .pingPong
        )

        // 3. Scrolling Neon Bar (11 frames across all 11 columns)
        var barFrames: [[UInt8]] = []
        let barColors: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127]
        for c in 0..<11 {
            var f = emptyFrame(fill: 0)
            for k in 0..<49 {
                let col = BitchBoyKeyMap.keyPositions[k].col
                if col == c {
                    f[k] = barColors[c]
                } else if abs(col - c) == 1 {
                    f[k] = 14
                }
            }
            barFrames.append(f)
        }
        let barEffect = CustomLightEffect(
            name: "11-COL LASER SWEEP",
            isProcedural: false,
            frames: barFrames,
            fps: 14.0,
            loopMode: .pingPong
        )

        // 4. Procedural Radial Synth Wave
        let synthEffect = CustomLightEffect(
            name: "RADIAL HYPERSPACE",
            isProcedural: true,
            frames: [emptyFrame(fill: 68)],
            fps: 15.0,
            loopMode: .loop,
            waveShape: .radial,
            waveDirection: .centerOut,
            waveFrequency: 1.4,
            minVelocity: 14,
            maxVelocity: 112
        )

        return [heartEffect, xEffect, barEffect, synthEffect]
    }
}

// --- REACTIVE MECHANICAL KEYBOARD INTERACTIVE FX MODELS ---
enum ReactiveKeyFXMode: String, CaseIterable, Codable, Identifiable {
    case ripple = "RIPPLE RING"
    case crossLaser = "LASER CROSS"
    case supernova = "SUPERNOVA"
    case heatTrail = "HEAT TRAIL"
    case meteorRain = "METEOR DROP"
    case arcLightning = "ARC LIGHTNING"
    case off = "OFF"

    var id: String { rawValue }

    var icon: String {
        switch self {
        case .ripple: return "dot.radiowaves.left.and.right"
        case .crossLaser: return "scope"
        case .supernova: return "sun.max.fill"
        case .heatTrail: return "flame.fill"
        case .meteorRain: return "cloud.bolt.rain.fill"
        case .arcLightning: return "bolt.fill"
        case .off: return "poweroff"
        }
    }
}

struct ReactivePulseEvent: Identifiable {
    let id = UUID()
    let originKeyIndex: Int
    let originCol: Double
    let originRow: Double
    let mode: ReactiveKeyFXMode
    let baseVelocity: UInt8
    var age: Double = 0.0 // 0.0 -> 1.0+
    var lightningTargets: [Int] = []
}

struct PresetSlot: Codable, Identifiable {
    var id = UUID()
    var name: String
    var controls: [ControlItem]
    var globalRules: String = ""
    var iconName: String = "dial.medium.fill"
    var accentVelocity: UInt8 = 68

    init(
        id: UUID = UUID(),
        name: String,
        controls: [ControlItem],
        globalRules: String = "",
        iconName: String = "dial.medium.fill",
        accentVelocity: UInt8 = 68
    ) {
        self.id = id
        self.name = name
        self.controls = controls
        self.globalRules = globalRules
        self.iconName = iconName
        self.accentVelocity = accentVelocity
    }

    enum CodingKeys: String, CodingKey {
        case id, name, controls, globalRules, iconName, accentVelocity
    }

    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = (try? c.decode(UUID.self, forKey: .id)) ?? UUID()
        name = try c.decode(String.self, forKey: .name)
        controls = try c.decode([ControlItem].self, forKey: .controls)
        globalRules = (try? c.decode(String.self, forKey: .globalRules)) ?? ""
        iconName = (try? c.decode(String.self, forKey: .iconName)) ?? "dial.medium.fill"
        accentVelocity = (try? c.decode(UInt8.self, forKey: .accentVelocity)) ?? 68
    }
}

// ============================================================================
// 2. HARDWARE ENGINE // COREMIDI & TELEMETRY
// ============================================================================

@MainActor
final class BitchBoyEngine: ObservableObject {
    @Published var controls: [ControlItem] = []
    @Published var selectedIndex: Int = 0
    @Published var selectedIndices: Set<Int> = []
    @Published var slots: [PresetSlot] = []
    @Published var activeSlot: Int = 0

    // Hardware status
    @Published var isConnected: Bool = false
    @Published var deviceName: String = "BitchBoy V1 (Hardware)"
    @Published var isLocked: Bool = false
    @Published var lastStreamMessage: String = "IDLE // READY"
    @Published var midiLogs: [String] = []

    // Customization & Themes
    @Published var currentTheme: ChassisTheme = .petrolTeal
    @Published var ledPalette: LEDPaletteMode = .multiVelocity
    @Published var soundPack: SoundPack = .arcade8Bit
    @Published var currentFXMode: LEDFXMode = .staticFX
    @Published var fxSpeed: Double = 1.0
    @Published var vuMeterLEDsEnabled: Bool = false
    @Published var isRainbowRunning: Bool = false

    // Mascot
    @Published var mascotScale: CGFloat = 1.0
    @Published var mascotSpeech: String = "ALL 69 HARDWARE CONTROLS LIVE!"
    @Published var mascotSpeechVisible: Bool = false

    // In-Chassis Drawer
    @Published var inChassisTab: String = "NOTES"
    @Published var userNotesText: String = "Show notes: Slot A configured for live Ableton clip triggers."

    // CoreMIDI internals
    private var midiClient = MIDIClientRef()
    private var inputPort = MIDIPortRef()
    private var outputPort = MIDIPortRef()
    private var bitchBoyDestination: MIDIEndpointRef = 0

    private var fxTimer: AnyCancellable?
    private var watchdogTimer: AnyCancellable?
    // --- LEFT WING: MIDI ROUTER & OUTPUT MATRIX STATE ---
    @Published var leftWingTab: String = "ROUTER" // "ROUTER", "TELEMETRY", "MACROS"
    @Published var midiThruEnabled: Bool = true
    @Published var virtualOutEnabled: Bool = true
    @Published var forwardUIInteractions: Bool = true
    @Published var forwardNotes: Bool = true
    @Published var forwardCCs: Bool = true
    @Published var remapChannel: Int = 0 // 0 = Original Ch 1, 1..16 = Forced Ch 1..16
    @Published var transposeSemitones: Int = 0 // -24 .. +24
    @Published var velocityCurve: MIDIVelocityCurve = .linear
    @Published var availableOutputTargets: [MIDIOutputTarget] = []
    @Published var selectedOutputNames: Set<String> = []
    @Published var txPacketCount: Int = 0
    @Published var lastRoutedMessage: String = "READY // NO TX YET"
    @Published var routerPacketHistory: [String] = []
    @Published var tapTempoBPM: Double = 120.0
    private var lastTapTimes: [Date] = []
    private var virtualSourceEndpoint = MIDIEndpointRef()

    // --- RIGHT WING: CUSTOM LIGHTING FX STUDIO STATE ---
    @Published var rightWingTab: String = "SEQUENCER" // "SEQUENCER", "WAVESYNTH", "LIBRARY"
    @Published var customEffects: [CustomLightEffect] = []
    @Published var editingCustomFX: CustomLightEffect = CustomLightEffect.buildStarterEffects()[0]
    @Published var editingFrameIndex: Int = 0
    @Published var customBrushVelocity: UInt8 = 68
    @Published var paintOnMainChassis: Bool = false
    @Published var isPlayingCustomFX: Bool = false
    @Published var activeCustomFXName: String = ""
    private var customFXStep: Int = 0
    private var customFXDirection: Int = 1

    // --- RIGHT WING BOTTOM: REACTIVE MECHANICAL KEYBOARD FX ENGINE STATE ---
    @Published var reactiveKeyFXMode: ReactiveKeyFXMode = .ripple
    @Published var reactiveFXSpeed: Double = 1.4
    @Published var reactiveFXColorVelocity: UInt8 = 68 // 0 = Rainbow Dynamic, >0 = Specific Neon Color
    @Published var reactiveFXRainbowColor: Bool = true
    @Published var activeReactivePulses: [ReactivePulseEvent] = []
    @Published var lastReactiveOriginKey: Int = -1
    private var reactiveFXTimer: AnyCancellable?
    private var lastSentReactiveLEDs: [UInt8] = Array(repeating: 255, count: 49)

    // --- UI SCALING / ZOOM & CUSTOM APP ICON STATE ---
    // --- ALWAYS ON TOP (FLOATING WINDOW) STATE ---
    @Published var isWindowAlwaysOnTop: Bool = false
    private let alwaysOnTopStorageKey = "BitchBoy_AlwaysOnTop_v1"

    @Published var uiScale: CGFloat = 1.15
    @Published var autoFitWindow: Bool = true
    @Published var lastComputedFitScale: CGFloat = 1.0
    private let uiScaleStorageKey = "BitchBoy_UIScale_v1"
    private let uiAutoFitStorageKey = "BitchBoy_UIAutoFit_v1"

    private let customFXStorageKey = "BitchBoy_CustomLightFX_v1"

    private let storageKey = "BitchBoy_SavedSlots_v5"

    init() {
        self.controls = Self.buildDefault69Map()
        self.slots = Self.buildDefault8MemorySlots()
        loadFromDisk()
        loadCustomEffectsFromDisk()
        loadUISettingsAndAppIcon()
        setupCoreMIDI()
        startHardwareWatchdog()
    }

    static func buildDefault8MemorySlots() -> [PresetSlot] {
        [
            PresetSlot(name: "SLOT A // PERFORMANCE", controls: Self.buildDefault69Map(), iconName: "music.mic", accentVelocity: 68),
            PresetSlot(name: "SLOT B // DAW PRODUCTION", controls: Self.buildDefault69Map(), iconName: "slider.horizontal.3", accentVelocity: 52),
            PresetSlot(name: "SLOT C // VISUALS & DMX", controls: Self.buildDefault69Map(), iconName: "sparkles", accentVelocity: 112),
            PresetSlot(name: "SLOT D // STREAM DECK", controls: Self.buildDefault69Map(), iconName: "square.grid.3x3.fill", accentVelocity: 24),
            PresetSlot(name: "SLOT E // SYNTH LEAD", controls: Self.buildDefault69Map(), iconName: "pianokeys", accentVelocity: 84),
            PresetSlot(name: "SLOT F // DRUM RACK", controls: Self.buildDefault69Map(), iconName: "waveform.path.ecg", accentVelocity: 14),
            PresetSlot(name: "SLOT G // VJ MATRIX", controls: Self.buildDefault69Map(), iconName: "tv.fill", accentVelocity: 98),
            PresetSlot(name: "SLOT H // MASTER CUSTOM", controls: Self.buildDefault69Map(), iconName: "cpu.fill", accentVelocity: 38)
        ]
    }

    static func buildDefault69Map() -> [ControlItem] {
        var items: [ControlItem] = []

        // 1. TOP-LEFT 8X4 KEY GRID (32 Mechanical Keys with LEDs)
        // Authentic hardware matrix
        let gridNotes: [[UInt8]] = [
            [64, 65, 66, 67, 96, 97, 98, 99],
            [60, 61, 62, 63, 92, 93, 94, 95],
            [56, 57, 58, 59, 88, 89, 90, 91],
            [52, 53, 54, 55, 84, 85, 86, 87]
        ]
        for r in 0..<4 {
            for c in 0..<8 {
                let lbl = "R\(r+1)C\(c+1)"
                items.append(ControlItem(
                    label: lbl,
                    customName: lbl,
                    kind: .key,
                    number: gridNotes[r][c],
                    zone: "8X4 GRID - ROW \(r+1), COL \(c+1)",
                    idleVelocity: 0,
                    activeVelocity: 127,
                    currentLED: 0,
                    feedbackMode: .momentary,
                    actionType: .passthrough
                ))
            }
        }

        // 2. MIDDLE 1X8 KEY STRIP (8 Keys with LEDs)
        let stripNotes: [UInt8] = [48, 49, 50, 51, 80, 81, 82, 83]
        for i in 0..<8 {
            let lbl = "S\(i+1)"
            items.append(ControlItem(
                label: lbl,
                customName: lbl,
                kind: .key,
                number: stripNotes[i],
                zone: "1X8 KEY STRIP - KEY \(i+1)",
                idleVelocity: 0,
                activeVelocity: 127,
                currentLED: 0,
                feedbackMode: .momentary,
                actionType: .passthrough
            ))
        }

        // 3. BOTTOM-RIGHT 3X3 KEYPAD (9 Keys with LEDs)
        let padNotes: [UInt8] = [44, 45, 46, 40, 41, 42, 36, 37, 38]
        for i in 0..<9 {
            let lbl = "P\(i+1)"
            items.append(ControlItem(
                label: lbl,
                customName: lbl,
                kind: .key,
                number: padNotes[i],
                zone: "3X3 KEYPAD - KEY \(i+1)",
                idleVelocity: 0,
                activeVelocity: 127,
                currentLED: 0
                ,
                feedbackMode: .momentary,
                actionType: .passthrough
            ))
        }

        // 4. BOTTOM-LEFT 8 VERTICAL FADERS (CC 0..7)
        for i in 0..<8 {
            let lbl = "VF\(i+1)"
            items.append(ControlItem(
                label: lbl,
                customName: lbl,
                kind: .vfader,
                number: UInt8(i),
                zone: "VERTICAL FADER \(i+1) (ANALOG CC)",
                idleVelocity: 0,
                activeVelocity: 0,
                liveValue: UInt8(i * 16),
                feedbackMode: .momentary,
                actionType: .passthrough
            ))
        }

        // 5. TOP-MIDDLE 4 HORIZONTAL FADERS (CC 8..11)
        for i in 0..<4 {
            let lbl = "HF\(i+1)"
            items.append(ControlItem(
                label: lbl,
                customName: lbl,
                kind: .hfader,
                number: UInt8(8 + i),
                zone: "HORIZONTAL FADER \(i+1) (ANALOG CC)",
                idleVelocity: 0,
                activeVelocity: 0,
                liveValue: UInt8(32 + i * 24),
                feedbackMode: .momentary,
                actionType: .passthrough
            ))
        }

        // 6. TOP-RIGHT 2X4 ROTARY KNOBS (CC 20..27)
        for i in 0..<8 {
            let lbl = "K\(i+1)"
            items.append(ControlItem(
                label: lbl,
                customName: lbl,
                kind: .knob,
                number: UInt8(20 + i),
                zone: "ROTARY KNOB \(i+1) (ANALOG CC)",
                idleVelocity: 0,
                activeVelocity: 0,
                liveValue: UInt8(40 + i * 10),
                feedbackMode: .momentary,
                actionType: .passthrough
            ))
        }

        return items
    }

    // ------------------------------------------------------------------------
    // CORE MIDI COMMUNICATIONS
    // ------------------------------------------------------------------------
    private func setupCoreMIDI() {
        var status = MIDIClientCreateWithBlock("BitchBoyClient" as CFString, &midiClient) { [weak self] notifyPtr in
            let msg = notifyPtr.pointee
            if msg.messageID == .msgSetupChanged {
                Task { @MainActor [weak self] in
                    self?.scanForBitchBoy()
                }
            }
        }
        guard status == noErr else { return }

        status = MIDIInputPortCreateWithProtocol(
            midiClient,
            "BitchBoyInput" as CFString,
            ._1_0,
            &inputPort
        ) { [weak self] eventList, _ in
            let packets = eventList.pointee
            var curPacket = packets.packet
            for _ in 0..<packets.numPackets {
                let w0 = curPacket.words.0
                let statusByte = UInt8((w0 >> 16) & 0xFF)
                let data1 = UInt8((w0 >> 8) & 0xFF)
                let data2 = UInt8(w0 & 0xFF)

                Task { @MainActor [weak self] in
                    self?.handleMIDIMessage(status: statusByte, data1: data1, data2: data2)
                }

                if packets.numPackets > 1 {
                    curPacket = MIDIEventPacketNext(&curPacket).pointee
                }
            }
        }

        MIDIOutputPortCreate(midiClient, "BitchBoyOutput" as CFString, &outputPort)
        MIDISourceCreate(midiClient, "BitchBoy Virtual Out" as CFString, &virtualSourceEndpoint)
        scanForBitchBoy()
    }

    func scanForBitchBoy() {
        let destCount = MIDIGetNumberOfDestinations()
        var found = false
        var targetDest: MIDIEndpointRef = 0
        var targetName = deviceName

        for i in 0..<destCount {
            let dest = MIDIGetDestination(i)
            let name = getEndpointName(dest)
            if isRealBitchBoy(name) {
                targetDest = dest
                targetName = name
                found = true
                break
            }
        }

        if !found && destCount > 0 {
            targetDest = MIDIGetDestination(0)
            targetName = getEndpointName(targetDest) + " (Simulated)"
            found = true
        }

        let srcCount = MIDIGetNumberOfSources()
        for i in 0..<srcCount {
            let src = MIDIGetSource(i)
            let sName = getEndpointName(src)
            // Skip our own Virtual Out source to prevent MIDI loopback!
            if src == virtualSourceEndpoint || sName.contains("BitchBoy Virtual Out") {
                continue
            }
            MIDIPortConnectSource(inputPort, src, nil)
        }

        // Refresh all available MIDI output destinations for the MIDI Router
        var targets: [MIDIOutputTarget] = []
        for i in 0..<destCount {
            let dest = MIDIGetDestination(i)
            let dName = getEndpointName(dest)
            if dName.contains("BitchBoy Virtual Out") { continue }
            let isBB = (dest == targetDest) || isRealBitchBoy(dName)
            targets.append(MIDIOutputTarget(name: dName, endpoint: dest, isHardwareBitchBoy: isBB))
        }
        let prevEmpty = availableOutputTargets.isEmpty
        availableOutputTargets = targets
        if prevEmpty {
            // Auto-select any non-BitchBoy external DAW / IAC / Synth targets on first launch
            for t in targets where !t.isHardwareBitchBoy {
                selectedOutputNames.insert(t.name)
            }
        }

        let wasConnected = isConnected
        let prevDest = bitchBoyDestination

        bitchBoyDestination = targetDest
        deviceName = targetName
        isConnected = found

        // CRITICAL FIX: Only send LED update when newly connected or destination changed.
        // This eliminates the 2-second watchdog LED blinking and preserves active light shows!
        if isConnected && (!wasConnected || prevDest != targetDest) {
            log("🔌 Connected to \(deviceName)")
            if currentFXMode == .staticFX {
                restorePresetLEDs()
            } else {
                applyFXMode(currentFXMode)
            }
        } else if !isConnected && wasConnected {
            log("⚠️ BitchBoy disconnected")
        }
    }

    private func isRealBitchBoy(_ name: String) -> Bool {
        let n = name.lowercased()
        if n.contains("qlab") || n.contains("iac") || n.contains("network") || n.contains("session") { return false }
        return n.contains("bitch") || n.contains("rp2040") || n.contains("pico") || n.contains("arduino") || n.contains("tinyusb") || n.contains("bb-") || n.contains("bb01") || n.contains("teeny")
    }

    private func getEndpointName(_ ep: MIDIEndpointRef) -> String {
        var name: Unmanaged<CFString>?
        let err = MIDIObjectGetStringProperty(ep, kMIDIPropertyDisplayName, &name)
        if err == noErr, let n = name {
            return n.takeRetainedValue() as String
        }
        return "MIDI Device"
    }

    private func startHardwareWatchdog() {
        watchdogTimer = Timer.publish(every: 2.0, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                self?.scanForBitchBoy()
            }
    }

    // ------------------------------------------------------------------------
    // TRANSMIT MIDI & LED HARDWARE
    // ------------------------------------------------------------------------
    func sendNoteOn(note: UInt8, velocity: UInt8) {
        guard isConnected, bitchBoyDestination != 0 else { return }
        var packetList = MIDIPacketList()
        var curPacket = MIDIPacketListInit(&packetList)
        let data: [UInt8] = [0x90, note, velocity]
        curPacket = MIDIPacketListAdd(&packetList, 1024, curPacket, 0, 3, data)
        if velocity == 0 {
            let noteOff: [UInt8] = [0x80, note, 0]
            curPacket = MIDIPacketListAdd(&packetList, 1024, curPacket, 0, 3, noteOff)
        }
        MIDISend(outputPort, bitchBoyDestination, &packetList)
    }

    func sendCC(cc: UInt8, value: UInt8) {
        guard isConnected, bitchBoyDestination != 0 else { return }
        var packetList = MIDIPacketList()
        var curPacket = MIDIPacketListInit(&packetList)
        let data: [UInt8] = [0xB0, cc, value]
        curPacket = MIDIPacketListAdd(&packetList, 1024, curPacket, 0, 3, data)
        MIDISend(outputPort, bitchBoyDestination, &packetList)
    }

    // ------------------------------------------------------------------------
    // INTERACTION & SIMULATION
    // ------------------------------------------------------------------------
    func handleKeyPressWithModifiers(index: Int, isShiftDown: Bool) {
        if paintOnMainChassis && index < 49 {
            paintCustomFXKey(keyIndex: index, velocity: customBrushVelocity)
            selectedIndex = index
            return
        }
        if isShiftDown {
            if selectedIndices.contains(index) {
                selectedIndices.remove(index)
            } else {
                selectedIndices.insert(index)
            }
            selectedIndex = index
        } else {
            selectedIndices = [index]
            selectedIndex = index
        }
        simulateKeyPress(at: index)
    }

    func simulateKeyPress(at index: Int) {
        guard index < controls.count else { return }
        selectedIndex = index
        if !selectedIndices.contains(index) {
            selectedIndices.insert(index)
        }

        controls[index].isPressed = true
        let note = controls[index].number
        let activeVel = controls[index].activeVelocity

        SoundEffect.keyTap.play(with: soundPack)
        lastStreamMessage = "[\(controls[index].label)] PRESS #\(note) V:\(activeVel)"
        log("🎹 Tap: \(controls[index].label) (Note #\(note) Vel: \(activeVel))")

        switch controls[index].feedbackMode {
        case .momentary:
            controls[index].currentLED = activeVel
            sendNoteOn(note: note, velocity: activeVel)
        case .toggle:
            controls[index].toggleLatched.toggle()
            let v = controls[index].toggleLatched ? activeVel : controls[index].idleVelocity
            controls[index].currentLED = v
            sendNoteOn(note: note, velocity: v)
        case .staticLED:
            controls[index].currentLED = controls[index].idleVelocity
            sendNoteOn(note: note, velocity: controls[index].idleVelocity)
        }

        executeAction(for: controls[index])
        if index < 49 {
            triggerReactiveKeyFX(at: index)
        }
        if forwardUIInteractions {
            routeIncomingMIDIToTargets(status: 0x90, data1: note, data2: activeVel, isFromHardware: false)
        }

        Task { @MainActor in
            try? await Task.sleep(nanoseconds: 120_000_000)
            self.controls[index].isPressed = false
            if self.controls[index].feedbackMode == .momentary && self.activeReactivePulses.isEmpty {
                let idleVel = self.controls[index].idleVelocity
                self.controls[index].currentLED = idleVel
                self.sendNoteOn(note: note, velocity: idleVel)
                self.lastStreamMessage = "[\(self.controls[index].label)] RELEASE #\(note) V:\(idleVel)"
            }
            if self.forwardUIInteractions {
                self.routeIncomingMIDIToTargets(status: 0x80, data1: note, data2: 0, isFromHardware: false)
            }
        }
    }

    // ------------------------------------------------------------------------
    // REACTIVE MECHANICAL KEYBOARD INTERACTIVE FX ENGINE (49-KEY MATRIX)
    // ------------------------------------------------------------------------
    func triggerReactiveKeyFX(at keyIndex: Int) {
        guard keyIndex >= 0 && keyIndex < 49, reactiveKeyFXMode != .off else { return }
        lastReactiveOriginKey = keyIndex
        let pos = BitchBoyKeyMap.keyPositions[keyIndex]
        let palette: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 127]
        let chosenVel: UInt8 = reactiveFXRainbowColor
            ? palette[(keyIndex * 3 + activeReactivePulses.count * 2) % palette.count]
            : reactiveFXColorVelocity

        var targets: [Int] = []
        if reactiveKeyFXMode == .arcLightning {
            var pool = Array(0..<49).filter { $0 != keyIndex }
            pool.shuffle()
            targets = Array(pool.prefix(8))
        }

        let pulse = ReactivePulseEvent(
            originKeyIndex: keyIndex,
            originCol: Double(pos.col),
            originRow: Double(pos.row),
            mode: reactiveKeyFXMode,
            baseVelocity: chosenVel,
            age: 0.0,
            lightningTargets: targets
        )
        activeReactivePulses.append(pulse)
        if activeReactivePulses.count > 12 {
            activeReactivePulses.removeFirst(activeReactivePulses.count - 12)
        }

        if reactiveFXTimer == nil {
            lastSentReactiveLEDs = Array(repeating: 255, count: 49)
            reactiveFXTimer = Timer.publish(every: 0.035, on: .main, in: .common)
                .autoconnect()
                .sink { [weak self] _ in
                    self?.stepReactiveKeyFX()
                }
        }
        stepReactiveKeyFX()
    }

    func triggerRandomDemoReactiveFX() {
        let centerKeys = [11, 12, 19, 20, 18, 21, 44]
        let randomKey = centerKeys.randomElement() ?? Int.random(in: 0..<49)
        if reactiveKeyFXMode == .off {
            reactiveKeyFXMode = .ripple
        }
        triggerReactiveKeyFX(at: randomKey)
        SoundEffect.keyTap.play(with: soundPack)
    }

    private func stepReactiveKeyFX() {
        let dt = 0.048 * max(0.3, reactiveFXSpeed)
        for i in activeReactivePulses.indices {
            activeReactivePulses[i].age += dt
        }
        activeReactivePulses.removeAll { $0.age > 1.05 }

        let palette: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 127]

        for k in 0..<49 {
            let baseVel: UInt8
            if controls[k].isPressed {
                baseVel = controls[k].activeVelocity
            } else if controls[k].feedbackMode == .toggle && controls[k].toggleLatched {
                baseVel = controls[k].activeVelocity
            } else {
                baseVel = controls[k].idleVelocity
            }

            var effectVel: UInt8 = 0
            var maxIntensity: Double = 0.0
            let kPos = BitchBoyKeyMap.keyPositions[k]
            let kc = Double(kPos.col)
            let kr = Double(kPos.row)

            for pulse in activeReactivePulses {
                let dx = kc - pulse.originCol
                let dy = kr - pulse.originRow
                let dist = sqrt(dx * dx + dy * dy)
                var intensity: Double = 0.0

                switch pulse.mode {
                case .ripple:
                    let radius = pulse.age * 11.5
                    let ringDiff = abs(dist - radius)
                    if ringDiff < 1.45 {
                        intensity = (1.0 - ringDiff / 1.45) * max(0.0, 1.0 - pulse.age * 0.75)
                    }
                case .crossLaser:
                    let beamPos = pulse.age * 11.0
                    if Int(kr) == Int(pulse.originRow) {
                        let d = abs( abs(dx) - beamPos )
                        if d < 1.6 {
                            intensity = (1.0 - d / 1.6) * (1.0 - pulse.age * 0.65)
                        }
                    }
                    if Int(kc) == Int(pulse.originCol) {
                        let d = abs( abs(dy) - beamPos * 0.65 )
                        if d < 1.3 {
                            intensity = max(intensity, (1.0 - d / 1.3) * (1.0 - pulse.age * 0.65))
                        }
                    }
                case .supernova:
                    let maxR = 6.5 * sin(pulse.age * .pi)
                    if dist <= maxR {
                        intensity = (1.0 - dist / max(0.5, maxR)) * (1.0 - pulse.age * 0.5)
                    }
                case .heatTrail:
                    if k == pulse.originKeyIndex {
                        intensity = max(0.0, 1.0 - pulse.age)
                    } else if dist <= 1.5 {
                        intensity = max(0.0, (0.65 - pulse.age * 0.85) * (1.0 - dist / 1.6))
                    }
                case .meteorRain:
                    if Int(kc) == Int(pulse.originCol) {
                        let dropRow = fmod(pulse.originRow + pulse.age * 7.5, 5.0)
                        let d = abs(kr - dropRow)
                        if d < 1.35 {
                            intensity = (1.0 - d / 1.35) * (1.0 - pulse.age * 0.65)
                        }
                    }
                case .arcLightning:
                    if k == pulse.originKeyIndex || pulse.lightningTargets.contains(k) {
                        let flicker = sin(pulse.age * 42.0 + Double(k)) > -0.2 ? 1.0 : 0.35
                        intensity = (1.0 - pulse.age * 0.85) * flicker
                    }
                case .off:
                    break
                }

                if intensity > maxIntensity {
                    maxIntensity = intensity
                    if reactiveFXRainbowColor {
                        let pIdx = abs(Int(round(dist + pulse.age * 8.0)) + pulse.originKeyIndex) % palette.count
                        effectVel = intensity > 0.72 ? 127 : palette[pIdx]
                    } else {
                        effectVel = intensity > 0.82 ? 127 : pulse.baseVelocity
                    }
                }
            }

            let finalVel: UInt8 = maxIntensity > 0.14 ? effectVel : baseVel
            if controls[k].currentLED != finalVel {
                controls[k].currentLED = finalVel
            }
            if lastSentReactiveLEDs[k] != finalVel {
                lastSentReactiveLEDs[k] = finalVel
                sendNoteOn(note: controls[k].number, velocity: finalVel)
            }
        }

        if activeReactivePulses.isEmpty {
            reactiveFXTimer?.cancel()
            reactiveFXTimer = nil
        }
    }

    func setControlLiveValue(index: Int, value: UInt8) {
        guard index < controls.count else { return }
        selectedIndex = index
        controls[index].liveValue = value
        sendCC(cc: controls[index].number, value: value)
        if forwardUIInteractions {
            routeIncomingMIDIToTargets(status: 0xB0, data1: controls[index].number, data2: value, isFromHardware: false)
        }
        lastStreamMessage = "[\(controls[index].label)] CC #\(controls[index].number) VAL: \(value)"
        SoundEffect.faderSlide.play(with: soundPack)
        if currentFXMode == .vuMeter && index >= 49 && index < 57 {
            updateVUMeterFaders()
        }
    }

    // ------------------------------------------------------------------------
    // LIGHTING EFFECTS ENGINE (15 PROCEDURAL SHOWS + CONTINUOUS HARDWARE SYNC)
    // ------------------------------------------------------------------------
    func cycleFXMode() {
        let modes = LEDFXMode.allCases
        if let idx = modes.firstIndex(of: currentFXMode) {
            let next = modes[(idx + 1) % modes.count]
            applyFXMode(next)
        }
    }

    func applyFXMode(_ mode: LEDFXMode) {
        isPlayingCustomFX = false
        activeCustomFXName = ""
        currentFXMode = mode
        vuMeterLEDsEnabled = (mode == .vuMeter)
        fxTimer?.cancel()
        fxTimer = nil

        switch mode {
        case .staticFX:
            restorePresetLEDs()
            log("💡 FX Mode: STATIC PRESET (Restored LEDs)")
        case .rainbowContinuous:
            startContinuousRainbowFX()
            log("🌈 FX Mode: CONTINUOUS RAINBOW WAVE")
        case .knightRider:
            startKnightRiderFX()
            log("⚡️ FX Mode: CYLON SCANNER")
        case .matrixRain:
            startMatrixRainFX()
            log("🌧️ FX Mode: DIGITAL MATRIX RAIN")
        case .cyberPulse:
            startCyberPulseFX()
            log("🔮 FX Mode: CYBERPUNK PULSE")
        case .vuMeter:
            startVUMeterFX()
            log("🎚️ FX Mode: LIVE FADER VU METER")
        case .fireEmbers:
            startFireEmbersFX()
            log("🔥 FX Mode: FIRE & EMBERS")
        case .waterRipple:
            startWaterRippleFX()
            log("💧 FX Mode: CENTER RIPPLE")
        case .starfield:
            startStarfieldFX()
            log("✨ FX Mode: TWINKLE STARS")
        case .audioStrobe:
            startAudioStrobeFX()
            log("⚡️ FX Mode: STROBE FLASH")
        case .heartbeat:
            startHeartbeatFX()
            log("💓 FX Mode: HEARTBEAT")
        case .plasmaWave:
            startPlasmaWaveFX()
            log("🌀 FX Mode: PLASMA FIELD")
        case .spiralVortex:
            startSpiralVortexFX()
            log("🌪️ FX Mode: SPIRAL VORTEX")
        case .eqDance:
            startEQDanceFX()
            log("📊 FX Mode: EQ DANCE")
        case .colorChase:
            startColorChaseFX()
            log("🐍 FX Mode: COLOR RUNNER")
        }
    }

    func restorePresetLEDs() {
        for i in 0..<49 {
            let vel = controls[i].idleVelocity
            controls[i].currentLED = vel
            sendNoteOn(note: controls[i].number, velocity: vel)
        }
    }

    // 1. Authentic Rainbow Wave Refresh Sweep (sweeps 11 columns and restores presets)
    func runRainbowWaveRefresh() {
        guard !isRainbowRunning else { return }
        isRainbowRunning = true
        log("✨ Sweeping Rainbow Wave Hardware LED Refresh...")

        Task { @MainActor in
            let rainbowVelocities: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127]
            let totalSteps = 22
            for step in 0..<totalSteps {
                for k in 0..<49 {
                    let col = BitchBoyKeyMap.keyPositions[k].col
                    let wavePos = step - col
                    if wavePos >= 0 && wavePos < rainbowVelocities.count {
                        let vel = rainbowVelocities[wavePos]
                        self.controls[k].currentLED = vel
                        self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                    } else if wavePos >= rainbowVelocities.count {
                        let vel = self.controls[k].idleVelocity
                        self.controls[k].currentLED = vel
                        self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                    }
                }
                try? await Task.sleep(nanoseconds: 50_000_000)
            }
            if self.currentFXMode == .staticFX {
                self.restorePresetLEDs()
            }
            self.isRainbowRunning = false
            self.log("✅ Rainbow Wave Refresh Complete")
        }
    }

    // 2. Continuous 11-Column Rainbow Wave (Loops forever until stopped)
    private func startContinuousRainbowFX() {
        let rainbowVelocities: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127]
        var step = 0
        let interval = 0.07 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                step += 1
                for k in 0..<49 {
                    let col = BitchBoyKeyMap.keyPositions[k].col
                    let vel = rainbowVelocities[(step + col) % rainbowVelocities.count]
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 3. Knight Rider / Cylon Scanner (Sweeps across columns 0..10)
    private func startKnightRiderFX() {
        var step = 0
        let interval = 0.05 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                step = (step + 1) % 20
                let activeCol = step <= 10 ? step : (20 - step)
                for k in 0..<49 {
                    let col = BitchBoyKeyMap.keyPositions[k].col
                    let dist = abs(col - activeCol)
                    let vel: UInt8 = dist == 0 ? 127 : (dist == 1 ? 55 : (dist == 2 ? 18 : 0))
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 4. Digital Matrix Rain (Cascading droplets down columns)
    private func startMatrixRainFX() {
        var step = 0
        let interval = 0.07 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                step += 1
                for k in 0..<49 {
                    let col = BitchBoyKeyMap.keyPositions[k].col
                    let row = BitchBoyKeyMap.keyPositions[k].row
                    let dropRow = (step + col * 3) % 7
                    let vel: UInt8 = (row == dropRow) ? 127 : (row == (dropRow - 1 + 7) % 7 ? 60 : (row == (dropRow - 2 + 7) % 7 ? 22 : 0))
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 5. Cyberpunk Pulse (Smooth breathing through neon velocities)
    private func startCyberPulseFX() {
        var phase: Double = 0
        let interval = 0.04 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                phase += 0.12
                let normalized = (sin(phase) + 1.0) * 0.5
                let vel = UInt8(normalized * 115.0 + 12.0)
                for k in 0..<49 {
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 6. Live Fader VU Meter (8 vertical faders control columns 0..7)
    private func startVUMeterFX() {
        let interval = 0.08 / max(0.2, fxSpeed)
        var bgTick = 0
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                bgTick += 1
                self.updateVUMeterFaders(bgTick: bgTick)
            }
    }

    func updateVUMeterFaders(bgTick: Int = 0) {
        for col in 0..<8 {
            let faderVal = Double(controls[49 + col].liveValue)
            let colKeyIndices = [32 + col, 24 + col, 16 + col, 8 + col, 0 + col]
            let thresholds: [Double] = [15, 40, 65, 90, 115]
            let colors: [UInt8] = [45, 55, 75, 100, 127]
            for seg in 0..<5 {
                let kIdx = colKeyIndices[seg]
                let isLit = faderVal >= thresholds[seg]
                let vel: UInt8 = isLit ? colors[seg] : 0
                if controls[kIdx].currentLED != vel {
                    controls[kIdx].currentLED = vel
                    sendNoteOn(note: controls[kIdx].number, velocity: vel)
                }
            }
        }
        let padWave = UInt8(((sin(Double(bgTick) * 0.2) + 1.0) * 0.5) * 40.0 + 10.0)
        for p in 40..<49 {
            if controls[p].currentLED != padWave {
                controls[p].currentLED = padWave
                sendNoteOn(note: controls[p].number, velocity: padWave)
            }
        }
    }

    // 7. Fire & Embers (Organic flame simulation with flickering coals)
    private func startFireEmbersFX() {
        let interval = 0.08 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                for k in 0..<49 {
                    let row = BitchBoyKeyMap.keyPositions[k].row
                    let baseHeat = max(0, 5 - row) * 22
                    let flicker = Int.random(in: -18...18)
                    let vel = UInt8(max(0, min(127, baseHeat + flicker)))
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 8. Center Ripple (Concentric shockwaves expanding outward)
    private func startWaterRippleFX() {
        var phase: Double = 0
        let interval = 0.05 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                phase += 0.25
                let centerCol = 3.5
                let centerRow = 2.0
                for k in 0..<49 {
                    let col = Double(BitchBoyKeyMap.keyPositions[k].col)
                    let row = Double(BitchBoyKeyMap.keyPositions[k].row)
                    let dist = sqrt(pow(col - centerCol, 2) + pow(row - centerRow, 2))
                    let wave = sin(dist * 1.6 - phase)
                    let vel = UInt8(max(0.0, wave) * 127.0)
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 9. Twinkle Stars (Deep-space twinkling night sky)
    private func startStarfieldFX() {
        var starBrightness: [UInt8] = Array(repeating: 0, count: 49)
        let interval = 0.08 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                for i in 0..<49 {
                    if starBrightness[i] > 14 {
                        starBrightness[i] -= 14
                    } else {
                        starBrightness[i] = 0
                    }
                }
                for _ in 0..<Int.random(in: 2...4) {
                    let rIdx = Int.random(in: 0..<49)
                    starBrightness[rIdx] = UInt8.random(in: 90...127)
                }
                for k in 0..<49 {
                    let vel = starBrightness[k]
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 10. Strobe Flash (Fast stroboscopic burst)
    private func startAudioStrobeFX() {
        var count = 0
        let interval = 0.07 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                count += 1
                let vel: UInt8 = (count % 3 == 0) ? 127 : 0
                for k in 0..<49 {
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 11. Heartbeat (Realistic double-thump cardiac pulse)
    private func startHeartbeatFX() {
        var step = 0
        let interval = 0.05 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                step = (step + 1) % 24
                let vel: UInt8
                switch step {
                case 0: vel = 127 // Lub
                case 1: vel = 80
                case 2: vel = 20
                case 4: vel = 110 // Dub
                case 5: vel = 60
                case 6: vel = 10
                default: vel = 0 // Pause
                }
                for k in 0..<49 {
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 12. Plasma Field (Trigonometric 2D undulating field)
    private func startPlasmaWaveFX() {
        var t: Double = 0
        let interval = 0.06 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                t += 0.16
                for k in 0..<49 {
                    let c = Double(BitchBoyKeyMap.keyPositions[k].col)
                    let r = Double(BitchBoyKeyMap.keyPositions[k].row)
                    let v1 = sin(c * 0.7 + t)
                    let v2 = cos(r * 0.8 + t * 0.9)
                    let v3 = sin((c + r) * 0.5 + t * 1.1)
                    let norm = (v1 + v2 + v3 + 3.0) / 6.0
                    let vel = UInt8(norm * 127.0)
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 13. Spiral Vortex (Rotating light beam around center)
    private func startSpiralVortexFX() {
        var angle: Double = 0
        let interval = 0.06 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                angle += 0.32
                let centerCol = 4.0
                let centerRow = 2.0
                for k in 0..<49 {
                    let dx = Double(BitchBoyKeyMap.keyPositions[k].col) - centerCol
                    let dy = Double(BitchBoyKeyMap.keyPositions[k].row) - centerRow
                    let keyAngle = atan2(dy, dx)
                    let diff = fmod(keyAngle - angle + .pi * 4, .pi * 2)
                    let vel = UInt8(max(0.0, 1.0 - diff / (.pi * 2)) * 127.0)
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // 14. EQ Dance (Graphic equalizer bands jumping to imaginary track)
    private func startEQDanceFX() {
        var colHeights = [1, 2, 3, 4, 3, 2, 4, 2]
        let interval = 0.08 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                for c in 0..<8 {
                    let delta = Int.random(in: -2...2)
                    colHeights[c] = max(0, min(5, colHeights[c] + delta))
                }
                for k in 0..<49 {
                    let col = BitchBoyKeyMap.keyPositions[k].col
                    let row = BitchBoyKeyMap.keyPositions[k].row
                    if col < 8 {
                        let activeLevel = 4 - row
                        let isLit = activeLevel < colHeights[col]
                        let vel: UInt8 = isLit ? UInt8(40 + activeLevel * 20) : 0
                        self.controls[k].currentLED = vel
                        self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                    } else {
                        let padVel = UInt8.random(in: 20...70)
                        self.controls[k].currentLED = padVel
                        self.sendNoteOn(note: self.controls[k].number, velocity: padVel)
                    }
                }
            }
    }

    // 15. Color Runner (Snake head chasing with fading tail)
    private func startColorChaseFX() {
        var head = 0
        let interval = 0.05 / max(0.2, fxSpeed)
        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                head = (head + 1) % 49
                for k in 0..<49 {
                    let dist = (head - k + 49) % 49
                    let vel: UInt8 = dist < 7 ? UInt8((7 - dist) * 18) : 0
                    self.controls[k].currentLED = vel
                    self.sendNoteOn(note: self.controls[k].number, velocity: vel)
                }
            }
    }

    // ------------------------------------------------------------------------
    // MASCOT & ACTIONS
    // ------------------------------------------------------------------------
    func triggerMascotTap() {
        mascotScale = 1.5
        SoundEffect.mascotSqueak.play(with: soundPack)
        let quotes = [
            "\"DON'T SQUEEZE THE BITCHBOY!\"",
            "\"YOU BITCH!\"",
            "\"MIDI PIPES ARE PURRING SMOOTH!\"",
            "\"CRANK THAT FADER TO 127!\"",
            "\"PRESS SHIFT FOR MULTI-SELECT!\"",
            "\"BEEPS, BOOPS, AND HEAVY BASS!\"",
            "\"TAYLOR FOSSEN APPROVED! 👑\"",
            "\"STANDING BY\"",
            "\"FART\"",
            "\"MY FAVORITE WORD IS BITCH\"",
            "\"NEON GLOW AT MAXIMUM WATTAGE!\""
        ]
        mascotSpeech = quotes.randomElement() ?? quotes[0]
        mascotSpeechVisible = true

        Task { @MainActor in
            try? await Task.sleep(nanoseconds: 850_000_000)
            self.mascotScale = 1.5
            try? await Task.sleep(nanoseconds: 5_500_000_000)
            withAnimation {
                self.mascotSpeechVisible = false
            }
        }
    }

    func blackoutPanic() {
        fxTimer?.cancel()
        fxTimer = nil
        currentFXMode = .staticFX
        for i in 0..<49 {
            controls[i].idleVelocity = 0
            controls[i].currentLED = 0
            sendNoteOn(note: controls[i].number, velocity: 0)
        }
        lastStreamMessage = "🚨 BLACKOUT / PANIC ALL LEDS OFF"
        log("🚨 PANIC BLACKOUT TRIGGERED")
    }

    func cloneCurrentSlot() {
        let nextIdx = (activeSlot + 1) % max(1, slots.count)
        cloneCurrentSlotTo(nextIdx)
    }

    func cloneCurrentSlotTo(_ targetIdx: Int) {
        guard slots.indices.contains(activeSlot), slots.indices.contains(targetIdx) else { return }
        saveCurrentSlot()
        let current = slots[activeSlot]
        let slotLetter = ["A", "B", "C", "D", "E", "F", "G", "H"][targetIdx % 8]
        slots[targetIdx].controls = current.controls
        slots[targetIdx].globalRules = current.globalRules
        slots[targetIdx].iconName = current.iconName
        slots[targetIdx].accentVelocity = current.accentVelocity
        slots[targetIdx].name = "SLOT \(slotLetter) // CLONE"
        saveCurrentSlot()
        SoundEffect.presetSwitch.play(with: soundPack)
        log("📋 Cloned \(current.name) into Slot \(slotLetter)")
    }

    func resetCurrentSlotToDefault() {
        guard slots.indices.contains(activeSlot) else { return }
        let defaults = Self.buildDefault8MemorySlots()
        let fallback = defaults[activeSlot % defaults.count]
        slots[activeSlot].controls = Self.buildDefault69Map()
        slots[activeSlot].globalRules = ""
        slots[activeSlot].name = fallback.name
        slots[activeSlot].iconName = fallback.iconName
        slots[activeSlot].accentVelocity = fallback.accentVelocity
        controls = slots[activeSlot].controls
        saveCurrentSlot()
        restorePresetLEDs()
        SoundEffect.presetSwitch.play(with: soundPack)
        log("🔄 Reset \(slots[activeSlot].name) to factory defaults")
    }

    func exportPresetToJSON() -> String {
        guard let data = try? JSONEncoder().encode(slots[activeSlot]),
              let str = String(data: data, encoding: .utf8) else { return "" }
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(str, forType: .string)
        log("⤓ Exported \(slots[activeSlot].name) to Clipboard JSON")
        return str
    }

    func importPresetFromClipboard() {
        guard let str = NSPasteboard.general.string(forType: .string),
              let data = str.data(using: .utf8),
              let decoded = try? JSONDecoder().decode(PresetSlot.self, from: data) else {
            log("❌ Invalid Preset JSON in Clipboard")
            return
        }
        slots[activeSlot] = decoded
        controls = decoded.controls
        saveCurrentSlot()
        restorePresetLEDs()
        log("⤒ Imported Preset into Slot \(activeSlot)")
    }

    // ------------------------------------------------------------------------
    // INCOMING MIDI PARSER
    // ------------------------------------------------------------------------
    private func handleMIDIMessage(status: UInt8, data1: UInt8, data2: UInt8) {
        // Forward physical BitchBoy MIDI input to Virtual Out & selected external programs!
        routeIncomingMIDIToTargets(status: status, data1: data1, data2: data2, isFromHardware: true)

        let msgType = status & 0xF0
        let isNoteOn = (msgType == 0x90) && (data2 > 0)
        let isNoteOff = (msgType == 0x80) || ((msgType == 0x90) && (data2 == 0))
        let isCC = (msgType == 0xB0)

        guard let idx = controls.firstIndex(where: {
            if isCC { return ($0.kind == .vfader || $0.kind == .hfader || $0.kind == .knob) && $0.number == data1 }
            else { return $0.kind == .key && $0.number == data1 }
        }) else { return }

        selectedIndex = idx

        if isNoteOn || isNoteOff {
            if isNoteOn {
                controls[idx].isPressed = true
                lastStreamMessage = "[\(controls[idx].label)] ON #\(data1) V:\(data2)"
                SoundEffect.keyTap.play(with: soundPack)

                switch controls[idx].feedbackMode {
                case .momentary:
                    controls[idx].currentLED = controls[idx].activeVelocity
                    sendNoteOn(note: data1, velocity: controls[idx].activeVelocity)
                case .toggle:
                    controls[idx].toggleLatched.toggle()
                    let v = controls[idx].toggleLatched ? controls[idx].activeVelocity : controls[idx].idleVelocity
                    controls[idx].currentLED = v
                    sendNoteOn(note: data1, velocity: v)
                case .staticLED:
                    controls[idx].currentLED = controls[idx].idleVelocity
                    sendNoteOn(note: data1, velocity: controls[idx].idleVelocity)
                }

                executeAction(for: controls[idx])
                if idx < 49 {
                    triggerReactiveKeyFX(at: idx)
                }
            } else if isNoteOff {
                controls[idx].isPressed = false
                lastStreamMessage = "[\(controls[idx].label)] OFF #\(data1) V:0"
                if activeReactivePulses.isEmpty {
                    let restoreVel: UInt8 = (controls[idx].feedbackMode == .toggle && controls[idx].toggleLatched) ? controls[idx].activeVelocity : controls[idx].idleVelocity
                    controls[idx].currentLED = restoreVel
                    Task { @MainActor in
                        try? await Task.sleep(nanoseconds: 15_000_000)
                        if self.activeReactivePulses.isEmpty {
                            self.sendNoteOn(note: data1, velocity: restoreVel)
                        }
                    }
                }
            }
        } else if isCC {
            controls[idx].liveValue = data2
            lastStreamMessage = "[\(controls[idx].label)] CC #\(data1) VAL:\(data2)"
            if currentFXMode == .vuMeter && idx >= 49 && idx < 57 {
                updateVUMeterFaders()
            }
        }
    }

    // ------------------------------------------------------------------------
    // MIDI ROUTER & MULTI-PROGRAM OUTPUT ENGINE
    // ------------------------------------------------------------------------
    func routeIncomingMIDIToTargets(status: UInt8, data1: UInt8, data2: UInt8, isFromHardware: Bool) {
        guard midiThruEnabled else { return }
        let msgType = status & 0xF0
        let isNote = (msgType == 0x90 || msgType == 0x80)
        let isCC = (msgType == 0xB0)

        if isNote && !forwardNotes { return }
        if isCC && !forwardCCs { return }

        // Channel Remap (0 = keep original channel, 1..16 = force channel 0..15)
        let outChan: UInt8 = remapChannel == 0 ? (status & 0x0F) : UInt8((remapChannel - 1) & 0x0F)
        let outStatus: UInt8 = msgType | outChan

        // Note Transpose & Velocity Curve
        let outData1: UInt8
        let outData2: UInt8
        if isNote {
            let shifted = max(0, min(127, Int(data1) + transposeSemitones))
            outData1 = UInt8(shifted)
            if msgType == 0x90 && data2 > 0 {
                outData2 = velocityCurve.transform(data2)
            } else {
                outData2 = data2
            }
        } else {
            outData1 = data1
            outData2 = data2
        }

        var packetList = MIDIPacketList()
        var curPacket = MIDIPacketListInit(&packetList)
        let bytes: [UInt8] = [outStatus, outData1, outData2]
        curPacket = MIDIPacketListAdd(&packetList, 1024, curPacket, 0, 3, bytes)

        // 1. Broadcast to macOS Virtual MIDI Source ("BitchBoy Virtual Out")
        if virtualOutEnabled && virtualSourceEndpoint != 0 {
            MIDIReceived(virtualSourceEndpoint, &packetList)
        }

        // 2. Send to all checked external MIDI Destinations (DAWs, IAC Driver, Synths, QLab, etc.)
        for target in availableOutputTargets where selectedOutputNames.contains(target.name) {
            // Avoid echoing back into the physical BitchBoy LED port when routing from hardware
            if isFromHardware && target.endpoint == bitchBoyDestination { continue }
            MIDISend(outputPort, target.endpoint, &packetList)
        }

        txPacketCount += 1
        let chDisplay = Int(outChan) + 1
        let hexStr = String(format: "%02X %02X %02X", outStatus, outData1, outData2)
        let desc: String
        if isCC {
            desc = "CH\(chDisplay) CC#\(outData1) = \(outData2)"
        } else if msgType == 0x90 && outData2 > 0 {
            desc = "CH\(chDisplay) NOTE ON #\(outData1) V:\(outData2)"
        } else {
            desc = "CH\(chDisplay) NOTE OFF #\(outData1)"
        }
        lastRoutedMessage = "\(desc) [\(hexStr)]"
        routerPacketHistory.insert("[\(hexStr)] \(desc)", at: 0)
        if routerPacketHistory.count > 16 {
            routerPacketHistory.removeLast()
        }
    }

    func toggleOutputTarget(_ name: String) {
        if selectedOutputNames.contains(name) {
            selectedOutputNames.remove(name)
        } else {
            selectedOutputNames.insert(name)
        }
    }

    func selectAllExternalOutputs() {
        for t in availableOutputTargets where !t.isHardwareBitchBoy {
            selectedOutputNames.insert(t.name)
        }
        log("🔌 Selected all \(selectedOutputNames.count) external MIDI outputs")
    }

    func sendTestNoteToOutputs() {
        let chord: [UInt8] = [60, 64, 67, 72]
        for n in chord {
            routeIncomingMIDIToTargets(status: 0x90, data1: n, data2: 110, isFromHardware: false)
        }
        SoundEffect.presetSwitch.play(with: soundPack)
        log("🎵 Sent Test C-Major Chord to routed MIDI outputs")
        Task { @MainActor in
            try? await Task.sleep(nanoseconds: 350_000_000)
            for n in chord {
                self.routeIncomingMIDIToTargets(status: 0x80, data1: n, data2: 0, isFromHardware: false)
            }
        }
    }

    func sendPanicAllNotesOffToOutputs() {
        for ch: UInt8 in 0..<16 {
            routeIncomingMIDIToTargets(status: 0xB0 | ch, data1: 123, data2: 0, isFromHardware: false)
        }
        log("🚨 Sent All-Notes-Off (CC 123) across Channels 1–16")
    }

    func openAudioMIDISetupApp() {
        let url = URL(fileURLWithPath: "/System/Applications/Utilities/Audio MIDI Setup.app")
        NSWorkspace.shared.open(url)
        log("🎛️ Opened macOS Audio MIDI Setup")
    }

    func registerTapTempo() {
        let now = Date()
        lastTapTimes.append(now)
        lastTapTimes = lastTapTimes.filter { now.timeIntervalSince($0) < 4.0 }
        if lastTapTimes.count >= 2 {
            var intervals: [Double] = []
            for i in 1..<lastTapTimes.count {
                intervals.append(lastTapTimes[i].timeIntervalSince(lastTapTimes[i - 1]))
            }
            let avg = intervals.reduce(0, +) / Double(intervals.count)
            if avg > 0.15 {
                tapTempoBPM = min(240.0, max(40.0, round(60.0 / avg)))
                // Also sync fxSpeed to BPM (120 BPM = 1.0x)
                fxSpeed = min(2.5, max(0.4, round((tapTempoBPM / 120.0) * 10.0) / 10.0))
                log("⏱️ Tap Tempo: \(Int(tapTempoBPM)) BPM (FX Speed \(String(format: "%.1fx", fxSpeed)))")
            }
        }
        SoundEffect.knobTick.play(with: soundPack)
    }

    // ------------------------------------------------------------------------
    // GROUND-UP CUSTOM LIGHTING FX STUDIO ENGINE
    // ------------------------------------------------------------------------
    // ------------------------------------------------------------------------
    // UI SCALING / ZOOM & CUSTOM APP ICON ENGINE
    // ------------------------------------------------------------------------
    static let consoleBaseWidth: CGFloat = 1566
    static let consoleBaseHeight: CGFloat = 780

    func loadUISettingsAndAppIcon() {
        // Always set the live macOS Dock / Application icon to our BitchBoyBadgeIcon / AppIcon artwork
        UserDefaults.standard.removeObject(forKey: "BitchBoy_CustomAppIconPath_v1")
        if let badgeIcon = NSImage(named: "BitchBoyBadgeIcon") {
            NSApplication.shared.applicationIconImage = badgeIcon
        }

        // Always keep autoFitWindow = true so dragging/resizing the window NEVER clips the controls!
        autoFitWindow = true
        if UserDefaults.standard.object(forKey: uiScaleStorageKey) != nil {
            let saved = UserDefaults.standard.double(forKey: uiScaleStorageKey)
            if saved >= 0.15 && saved <= 2.5 {
                uiScale = CGFloat(saved)
            }
        }
        if UserDefaults.standard.object(forKey: alwaysOnTopStorageKey) != nil {
            isWindowAlwaysOnTop = UserDefaults.standard.bool(forKey: alwaysOnTopStorageKey)
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.15) {
            self.applyWindowAlwaysOnTop()
        }
    }

    func toggleWindowAlwaysOnTop() {
        isWindowAlwaysOnTop.toggle()
        UserDefaults.standard.set(isWindowAlwaysOnTop, forKey: alwaysOnTopStorageKey)
        applyWindowAlwaysOnTop()
        SoundEffect.knobTick.play(with: soundPack)
        log(isWindowAlwaysOnTop ? "📌 Window pinned Always On Top" : "📌 Window unpinned (normal level)")
    }

    func applyWindowAlwaysOnTop() {
        if let badgeIcon = NSImage(named: "BitchBoyBadgeIcon") {
            NSApplication.shared.applicationIconImage = badgeIcon
        }
        let minW = Self.consoleBaseWidth * 0.15
        let minH = Self.consoleBaseHeight * 0.15
        for window in NSApplication.shared.windows {
            let className = String(describing: type(of: window))
            if className.contains("StatusBar") || className.contains("Menu") { continue }
            window.title = "BitchManager"
            window.level = isWindowAlwaysOnTop ? .floating : .normal
            window.contentMinSize = NSSize(width: minW, height: minH)
            window.contentAspectRatio = NSSize(width: Self.consoleBaseWidth, height: Self.consoleBaseHeight)
        }
    }

    func resizeMainWindowToScale(_ scale: CGFloat) {
        let clamped = min(2.2, max(0.15, scale))
        let targetW = round(Self.consoleBaseWidth * clamped)
        let targetH = round(Self.consoleBaseHeight * clamped)
        DispatchQueue.main.async {
            for window in NSApplication.shared.windows {
                let className = String(describing: type(of: window))
                if className.contains("StatusBar") || className.contains("Menu") { continue }
                window.contentMinSize = NSSize(width: Self.consoleBaseWidth * 0.15, height: Self.consoleBaseHeight * 0.15)
                window.contentAspectRatio = NSSize(width: Self.consoleBaseWidth, height: Self.consoleBaseHeight)
                window.setContentSize(NSSize(width: targetW, height: targetH))
            }
        }
    }

    func setManualZoom(_ newScale: CGFloat) {
        let clamped = min(2.2, max(0.15, (newScale * 20.0).rounded() / 20.0))
        uiScale = clamped
        autoFitWindow = true // Keep auto-fit active so resizing the window afterwards still scales the UI and never clips!
        UserDefaults.standard.set(Double(uiScale), forKey: uiScaleStorageKey)
        resizeMainWindowToScale(clamped)
        log("🔍 Window & UI scaled to \(Int(round(clamped * 100)))%")
    }

    func zoomInUI() {
        let base = autoFitWindow ? lastComputedFitScale : uiScale
        setManualZoom(base + 0.10)
    }

    func zoomOutUI() {
        let base = autoFitWindow ? lastComputedFitScale : uiScale
        setManualZoom(base - 0.10)
    }

    func enableAutoFitWindow() {
        autoFitWindow = true
        UserDefaults.standard.set(true, forKey: uiAutoFitStorageKey)
        log("📐 UI Scaling set to Auto-Fit Window")
    }

    func loadCustomEffectsFromDisk() {
        if let data = UserDefaults.standard.data(forKey: customFXStorageKey),
           let decoded = try? JSONDecoder().decode([CustomLightEffect].self, from: data),
           !decoded.isEmpty {
            self.customEffects = decoded
            self.editingCustomFX = decoded[0]
            return
        }
        let starters = CustomLightEffect.buildStarterEffects()
        self.customEffects = starters
        self.editingCustomFX = starters[0]
        saveCustomEffectsToDisk()
    }

    func saveCustomEffectsToDisk() {
        if let encoded = try? JSONEncoder().encode(customEffects) {
            UserDefaults.standard.set(encoded, forKey: customFXStorageKey)
        }
    }

    func saveEditingCustomFXToLibrary() {
        if let idx = customEffects.firstIndex(where: { $0.id == editingCustomFX.id }) {
            customEffects[idx] = editingCustomFX
        } else {
            var copy = editingCustomFX
            copy.id = UUID()
            customEffects.insert(copy, at: 0)
            editingCustomFX = copy
        }
        saveCustomEffectsToDisk()
        SoundEffect.presetSwitch.play(with: soundPack)
        log("💾 Saved Custom Light Show: '\(editingCustomFX.name)'")
    }

    func createNewBlankCustomFX() {
        let blank = CustomLightEffect(
            name: "CUSTOM SHOW #\(customEffects.count + 1)",
            isProcedural: false,
            frames: [CustomLightEffect.emptyFrame(fill: 0)],
            fps: 10.0,
            loopMode: .loop
        )
        editingCustomFX = blank
        editingFrameIndex = 0
        previewCurrentCustomFrameOnHardware()
        log("✨ Created new blank Custom Light Show")
    }

    func deleteSavedCustomFX(_ fx: CustomLightEffect) {
        customEffects.removeAll { $0.id == fx.id }
        if customEffects.isEmpty {
            customEffects = CustomLightEffect.buildStarterEffects()
        }
        saveCustomEffectsToDisk()
        log("🗑️ Deleted Custom Show '\(fx.name)'")
    }

    func selectCustomFXForEditing(_ fx: CustomLightEffect) {
        editingCustomFX = fx
        editingFrameIndex = 0
        rightWingTab = fx.isProcedural ? "WAVESYNTH" : "SEQUENCER"
        previewCurrentCustomFrameOnHardware()
        log("✏️ Loaded '\(fx.name)' into FX Studio")
    }

    func previewCurrentCustomFrameOnHardware() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        let frame = editingCustomFX.frames[safeIdx]
        for k in 0..<min(49, frame.count) {
            let v = frame[k]
            controls[k].currentLED = v
            sendNoteOn(note: controls[k].number, velocity: v)
        }
    }

    func paintCustomFXKey(keyIndex: Int, velocity: UInt8) {
        guard keyIndex >= 0 && keyIndex < 49 else { return }
        if editingCustomFX.frames.isEmpty {
            editingCustomFX.frames = [CustomLightEffect.emptyFrame()]
        }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        editingFrameIndex = safeIdx
        // Toggle off if already painted with the exact same non-zero velocity
        let currentVel = editingCustomFX.frames[safeIdx][keyIndex]
        let newVel: UInt8 = (currentVel == velocity && velocity > 0) ? 0 : velocity
        editingCustomFX.frames[safeIdx][keyIndex] = newVel
        controls[keyIndex].currentLED = newVel
        sendNoteOn(note: controls[keyIndex].number, velocity: newVel)
        SoundEffect.keyTap.play(with: soundPack)
    }

    func addCustomFXFrame() {
        let newFrame = CustomLightEffect.emptyFrame(fill: 0)
        editingCustomFX.frames.append(newFrame)
        editingFrameIndex = editingCustomFX.frames.count - 1
        previewCurrentCustomFrameOnHardware()
        SoundEffect.knobTick.play(with: soundPack)
    }

    func duplicateCustomFXFrame() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        let copy = editingCustomFX.frames[safeIdx]
        editingCustomFX.frames.insert(copy, at: safeIdx + 1)
        editingFrameIndex = safeIdx + 1
        previewCurrentCustomFrameOnHardware()
        SoundEffect.knobTick.play(with: soundPack)
    }

    func deleteCustomFXFrame() {
        guard editingCustomFX.frames.count > 1 else {
            clearCurrentCustomFXFrame()
            return
        }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        editingCustomFX.frames.remove(at: safeIdx)
        editingFrameIndex = max(0, safeIdx - 1)
        previewCurrentCustomFrameOnHardware()
    }

    func clearCurrentCustomFXFrame() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        editingCustomFX.frames[safeIdx] = CustomLightEffect.emptyFrame(fill: 0)
        previewCurrentCustomFrameOnHardware()
    }

    func fillCurrentCustomFXFrame(with vel: UInt8) {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        editingCustomFX.frames[safeIdx] = CustomLightEffect.emptyFrame(fill: vel)
        previewCurrentCustomFrameOnHardware()
    }

    func shiftCurrentFrame(dx: Int, dy: Int) {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        let oldFrame = editingCustomFX.frames[safeIdx]
        var newFrame = CustomLightEffect.emptyFrame(fill: 0)

        for k in 0..<49 {
            let pos = BitchBoyKeyMap.keyPositions[k]
            let srcCol = (pos.col - dx + 11) % 11
            let srcRow = (pos.row - dy + 5) % 5
            if let srcIdx = BitchBoyKeyMap.keyPositions.firstIndex(where: { $0.col == srcCol && $0.row == srcRow }) {
                newFrame[k] = oldFrame[srcIdx]
            }
        }
        editingCustomFX.frames[safeIdx] = newFrame
        previewCurrentCustomFrameOnHardware()
        SoundEffect.knobTick.play(with: soundPack)
    }

    func invertCurrentFrame() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        for k in 0..<49 {
            let v = editingCustomFX.frames[safeIdx][k]
            editingCustomFX.frames[safeIdx][k] = (v == 0) ? customBrushVelocity : 0
        }
        previewCurrentCustomFrameOnHardware()
    }

    func mirrorCurrentFrameHorizontal() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        let oldFrame = editingCustomFX.frames[safeIdx]
        var newFrame = oldFrame
        // Mirror 8x4 & 1x8 (cols 0..7) and 3x3 (cols 8..10)
        for k in 0..<49 {
            let pos = BitchBoyKeyMap.keyPositions[k]
            let mirrorCol = pos.col < 8 ? (7 - pos.col) : (18 - pos.col)
            if let srcIdx = BitchBoyKeyMap.keyPositions.firstIndex(where: { $0.col == mirrorCol && $0.row == pos.row }) {
                newFrame[k] = oldFrame[srcIdx]
            }
        }
        editingCustomFX.frames[safeIdx] = newFrame
        previewCurrentCustomFrameOnHardware()
    }

    func randomizeCurrentFrameSparks() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        let palette: [UInt8] = [0, 0, 5, 14, 24, 38, 52, 68, 84, 98, 112, 127]
        for k in 0..<49 {
            editingCustomFX.frames[safeIdx][k] = palette.randomElement() ?? 0
        }
        previewCurrentCustomFrameOnHardware()
    }

    func applyRainbowToCurrentFrame() {
        guard !editingCustomFX.frames.isEmpty else { return }
        let safeIdx = min(max(0, editingFrameIndex), editingCustomFX.frames.count - 1)
        let rainbow: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127]
        for k in 0..<49 {
            let col = BitchBoyKeyMap.keyPositions[k].col
            editingCustomFX.frames[safeIdx][k] = rainbow[(col + safeIdx) % rainbow.count]
        }
        previewCurrentCustomFrameOnHardware()
    }

    // Evaluates a procedural wave at phase t (0..1+) for key index k (0..<49)
    func evaluateProceduralWave(fx: CustomLightEffect, keyIndex: Int, phase: Double) -> UInt8 {
        let pos = BitchBoyKeyMap.keyPositions[keyIndex]
        let x = Double(pos.col) / 10.0
        let y = Double(pos.row) / 4.0
        let cx = x - 0.45
        let cy = y - 0.5

        let coord: Double
        switch fx.waveDirection {
        case .leftToRight: coord = x
        case .topToBottom: coord = y
        case .diagonal: coord = (x + y) * 0.5
        case .centerOut: coord = sqrt(cx * cx + cy * cy) * 1.4
        case .outerIn: coord = 1.0 - min(1.0, sqrt(cx * cx + cy * cy) * 1.4)
        }

        let arg = (coord * fx.waveFrequency - phase) * 2.0 * .pi
        let norm: Double
        switch fx.waveShape {
        case .sine:
            norm = (sin(arg) + 1.0) * 0.5
        case .sawtooth:
            let f = (coord * fx.waveFrequency - phase)
            norm = f - floor(f)
        case .triangle:
            let f = (coord * fx.waveFrequency - phase)
            let frac = f - floor(f)
            norm = frac < 0.5 ? (frac * 2.0) : (1.0 - (frac - 0.5) * 2.0)
        case .square:
            norm = sin(arg) >= 0 ? 1.0 : 0.0
        case .radial:
            let dist = sqrt(cx * cx + cy * cy)
            norm = (sin((dist * 3.0 * fx.waveFrequency - phase) * 2.0 * .pi) + 1.0) * 0.5
        case .spiral:
            let angle = atan2(cy, cx) / (2.0 * .pi)
            let dist = sqrt(cx * cx + cy * cy)
            norm = (sin((angle * 2.0 + dist * fx.waveFrequency - phase) * 2.0 * .pi) + 1.0) * 0.5
        case .sparkle:
            let hash = sin(Double(keyIndex * 37) + floor(phase * 12.0) * 17.3)
            norm = hash > 0.25 ? ((hash - 0.25) / 0.75) : 0.0
        }

        let lo = Double(min(fx.minVelocity, fx.maxVelocity))
        let hi = Double(max(fx.minVelocity, fx.maxVelocity))
        return UInt8(clamping: Int(round(lo + norm * (hi - lo))))
    }

    func bakeProceduralWaveToFrames() {
        var baked: [[UInt8]] = []
        let frameCount = 12
        for fIdx in 0..<frameCount {
            let phase = Double(fIdx) / Double(frameCount)
            var frame = CustomLightEffect.emptyFrame()
            for k in 0..<49 {
                frame[k] = evaluateProceduralWave(fx: editingCustomFX, keyIndex: k, phase: phase)
            }
            baked.append(frame)
        }
        editingCustomFX.frames = baked
        editingCustomFX.isProcedural = false
        editingFrameIndex = 0
        rightWingTab = "SEQUENCER"
        previewCurrentCustomFrameOnHardware()
        SoundEffect.presetSwitch.play(with: soundPack)
        log("⚡ Baked Procedural Wave into 12 editable keyframes!")
    }

    func playCustomEffect(_ fx: CustomLightEffect) {
        fxTimer?.cancel()
        fxTimer = nil
        currentFXMode = .staticFX
        vuMeterLEDsEnabled = false
        editingCustomFX = fx
        isPlayingCustomFX = true
        activeCustomFXName = fx.name
        customFXStep = 0
        customFXDirection = 1

        let interval = max(0.035, 1.0 / max(1.0, fx.fps * fxSpeed))
        var phase: Double = 0.0

        fxTimer = Timer.publish(every: interval, on: .main, in: .common)
            .autoconnect()
            .sink { [weak self] _ in
                guard let self = self else { return }
                let activeFX = self.editingCustomFX
                if activeFX.isProcedural {
                    phase += 0.06 * self.fxSpeed
                    for k in 0..<49 {
                        let v = self.evaluateProceduralWave(fx: activeFX, keyIndex: k, phase: phase)
                        if self.controls[k].currentLED != v {
                            self.controls[k].currentLED = v
                            self.sendNoteOn(note: self.controls[k].number, velocity: v)
                        }
                    }
                } else {
                    let count = activeFX.frames.count
                    guard count > 0 else { return }
                    let idx = min(max(0, self.customFXStep), count - 1)
                    self.editingFrameIndex = idx
                    let frame = activeFX.frames[idx]
                    for k in 0..<min(49, frame.count) {
                        let v = frame[k]
                        if self.controls[k].currentLED != v {
                            self.controls[k].currentLED = v
                            self.sendNoteOn(note: self.controls[k].number, velocity: v)
                        }
                    }

                    if count > 1 {
                        switch activeFX.loopMode {
                        case .loop:
                            self.customFXStep = (self.customFXStep + 1) % count
                        case .pingPong:
                            let next = self.customFXStep + self.customFXDirection
                            if next >= count {
                                self.customFXDirection = -1
                                self.customFXStep = max(0, count - 2)
                            } else if next < 0 {
                                self.customFXDirection = 1
                                self.customFXStep = min(count - 1, 1)
                            } else {
                                self.customFXStep = next
                            }
                        case .oneShot:
                            if self.customFXStep + 1 < count {
                                self.customFXStep += 1
                            } else {
                                self.stopCustomEffect()
                            }
                        }
                    }
                }
            }
        log("▶️ Playing Custom FX: \(fx.name)")
    }

    func stopCustomEffect() {
        isPlayingCustomFX = false
        activeCustomFXName = ""
        fxTimer?.cancel()
        fxTimer = nil
        restorePresetLEDs()
        log("⏹ Stopped Custom FX (Restored Preset LEDs)")
    }

    func exportCustomFXToClipboard() {
        guard let data = try? JSONEncoder().encode(editingCustomFX),
              let str = String(data: data, encoding: .utf8) else { return }
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(str, forType: .string)
        log("⤓ Exported Custom FX '\(editingCustomFX.name)' to Clipboard JSON")
    }

    func importCustomFXFromClipboard() {
        guard let str = NSPasteboard.general.string(forType: .string),
              let data = str.data(using: .utf8),
              var decoded = try? JSONDecoder().decode(CustomLightEffect.self, from: data) else {
            log("❌ Clipboard does not contain valid Custom FX JSON")
            return
        }
        decoded.id = UUID()
        customEffects.insert(decoded, at: 0)
        editingCustomFX = decoded
        editingFrameIndex = 0
        saveCustomEffectsToDisk()
        previewCurrentCustomFrameOnHardware()
        log("⤒ Imported Custom FX '\(decoded.name)'!")
    }

    func executeAction(for item: ControlItem) {
        switch item.actionType {
        case .passthrough: break
        case .qwerty:
            var totalDelay = 0.0
            for step in item.qwertySteps {
                totalDelay += Double(step.delayMs) / 1000.0
                let delay = totalDelay
                Task { @MainActor in
                    if delay > 0 {
                        try? await Task.sleep(nanoseconds: UInt64(delay * 1_000_000_000))
                    }
                    self.triggerSingleQwerty(key: step.key, cmd: step.cmd, shift: step.shift, alt: step.alt, ctrl: step.ctrl)
                }
            }
        case .osc:
            sendOSC(host: item.oscHost, port: item.oscPort, address: item.oscAddress)
        case .applescript:
            let task = Process()
            task.launchPath = "/usr/bin/osascript"
            task.arguments = ["-e", item.appleScript]
            try? task.run()
        }
    }

    func triggerSingleQwerty(key: String, cmd: Bool, shift: Bool, alt: Bool, ctrl: Bool) {
        let keyMap: [String: Int] = [
            "space": 49, "return": 36, "enter": 76, "escape": 53, "esc": 53,
            "tab": 48, "delete": 51, "up": 126, "down": 125, "left": 123, "right": 124,
            "f1": 122, "f2": 120, "f3": 99, "f4": 118, "f5": 96, "f6": 97,
            "f7": 98, "f8": 100, "f9": 101, "f10": 109, "f11": 103, "f12": 111
        ]
        var mods: [String] = []
        if cmd { mods.append("command down") }
        if shift { mods.append("shift down") }
        if alt { mods.append("option down") }
        if ctrl { mods.append("control down") }
        let modClause = mods.isEmpty ? "" : " using {\(mods.joined(separator: ", "))}"

        let k = key.lowercased().trimmingCharacters(in: .whitespaces)
        let script = (keyMap[k] != nil)
            ? "tell application \"System Events\" to key code \(keyMap[k]!)\(modClause)"
            : "tell application \"System Events\" to keystroke \"\(k)\"\(modClause)"
        let task = Process()
        task.launchPath = "/usr/bin/osascript"
        task.arguments = ["-e", script]
        try? task.run()
        log("⌨️ Fired QWERTY: \(k.uppercased())")
    }

    func sendOSC(host: String, port: UInt16, address: String) {
        guard let nwPort = NWEndpoint.Port(rawValue: port) else { return }
        let conn = NWConnection(host: NWEndpoint.Host(host), port: nwPort, using: .udp)
        conn.start(queue: .global())
        var data = Data(address.utf8)
        let pad = 4 - (data.count % 4)
        data.append(contentsOf: repeatElement(0, count: pad))
        data.append(contentsOf: [0x2C, 0x00, 0x00, 0x00])
        conn.send(content: data, completion: .contentProcessed { _ in conn.cancel() })
        log("📡 OSC UDP -> \(host):\(port) \(address)")
    }

    func selectSlot(_ idx: Int) {
        guard slots.indices.contains(idx) else { return }
        saveCurrentSlot()
        activeSlot = idx
        controls = slots[idx].controls
        globalRules = slots[idx].globalRules
        SoundEffect.presetSwitch.play(with: soundPack)
        log("Loaded \(slots[idx].name)")
        runRainbowWaveRefresh()
    }

    var globalRules: String {
        get { slots.indices.contains(activeSlot) ? slots[activeSlot].globalRules : "" }
        set {
            if slots.indices.contains(activeSlot) {
                slots[activeSlot].globalRules = newValue
            }
        }
    }

    func saveCurrentSlot() {
        guard slots.indices.contains(activeSlot) else { return }
        slots[activeSlot].controls = controls
        slots[activeSlot].globalRules = globalRules
        if let encoded = try? JSONEncoder().encode(slots) {
            UserDefaults.standard.set(encoded, forKey: storageKey)
        }
    }

    private func loadFromDisk() {
        let default8 = Self.buildDefault8MemorySlots()
        if let data = UserDefaults.standard.data(forKey: storageKey),
           let decoded = try? JSONDecoder().decode([PresetSlot].self, from: data),
           (decoded.count == 4 || decoded.count == 8) {
            if let firstSlot = decoded.first, firstSlot.controls.count == 69,
               firstSlot.controls[0].number == 64,
               firstSlot.controls[4].number == 96,
               firstSlot.controls[32].number == 48,
               firstSlot.controls[36].number == 80,
               firstSlot.controls[40].number == 44 {
                if decoded.count == 4 {
                    var migrated = decoded
                    migrated.append(contentsOf: default8[4..<8])
                    self.slots = migrated
                } else {
                    self.slots = decoded
                }
                self.controls = self.slots[activeSlot].controls
                return
            }
        }

        self.slots = default8
        self.controls = slots[0].controls
        saveCurrentSlot()
    }

    func setZoneLEDs(range: Range<Int>, velocity: UInt8) {
        for i in range where controls[i].hasLED {
            controls[i].idleVelocity = velocity
            controls[i].currentLED = velocity
            sendNoteOn(note: controls[i].number, velocity: velocity)
        }
        saveCurrentSlot()
        SoundEffect.keyTap.play(with: soundPack)
        log("⚡️ Applied Velocity \(velocity) to Range [\(range.lowerBound)..\(range.upperBound - 1)]")
    }

    func log(_ msg: String) {
        let formatter = DateFormatter()
        formatter.dateFormat = "HH:mm:ss"
        let ts = formatter.string(from: Date())
        midiLogs.insert("[\(ts)] \(msg)", at: 0)
        if midiLogs.count > 40 { midiLogs.removeLast() }
    }
}

// ============================================================================
// 3. HARDWARE VISUAL COMPONENTS
// ============================================================================

struct ScrewRivetView: View {
    var size: CGFloat = 11

    var body: some View {
        ZStack {
            Circle()
                .fill(
                    RadialGradient(
                        colors: [Color(white: 0.94), Color(white: 0.65), Color(white: 0.35)],
                        center: .topLeading,
                        startRadius: 1,
                        endRadius: size / 1.7
                    )
                )
                .frame(width: size, height: size)
                .shadow(color: .black.opacity(0.7), radius: 1, x: 0.5, y: 0.5)

            // Cross slot
            Rectangle()
                .fill(Color(white: 0.18))
                .frame(width: size * 0.65, height: 1.2)
            Rectangle()
                .fill(Color(white: 0.18))
                .frame(width: 1.2, height: size * 0.65)
        }
    }
}

struct BitchBoyOfficialMascotView: View {
    var auraColor: Color = .cyan
    var onTap: (() -> Void)? = nil

    @State private var isHovered = false
    @State private var isPressed = false

    var mascotNSImage: NSImage? {
        if let img = NSImage(named: "BitchBoyMascot") { return img }
        if let img = NSImage(contentsOfFile: "/Users/taylorfossen/Documents/BB/BB/Assets.xcassets/BitchBoyMascot.imageset/bitchboy.png") { return img }
        if let img = NSImage(contentsOfFile: "/Users/taylorfossen/Downloads/DRAFT BITCHBOY_files/bitchboy.png") { return img }
        return nil
    }

    var body: some View {
        Button(action: {
            isPressed = true
            onTap?()
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.16) {
                isPressed = false
            }
        }) {
            Group {
                if let nsImg = mascotNSImage {
                    Image(nsImage: nsImg)
                        .interpolation(.none) // Keep crisp pixel art edge
                        .resizable()
                        .scaledToFit()
                        .frame(width: 160, height: 160) // 2x bigger than the old 80x80 fit!
                        .blendMode(.screen) // Melts any dark background into the PCB
                        .shadow(color: auraColor.opacity(isHovered ? 1.0 : 0.85), radius: isHovered ? 10 : 8)
                        .shadow(color: auraColor.opacity(isHovered ? 0.75 : 0.45), radius: isHovered ? 22 : 18)
                } else {
                    Image(systemName: "gamecontroller.fill")
                        .font(.system(size: 64))
                        .foregroundColor(auraColor)
                        .shadow(color: auraColor.opacity(0.85), radius: 8)
                }
            }
            .frame(width: 186, height: 96)
            .scaleEffect(isPressed ? 0.94 : (isHovered ? 1.06 : 1.0))
            .animation(.spring(response: 0.25, dampingFraction: 0.6), value: isPressed)
            .animation(.spring(response: 0.3, dampingFraction: 0.7), value: isHovered)
            .onHover { h in isHovered = h }
        }
        .buttonStyle(.plain)
    }
}

struct BitchBoyGlowingLogoView: View {
    var auraColor: Color = .cyan
    var onTap: (() -> Void)? = nil

    @State private var isHovered = false
    @State private var isPressed = false

    var logoNSImage: NSImage? {
        if let img = NSImage(named: "BitchBoyWordLogo") { return img }
        if let img = NSImage(contentsOfFile: "/Users/taylorfossen/Documents/BB/BB/Assets.xcassets/BitchBoyWordLogo.imageset/bitchboy_logo.png") { return img }
        if let img = NSImage(contentsOfFile: "/Users/taylorfossen/Downloads/Bitchboy23.png") { return img }
        return nil
    }

    var body: some View {
        Button(action: {
            isPressed = true
            onTap?()
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.16) {
                isPressed = false
            }
        }) {
            Group {
                if let nsImg = logoNSImage {
                    Image(nsImage: nsImg)
                        .interpolation(.high)
                        .resizable()
                        .scaledToFit()
                        .blendMode(.screen)
                        .shadow(color: auraColor.opacity(isHovered ? 1.0 : 0.85), radius: isHovered ? 10 : 8)
                        .shadow(color: auraColor.opacity(isHovered ? 0.75 : 0.45), radius: isHovered ? 22 : 18)
                } else {
                    Text("BiTCHBOY")
                        .font(.system(size: 24, weight: .heavy, design: .monospaced))
                        .foregroundColor(.white)
                        .shadow(color: auraColor.opacity(0.85), radius: 8)
                }
            }
            .scaleEffect(isPressed ? 0.94 : (isHovered ? 1.06 : 1.0))
            .animation(.spring(response: 0.25, dampingFraction: 0.6), value: isPressed)
            .animation(.spring(response: 0.3, dampingFraction: 0.7), value: isHovered)
            .onHover { h in isHovered = h }
        }
        .buttonStyle(.plain)
    }
}

struct HardwareKeyCap: View {
    let item: ControlItem
    let isSelected: Bool
    let color: Color
    let sectionBorder: Color
    let onTap: () -> Void

    var isLit: Bool { item.currentLED > 0 }

    var body: some View {
        Button(action: onTap) {
            ZStack {
                // Base background
                RoundedRectangle(cornerRadius: 6)
                    .fill(
                        LinearGradient(
                            colors: isLit
                                ? [color.opacity(0.85), color.opacity(0.40)]
                                : [Color(red: 0.18, green: 0.08, blue: 0.08), Color(red: 0.12, green: 0.05, blue: 0.05)],
                            startPoint: .topLeading,
                            endPoint: .bottomTrailing
                        )
                    )

                // Key face bezel
                RoundedRectangle(cornerRadius: 6)
                    .stroke(
                        isSelected
                            ? Color(red: 0.22, green: 0.96, blue: 0.76)
                            : sectionBorder.opacity(0.85),
                        lineWidth: isSelected ? 2.0 : 1.0
                    )
                    .shadow(color: isSelected ? Color(red: 0.22, green: 0.96, blue: 0.76).opacity(0.8) : (isLit ? color.opacity(0.6) : .clear), radius: isLit ? 6 : 2)

                // Labels & Center Action Cross
                VStack(spacing: 0) {
                    Text(item.customName.isEmpty ? item.label : item.customName)
                        .font(.system(size: 8, weight: .bold, design: .monospaced))
                        .foregroundColor(isLit ? .white : Color(white: 0.7))
                        .padding(.top, 2)
                        .lineLimit(1)

                    Spacer(minLength: 0)

                    // Inset cross (+) or action badge
                    ZStack {
                        Circle()
                            .stroke(Color.white.opacity(0.2), lineWidth: 1)
                            .frame(width: 14, height: 14)

                        Image(systemName: actionIconName(item.actionType))
                            .font(.system(size: 7.5, weight: .semibold))
                            .foregroundColor(isLit ? .white : Color(white: 0.65))
                    }

                    Spacer(minLength: 0)

                    Text("N\(item.number)")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(isLit ? .white.opacity(0.9) : Color(white: 0.55))
                        .padding(.bottom, 2)
                }
            }
            .frame(width: 44, height: 40)
            .scaleEffect(item.isPressed ? 0.92 : 1.0)
            .animation(.spring(response: 0.15, dampingFraction: 0.6), value: item.isPressed)
        }
        .buttonStyle(.plain)
    }

    private func actionIconName(_ type: ActionType) -> String {
        switch type {
        case .passthrough: return "plus"
        case .qwerty: return "command"
        case .osc: return "antenna.radiowaves.left.and.right"
        case .applescript: return "applescript"
        }
    }
}

struct HardwareVerticalFader: View {
    let item: ControlItem
    let isSelected: Bool
    let onSelect: () -> Void
    let onValueChange: (UInt8) -> Void

    var body: some View {
        VStack(spacing: 4) {
            Text(item.label)
                .font(.system(size: 8, weight: .bold, design: .monospaced))
                .foregroundColor(isSelected ? Color(red: 0.38, green: 0.94, blue: 0.78) : Color(white: 0.65))

            // Vertical Track & Cap
            GeometryReader { geo in
                let trackHeight = geo.size.height - 20
                let normalizedVal = CGFloat(item.liveValue) / 127.0
                let capY = (1.0 - normalizedVal) * trackHeight

                ZStack(alignment: .top) {
                    // Recessed slot
                    RoundedRectangle(cornerRadius: 3)
                        .fill(Color(red: 0.02, green: 0.04, blue: 0.04))
                        .frame(width: 6, height: geo.size.height)
                        .overlay(RoundedRectangle(cornerRadius: 3).stroke(Color.white.opacity(0.14), lineWidth: 1))

                    // Coral Rectangular Fader Cap
                    ZStack {
                        RoundedRectangle(cornerRadius: 3)
                            .fill(
                                LinearGradient(
                                    colors: [
                                        Color(red: 0.98, green: 0.52, blue: 0.44),
                                        Color(red: 0.85, green: 0.32, blue: 0.28)
                                    ],
                                    startPoint: .top,
                                    endPoint: .bottom
                                )
                            )
                            .frame(width: 22, height: 18)
                            .overlay(RoundedRectangle(cornerRadius: 3).stroke(Color.white.opacity(0.35), lineWidth: 1))
                            .shadow(color: .black.opacity(0.8), radius: 3, x: 1, y: 1)

                        // Center white notch line
                        Rectangle()
                            .fill(Color.white.opacity(0.85))
                            .frame(width: 14, height: 1.5)
                    }
                    .offset(y: capY)
                }
                .frame(maxWidth: .infinity, alignment: .center)
                .contentShape(Rectangle())
                .gesture(
                    DragGesture(minimumDistance: 0)
                        .onChanged { gesture in
                            onSelect()
                            let clampedY = min(max(0, gesture.location.y - 9), trackHeight)
                            let inverted = 1.0 - (clampedY / trackHeight)
                            let newVal = UInt8(inverted * 127.0)
                            onValueChange(newVal)
                        }
                )
            }
            .frame(width: 42, height: 110)

            Text("CC\(item.number)")
                .font(.system(size: 7, weight: .bold, design: .monospaced))
                .foregroundColor(Color(red: 0.94, green: 0.76, blue: 0.38))
        }
        .padding(.vertical, 4)
        .padding(.horizontal, 2)
        .background(isSelected ? Color.white.opacity(0.06) : Color.clear)
        .clipShape(RoundedRectangle(cornerRadius: 5))
        .onTapGesture { onSelect() }
    }
}

struct HardwareHorizontalFader: View {
    let item: ControlItem
    let isSelected: Bool
    let onSelect: () -> Void
    let onValueChange: (UInt8) -> Void

    var body: some View {
        HStack(spacing: 8) {
            Text(item.label)
                .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                .foregroundColor(isSelected ? Color(red: 0.38, green: 0.94, blue: 0.78) : Color(white: 0.65))
                .frame(width: 28, alignment: .leading)

            GeometryReader { geo in
                let trackWidth = geo.size.width - 20
                let normalizedVal = CGFloat(item.liveValue) / 127.0
                let capX = normalizedVal * trackWidth

                ZStack(alignment: .leading) {
                    // Recessed track
                    RoundedRectangle(cornerRadius: 3)
                        .fill(Color(red: 0.02, green: 0.04, blue: 0.04))
                        .frame(height: 6)
                        .overlay(RoundedRectangle(cornerRadius: 3).stroke(Color.white.opacity(0.12), lineWidth: 1))

                    // Coral Rectangular Fader Cap
                    ZStack {
                        RoundedRectangle(cornerRadius: 3)
                            .fill(
                                LinearGradient(
                                    colors: [
                                        Color(red: 0.98, green: 0.52, blue: 0.44),
                                        Color(red: 0.85, green: 0.32, blue: 0.28)
                                    ],
                                    startPoint: .top,
                                    endPoint: .bottom
                                )
                            )
                            .frame(width: 18, height: 22)
                            .overlay(RoundedRectangle(cornerRadius: 3).stroke(Color.white.opacity(0.35), lineWidth: 1))
                            .shadow(color: .black.opacity(0.8), radius: 3, x: 1, y: 1)

                        // Center black notch line
                        Rectangle()
                            .fill(Color.black.opacity(0.75))
                            .frame(width: 1.5, height: 16)
                    }
                    .offset(x: capX)
                }
                .frame(maxHeight: .infinity, alignment: .center)
                .contentShape(Rectangle())
                .gesture(
                    DragGesture(minimumDistance: 0)
                        .onChanged { gesture in
                            onSelect()
                            let clampedX = min(max(0, gesture.location.x - 9), trackWidth)
                            let newVal = UInt8((clampedX / trackWidth) * 127.0)
                            onValueChange(newVal)
                        }
                )
            }
            .frame(height: 24)

            Text("CC\(item.number)")
                .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                .foregroundColor(Color(red: 0.94, green: 0.76, blue: 0.38))
                .frame(width: 34, alignment: .trailing)
        }
        .padding(.vertical, 2)
        .padding(.horizontal, 4)
        .background(isSelected ? Color.white.opacity(0.06) : Color.clear)
        .clipShape(RoundedRectangle(cornerRadius: 5))
        .onTapGesture { onSelect() }
    }
}

struct HardwareKnob: View {
    let item: ControlItem
    let isSelected: Bool
    let onSelect: () -> Void
    let onValueChange: (UInt8) -> Void

    var body: some View {
        VStack(spacing: 3) {
            ZStack {
                // Dial body with metallic rim
                Circle()
                    .fill(
                        RadialGradient(
                            colors: [Color(white: 0.22), Color(white: 0.10)],
                            center: .center,
                            startRadius: 2,
                            endRadius: 16
                        )
                    )
                    .frame(width: 32, height: 32)
                    .overlay(
                        Circle().stroke(isSelected ? Color(red: 0.22, green: 0.96, blue: 0.76) : Color.white.opacity(0.2), lineWidth: isSelected ? 1.8 : 1.0)
                    )
                    .shadow(color: isSelected ? Color(red: 0.22, green: 0.96, blue: 0.76).opacity(0.5) : .black.opacity(0.6), radius: 3)

                // Indicator line
                Rectangle()
                    .fill(Color(white: 0.98))
                    .frame(width: 2, height: 10)
                    .offset(y: -7)
                    .rotationEffect(.degrees(Double(item.liveValue) / 127.0 * 270.0 - 135.0))
            }
            .gesture(
                DragGesture(minimumDistance: 0)
                    .onChanged { gesture in
                        onSelect()
                        let delta = -gesture.translation.height / 100.0
                        let current = Double(item.liveValue) / 127.0
                        let clamped = min(max(0.0, current + delta * 0.05), 1.0)
                        onValueChange(UInt8(clamped * 127.0))
                    }
            )

            HStack(spacing: 3) {
                Text(item.label)
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .foregroundColor(isSelected ? Color(red: 0.22, green: 0.96, blue: 0.76) : Color(red: 0.45, green: 0.88, blue: 0.75))
                Text("CC\(item.number)")
                    .font(.system(size: 7, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(red: 0.96, green: 0.78, blue: 0.35))
            }
        }
        .frame(width: 58)
        .padding(2)
        .onTapGesture { onSelect() }
    }
}

// ============================================================================
// 4. MAIN CONTENT VIEW // TWO SYMMETRIC UNITED COLUMNS
// ============================================================================

struct WindowLevelAndSizeAccessor: NSViewRepresentable {
    var isAlwaysOnTop: Bool
    var baseSize: NSSize
    var minScale: CGFloat

    func makeNSView(context: Context) -> NSView {
        let view = NSView()
        DispatchQueue.main.async {
            configureWindow(view.window)
        }
        return view
    }

    func updateNSView(_ nsView: NSView, context: Context) {
        DispatchQueue.main.async {
            configureWindow(nsView.window)
        }
    }

    private func configureWindow(_ window: NSWindow?) {
        guard let window = window else { return }
        window.level = isAlwaysOnTop ? .floating : .normal
        let minContentW = baseSize.width * minScale
        let minContentH = baseSize.height * minScale
        window.contentMinSize = NSSize(width: minContentW, height: minContentH)
        window.contentAspectRatio = baseSize

        // If the window's current aspect ratio is out of sync and would cause letterboxing or clipping, snap height to match width aspect ratio
        if let contentView = window.contentView {
            let cw = max(minContentW, contentView.bounds.width)
            let expectedH = round(cw * (baseSize.height / baseSize.width))
            if abs(contentView.bounds.height - expectedH) > 6 {
                window.setContentSize(NSSize(width: cw, height: expectedH))
            }
        }
    }
}

struct ContentView: View {
    @ObservedObject var engine: BitchBoyEngine
    @State private var isHoveringLoveBadge: Bool = false
    @State private var isHoveringTitleLogo: Bool = false
    @State private var activeMemoryBank: Int = 0 // 0 = BANK 1 (A-D), 1 = BANK 2 (E-H)
    @State private var showSlotCustomizer: Bool = false

    var selectedControl: ControlItem {
        if engine.selectedIndex < engine.controls.count {
            return engine.controls[engine.selectedIndex]
        }
        return engine.controls[0]
    }

    private func ledColor(_ vel: UInt8) -> Color {
        if vel == 0 { return Color(white: 0.14) }

        switch engine.ledPalette {
        case .multiVelocity:
            if vel < 16 { return Color(red: 1.0, green: 0.23, blue: 0.19) }
            if vel < 32 { return Color(red: 1.0, green: 0.58, blue: 0.0) }
            if vel < 48 { return Color(red: 1.0, green: 0.84, blue: 0.04) }
            if vel < 64 { return Color(red: 0.19, green: 0.82, blue: 0.35) }
            if vel < 85 { return Color(red: 0.0, green: 0.83, blue: 1.0) }
            if vel < 108 { return Color(red: 0.69, green: 0.32, blue: 0.87) }
            return Color(red: 0.88, green: 0.98, blue: 1.0)
        case .cyberCyan:
            return Color(red: 0.22, green: 0.96, blue: 0.85)
        case .matrixGreen:
            return Color(red: 0.15, green: 0.95, blue: 0.35)
        case .solarAmber:
            return Color(red: 1.0, green: 0.72, blue: 0.10)
        case .synthMagenta:
            return Color(red: 0.95, green: 0.25, blue: 0.85)
        case .arcticWhite:
            return Color(white: 0.96)
        }
    }

    var body: some View {
        GeometryReader { windowGeo in
            let baseWidth: CGFloat = BitchBoyEngine.consoleBaseWidth
            let baseHeight: CGFloat = BitchBoyEngine.consoleBaseHeight
            // ALWAYS fit inside windowGeo.size so the window NEVER clips any controls at any size!
            let fitW = max(0.15, windowGeo.size.width / baseWidth)
            let fitH = max(0.15, windowGeo.size.height / baseHeight)
            let fitScale = min(2.4, min(fitW, fitH))

            ZStack {
                // Lock NSWindow aspect ratio & minimum size (15% of controller) so dragging the window scales the whole controller proportionally with zero clipping
                WindowLevelAndSizeAccessor(
                    isAlwaysOnTop: engine.isWindowAlwaysOnTop,
                    baseSize: NSSize(width: baseWidth, height: baseHeight),
                    minScale: 0.15
                )
                .frame(width: 0, height: 0)

                // Dark Cyber Background
                Color(red: 0.02, green: 0.05, blue: 0.04)
                    .ignoresSafeArea()

                VStack(spacing: 12) {
                    topNavigationBar
                    mainHardwareFaceplate
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 14)
                .frame(width: baseWidth, height: baseHeight, alignment: .top)
                .scaleEffect(fitScale, anchor: .center)
                .frame(width: windowGeo.size.width, height: windowGeo.size.height, alignment: .center)
            }
            .onAppear {
                engine.lastComputedFitScale = fitScale
                engine.applyWindowAlwaysOnTop()
            }
            .onChange(of: windowGeo.size) { _ in
                engine.lastComputedFitScale = fitScale
            }
        }
        .preferredColorScheme(.dark)
    }

    // ------------------------------------------------------------------------
    // TOP NAVIGATION & SYSTEM BAR
    // ------------------------------------------------------------------------
    private var topNavigationBar: some View {
        HStack(spacing: 12) {
            // Logo & OS Title (Neon Glowing BITCHMANAGER Header)
            HStack(spacing: 10) {
                Image("BitchBoyBadgeIcon")
                    .resizable()
                    .interpolation(.high)
                    .scaledToFit()
                    .frame(width: 36, height: 36)
                    .shadow(color: engine.currentTheme.accentPrimary.opacity(isHoveringTitleLogo ? 0.95 : 0.65), radius: isHoveringTitleLogo ? 9 : 5, x: 0, y: 0)
                    .shadow(color: engine.currentTheme.accentPrimary.opacity(isHoveringTitleLogo ? 0.65 : 0.35), radius: isHoveringTitleLogo ? 16 : 10, x: 0, y: 0)

                VStack(alignment: .leading, spacing: 1) {
                    HStack(spacing: 6) {
                        Text("BITCHMANAGER")
                            .font(.system(size: 15, weight: .heavy, design: .monospaced))
                            .foregroundStyle(
                                LinearGradient(
                                    colors: [
                                        .white,
                                        isHoveringTitleLogo ? .white : engine.currentTheme.accentPrimary.opacity(0.96)
                                    ],
                                    startPoint: .top,
                                    endPoint: .bottom
                                )
                            )
                            .tracking(1.1)
                            .shadow(color: engine.currentTheme.accentPrimary.opacity(0.95), radius: isHoveringTitleLogo ? 6 : 4, x: 0, y: 0)
                            .shadow(color: engine.currentTheme.accentPrimary.opacity(isHoveringTitleLogo ? 0.90 : 0.68), radius: isHoveringTitleLogo ? 12 : 9, x: 0, y: 0)
                            .shadow(color: engine.currentTheme.accentPrimary.opacity(isHoveringTitleLogo ? 0.65 : 0.42), radius: isHoveringTitleLogo ? 22 : 16, x: 0, y: 0)
                            .scaleEffect(isHoveringTitleLogo ? 1.03 : 1.0)
                            .animation(.easeInOut(duration: 0.2), value: isHoveringTitleLogo)

                        Text(engine.isLocked ? "69-MAP LOCKED" : "69-MAP UNLOCKED")
                            .font(.system(size: 8, weight: .bold, design: .monospaced))
                            .padding(.horizontal, 6)
                            .padding(.vertical, 2)
                            .background(engine.isLocked ? engine.currentTheme.accentPrimary : Color.yellow)
                            .foregroundColor(.black)
                            .clipShape(Capsule())
                    }

                    Text("BLOCK SYSTEM HARDWARE // LED & ACTION ENGINE")
                        .font(.system(size: 8, weight: .semibold, design: .monospaced))
                        .foregroundColor(engine.currentTheme.headerText)
                        .tracking(0.6)
                        .shadow(color: engine.currentTheme.accentPrimary.opacity(0.35), radius: 4, x: 0, y: 0)
                }
            }
            .onHover { hovering in
                isHoveringTitleLogo = hovering
            }

            Spacer(minLength: 8)

            // Status Pill (Online / Offline)
            HStack(spacing: 8) {
                Circle()
                    .fill(engine.isConnected ? Color(red: 0.2, green: 0.95, blue: 0.45) : Color(red: 0.98, green: 0.25, blue: 0.25))
                    .frame(width: 8, height: 8)
                    .shadow(color: engine.isConnected ? Color.green : Color.red, radius: 4)

                VStack(alignment: .leading, spacing: 1) {
                    Text(engine.isConnected ? "ONLINE // CONNECTED" : "OFFLINE // DISCONNECTED")
                        .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                        .foregroundColor(engine.isConnected ? Color(red: 0.2, green: 0.95, blue: 0.45) : Color(red: 0.98, green: 0.35, blue: 0.35))

                    Text(engine.isConnected ? "\(engine.deviceName) // Ready" : "Unplugged // Waiting for BitchBoy...")
                        .font(.system(size: 8.5, weight: .medium, design: .monospaced))
                        .foregroundColor(Color(white: 0.65))
                }
            }
            .padding(.horizontal, 14)
            .padding(.vertical, 7)
            .background(Color.black.opacity(0.5))
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(engine.isConnected ? Color(red: 0.2, green: 0.95, blue: 0.45).opacity(0.6) : Color(red: 0.98, green: 0.35, blue: 0.35).opacity(0.6), lineWidth: 1)
            )
            .clipShape(RoundedRectangle(cornerRadius: 12))

            Spacer(minLength: 8)

            // Right Customization & Quick Actions + Made with Love
            HStack(spacing: 8) {
                // Made with Love Signature (Glows brightly when the mouse hovers over it!)
                HStack(spacing: 4) {
                    Text("❤️ Made with love - Taylor Fossen")
                        .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                        .foregroundColor(isHoveringLoveBadge ? Color.white : Color.pink.opacity(0.92))
                }
                .padding(.horizontal, 10)
                .padding(.vertical, 6)
                .background(
                    Capsule()
                        .fill(
                            isHoveringLoveBadge
                                ? Color.pink.opacity(0.42)
                                : Color.black.opacity(0.48)
                        )
                )
                .overlay(
                    Capsule()
                        .stroke(
                            isHoveringLoveBadge ? Color.pink : Color.pink.opacity(0.45),
                            lineWidth: isHoveringLoveBadge ? 1.6 : 1.0
                        )
                )
                .shadow(
                    color: isHoveringLoveBadge ? Color.pink.opacity(0.95) : Color.clear,
                    radius: isHoveringLoveBadge ? 10 : 0
                )
                .shadow(
                    color: isHoveringLoveBadge ? Color(red: 1.0, green: 0.35, blue: 0.75).opacity(0.75) : Color.clear,
                    radius: isHoveringLoveBadge ? 18 : 0
                )
                .scaleEffect(isHoveringLoveBadge ? 1.05 : 1.0)
                .animation(.easeInOut(duration: 0.18), value: isHoveringLoveBadge)
                .onHover { hovering in
                    isHoveringLoveBadge = hovering
                }

                // ALWAYS ON TOP (TOP-MOST WINDOW PIN BUTTON)
                Button(action: {
                    engine.toggleWindowAlwaysOnTop()
                }) {
                    HStack(spacing: 4) {
                        Image(systemName: engine.isWindowAlwaysOnTop ? "pin.fill" : "pin")
                            .font(.system(size: 8.5, weight: .bold))
                            .rotationEffect(.degrees(engine.isWindowAlwaysOnTop ? 0 : 45))
                        Text(engine.isWindowAlwaysOnTop ? "PINNED TOP" : "TOP MOST")
                            .font(.system(size: 8, weight: .heavy, design: .monospaced))
                    }
                    .padding(.horizontal, 8)
                    .padding(.vertical, 6)
                    .background(engine.isWindowAlwaysOnTop ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                    .foregroundColor(engine.isWindowAlwaysOnTop ? .black : engine.currentTheme.accentPrimary)
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                    .overlay(
                        RoundedRectangle(cornerRadius: 8)
                            .stroke(engine.isWindowAlwaysOnTop ? Color.white.opacity(0.6) : Color.white.opacity(0.18), lineWidth: 1)
                    )
                    .shadow(color: engine.isWindowAlwaysOnTop ? engine.currentTheme.accentPrimary.opacity(0.6) : .clear, radius: 6)
                }
                .buttonStyle(.plain)

                // UI SCALE / ZOOM CONTROLLER + APP ICON MENU
                HStack(spacing: 3) {
                    Button(action: {
                        engine.zoomOutUI()
                    }) {
                        Image(systemName: "minus.magnifyingglass")
                            .font(.system(size: 9, weight: .bold))
                            .padding(.horizontal, 6)
                            .padding(.vertical, 6)
                            .background(Color.white.opacity(0.1))
                            .foregroundColor(.white)
                            .clipShape(RoundedRectangle(cornerRadius: 6))
                    }
                    .buttonStyle(.plain)

                    Menu {
                        Button(action: {
                            engine.enableAutoFitWindow()
                        }) {
                            HStack {
                                Image(systemName: "arrow.up.left.and.arrow.down.right")
                                Text("AUTO-FIT TO WINDOW")
                                if engine.autoFitWindow {
                                    Spacer()
                                    Image(systemName: "checkmark")
                                }
                            }
                        }
                        Divider()
                        ForEach([15, 25, 35, 50, 65, 75, 85, 100, 115, 125, 140, 160, 180, 200], id: \.self) { pct in
                            Button(action: {
                                engine.setManualZoom(CGFloat(pct) / 100.0)
                            }) {
                                HStack {
                                    Text("ZOOM \(pct)%")
                                    if !engine.autoFitWindow && Int(round(engine.uiScale * 100)) == pct {
                                        Spacer()
                                        Image(systemName: "checkmark")
                                    }
                                }
                            }
                        }
                    } label: {
                        let displayPct = Int(round(engine.lastComputedFitScale * 100))
                        Text("SCALE \(displayPct)%")
                            .font(.system(size: 8, weight: .heavy, design: .monospaced))
                            .padding(.horizontal, 6)
                            .padding(.vertical, 6)
                            .background(engine.autoFitWindow ? engine.currentTheme.accentPrimary.opacity(0.22) : Color.white.opacity(0.1))
                            .foregroundColor(engine.currentTheme.accentPrimary)
                            .clipShape(RoundedRectangle(cornerRadius: 6))
                    }
                    .menuStyle(.borderlessButton)

                    Button(action: {
                        engine.zoomInUI()
                    }) {
                        Image(systemName: "plus.magnifyingglass")
                            .font(.system(size: 9, weight: .bold))
                            .padding(.horizontal, 6)
                            .padding(.vertical, 6)
                            .background(Color.white.opacity(0.1))
                            .foregroundColor(.white)
                            .clipShape(RoundedRectangle(cornerRadius: 6))
                    }
                    .buttonStyle(.plain)
                }
                .padding(2)
                .background(Color.black.opacity(0.45))
                .clipShape(RoundedRectangle(cornerRadius: 8))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.18), lineWidth: 1))

                // Preset Slot Menu
                Menu {
                    ForEach(0..<engine.slots.count, id: \.self) { idx in
                        Button(action: {
                            engine.selectSlot(idx)
                            activeMemoryBank = idx / 4
                        }) {
                            HStack {
                                Text(engine.slots[idx].name)
                                if engine.activeSlot == idx {
                                    Spacer()
                                    Image(systemName: "checkmark")
                                }
                            }
                        }
                    }
                    Divider()
                    Button("💾 Save Slot to Memory") {
                        engine.saveCurrentSlot()
                        engine.log("💾 Presets saved to internal memory.")
                    }
                    Button("📋 Clone to Next Slot") {
                        engine.cloneCurrentSlot()
                    }
                    Button("⤓ Export Slot JSON") {
                        _ = engine.exportPresetToJSON()
                    }
                    Button("⤒ Import Slot JSON") {
                        engine.importPresetFromClipboard()
                    }
                    Divider()
                    Button("🚨 Panic Blackout") {
                        engine.blackoutPanic()
                    }
                } label: {
                    let slotLetters = ["SLOT A", "SLOT B", "SLOT C", "SLOT D", "SLOT E", "SLOT F", "SLOT G", "SLOT H"]
                    HStack(spacing: 4) {
                        Image(systemName: engine.slots.indices.contains(engine.activeSlot) ? engine.slots[engine.activeSlot].iconName : "folder.fill")
                        Text(slotLetters[engine.activeSlot % slotLetters.count])
                            .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                    }
                    .padding(.horizontal, 9)
                    .padding(.vertical, 7)
                    .background(Color.white.opacity(0.08))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.18), lineWidth: 1))
                }

                // Theme Picker Menu
                Menu {
                    ForEach(ChassisTheme.allCases) { th in
                        Button(th.rawValue) {
                            engine.currentTheme = th
                            SoundEffect.presetSwitch.play(with: engine.soundPack)
                        }
                    }
                } label: {
                    HStack(spacing: 4) {
                        Text("🎨")
                        Text(engine.currentTheme.rawValue)
                            .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background(Color.white.opacity(0.08))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.18), lineWidth: 1))
                }

                // Sound Pack Menu
                Menu {
                    ForEach(SoundPack.allCases) { pack in
                        Button(pack.rawValue) {
                            engine.soundPack = pack
                            SoundEffect.presetSwitch.play(with: pack)
                        }
                    }
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: engine.soundPack == .mute ? "speaker.slash.fill" : "speaker.wave.2.fill")
                        Text(engine.soundPack.rawValue)
                            .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background(Color.white.opacity(0.08))
                    .foregroundColor(Color(white: 0.85))
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.18), lineWidth: 1))
                }

                // Rainbow Refresh
                Button(action: {
                    engine.runRainbowWaveRefresh()
                }) {
                    HStack(spacing: 4) {
                        Text("✨🌈")
                        Text("RAINBOW REFRESH")
                            .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background(
                        LinearGradient(
                            colors: [Color(red: 0.45, green: 0.25, blue: 0.75), Color(red: 0.25, green: 0.65, blue: 0.75)],
                            startPoint: .leading,
                            endPoint: .trailing
                        )
                    )
                    .foregroundColor(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                }
                .buttonStyle(.plain)
                .disabled(engine.isRainbowRunning)

                // FX Light Show Menu
                Menu {
                    ForEach(LEDFXMode.allCases) { mode in
                        Button(action: {
                            engine.applyFXMode(mode)
                        }) {
                            HStack {
                                Image(systemName: mode.icon)
                                Text(mode.rawValue)
                                if engine.currentFXMode == mode {
                                    Spacer()
                                    Image(systemName: "checkmark")
                                }
                            }
                        }
                    }
                    Divider()
                    Text("★ USER CUSTOM LIGHT SHOWS")
                    ForEach(engine.customEffects) { cfx in
                        Button(action: {
                            engine.playCustomEffect(cfx)
                        }) {
                            HStack {
                                Image(systemName: cfx.isProcedural ? "waveform.path.ecg" : "film.stack")
                                Text("★ \(cfx.name)")
                                if engine.isPlayingCustomFX && engine.activeCustomFXName == cfx.name {
                                    Spacer()
                                    Image(systemName: "checkmark")
                                }
                            }
                        }
                    }
                    Divider()
                    Button("⏹ STOP FX (RESTORE PRESET)") {
                        engine.stopCustomEffect()
                        engine.applyFXMode(.staticFX)
                    }
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: engine.isPlayingCustomFX ? "wand.and.stars" : (engine.currentFXMode == .staticFX ? "sparkles" : engine.currentFXMode.icon))
                        Text(engine.isPlayingCustomFX ? "CUSTOM: \(engine.activeCustomFXName)" : "FX: \(engine.currentFXMode.rawValue)")
                            .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                        Image(systemName: "chevron.down")
                            .font(.system(size: 7))
                    }
                    .padding(.horizontal, 10)
                    .padding(.vertical, 7)
                    .background((engine.currentFXMode == .staticFX && !engine.isPlayingCustomFX) ? Color(red: 0.08, green: 0.25, blue: 0.24) : Color(red: 0.92, green: 0.45, blue: 0.15).opacity(0.88))
                    .foregroundColor((engine.currentFXMode == .staticFX && !engine.isPlayingCustomFX) ? Color(red: 0.22, green: 0.96, blue: 0.76) : .white)
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color(red: 0.22, green: 0.96, blue: 0.76).opacity(0.6), lineWidth: 1))
                }
                .menuStyle(.borderlessButton)

                if engine.currentFXMode != .staticFX || engine.isPlayingCustomFX {
                    Button(action: {
                        engine.stopCustomEffect()
                        engine.applyFXMode(.staticFX)
                    }) {
                        HStack(spacing: 3) {
                            Image(systemName: "stop.fill")
                            Text("STOP FX")
                        }
                        .font(.system(size: 8, weight: .heavy, design: .monospaced))
                        .padding(.horizontal, 8)
                        .padding(.vertical, 7)
                        .background(Color.red.opacity(0.85))
                        .foregroundColor(.white)
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                    }
                    .buttonStyle(.plain)
                }

                // Lock Toggle
                Button(action: {
                    engine.isLocked.toggle()
                    SoundEffect.knobTick.play(with: engine.soundPack)
                }) {
                    HStack(spacing: 3) {
                        Image(systemName: engine.isLocked ? "lock.fill" : "lock.open.fill")
                        Text(engine.isLocked ? "LOCKED" : "UNLOCKED")
                            .font(.system(size: 8.5, weight: .bold, design: .monospaced))
                    }
                    .padding(.horizontal, 9)
                    .padding(.vertical, 7)
                    .background(Color.white.opacity(0.08))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                    .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.15), lineWidth: 1))
                }
                .buttonStyle(.plain)
            }
        }
    }

    // ------------------------------------------------------------------------
    // MAIN HARDWARE FACEPLATE (THE AUTHENTIC CONSOLE LAYOUT)
    // ------------------------------------------------------------------------
    private var mainHardwareFaceplate: some View {
        ZStack {
            // Chassis Surface
            RoundedRectangle(cornerRadius: 20)
                .fill(engine.currentTheme.pcbBackground)

            // Metallic Bezel
            RoundedRectangle(cornerRadius: 20)
                .stroke(
                    LinearGradient(
                        colors: engine.currentTheme.outerBezelColors,
                        startPoint: .topLeading,
                        endPoint: .bottomTrailing
                    ),
                    lineWidth: 5.5
                )

            // Metallic Screws in Bezel Corners & Centers
            VStack {
                HStack {
                    ScrewRivetView(size: 13)
                    Spacer()
                    ScrewRivetView(size: 13)
                    Spacer()
                    ScrewRivetView(size: 13)
                }
                Spacer()
                HStack {
                    ScrewRivetView(size: 13)
                    Spacer()
                    ScrewRivetView(size: 13)
                    Spacer()
                    ScrewRivetView(size: 13)
                }
            }
            .padding(7)

            // FULL-CHASSIS HARDWARE CONSOLE: LEFT WING + CENTER BITCHBOY MATRIX + RIGHT WING
            HStack(alignment: .top, spacing: 24) {
                // LEFT WING (RED BOX #1 & #3): MIDI OUTPUT ROUTER / PATCHBAY + 20-CC TELEMETRY + MACROS + MEMORY SLOTS BANK
                VStack(alignment: .leading, spacing: 10) {
                    leftWingStudioPanel
                    leftWingMemorySlotsPanel
                }
                .frame(width: 266)

                // CENTER HARDWARE CONSOLE: 8x4 Grid + 1x8 Strip + 8 VFaders + 4 HFaders + Logo + HUD + Knobs + 3x3 Keypad
                HStack(alignment: .top, spacing: 18) {
                    // LEFT COLUMN: 8x4 Grid + 1x8 Strip + 8 Vertical Faders + In-Chassis Notes (Width: 414)
                    VStack(alignment: .leading, spacing: 8) {
                        section1KeyGrid8x4
                        section2KeyStrip1x8
                        section4VerticalFaders
                        inChassisUserNotes
                    }
                    .frame(width: 414)

                    // RIGHT SECTION: Middle Column (Faders + Glowing Logo + HUD) & Far-Right Column (Knobs + Mascot + Keypad)
                    HStack(alignment: .top, spacing: 14) {
                        // Middle Column: 4 Horizontal Faders + Glowing BitchBoy Logo + Center Inspector HUD (Width: 254)
                        VStack(alignment: .leading, spacing: 10) {
                            section5HorizontalFaders
                                .frame(width: 254)

                            // Glowing BitchBoy Mascot Logo (Style and glow of BitchBoy Mascot)
                            BitchBoyGlowingLogoView(auraColor: engine.currentTheme.accentPrimary) {
                                SoundEffect.mascotSqueak.play(with: engine.soundPack)
                                engine.log("✨ BITCHBOY LOGO ACTIVATED")
                            }
                            .frame(width: 254, height: 68)

                            centerInspectorHUD
                                .frame(width: 254, height: 256)
                        }
                        .frame(width: 254)

                        // Far-Right Column: 2x4 Knobs + Official Mascot & 3x3 Keypad (Width: 194)
                        VStack(alignment: .center, spacing: 10) {
                            section6RotaryKnobs
                                .frame(width: 194)

                            section3KeypadAndMascot
                                .frame(width: 194)
                        }
                        .frame(width: 194)
                    }
                    .frame(width: 462)
                }

                // RIGHT WING (RED BOX #2 & #4): GROUND-UP CUSTOM LIGHTING FX STUDIO + REACTIVE MECHANICAL KEYBOARD FX ENGINE
                VStack(alignment: .leading, spacing: 10) {
                    rightWingStudioPanel
                    rightWingReactiveKeyFXPanel
                }
                .frame(width: 274)
            }
            .frame(maxWidth: .infinity, alignment: .center)
            .padding(.horizontal, 18)
            .padding(.vertical, 18)
        }
    }

    // ------------------------------------------------------------------------
    // LEFT WING (RED BOX #1): MIDI INPUT -> ANY PROGRAM / OUTPUT ROUTER & SCOPE
    // ------------------------------------------------------------------------
    private var leftWingStudioPanel: some View {
        VStack(alignment: .leading, spacing: 8) {
            // Wing Header & Mode Switcher
            HStack {
                Image(systemName: "point.3.connected.trianglepath.dotted")
                    .font(.system(size: 10, weight: .bold))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                Text("MIDI ROUTER // OUT")
                    .font(.system(size: 9, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                Spacer()
                Circle()
                    .fill(engine.midiThruEnabled ? Color.green : Color.orange)
                    .frame(width: 7, height: 7)
                Text(engine.midiThruEnabled ? "THRU ON" : "MUTED")
                    .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.midiThruEnabled ? Color.green : Color.orange)
            }

            // 3-Tab Switcher
            HStack(spacing: 4) {
                wingTabButton(title: "MIDI ROUTER", active: engine.leftWingTab == "ROUTER") {
                    engine.leftWingTab = "ROUTER"
                }
                wingTabButton(title: "20-CC SCOPE", active: engine.leftWingTab == "TELEMETRY") {
                    engine.leftWingTab = "TELEMETRY"
                }
                wingTabButton(title: "MACROS", active: engine.leftWingTab == "MACROS") {
                    engine.leftWingTab = "MACROS"
                }
            }

            if engine.leftWingTab == "ROUTER" {
                leftWingMIDIRouterContent
            } else if engine.leftWingTab == "TELEMETRY" {
                leftWingTelemetryScopeContent
            } else {
                leftWingMacrosContent
            }
        }
        .padding(10)
        .background(Color.black.opacity(0.42))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(engine.currentTheme.sectionOutline.opacity(0.75), lineWidth: 1.2)
        )
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var leftWingMIDIRouterContent: some View {
        VStack(alignment: .leading, spacing: 7) {
            // 1. Virtual MIDI Output Port Card (Appears in Ableton, Logic, QLab, Resolume, etc.)
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Image(systemName: "dot.radiowaves.left.and.right")
                        .foregroundColor(engine.virtualOutEnabled ? Color(red: 0.2, green: 0.96, blue: 0.65) : .gray)
                    Text("VIRTUAL PORT (ALL DAWS)")
                        .font(.system(size: 8, weight: .heavy, design: .monospaced))
                        .foregroundColor(.white)
                    Spacer()
                    Toggle("", isOn: $engine.virtualOutEnabled)
                        .toggleStyle(.switch)
                        .scaleEffect(0.65)
                        .frame(width: 34, height: 16)
                }
                Text("Port Name: 'BitchBoy Virtual Out'")
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                Text("Select 'BitchBoy Virtual Out' in Ableton, Logic, QLab, Resolume, or any synth!")
                    .font(.system(size: 7, design: .monospaced))
                    .foregroundColor(Color(white: 0.62))
                    .fixedSize(horizontal: false, vertical: true)
            }
            .padding(7)
            .background(Color(red: 0.04, green: 0.16, blue: 0.14).opacity(0.85))
            .clipShape(RoundedRectangle(cornerRadius: 8))
            .overlay(RoundedRectangle(cornerRadius: 8).stroke(engine.currentTheme.accentPrimary.opacity(0.45), lineWidth: 1))

            // 2. Direct MIDI Output Destinations List
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text("SEND TO PROGRAMS / PORTS:")
                        .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                        .foregroundColor(engine.currentTheme.headerText)
                    Spacer()
                    Button("ALL APPS") {
                        engine.selectAllExternalOutputs()
                    }
                    .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                    .padding(.horizontal, 5)
                    .padding(.vertical, 2)
                    .background(Color.white.opacity(0.12))
                    .foregroundColor(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 4))
                    .buttonStyle(.plain)

                    Button("↻") {
                        engine.scanForBitchBoy()
                    }
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .padding(.horizontal, 5)
                    .padding(.vertical, 2)
                    .background(Color.white.opacity(0.12))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                    .clipShape(RoundedRectangle(cornerRadius: 4))
                    .buttonStyle(.plain)
                }

                ScrollView {
                    VStack(alignment: .leading, spacing: 3) {
                        if engine.availableOutputTargets.isEmpty {
                            Text("No external MIDI ports detected. Use 'BitchBoy Virtual Out' or enable IAC Driver.")
                                .font(.system(size: 7, design: .monospaced))
                                .foregroundColor(Color(white: 0.55))
                                .padding(4)
                        } else {
                            ForEach(engine.availableOutputTargets) { target in
                                let isSelected = engine.selectedOutputNames.contains(target.name)
                                Button(action: {
                                    engine.toggleOutputTarget(target.name)
                                }) {
                                    HStack(spacing: 6) {
                                        Image(systemName: isSelected ? "checkmark.square.fill" : "square")
                                            .font(.system(size: 9))
                                            .foregroundColor(isSelected ? engine.currentTheme.accentPrimary : Color(white: 0.5))
                                        VStack(alignment: .leading, spacing: 0) {
                                            Text(target.name)
                                                .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                                                .foregroundColor(.white)
                                                .lineLimit(1)
                                            Text(target.isHardwareBitchBoy ? "Hardware Controller Port" : "External App / Synth / Bus")
                                                .font(.system(size: 6.5, design: .monospaced))
                                                .foregroundColor(target.isHardwareBitchBoy ? Color.orange : Color(white: 0.55))
                                        }
                                        Spacer()
                                    }
                                    .padding(.horizontal, 6)
                                    .padding(.vertical, 4)
                                    .background(isSelected ? engine.currentTheme.accentPrimary.opacity(0.16) : Color.black.opacity(0.35))
                                    .clipShape(RoundedRectangle(cornerRadius: 5))
                                }
                                .buttonStyle(.plain)
                            }
                        }
                    }
                }
                .frame(height: 62)
                .padding(4)
                .background(Color.black.opacity(0.45))
                .clipShape(RoundedRectangle(cornerRadius: 6))

                Button(action: {
                    engine.openAudioMIDISetupApp()
                }) {
                    HStack(spacing: 4) {
                        Image(systemName: "slider.horizontal.below.rectangle")
                        Text("OPEN MACOS AUDIO MIDI / IAC")
                    }
                    .font(.system(size: 7, weight: .bold, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 4)
                    .background(Color.white.opacity(0.08))
                    .foregroundColor(Color(white: 0.85))
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)
            }

            // 3. Channel Remapper, Transpose & Velocity Curve
            VStack(alignment: .leading, spacing: 5) {
                Text("MIDI TRANSFORMER & FILTERS")
                    .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)

                // Output Channel Picker
                HStack {
                    Text("OUT CHANNEL:")
                        .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Spacer()
                    Picker("", selection: $engine.remapChannel) {
                        Text("ORIG (CH 1)").tag(0)
                        ForEach(1...16, id: \.self) { ch in
                            Text("FORCE CH \(ch)").tag(ch)
                        }
                    }
                    .pickerStyle(.menu)
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .frame(width: 118)
                }

                // Transpose Semitones
                HStack(spacing: 4) {
                    Text("TRANSPOSE:")
                        .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Spacer()
                    Button("-12") { engine.transposeSemitones = max(-24, engine.transposeSemitones - 12) }
                        .buttonStyle(.plain).font(.system(size: 7, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 4).padding(.vertical, 2).background(Color.white.opacity(0.1)).clipShape(RoundedRectangle(cornerRadius: 3))
                    Button("-") { engine.transposeSemitones = max(-24, engine.transposeSemitones - 1) }
                        .buttonStyle(.plain).font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 5).padding(.vertical, 2).background(Color.white.opacity(0.1)).clipShape(RoundedRectangle(cornerRadius: 3))
                    Text(engine.transposeSemitones >= 0 ? "+\(engine.transposeSemitones)" : "\(engine.transposeSemitones)")
                        .font(.system(size: 8, weight: .heavy, design: .monospaced))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                        .frame(width: 28)
                    Button("+") { engine.transposeSemitones = min(24, engine.transposeSemitones + 1) }
                        .buttonStyle(.plain).font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 5).padding(.vertical, 2).background(Color.white.opacity(0.1)).clipShape(RoundedRectangle(cornerRadius: 3))
                    Button("+12") { engine.transposeSemitones = min(24, engine.transposeSemitones + 12) }
                        .buttonStyle(.plain).font(.system(size: 7, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 4).padding(.vertical, 2).background(Color.white.opacity(0.1)).clipShape(RoundedRectangle(cornerRadius: 3))
                }

                // Velocity Curve
                HStack(spacing: 3) {
                    Text("CURVE:")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Spacer()
                    ForEach(MIDIVelocityCurve.allCases) { curve in
                        Button(curve.rawValue) {
                            engine.velocityCurve = curve
                        }
                        .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 5)
                        .padding(.vertical, 3)
                        .background(engine.velocityCurve == curve ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                        .foregroundColor(engine.velocityCurve == curve ? .black : .white)
                        .clipShape(RoundedRectangle(cornerRadius: 4))
                        .buttonStyle(.plain)
                    }
                }

                // Filter Checkboxes
                HStack(spacing: 6) {
                    filterTogglePill(label: "KEYS", isOn: $engine.forwardNotes)
                    filterTogglePill(label: "FADERS/CC", isOn: $engine.forwardCCs)
                    filterTogglePill(label: "UI SIM", isOn: $engine.forwardUIInteractions)
                }
            }
            .padding(7)
            .background(Color.black.opacity(0.4))
            .clipShape(RoundedRectangle(cornerRadius: 8))

            // 4. Test Note & Panic Bar + Live Outgoing Telemetry
            HStack(spacing: 6) {
                Button(action: {
                    engine.sendTestNoteToOutputs()
                }) {
                    HStack(spacing: 3) {
                        Image(systemName: "music.note.list")
                        Text("TEST CHORD")
                    }
                    .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 6)
                    .background(Color(red: 0.14, green: 0.45, blue: 0.36))
                    .foregroundColor(Color(red: 0.35, green: 0.98, blue: 0.78))
                    .clipShape(RoundedRectangle(cornerRadius: 6))
                }
                .buttonStyle(.plain)

                Button(action: {
                    engine.sendPanicAllNotesOffToOutputs()
                }) {
                    HStack(spacing: 3) {
                        Image(systemName: "exclamationmark.triangle.fill")
                        Text("PANIC OFF")
                    }
                    .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 6)
                    .background(Color(red: 0.55, green: 0.16, blue: 0.16))
                    .foregroundColor(Color(red: 1.0, green: 0.65, blue: 0.65))
                    .clipShape(RoundedRectangle(cornerRadius: 6))
                }
                .buttonStyle(.plain)
            }

            // Live TX Packet Strip
            VStack(alignment: .leading, spacing: 2) {
                HStack {
                    Text("TX OUT STREAM")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.5))
                    Spacer()
                    Text("PKTS: \(engine.txPacketCount)")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                }
                Text(engine.lastRoutedMessage)
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(red: 0.3, green: 0.98, blue: 0.75))
                    .lineLimit(1)
            }
            .padding(6)
            .background(Color.black.opacity(0.55))
            .clipShape(RoundedRectangle(cornerRadius: 6))
        }
    }

    private var leftWingTelemetryScopeContent: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("8 VERTICAL FADERS (CC 0–7)")
                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            HStack(alignment: .bottom, spacing: 6) {
                ForEach(0..<8, id: \.self) { i in
                    let val = engine.controls[49 + i].liveValue
                    VStack(spacing: 2) {
                        Text("\(val)")
                            .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                            .foregroundColor(engine.currentTheme.accentPrimary)
                        ZStack(alignment: .bottom) {
                            RoundedRectangle(cornerRadius: 3)
                                .fill(Color.white.opacity(0.08))
                                .frame(width: 20, height: 42)
                            RoundedRectangle(cornerRadius: 3)
                                .fill(engine.currentTheme.accentPrimary)
                                .frame(width: 20, height: max(3, CGFloat(val) / 127.0 * 42.0))
                        }
                        Text("VF\(i+1)")
                            .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                            .foregroundColor(Color(white: 0.65))
                    }
                }
            }
            .frame(maxWidth: .infinity)
            .padding(6)
            .background(Color.black.opacity(0.4))
            .clipShape(RoundedRectangle(cornerRadius: 8))

            Text("4 HORIZ FADERS & 8 KNOBS (CC 8–11, 20–27)")
                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            VStack(spacing: 3) {
                ForEach(57..<69, id: \.self) { idx in
                    let item = engine.controls[idx]
                    HStack(spacing: 5) {
                        Text(item.label)
                            .font(.system(size: 7, weight: .bold, design: .monospaced))
                            .foregroundColor(.white)
                            .frame(width: 26, alignment: .leading)
                        GeometryReader { geo in
                            ZStack(alignment: .leading) {
                                RoundedRectangle(cornerRadius: 2).fill(Color.white.opacity(0.1))
                                RoundedRectangle(cornerRadius: 2)
                                    .fill(Color(red: 0.96, green: 0.72, blue: 0.28))
                                    .frame(width: max(3, geo.size.width * CGFloat(item.liveValue) / 127.0))
                            }
                        }
                        .frame(height: 6)
                        Text(String(format: "%03d", item.liveValue))
                            .font(.system(size: 7, weight: .bold, design: .monospaced))
                            .foregroundColor(Color(red: 0.96, green: 0.72, blue: 0.28))
                            .frame(width: 24, alignment: .trailing)
                    }
                }
            }
            .padding(6)
            .background(Color.black.opacity(0.4))
            .clipShape(RoundedRectangle(cornerRadius: 8))

            Text("LIVE MIDI HEX PACKET MONITOR")
                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            ScrollView {
                VStack(alignment: .leading, spacing: 2) {
                    if engine.routerPacketHistory.isEmpty {
                        Text("Waiting for MIDI packets...")
                            .font(.system(size: 7, design: .monospaced))
                            .foregroundColor(Color(white: 0.5))
                    } else {
                        ForEach(engine.routerPacketHistory, id: \.self) { pkt in
                            Text(pkt)
                                .font(.system(size: 7, design: .monospaced))
                                .foregroundColor(Color(red: 0.28, green: 0.96, blue: 0.74))
                        }
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)
            }
            .frame(height: 54)
            .padding(6)
            .background(Color.black.opacity(0.55))
            .clipShape(RoundedRectangle(cornerRadius: 6))
        }
    }

    private var leftWingMacrosContent: some View {
        VStack(alignment: .leading, spacing: 8) {
            // Tap Tempo BPM Clock
            HStack {
                VStack(alignment: .leading, spacing: 2) {
                    Text("MASTER CLOCK // TAP TEMPO")
                        .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                        .foregroundColor(engine.currentTheme.headerText)
                    Text("\(Int(engine.tapTempoBPM)) BPM (FX: \(String(format: "%.1fx", engine.fxSpeed)))")
                        .font(.system(size: 11, weight: .heavy, design: .monospaced))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                }
                Spacer()
                Button(action: {
                    engine.registerTapTempo()
                }) {
                    Text("🥁 TAP BPM")
                        .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                        .padding(.horizontal, 10)
                        .padding(.vertical, 8)
                        .background(engine.currentTheme.accentPrimary)
                        .foregroundColor(.black)
                        .clipShape(RoundedRectangle(cornerRadius: 8))
                }
                .buttonStyle(.plain)
            }
            .padding(8)
            .background(Color.black.opacity(0.45))
            .clipShape(RoundedRectangle(cornerRadius: 8))

            Text("8 INSTANT HARDWARE SCENE MACROS")
                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            LazyVGrid(columns: [GridItem(.flexible()), GridItem(.flexible())], spacing: 6) {
                macroTriggerPad(title: "ZERO ALL FADERS", icon: "arrow.down.to.line") {
                    for i in 49..<61 { engine.setControlLiveValue(index: i, value: 0) }
                }
                macroTriggerPad(title: "MAX ALL FADERS", icon: "arrow.up.to.line") {
                    for i in 49..<61 { engine.setControlLiveValue(index: i, value: 127) }
                }
                macroTriggerPad(title: "CENTER 8 KNOBS", icon: "dial.medium.fill") {
                    for i in 61..<69 { engine.setControlLiveValue(index: i, value: 64) }
                }
                macroTriggerPad(title: "PARTY SPARK LEDS", icon: "sparkles") {
                    let pal: [UInt8] = [5, 14, 24, 38, 52, 68, 84, 98, 112, 127]
                    for i in 0..<49 {
                        let v = pal.randomElement() ?? 68
                        engine.controls[i].idleVelocity = v
                        engine.controls[i].currentLED = v
                        engine.sendNoteOn(note: engine.controls[i].number, velocity: v)
                    }
                    engine.saveCurrentSlot()
                }
                macroTriggerPad(title: "ALL 49 LEDS MAX", icon: "sun.max.fill") {
                    engine.setZoneLEDs(range: 0..<49, velocity: 127)
                }
                macroTriggerPad(title: "BLACKOUT ALL 49", icon: "moon.fill") {
                    engine.blackoutPanic()
                }
                macroTriggerPad(title: "SEND C-MAJ9 CHORD", icon: "pianokeys") {
                    engine.sendTestNoteToOutputs()
                }
                macroTriggerPad(title: "ALL NOTES OFF", icon: "xmark.octagon.fill") {
                    engine.sendPanicAllNotesOffToOutputs()
                }
            }
        }
    }

    // ------------------------------------------------------------------------
    // LEFT WING (RED BOX #3): HARDWARE MEMORY SLOTS & CUSTOM PRESET BANK DECK
    // ------------------------------------------------------------------------
    private var leftWingMemorySlotsPanel: some View {
        let slotLetters = ["A", "B", "C", "D", "E", "F", "G", "H"]
        let bankRange: Range<Int> = activeMemoryBank == 0 ? (0..<min(4, engine.slots.count)) : (min(4, engine.slots.count)..<min(8, engine.slots.count))
        let activeIdx = min(max(0, engine.activeSlot), max(0, engine.slots.count - 1))
        let activeSlotColor = engine.slots.indices.contains(activeIdx) ? ledColor(engine.slots[activeIdx].accentVelocity) : engine.currentTheme.accentPrimary

        return VStack(alignment: .leading, spacing: 6) {
            // Header & Bank A-D / E-H Switcher
            HStack(spacing: 5) {
                Image(systemName: "memorychip.fill")
                    .font(.system(size: 10, weight: .bold))
                    .foregroundColor(activeSlotColor)
                    .shadow(color: activeSlotColor.opacity(0.8), radius: 4)
                Text("MEMORY SLOTS // BANK")
                    .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                Spacer()

                HStack(spacing: 3) {
                    Button(action: {
                        activeMemoryBank = 0
                        SoundEffect.knobTick.play(with: engine.soundPack)
                    }) {
                        Text("A–D")
                            .font(.system(size: 7, weight: .heavy, design: .monospaced))
                            .padding(.horizontal, 6)
                            .padding(.vertical, 2.5)
                            .background(activeMemoryBank == 0 ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                            .foregroundColor(activeMemoryBank == 0 ? .black : Color(white: 0.75))
                            .clipShape(RoundedRectangle(cornerRadius: 4))
                    }
                    .buttonStyle(.plain)

                    if engine.slots.count > 4 {
                        Button(action: {
                            activeMemoryBank = 1
                            SoundEffect.knobTick.play(with: engine.soundPack)
                        }) {
                            Text("E–H")
                                .font(.system(size: 7, weight: .heavy, design: .monospaced))
                                .padding(.horizontal, 6)
                                .padding(.vertical, 2.5)
                                .background(activeMemoryBank == 1 ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                                .foregroundColor(activeMemoryBank == 1 ? .black : Color(white: 0.75))
                                .clipShape(RoundedRectangle(cornerRadius: 4))
                        }
                        .buttonStyle(.plain)
                    }
                }
            }

            // 2x2 Tactile Illuminated Memory Slot Pads
            LazyVGrid(columns: [GridItem(.flexible(), spacing: 6), GridItem(.flexible(), spacing: 6)], spacing: 6) {
                ForEach(Array(bankRange), id: \.self) { idx in
                    let slot = engine.slots[idx]
                    let isSelected = (engine.activeSlot == idx)
                    let slotAccent = ledColor(slot.accentVelocity)
                    let sourceControls = isSelected ? engine.controls : slot.controls
                    let litCount = sourceControls.prefix(49).filter { $0.idleVelocity > 0 }.count
                    let macroCount = sourceControls.filter { $0.actionType != .passthrough }.count
                    let shortTitle = slot.name
                        .replacingOccurrences(of: "SLOT \(slotLetters[idx % 8]) // ", with: "")
                        .replacingOccurrences(of: "SLOT \(slotLetters[idx % 8]) ", with: "")

                    Button(action: {
                        engine.selectSlot(idx)
                    }) {
                        VStack(alignment: .leading, spacing: 3) {
                            HStack(spacing: 4) {
                                Text("SLOT \(slotLetters[idx % 8])")
                                    .font(.system(size: 7, weight: .heavy, design: .monospaced))
                                    .padding(.horizontal, 4.5)
                                    .padding(.vertical, 1.5)
                                    .background(isSelected ? slotAccent : Color.white.opacity(0.12))
                                    .foregroundColor(isSelected ? .black : slotAccent)
                                    .clipShape(RoundedRectangle(cornerRadius: 3))

                                Image(systemName: slot.iconName)
                                    .font(.system(size: 8.5, weight: .bold))
                                    .foregroundColor(isSelected ? .white : slotAccent)

                                Spacer(minLength: 0)

                                Circle()
                                    .fill(isSelected ? Color.green : slotAccent.opacity(0.45))
                                    .frame(width: 6, height: 6)
                                    .shadow(color: isSelected ? Color.green : .clear, radius: 3)
                            }

                            Text(shortTitle.isEmpty ? slot.name : shortTitle)
                                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                                .foregroundColor(isSelected ? .white : Color(white: 0.85))
                                .lineLimit(1)

                            HStack(spacing: 4) {
                                Text("\(litCount) LED")
                                    .font(.system(size: 6, weight: .bold, design: .monospaced))
                                    .foregroundColor(isSelected ? slotAccent : Color(white: 0.55))
                                Text("•")
                                    .font(.system(size: 6, weight: .bold))
                                    .foregroundColor(Color(white: 0.4))
                                Text("\(macroCount) ACT")
                                    .font(.system(size: 6, weight: .bold, design: .monospaced))
                                    .foregroundColor(macroCount > 0 ? Color.yellow.opacity(0.9) : Color(white: 0.55))
                                Spacer(minLength: 0)
                            }
                        }
                        .padding(6)
                        .background(
                            RoundedRectangle(cornerRadius: 7)
                                .fill(isSelected ? slotAccent.opacity(0.22) : Color.black.opacity(0.5))
                        )
                        .overlay(
                            RoundedRectangle(cornerRadius: 7)
                                .stroke(isSelected ? slotAccent : Color.white.opacity(0.14), lineWidth: isSelected ? 1.5 : 0.8)
                        )
                        .shadow(color: isSelected ? slotAccent.opacity(0.45) : .clear, radius: 5)
                    }
                    .buttonStyle(.plain)
                }
            }

            // Customizer Row: Inline Rename + Icon & Accent Color Swatches for Active Slot
            if engine.slots.indices.contains(activeIdx) {
                VStack(alignment: .leading, spacing: 4) {
                    HStack(spacing: 5) {
                        // Cycle Icon Button / Menu
                        Menu {
                            let icons = [
                                ("music.mic", "LIVE / VOCAL"),
                                ("slider.horizontal.3", "DAW MIXER"),
                                ("sparkles", "LIGHT SHOW / DMX"),
                                ("square.grid.3x3.fill", "STREAM DECK"),
                                ("pianokeys", "SYNTH / KEYS"),
                                ("waveform.path.ecg", "DRUMS / FX"),
                                ("tv.fill", "VJ / RESOLUME"),
                                ("cpu.fill", "HARDWARE MACRO"),
                                ("bolt.fill", "HIGH ENERGY"),
                                ("flame.fill", "FIRE SET")
                            ]
                            ForEach(icons, id: \.0) { ic in
                                Button(action: {
                                    engine.slots[activeIdx].iconName = ic.0
                                    engine.saveCurrentSlot()
                                }) {
                                    Label(ic.1, systemImage: ic.0)
                                }
                            }
                        } label: {
                            Image(systemName: engine.slots[activeIdx].iconName)
                                .font(.system(size: 9, weight: .bold))
                                .foregroundColor(activeSlotColor)
                                .frame(width: 20, height: 20)
                                .background(Color.white.opacity(0.08))
                                .clipShape(RoundedRectangle(cornerRadius: 4))
                        }
                        .menuStyle(.borderlessButton)
                        .frame(width: 22)

                        // Editable Slot Name TextField
                        TextField(
                            "SLOT NAME",
                            text: Binding(
                                get: { engine.slots[activeIdx].name },
                                set: { newVal in
                                    engine.slots[activeIdx].name = newVal
                                    engine.saveCurrentSlot()
                                }
                            )
                        )
                        .textFieldStyle(.plain)
                        .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                        .foregroundColor(.white)
                        .padding(.horizontal, 6)
                        .padding(.vertical, 3.5)
                        .background(Color.black.opacity(0.55))
                        .clipShape(RoundedRectangle(cornerRadius: 4))
                        .overlay(RoundedRectangle(cornerRadius: 4).stroke(activeSlotColor.opacity(0.45), lineWidth: 0.8))

                        // Toggle Color Swatch Bar
                        Button(action: {
                            withAnimation(.easeInOut(duration: 0.15)) {
                                showSlotCustomizer.toggle()
                            }
                        }) {
                            Image(systemName: showSlotCustomizer ? "paintpalette.fill" : "paintpalette")
                                .font(.system(size: 8.5, weight: .bold))
                                .foregroundColor(showSlotCustomizer ? .black : activeSlotColor)
                                .frame(width: 20, height: 20)
                                .background(showSlotCustomizer ? activeSlotColor : Color.white.opacity(0.08))
                                .clipShape(RoundedRectangle(cornerRadius: 4))
                        }
                        .buttonStyle(.plain)
                    }

                    if showSlotCustomizer {
                        HStack(spacing: 4) {
                            Text("COLOR:")
                                .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                                .foregroundColor(Color(white: 0.6))
                            let swatches: [UInt8] = [5, 24, 38, 52, 68, 98, 112, 127]
                            ForEach(swatches, id: \.self) { vel in
                                Circle()
                                    .fill(ledColor(vel))
                                    .frame(width: 12, height: 12)
                                    .overlay(
                                        Circle()
                                            .stroke(Color.white, lineWidth: engine.slots[activeIdx].accentVelocity == vel ? 1.5 : 0)
                                    )
                                    .onTapGesture {
                                        engine.slots[activeIdx].accentVelocity = vel
                                        engine.saveCurrentSlot()
                                        SoundEffect.knobTick.play(with: engine.soundPack)
                                    }
                            }
                            Spacer()
                            Button("SYNC 49 LEDS") {
                                engine.setZoneLEDs(range: 0..<49, velocity: engine.slots[activeIdx].accentVelocity)
                            }
                            .font(.system(size: 6, weight: .heavy, design: .monospaced))
                            .padding(.horizontal, 5)
                            .padding(.vertical, 2)
                            .background(activeSlotColor.opacity(0.25))
                            .foregroundColor(activeSlotColor)
                            .clipShape(RoundedRectangle(cornerRadius: 3))
                            .buttonStyle(.plain)
                        }
                        .padding(.top, 2)
                    }
                }
                .padding(5)
                .background(Color.black.opacity(0.38))
                .clipShape(RoundedRectangle(cornerRadius: 6))
            }

            // 1-Click Memory Deck Action Bar (Save, Clone To..., JSON Copy/Paste, Reset, Blackout)
            HStack(spacing: 4) {
                Button(action: {
                    engine.saveCurrentSlot()
                    SoundEffect.presetSwitch.play(with: engine.soundPack)
                    engine.log("💾 Saved \(engine.slots[activeIdx].name) to internal memory.")
                }) {
                    HStack(spacing: 2) {
                        Image(systemName: "externaldrive.fill.badge.checkmark")
                        Text("SAVE")
                    }
                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 4.5)
                    .background(Color(red: 0.12, green: 0.46, blue: 0.36))
                    .foregroundColor(Color(red: 0.35, green: 0.98, blue: 0.78))
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)

                Menu {
                    ForEach(0..<engine.slots.count, id: \.self) { targetIdx in
                        if targetIdx != activeIdx {
                            Button("Clone into Slot \(slotLetters[targetIdx % 8]) (\(engine.slots[targetIdx].name))") {
                                engine.cloneCurrentSlotTo(targetIdx)
                            }
                        }
                    }
                } label: {
                    HStack(spacing: 2) {
                        Image(systemName: "doc.on.doc.fill")
                        Text("CLONE")
                    }
                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 4.5)
                    .background(Color.white.opacity(0.1))
                    .foregroundColor(.white)
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .menuStyle(.borderlessButton)

                Button(action: {
                    _ = engine.exportPresetToJSON()
                    SoundEffect.keyTap.play(with: engine.soundPack)
                }) {
                    HStack(spacing: 2) {
                        Image(systemName: "arrow.up.doc.fill")
                        Text("EXP")
                    }
                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                    .padding(.horizontal, 6)
                    .padding(.vertical, 4.5)
                    .background(Color.white.opacity(0.1))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)

                Button(action: {
                    engine.importPresetFromClipboard()
                    SoundEffect.presetSwitch.play(with: engine.soundPack)
                }) {
                    HStack(spacing: 2) {
                        Image(systemName: "arrow.down.doc.fill")
                        Text("IMP")
                    }
                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                    .padding(.horizontal, 6)
                    .padding(.vertical, 4.5)
                    .background(Color.white.opacity(0.1))
                    .foregroundColor(Color.yellow)
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)

                Button(action: {
                    engine.resetCurrentSlotToDefault()
                }) {
                    Image(systemName: "arrow.counterclockwise")
                        .font(.system(size: 7.5, weight: .heavy))
                        .padding(.horizontal, 6)
                        .padding(.vertical, 4.5)
                        .background(Color(red: 0.48, green: 0.16, blue: 0.16))
                        .foregroundColor(Color(red: 1.0, green: 0.65, blue: 0.65))
                        .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)
            }
        }
        .padding(9)
        .background(Color.black.opacity(0.46))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(activeSlotColor.opacity(0.75), lineWidth: 1.2)
        )
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // ------------------------------------------------------------------------
    // RIGHT WING (RED BOX #2): GROUND-UP CUSTOM LIGHTING FX STUDIO
    // ------------------------------------------------------------------------
    private var rightWingStudioPanel: some View {
        VStack(alignment: .leading, spacing: 7) {
            // Header
            HStack {
                Image(systemName: "wand.and.stars")
                    .font(.system(size: 10, weight: .bold))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                Text("CUSTOM FX STUDIO")
                    .font(.system(size: 9, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                Spacer()
                if engine.isPlayingCustomFX {
                    HStack(spacing: 3) {
                        Circle().fill(Color.green).frame(width: 6, height: 6)
                        Text("LIVE ON HW")
                            .font(.system(size: 7, weight: .heavy, design: .monospaced))
                            .foregroundColor(.green)
                    }
                }
            }

            // 3-Tab Switcher: Frame Sequencer / Procedural Wave Synth / Saved Shows
            HStack(spacing: 4) {
                wingTabButton(title: "SEQUENCER", active: engine.rightWingTab == "SEQUENCER") {
                    engine.editingCustomFX.isProcedural = false
                    engine.rightWingTab = "SEQUENCER"
                }
                wingTabButton(title: "WAVE SYNTH", active: engine.rightWingTab == "WAVESYNTH") {
                    engine.editingCustomFX.isProcedural = true
                    engine.rightWingTab = "WAVESYNTH"
                }
                wingTabButton(title: "MY SHOWS (\(engine.customEffects.count))", active: engine.rightWingTab == "LIBRARY") {
                    engine.rightWingTab = "LIBRARY"
                }
            }

            if engine.rightWingTab == "SEQUENCER" {
                rightWingFrameSequencerContent
            } else if engine.rightWingTab == "WAVESYNTH" {
                rightWingWaveSynthContent
            } else {
                rightWingSavedLibraryContent
            }
        }
        .padding(10)
        .background(Color.black.opacity(0.42))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(engine.currentTheme.sectionOutline.opacity(0.75), lineWidth: 1.2)
        )
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var rightWingFrameSequencerContent: some View {
        VStack(alignment: .leading, spacing: 6) {
            // Show Name + Play/Save Row
            HStack(spacing: 4) {
                TextField("Show Name", text: $engine.editingCustomFX.name)
                    .font(.system(size: 8, weight: .bold, design: .monospaced))
                    .textFieldStyle(.plain)
                    .padding(.horizontal, 6)
                    .padding(.vertical, 4)
                    .background(Color.black.opacity(0.55))
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                    .overlay(RoundedRectangle(cornerRadius: 5).stroke(Color.white.opacity(0.15), lineWidth: 1))

                Button(action: {
                    if engine.isPlayingCustomFX {
                        engine.stopCustomEffect()
                    } else {
                        engine.editingCustomFX.isProcedural = false
                        engine.playCustomEffect(engine.editingCustomFX)
                    }
                }) {
                    HStack(spacing: 3) {
                        Image(systemName: engine.isPlayingCustomFX ? "stop.fill" : "play.fill")
                        Text(engine.isPlayingCustomFX ? "STOP" : "PLAY")
                    }
                    .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                    .padding(.horizontal, 8)
                    .padding(.vertical, 5)
                    .background(engine.isPlayingCustomFX ? Color.red : engine.currentTheme.accentPrimary)
                    .foregroundColor(engine.isPlayingCustomFX ? .white : .black)
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)

                Button(action: {
                    engine.saveEditingCustomFXToLibrary()
                }) {
                    Text("💾 SAVE")
                        .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                        .padding(.horizontal, 7)
                        .padding(.vertical, 5)
                        .background(Color(red: 0.16, green: 0.45, blue: 0.34))
                        .foregroundColor(Color(red: 0.4, green: 0.98, blue: 0.78))
                        .clipShape(RoundedRectangle(cornerRadius: 5))
                }
                .buttonStyle(.plain)
            }

            // Toggle to paint directly on the big 49-key BitchBoy console!
            Button(action: {
                engine.paintOnMainChassis.toggle()
            }) {
                HStack(spacing: 5) {
                    Image(systemName: engine.paintOnMainChassis ? "paintbrush.pointed.fill" : "paintbrush")
                    Text(engine.paintOnMainChassis ? "PAINTING ON MAIN 49 KEYS: ON" : "CLICK TO PAINT ON MAIN 49 KEYS")
                        .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                    Spacer()
                }
                .padding(.horizontal, 8)
                .padding(.vertical, 5)
                .background(engine.paintOnMainChassis ? Color(red: 0.95, green: 0.50, blue: 0.15) : Color.white.opacity(0.08))
                .foregroundColor(engine.paintOnMainChassis ? .black : engine.currentTheme.accentPrimary)
                .clipShape(RoundedRectangle(cornerRadius: 6))
            }
            .buttonStyle(.plain)

            // Interactive 49-Key Mini Canvas Preview & Painter
            VStack(alignment: .leading, spacing: 3) {
                HStack {
                    let safeFrame = min(max(0, engine.editingFrameIndex), max(0, engine.editingCustomFX.frames.count - 1))
                    Text("FRAME \(safeFrame + 1)/\(engine.editingCustomFX.frames.count) CANVAS (CLICK TO PAINT)")
                        .font(.system(size: 7, weight: .heavy, design: .monospaced))
                        .foregroundColor(Color(white: 0.65))
                    Spacer()
                    Text("BRUSH: \(engine.customBrushVelocity)")
                        .font(.system(size: 7, weight: .heavy, design: .monospaced))
                        .foregroundColor(ledColor(engine.customBrushVelocity))
                }

                HStack(alignment: .top, spacing: 6) {
                    // Mini 8x4 + 1x8
                    VStack(spacing: 3) {
                        ForEach(0..<4, id: \.self) { r in
                            HStack(spacing: 2) {
                                ForEach(0..<8, id: \.self) { c in
                                    let kIdx = r * 8 + c
                                    miniCanvasKey(keyIndex: kIdx)
                                }
                            }
                        }
                        Divider().background(Color.white.opacity(0.2))
                        HStack(spacing: 2) {
                            ForEach(32..<40, id: \.self) { kIdx in
                                miniCanvasKey(keyIndex: kIdx)
                            }
                        }
                    }

                    // Mini 3x3 Keypad
                    VStack(spacing: 2) {
                        Text("3x3")
                            .font(.system(size: 6, weight: .bold, design: .monospaced))
                            .foregroundColor(Color(white: 0.5))
                        ForEach(0..<3, id: \.self) { r in
                            HStack(spacing: 2) {
                                ForEach(0..<3, id: \.self) { c in
                                    let kIdx = 40 + r * 3 + c
                                    miniCanvasKey(keyIndex: kIdx)
                                }
                            }
                        }
                    }
                }
                .padding(6)
                .frame(maxWidth: .infinity)
                .background(Color.black.opacity(0.6))
                .clipShape(RoundedRectangle(cornerRadius: 8))
                .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.12), lineWidth: 1))
            }

            // 11-Color Neon Brush Swatches + Velocity Slider
            VStack(alignment: .leading, spacing: 3) {
                Text("NEON COLOR BRUSH PALETTE")
                    .font(.system(size: 7, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)

                let swatches: [(String, UInt8)] = [
                    ("OFF", 0), ("RED", 5), ("ORG", 14), ("YLW", 24),
                    ("LIM", 38), ("GRN", 52), ("CYN", 68), ("BLU", 84),
                    ("PRP", 98), ("PNK", 112), ("WHT", 127)
                ]
                LazyVGrid(columns: Array(repeating: GridItem(.flexible(), spacing: 3), count: 6), spacing: 3) {
                    ForEach(swatches, id: \.1) { item in
                        let isSel = (engine.customBrushVelocity == item.1)
                        Button(action: {
                            engine.customBrushVelocity = item.1
                        }) {
                            HStack(spacing: 2) {
                                Circle()
                                    .fill(item.1 == 0 ? Color(white: 0.25) : ledColor(item.1))
                                    .frame(width: 6, height: 6)
                                Text(item.0)
                                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                            }
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 3)
                            .background(isSel ? Color.white.opacity(0.24) : Color.black.opacity(0.45))
                            .foregroundColor(.white)
                            .clipShape(RoundedRectangle(cornerRadius: 4))
                            .overlay(RoundedRectangle(cornerRadius: 4).stroke(isSel ? engine.currentTheme.accentPrimary : Color.white.opacity(0.1), lineWidth: 1))
                        }
                        .buttonStyle(.plain)
                    }
                }
            }

            // Keyframe Timeline Strip
            VStack(alignment: .leading, spacing: 3) {
                HStack {
                    Text("TIMELINE FRAMES")
                        .font(.system(size: 7, weight: .heavy, design: .monospaced))
                        .foregroundColor(engine.currentTheme.headerText)
                    Spacer()
                    Button("+ ADD") { engine.addCustomFXFrame() }
                        .buttonStyle(.plain).font(.system(size: 6.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 5).padding(.vertical, 2).background(engine.currentTheme.accentPrimary.opacity(0.25)).foregroundColor(engine.currentTheme.accentPrimary).clipShape(RoundedRectangle(cornerRadius: 3))
                    Button("📋 DUP") { engine.duplicateCustomFXFrame() }
                        .buttonStyle(.plain).font(.system(size: 6.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 5).padding(.vertical, 2).background(Color.white.opacity(0.12)).foregroundColor(.white).clipShape(RoundedRectangle(cornerRadius: 3))
                    Button("🗑️ DEL") { engine.deleteCustomFXFrame() }
                        .buttonStyle(.plain).font(.system(size: 6.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 5).padding(.vertical, 2).background(Color.red.opacity(0.25)).foregroundColor(Color(red: 1, green: 0.5, blue: 0.5)).clipShape(RoundedRectangle(cornerRadius: 3))
                }

                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(spacing: 4) {
                        ForEach(0..<engine.editingCustomFX.frames.count, id: \.self) { fIdx in
                            let isCur = (engine.editingFrameIndex == fIdx)
                            Button(action: {
                                engine.editingFrameIndex = fIdx
                                engine.previewCurrentCustomFrameOnHardware()
                            }) {
                                Text("F\(fIdx + 1)")
                                    .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                                    .padding(.horizontal, 7)
                                    .padding(.vertical, 4)
                                    .background(isCur ? engine.currentTheme.accentPrimary : Color.black.opacity(0.5))
                                    .foregroundColor(isCur ? .black : .white)
                                    .clipShape(RoundedRectangle(cornerRadius: 4))
                            }
                            .buttonStyle(.plain)
                        }
                    }
                }
            }

            // 1-Click Pattern Shifters & Frame Tools
            VStack(alignment: .leading, spacing: 3) {
                Text("PATTERN SHIFTERS & GENERATORS")
                    .font(.system(size: 7, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)

                HStack(spacing: 3) {
                    miniToolBtn("◀ L") { engine.shiftCurrentFrame(dx: -1, dy: 0) }
                    miniToolBtn("R ▶") { engine.shiftCurrentFrame(dx: 1, dy: 0) }
                    miniToolBtn("▲ U") { engine.shiftCurrentFrame(dx: 0, dy: -1) }
                    miniToolBtn("▼ D") { engine.shiftCurrentFrame(dx: 0, dy: 1) }
                    miniToolBtn("🪞 MIR") { engine.mirrorCurrentFrameHorizontal() }
                }
                HStack(spacing: 3) {
                    miniToolBtn("🌈 WAVE") { engine.applyRainbowToCurrentFrame() }
                    miniToolBtn("🎲 SPARK") { engine.randomizeCurrentFrameSparks() }
                    miniToolBtn("🔄 INV") { engine.invertCurrentFrame() }
                    miniToolBtn("⬛ FILL") { engine.fillCurrentCustomFXFrame(with: engine.customBrushVelocity) }
                    miniToolBtn("🧹 CLR") { engine.clearCurrentCustomFXFrame() }
                }
            }

            // FPS & Loop Behavior
            VStack(alignment: .leading, spacing: 3) {
                HStack {
                    Text("SPEED: \(Int(engine.editingCustomFX.fps)) FPS")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Slider(value: $engine.editingCustomFX.fps, in: 1...24, step: 1) { _ in
                        if engine.isPlayingCustomFX {
                            engine.playCustomEffect(engine.editingCustomFX)
                        }
                    }
                    .accentColor(engine.currentTheme.accentPrimary)
                }

                HStack(spacing: 4) {
                    Text("MODE:")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Spacer()
                    ForEach(CustomFXLoopMode.allCases) { lm in
                        Button(lm.rawValue) {
                            engine.editingCustomFX.loopMode = lm
                        }
                        .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                        .padding(.horizontal, 6)
                        .padding(.vertical, 3)
                        .background(engine.editingCustomFX.loopMode == lm ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                        .foregroundColor(engine.editingCustomFX.loopMode == lm ? .black : .white)
                        .clipShape(RoundedRectangle(cornerRadius: 4))
                        .buttonStyle(.plain)
                    }
                }
            }
            .padding(5)
            .background(Color.black.opacity(0.35))
            .clipShape(RoundedRectangle(cornerRadius: 6))
        }
    }

    private var rightWingWaveSynthContent: some View {
        VStack(alignment: .leading, spacing: 7) {
            Text("PROCEDURAL LIGHTING SYNTHESIZER")
                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            TextField("Synth Show Name", text: $engine.editingCustomFX.name)
                .font(.system(size: 8, weight: .bold, design: .monospaced))
                .textFieldStyle(.plain)
                .padding(5)
                .background(Color.black.opacity(0.5))
                .clipShape(RoundedRectangle(cornerRadius: 5))

            VStack(alignment: .leading, spacing: 4) {
                Text("WAVEFORM FORMULA:")
                    .font(.system(size: 7, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(white: 0.7))
                Picker("", selection: $engine.editingCustomFX.waveShape) {
                    ForEach(CustomWaveShape.allCases) { ws in
                        Text(ws.rawValue).tag(ws)
                    }
                }
                .pickerStyle(.menu)
                .font(.system(size: 7.5, weight: .bold, design: .monospaced))

                Text("VECTOR DIRECTION:")
                    .font(.system(size: 7, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(white: 0.7))
                Picker("", selection: $engine.editingCustomFX.waveDirection) {
                    ForEach(CustomWaveDirection.allCases) { wd in
                        Text(wd.rawValue).tag(wd)
                    }
                }
                .pickerStyle(.menu)
                .font(.system(size: 7.5, weight: .bold, design: .monospaced))

                HStack {
                    Text("DENSITY:")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Slider(value: $engine.editingCustomFX.waveFrequency, in: 0.3...3.0, step: 0.1)
                        .accentColor(engine.currentTheme.accentPrimary)
                    Text(String(format: "%.1fx", engine.editingCustomFX.waveFrequency))
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                }

                HStack {
                    Text("SPEED FPS:")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Slider(value: $engine.editingCustomFX.fps, in: 2...25, step: 1)
                        .accentColor(engine.currentTheme.accentPrimary)
                    Text("\(Int(engine.editingCustomFX.fps))")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                }

                HStack {
                    Text("MIN COLOR: \(engine.editingCustomFX.minVelocity)")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(ledColor(engine.editingCustomFX.minVelocity))
                    Slider(
                        value: Binding(
                            get: { Double(engine.editingCustomFX.minVelocity) },
                            set: { engine.editingCustomFX.minVelocity = UInt8($0) }
                        ),
                        in: 0...127,
                        step: 1
                    )
                }

                HStack {
                    Text("MAX COLOR: \(engine.editingCustomFX.maxVelocity)")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(ledColor(engine.editingCustomFX.maxVelocity))
                    Slider(
                        value: Binding(
                            get: { Double(engine.editingCustomFX.maxVelocity) },
                            set: { engine.editingCustomFX.maxVelocity = UInt8($0) }
                        ),
                        in: 0...127,
                        step: 1
                    )
                }
            }
            .padding(6)
            .background(Color.black.opacity(0.4))
            .clipShape(RoundedRectangle(cornerRadius: 8))

            Button(action: {
                engine.editingCustomFX.isProcedural = true
                engine.playCustomEffect(engine.editingCustomFX)
            }) {
                HStack {
                    Image(systemName: "play.circle.fill")
                    Text("▶️ STREAM LIVE SYNTH TO BITCHBOY")
                }
                .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 6)
                .background(engine.currentTheme.accentPrimary)
                .foregroundColor(.black)
                .clipShape(RoundedRectangle(cornerRadius: 6))
            }
            .buttonStyle(.plain)

            Button(action: {
                engine.bakeProceduralWaveToFrames()
            }) {
                HStack {
                    Image(systemName: "film.stack")
                    Text("⚡ BAKE INTO 12 KEYFRAMES FOR EDITING")
                }
                .font(.system(size: 7, weight: .heavy, design: .monospaced))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 6)
                .background(Color(red: 0.92, green: 0.52, blue: 0.16))
                .foregroundColor(.black)
                .clipShape(RoundedRectangle(cornerRadius: 6))
            }
            .buttonStyle(.plain)

            Button(action: {
                engine.editingCustomFX.isProcedural = true
                engine.saveEditingCustomFXToLibrary()
            }) {
                Text("💾 SAVE PROCEDURAL SHOW TO MY SHOWS")
                    .font(.system(size: 7, weight: .heavy, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 5)
                    .background(Color(red: 0.16, green: 0.45, blue: 0.34))
                    .foregroundColor(Color(red: 0.4, green: 0.98, blue: 0.78))
                    .clipShape(RoundedRectangle(cornerRadius: 6))
            }
            .buttonStyle(.plain)
        }
    }

    private var rightWingSavedLibraryContent: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                Button("+ NEW BLANK SHOW") {
                    engine.createNewBlankCustomFX()
                    engine.rightWingTab = "SEQUENCER"
                }
                .font(.system(size: 7, weight: .heavy, design: .monospaced))
                .padding(.horizontal, 7)
                .padding(.vertical, 4)
                .background(engine.currentTheme.accentPrimary)
                .foregroundColor(.black)
                .clipShape(RoundedRectangle(cornerRadius: 5))
                .buttonStyle(.plain)

                Spacer()

                Button("⤓ EXPORT") { engine.exportCustomFXToClipboard() }
                    .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                    .padding(.horizontal, 5).padding(.vertical, 4)
                    .background(Color.white.opacity(0.1)).foregroundColor(.white).clipShape(RoundedRectangle(cornerRadius: 4)).buttonStyle(.plain)

                Button("⤒ IMPORT") { engine.importCustomFXFromClipboard() }
                    .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                    .padding(.horizontal, 5).padding(.vertical, 4)
                    .background(Color.white.opacity(0.1)).foregroundColor(.white).clipShape(RoundedRectangle(cornerRadius: 4)).buttonStyle(.plain)
            }

            ScrollView {
                VStack(spacing: 5) {
                    ForEach(engine.customEffects) { fx in
                        let isRunning = engine.isPlayingCustomFX && engine.activeCustomFXName == fx.name
                        HStack(spacing: 6) {
                            VStack(alignment: .leading, spacing: 2) {
                                Text(fx.name)
                                    .font(.system(size: 8, weight: .heavy, design: .monospaced))
                                    .foregroundColor(.white)
                                    .lineLimit(1)
                                Text(fx.isProcedural ? "WAVE SYNTH // \(fx.waveShape.rawValue)" : "\(fx.frames.count) FRAMES // \(Int(fx.fps)) FPS // \(fx.loopMode.rawValue)")
                                    .font(.system(size: 6.5, design: .monospaced))
                                    .foregroundColor(Color(white: 0.6))
                            }
                            Spacer()
                            Button(isRunning ? "⏹" : "▶️ RUN") {
                                if isRunning {
                                    engine.stopCustomEffect()
                                } else {
                                    engine.playCustomEffect(fx)
                                }
                            }
                            .font(.system(size: 7, weight: .heavy, design: .monospaced))
                            .padding(.horizontal, 6)
                            .padding(.vertical, 4)
                            .background(isRunning ? Color.red : engine.currentTheme.accentPrimary)
                            .foregroundColor(isRunning ? .white : .black)
                            .clipShape(RoundedRectangle(cornerRadius: 4))
                            .buttonStyle(.plain)

                            Button("✏️") {
                                engine.selectCustomFXForEditing(fx)
                            }
                            .font(.system(size: 7, design: .monospaced))
                            .padding(4)
                            .background(Color.white.opacity(0.12))
                            .clipShape(RoundedRectangle(cornerRadius: 4))
                            .buttonStyle(.plain)

                            Button("🗑️") {
                                engine.deleteSavedCustomFX(fx)
                            }
                            .font(.system(size: 7, design: .monospaced))
                            .padding(4)
                            .background(Color.red.opacity(0.22))
                            .clipShape(RoundedRectangle(cornerRadius: 4))
                            .buttonStyle(.plain)
                        }
                        .padding(7)
                        .background(isRunning ? engine.currentTheme.accentPrimary.opacity(0.18) : Color.black.opacity(0.45))
                        .clipShape(RoundedRectangle(cornerRadius: 6))
                        .overlay(RoundedRectangle(cornerRadius: 6).stroke(isRunning ? engine.currentTheme.accentPrimary : Color.white.opacity(0.1), lineWidth: 1))
                    }
                }
            }
            .frame(height: 196)
        }
    }

    // ------------------------------------------------------------------------
    // RIGHT WING (RED BOX #4): REACTIVE MECHANICAL KEYBOARD KEY-PRESS FX ENGINE
    // ------------------------------------------------------------------------
    private var rightWingReactiveKeyFXPanel: some View {
        let isEnabled = (engine.reactiveKeyFXMode != .off)
        let activeColor = engine.reactiveFXRainbowColor ? engine.currentTheme.accentPrimary : ledColor(engine.reactiveFXColorVelocity)

        return VStack(alignment: .leading, spacing: 6) {
            // Header & Live Pulse Status
            HStack(spacing: 5) {
                Image(systemName: "waveform.circle.fill")
                    .font(.system(size: 10.5, weight: .bold))
                    .foregroundColor(isEnabled ? activeColor : Color(white: 0.5))
                    .shadow(color: isEnabled ? activeColor.opacity(0.85) : .clear, radius: 4)

                Text("REACTIVE KEY FX // LIVE")
                    .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)

                Spacer()

                Button(action: {
                    if engine.reactiveKeyFXMode == .off {
                        engine.reactiveKeyFXMode = .ripple
                    } else {
                        engine.reactiveKeyFXMode = .off
                        engine.activeReactivePulses.removeAll()
                        engine.restorePresetLEDs()
                    }
                    SoundEffect.knobTick.play(with: engine.soundPack)
                }) {
                    HStack(spacing: 3) {
                        Circle()
                            .fill(isEnabled ? Color.green : Color(white: 0.4))
                            .frame(width: 6, height: 6)
                            .shadow(color: isEnabled ? Color.green : .clear, radius: 3)
                        Text(isEnabled ? "ACTIVE" : "OFF")
                            .font(.system(size: 7, weight: .heavy, design: .monospaced))
                    }
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2.5)
                    .background(isEnabled ? Color.green.opacity(0.18) : Color.white.opacity(0.08))
                    .foregroundColor(isEnabled ? Color.green : Color(white: 0.6))
                    .clipShape(RoundedRectangle(cornerRadius: 4))
                }
                .buttonStyle(.plain)
            }

            // 6 Interactive Mechanical Keyboard FX Mode Pads (2x3 Grid)
            let modes: [ReactiveKeyFXMode] = [.ripple, .crossLaser, .supernova, .heatTrail, .meteorRain, .arcLightning]
            LazyVGrid(columns: [GridItem(.flexible(), spacing: 5), GridItem(.flexible(), spacing: 5), GridItem(.flexible(), spacing: 5)], spacing: 5) {
                ForEach(modes) { mode in
                    let isSel = (engine.reactiveKeyFXMode == mode)
                    Button(action: {
                        engine.reactiveKeyFXMode = mode
                        engine.triggerRandomDemoReactiveFX()
                    }) {
                        VStack(spacing: 3) {
                            Image(systemName: mode.icon)
                                .font(.system(size: 10.5, weight: .bold))
                                .foregroundColor(isSel ? .black : activeColor)
                            Text(mode.rawValue)
                                .font(.system(size: 6, weight: .heavy, design: .monospaced))
                                .foregroundColor(isSel ? .black : .white)
                                .lineLimit(1)
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 5.5)
                        .background(
                            RoundedRectangle(cornerRadius: 6)
                                .fill(isSel ? activeColor : Color.black.opacity(0.5))
                        )
                        .overlay(
                            RoundedRectangle(cornerRadius: 6)
                                .stroke(isSel ? Color.white.opacity(0.75) : activeColor.opacity(0.28), lineWidth: isSel ? 1.3 : 0.8)
                        )
                        .shadow(color: isSel ? activeColor.opacity(0.55) : .clear, radius: 4)
                    }
                    .buttonStyle(.plain)
                }
            }

            // Wave Speed & Color Mode Selector (Rainbow Dynamic vs Solid Neon Swatches)
            VStack(alignment: .leading, spacing: 4) {
                HStack(spacing: 6) {
                    Text("WAVE SPEED:")
                        .font(.system(size: 6.5, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.7))
                    Slider(value: $engine.reactiveFXSpeed, in: 0.4...3.0, step: 0.1)
                        .accentColor(activeColor)
                    Text(String(format: "%.1fx", engine.reactiveFXSpeed))
                        .font(.system(size: 7, weight: .heavy, design: .monospaced))
                        .foregroundColor(activeColor)
                        .frame(width: 24, alignment: .trailing)
                }

                HStack(spacing: 4) {
                    Button(action: {
                        engine.reactiveFXRainbowColor = true
                        engine.triggerRandomDemoReactiveFX()
                    }) {
                        HStack(spacing: 2) {
                            Text("🌈")
                                .font(.system(size: 7))
                            Text("PRISM")
                                .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                        }
                        .padding(.horizontal, 5)
                        .padding(.vertical, 3)
                        .background(engine.reactiveFXRainbowColor ? engine.currentTheme.accentPrimary : Color.white.opacity(0.1))
                        .foregroundColor(engine.reactiveFXRainbowColor ? .black : .white)
                        .clipShape(RoundedRectangle(cornerRadius: 4))
                    }
                    .buttonStyle(.plain)

                    let colorSwatches: [UInt8] = [5, 14, 24, 52, 68, 84, 98, 112, 127]
                    ForEach(colorSwatches, id: \.self) { vel in
                        let isPicked = (!engine.reactiveFXRainbowColor && engine.reactiveFXColorVelocity == vel)
                        Circle()
                            .fill(ledColor(vel))
                            .frame(width: 13, height: 13)
                            .overlay(
                                Circle()
                                    .stroke(Color.white, lineWidth: isPicked ? 1.6 : 0)
                            )
                            .shadow(color: isPicked ? ledColor(vel) : .clear, radius: 3)
                            .onTapGesture {
                                engine.reactiveFXRainbowColor = false
                                engine.reactiveFXColorVelocity = vel
                                engine.triggerRandomDemoReactiveFX()
                            }
                    }
                    Spacer(minLength: 0)
                }
            }
            .padding(5)
            .background(Color.black.opacity(0.4))
            .clipShape(RoundedRectangle(cornerRadius: 6))

            // Interactive Test Pulse Button + Live Active Wave Counter
            HStack(spacing: 6) {
                Button(action: {
                    engine.triggerRandomDemoReactiveFX()
                }) {
                    HStack(spacing: 4) {
                        Image(systemName: "hand.tap.fill")
                        Text("TEST RIPPLE PULSE")
                    }
                    .font(.system(size: 7, weight: .heavy, design: .monospaced))
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 5)
                    .background(activeColor.opacity(0.24))
                    .foregroundColor(activeColor)
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                    .overlay(RoundedRectangle(cornerRadius: 5).stroke(activeColor.opacity(0.55), lineWidth: 1))
                }
                .buttonStyle(.plain)

                Text(engine.activeReactivePulses.isEmpty ? "PRESS ANY KEY" : "⚡ \(engine.activeReactivePulses.count) WAVES")
                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.activeReactivePulses.isEmpty ? Color(white: 0.55) : Color.yellow)
                    .padding(.horizontal, 6)
                    .padding(.vertical, 5)
                    .background(Color.black.opacity(0.5))
                    .clipShape(RoundedRectangle(cornerRadius: 5))
            }
        }
        .padding(9)
        .background(Color.black.opacity(0.46))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(isEnabled ? activeColor.opacity(0.75) : engine.currentTheme.sectionOutline.opacity(0.5), lineWidth: 1.2)
        )
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    // --- WING HELPER VIEWS ---
    private func wingTabButton(title: String, active: Bool, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(title)
                .font(.system(size: 7, weight: .heavy, design: .monospaced))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 5)
                .background(active ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                .foregroundColor(active ? .black : Color(white: 0.78))
                .clipShape(RoundedRectangle(cornerRadius: 5))
        }
        .buttonStyle(.plain)
    }

    private func filterTogglePill(label: String, isOn: Binding<Bool>) -> some View {
        Button(action: { isOn.wrappedValue.toggle() }) {
            HStack(spacing: 3) {
                Image(systemName: isOn.wrappedValue ? "checkmark.circle.fill" : "circle")
                    .font(.system(size: 7.5))
                Text(label)
                    .font(.system(size: 6.5, weight: .bold, design: .monospaced))
            }
            .frame(maxWidth: .infinity)
            .padding(.vertical, 4)
            .background(isOn.wrappedValue ? engine.currentTheme.accentPrimary.opacity(0.22) : Color.white.opacity(0.06))
            .foregroundColor(isOn.wrappedValue ? engine.currentTheme.accentPrimary : Color(white: 0.5))
            .clipShape(RoundedRectangle(cornerRadius: 4))
        }
        .buttonStyle(.plain)
    }

    private func macroTriggerPad(title: String, icon: String, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            VStack(spacing: 4) {
                Image(systemName: icon)
                    .font(.system(size: 12, weight: .bold))
                    .foregroundColor(engine.currentTheme.accentPrimary)
                Text(title)
                    .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(.white)
                    .multilineTextAlignment(.center)
                    .lineLimit(2)
            }
            .frame(maxWidth: .infinity)
            .padding(.vertical, 10)
            .padding(.horizontal, 4)
            .background(Color.black.opacity(0.45))
            .clipShape(RoundedRectangle(cornerRadius: 7))
            .overlay(RoundedRectangle(cornerRadius: 7).stroke(engine.currentTheme.accentPrimary.opacity(0.35), lineWidth: 1))
        }
        .buttonStyle(.plain)
    }

    private func miniCanvasKey(keyIndex: Int) -> some View {
        let safeFrame = min(max(0, engine.editingFrameIndex), max(0, engine.editingCustomFX.frames.count - 1))
        let vel: UInt8 = (!engine.editingCustomFX.frames.isEmpty && keyIndex < engine.editingCustomFX.frames[safeFrame].count)
            ? engine.editingCustomFX.frames[safeFrame][keyIndex]
            : 0
        return Button(action: {
            engine.paintCustomFXKey(keyIndex: keyIndex, velocity: engine.customBrushVelocity)
        }) {
            RoundedRectangle(cornerRadius: 3)
                .fill(vel == 0 ? Color(white: 0.14) : ledColor(vel))
                .frame(width: 17, height: 15)
                .overlay(
                    RoundedRectangle(cornerRadius: 3)
                        .stroke(vel > 0 ? Color.white.opacity(0.6) : Color.white.opacity(0.15), lineWidth: 0.8)
                )
        }
        .buttonStyle(.plain)
    }

    private func miniToolBtn(_ title: String, action: @escaping () -> Void) -> some View {
        Button(action: action) {
            Text(title)
                .font(.system(size: 6.5, weight: .heavy, design: .monospaced))
                .frame(maxWidth: .infinity)
                .padding(.vertical, 4)
                .background(Color.white.opacity(0.1))
                .foregroundColor(.white)
                .clipShape(RoundedRectangle(cornerRadius: 4))
        }
        .buttonStyle(.plain)
    }

    // ------------------------------------------------------------------------
    // SECTION 1: TOP-LEFT 8X4 KEY GRID (32 KEYS)
    // ------------------------------------------------------------------------
    private var section1KeyGrid8x4: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text("1. TOP-LEFT 8X4 KEY GRID (32 KEYS)")
                    .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                Spacer()
                Text("CLICK KEY OR PRESS BITCHBOY")
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(red: 0.15, green: 0.85, blue: 0.95))
            }
            .frame(width: 410)

            VStack(spacing: 4) {
                ForEach(0..<4, id: \.self) { r in
                    HStack(spacing: 4) {
                        ForEach(0..<8, id: \.self) { c in
                            let idx = r * 8 + c
                            HardwareKeyCap(
                                item: engine.controls[idx],
                                isSelected: engine.selectedIndices.contains(idx) || engine.selectedIndex == idx,
                                color: ledColor(engine.controls[idx].currentLED),
                                sectionBorder: engine.currentTheme.sectionOutline,
                                onTap: {
                                    let isShift = NSEvent.modifierFlags.contains(.shift)
                                    engine.handleKeyPressWithModifiers(index: idx, isShiftDown: isShift)
                                }
                            )
                        }
                    }
                }
            }
            .padding(6)
            .background(Color(red: 0.08, green: 0.04, blue: 0.04).opacity(0.8))
            .overlay(
                RoundedRectangle(cornerRadius: 10)
                    .stroke(engine.currentTheme.sectionOutline.opacity(0.85), lineWidth: 1.2)
            )
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
        .frame(width: 410)
    }

    // ------------------------------------------------------------------------
    // SECTION 2: MIDDLE 1X8 KEY STRIP (8 KEYS)
    // ------------------------------------------------------------------------
    private var section2KeyStrip1x8: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text("2. MIDDLE 1X8 KEY STRIP (8 KEYS)")
                    .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                Spacer()
                Text("ICON SHOWS MAPPED ACTION")
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(red: 0.96, green: 0.78, blue: 0.35))
            }
            .frame(width: 410)

            HStack(spacing: 4) {
                ForEach(32..<40, id: \.self) { idx in
                    HardwareKeyCap(
                        item: engine.controls[idx],
                        isSelected: engine.selectedIndices.contains(idx) || engine.selectedIndex == idx,
                        color: ledColor(engine.controls[idx].currentLED),
                        sectionBorder: engine.currentTheme.sectionOutline,
                        onTap: {
                            let isShift = NSEvent.modifierFlags.contains(.shift)
                            engine.handleKeyPressWithModifiers(index: idx, isShiftDown: isShift)
                        }
                    )
                }
            }
            .padding(6)
            .background(Color(red: 0.08, green: 0.04, blue: 0.04).opacity(0.8))
            .overlay(
                RoundedRectangle(cornerRadius: 10)
                    .stroke(engine.currentTheme.sectionOutline.opacity(0.85), lineWidth: 1.2)
            )
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
        .frame(width: 410)
    }

    // ------------------------------------------------------------------------
    // SECTION 4: BOTTOM-LEFT 8 VERTICAL FADERS (CC 0–7)
    // ------------------------------------------------------------------------
    private var section4VerticalFaders: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                Text("4. BOTTOM-LEFT 8 VERTICAL FADERS (CC 0–7)")
                    .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                Spacer()
                Button(action: {
                    engine.vuMeterLEDsEnabled.toggle()
                    if engine.vuMeterLEDsEnabled {
                        engine.applyFXMode(.vuMeter)
                    } else {
                        engine.applyFXMode(.staticFX)
                    }
                }) {
                    HStack(spacing: 3) {
                        Image(systemName: "slider.vertical.3")
                        Text(engine.vuMeterLEDsEnabled ? "VU METER LEDS: ON" : "VU METER LEDS: OFF")
                    }
                    .font(.system(size: 7, weight: .bold, design: .monospaced))
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2)
                    .background(Color.black.opacity(0.5))
                    .foregroundColor(engine.vuMeterLEDsEnabled ? Color(red: 0.22, green: 0.96, blue: 0.76) : Color(white: 0.6))
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                    .overlay(RoundedRectangle(cornerRadius: 5).stroke(Color(red: 0.22, green: 0.96, blue: 0.76).opacity(0.5), lineWidth: 1))
                }
                .buttonStyle(.plain)
            }
            .frame(width: 410)

            HStack(spacing: 4) {
                ForEach(49..<57, id: \.self) { idx in
                    HardwareVerticalFader(
                        item: engine.controls[idx],
                        isSelected: engine.selectedIndex == idx,
                        onSelect: { engine.selectedIndex = idx },
                        onValueChange: { val in engine.setControlLiveValue(index: idx, value: val) }
                    )
                }
            }
            .padding(6)
            .background(Color.black.opacity(0.35))
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
        .frame(width: 410)
    }

    // ------------------------------------------------------------------------
    // IN-CHASSIS NOTES & SHORTCUTS
    // ------------------------------------------------------------------------
    private var inChassisUserNotes: some View {
        VStack(alignment: .leading, spacing: 6) {
            HStack {
                Text("USER NOTES")
                    .font(.system(size: 9.5, weight: .heavy, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)
                    .tracking(1.0)

                Spacer()

                HStack(spacing: 4) {
                    inChassisTabBtn(title: "CUES", tab: "NOTES")
                    inChassisTabBtn(title: "PALETTE", tab: "CUSTOMIZE")
                    inChassisTabBtn(title: "FX SHOWS", tab: "FX")
                    inChassisTabBtn(title: "TERMINAL", tab: "TERMINAL")
                }
            }

            if engine.inChassisTab == "NOTES" {
                TextField("Click to enter show notes, cue reminders, or stage cues...", text: $engine.userNotesText)
                    .font(.system(size: 8.5, design: .monospaced))
                    .textFieldStyle(.plain)
                    .padding(6)
                    .background(Color.black.opacity(0.5))
                    .clipShape(RoundedRectangle(cornerRadius: 6))
                    .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color.white.opacity(0.12), lineWidth: 1))
            } else if engine.inChassisTab == "CUSTOMIZE" {
                HStack(spacing: 6) {
                    Text("LED COLOR:")
                        .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.65))

                    Picker("LED Palette", selection: $engine.ledPalette) {
                        ForEach(LEDPaletteMode.allCases) { pal in Text(pal.rawValue).tag(pal) }
                    }
                    .pickerStyle(.menu)
                    .font(.system(size: 8, design: .monospaced))
                }
                .padding(4)
                .background(Color.black.opacity(0.4))
                .clipShape(RoundedRectangle(cornerRadius: 6))
            } else if engine.inChassisTab == "FX" {
                VStack(spacing: 4) {
                    HStack(spacing: 6) {
                        Image(systemName: engine.currentFXMode.icon)
                            .font(.system(size: 11))
                            .foregroundColor(engine.currentTheme.accentPrimary)

                        Picker("Light Show", selection: Binding(
                            get: { engine.currentFXMode },
                            set: { mode in engine.applyFXMode(mode) }
                        )) {
                            ForEach(LEDFXMode.allCases) { mode in
                                Text(mode.rawValue).tag(mode)
                            }
                        }
                        .pickerStyle(.menu)
                        .font(.system(size: 8, weight: .bold, design: .monospaced))

                        Spacer()

                        Button(action: {
                            engine.applyFXMode(.staticFX)
                        }) {
                            Text("STOP FX")
                                .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                                .padding(.horizontal, 6)
                                .padding(.vertical, 2)
                                .background(Color.red.opacity(0.35))
                                .foregroundColor(Color(red: 1.0, green: 0.45, blue: 0.45))
                                .clipShape(RoundedRectangle(cornerRadius: 4))
                                .overlay(RoundedRectangle(cornerRadius: 4).stroke(Color.red.opacity(0.6), lineWidth: 1))
                        }
                        .buttonStyle(.plain)
                    }

                    HStack(spacing: 6) {
                        Text("SPEED:")
                            .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                            .foregroundColor(Color(white: 0.65))

                        Slider(
                            value: $engine.fxSpeed,
                            in: 0.4...2.5,
                            step: 0.1
                        )
                        .accentColor(engine.currentTheme.accentPrimary)

                        Text(String(format: "%.1fx", engine.fxSpeed))
                            .font(.system(size: 8, weight: .bold, design: .monospaced))
                            .foregroundColor(engine.currentTheme.accentPrimary)
                            .frame(width: 32)
                    }
                }
                .padding(4)
                .background(Color.black.opacity(0.4))
                .clipShape(RoundedRectangle(cornerRadius: 6))
            } else if engine.inChassisTab == "TERMINAL" {
                ScrollView {
                    VStack(alignment: .leading, spacing: 1) {
                        ForEach(engine.midiLogs.prefix(6), id: \.self) { log in
                            Text(log)
                                .font(.system(size: 7.5, design: .monospaced))
                                .foregroundColor(Color(red: 0.25, green: 0.92, blue: 0.72))
                        }
                    }
                    .frame(maxWidth: .infinity, alignment: .leading)
                }
                .frame(height: 38)
            }
        }
        .padding(8)
        .frame(width: 408)
        .background(Color.black.opacity(0.35))
        .clipShape(RoundedRectangle(cornerRadius: 8))
        .overlay(RoundedRectangle(cornerRadius: 8).stroke(Color.white.opacity(0.1), lineWidth: 1))
    }

    private func inChassisTabBtn(title: String, tab: String) -> some View {
        Button(action: { engine.inChassisTab = tab }) {
            Text(title)
                .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                .padding(.horizontal, 6)
                .padding(.vertical, 2)
                .background(engine.inChassisTab == tab ? engine.currentTheme.accentPrimary : Color.white.opacity(0.08))
                .foregroundColor(engine.inChassisTab == tab ? .black : Color.white.opacity(0.7))
                .clipShape(RoundedRectangle(cornerRadius: 4))
        }
        .buttonStyle(.plain)
    }

    // ------------------------------------------------------------------------
    // SECTION 5: 4 HORIZONTAL FADERS (CC 8–11)
    // ------------------------------------------------------------------------
    private var section5HorizontalFaders: some View {
        VStack(alignment: .leading, spacing: 5) {
            Text("5. 4 HORIZONTAL FADERS (CC 8–11)")
                .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            VStack(spacing: 3) {
                ForEach(57..<61, id: \.self) { idx in
                    HardwareHorizontalFader(
                        item: engine.controls[idx],
                        isSelected: engine.selectedIndex == idx,
                        onSelect: { engine.selectedIndex = idx },
                        onValueChange: { val in engine.setControlLiveValue(index: idx, value: val) }
                    )
                }
            }
            .padding(6)
            .background(Color.black.opacity(0.35))
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
    }

    // ------------------------------------------------------------------------
    // SECTION 6: 2X4 ROTARY KNOBS (CC 20–27)
    // ------------------------------------------------------------------------
    private var section6RotaryKnobs: some View {
        VStack(alignment: .leading, spacing: 5) {
            Text("6. 2X4 KNOBS (CC 20–27)")
                .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            VStack(spacing: 4) {
                ForEach(0..<4, id: \.self) { r in
                    HStack(spacing: 8) {
                        ForEach(0..<2, id: \.self) { c in
                            let idx = 61 + r * 2 + c
                            HardwareKnob(
                                item: engine.controls[idx],
                                isSelected: engine.selectedIndex == idx,
                                onSelect: { engine.selectedIndex = idx },
                                onValueChange: { val in engine.setControlLiveValue(index: idx, value: val) }
                            )
                        }
                    }
                }
            }
            .padding(6)
            .background(Color.black.opacity(0.35))
            .clipShape(RoundedRectangle(cornerRadius: 10))
        }
    }

    // ------------------------------------------------------------------------
    // CENTER INSPECTOR HUD
    // ------------------------------------------------------------------------
    private var centerInspectorHUD: some View {
        ZStack {
            // Inspector Bezel with screws
            RoundedRectangle(cornerRadius: 12)
                .fill(Color(red: 0.05, green: 0.10, blue: 0.09))
                .overlay(
                    RoundedRectangle(cornerRadius: 12)
                        .stroke(
                            LinearGradient(
                                colors: [Color(white: 0.95), Color(white: 0.75)],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            ),
                            lineWidth: 3
                        )
                )

            // 4 corner screws
            VStack {
                HStack {
                    ScrewRivetView(size: 9)
                    Spacer()
                    ScrewRivetView(size: 9)
                }
                Spacer()
                HStack {
                    ScrewRivetView(size: 9)
                    Spacer()
                    ScrewRivetView(size: 9)
                }
            }
            .padding(4)

            // Inspector Content
            VStack(alignment: .leading, spacing: 6) {
                // Header badge
                HStack {
                    Text("\(selectedControl.label) // N\(selectedControl.number)")
                        .font(.system(size: 9, weight: .heavy, design: .monospaced))
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(engine.currentTheme.accentPrimary)
                        .foregroundColor(.black)
                        .clipShape(RoundedRectangle(cornerRadius: 4))

                    Spacer()

                    // Multi-select indicator badge
                    if engine.selectedIndices.count > 1 {
                        Text("\(engine.selectedIndices.count) SELECTED")
                            .font(.system(size: 7.5, weight: .heavy, design: .monospaced))
                            .padding(.horizontal, 5)
                            .padding(.vertical, 2)
                            .background(Color.cyan)
                            .foregroundColor(.black)
                            .clipShape(Capsule())
                    }

                    // Key color swatch preview
                    RoundedRectangle(cornerRadius: 4)
                        .fill(ledColor(selectedControl.currentLED))
                        .frame(width: 16, height: 16)
                        .overlay(RoundedRectangle(cornerRadius: 4).stroke(Color.white.opacity(0.4), lineWidth: 1))
                }

                // Custom Name Editor
                HStack(spacing: 4) {
                    Text("NAME:")
                        .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.6))
                    TextField("Custom Name", text: $engine.controls[engine.selectedIndex].customName)
                        .font(.system(size: 8, weight: .bold, design: .monospaced))
                        .textFieldStyle(.plain)
                        .padding(.horizontal, 4)
                        .padding(.vertical, 2)
                        .background(Color.black.opacity(0.5))
                        .clipShape(RoundedRectangle(cornerRadius: 4))
                }

                // Subtitle
                Text(selectedControl.zone)
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .foregroundColor(engine.currentTheme.headerText)

                // Feedback Mode Row
                HStack {
                    Text("Mode: \(selectedControl.feedbackMode.rawValue) // \(selectedControl.actionType.rawValue)")
                        .font(.system(size: 8, weight: .semibold, design: .monospaced))
                        .foregroundColor(Color(red: 0.96, green: 0.78, blue: 0.35))
                    Spacer()
                }

                if selectedControl.hasLED {
                    // Idle Velocity Slider (Batch updates all selected!)
                    VStack(alignment: .leading, spacing: 2) {
                        HStack {
                            Text(engine.selectedIndices.count > 1 ? "IDLE LED VELOCITY (BATCH):" : "IDLE LED VELOCITY:")
                                .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                                .foregroundColor(Color(white: 0.7))
                            Spacer()
                            Text(String(format: "%03d", engine.controls[engine.selectedIndex].idleVelocity))
                                .font(.system(size: 8, weight: .heavy, design: .monospaced))
                                .foregroundColor(engine.currentTheme.accentPrimary)
                        }
                        Slider(
                            value: Binding(
                                get: { Double(engine.controls[engine.selectedIndex].idleVelocity) },
                                set: { v in
                                    let u = UInt8(v)
                                    let targets = engine.selectedIndices.count > 1 ? engine.selectedIndices : [engine.selectedIndex]
                                    for idx in targets where engine.controls[idx].hasLED {
                                        engine.controls[idx].idleVelocity = u
                                        engine.controls[idx].currentLED = u
                                        engine.sendNoteOn(note: engine.controls[idx].number, velocity: u)
                                    }
                                    engine.saveCurrentSlot()
                                }
                            ),
                            in: 0...127,
                            step: 1
                        )
                        .accentColor(Color(red: 0.85, green: 0.35, blue: 0.30))
                    }

                    // Pressed Velocity Slider (Batch updates all selected!)
                    VStack(alignment: .leading, spacing: 2) {
                        HStack {
                            Text(engine.selectedIndices.count > 1 ? "PRESSED LED VELOCITY (BATCH):" : "PRESSED LED VELOCITY:")
                                .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                                .foregroundColor(Color(white: 0.7))
                            Spacer()
                            Text(String(format: "%03d", engine.controls[engine.selectedIndex].activeVelocity))
                                .font(.system(size: 8, weight: .heavy, design: .monospaced))
                                .foregroundColor(Color(red: 0.96, green: 0.78, blue: 0.35))
                        }
                        Slider(
                            value: Binding(
                                get: { Double(engine.controls[engine.selectedIndex].activeVelocity) },
                                set: { v in
                                    let u = UInt8(v)
                                    let targets = engine.selectedIndices.count > 1 ? engine.selectedIndices : [engine.selectedIndex]
                                    for idx in targets where engine.controls[idx].hasLED {
                                        engine.controls[idx].activeVelocity = u
                                    }
                                    engine.saveCurrentSlot()
                                }
                            ),
                            in: 0...127,
                            step: 1
                        )
                        .accentColor(Color(red: 0.85, green: 0.35, blue: 0.30))
                    }

                    // Large Fill Buttons
                    HStack(spacing: 4) {
                        Button("FILL 8x4") { engine.setZoneLEDs(range: 0..<32, velocity: selectedControl.idleVelocity) }
                        Button("FILL 1x8") { engine.setZoneLEDs(range: 32..<40, velocity: selectedControl.idleVelocity) }
                        Button("FILL 3x3") { engine.setZoneLEDs(range: 40..<49, velocity: selectedControl.idleVelocity) }
                        Button("ALL 49") { engine.setZoneLEDs(range: 0..<49, velocity: selectedControl.idleVelocity) }
                    }
                    .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                    .buttonStyle(.plain)
                    .foregroundColor(Color(red: 0.96, green: 0.78, blue: 0.35))
                    .padding(.horizontal, 4)
                    .padding(.vertical, 3)
                    .background(Color.black.opacity(0.4))
                    .clipShape(RoundedRectangle(cornerRadius: 5))
                } else {
                    Text("ANALOG CC VALUE: \(selectedControl.liveValue)")
                        .font(.system(size: 8, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(red: 0.96, green: 0.78, blue: 0.35))
                        .padding(.vertical, 10)
                }

                Spacer(minLength: 0)

                // RX Stream Telemetry
                HStack {
                    Text("RX STREAM:")
                        .font(.system(size: 7, weight: .bold, design: .monospaced))
                        .foregroundColor(Color(white: 0.5))
                    Spacer()
                    Text(engine.lastStreamMessage)
                        .font(.system(size: 7.5, weight: .bold, design: .monospaced))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                }
            }
            .padding(10)
        }
    }

    // ------------------------------------------------------------------------
    // SECTION 3: 3X3 KEYPAD & OFFICIAL MASCOT (9 KEYS)
    // ------------------------------------------------------------------------
    private var section3KeypadAndMascot: some View {
        VStack(spacing: 5) {
            // Interactive Official Mascot (bitchboy.png) with Speech Bubble
            ZStack(alignment: .top) {
                // Speech Bubble
                if engine.mascotSpeechVisible {
                    Text(engine.mascotSpeech)
                        .font(.system(size: 7, weight: .heavy, design: .monospaced))
                        .padding(.horizontal, 6)
                        .padding(.vertical, 3)
                        .background(Color.black.opacity(0.85))
                        .foregroundColor(engine.currentTheme.accentPrimary)
                        .clipShape(Capsule())
                        .overlay(Capsule().stroke(engine.currentTheme.accentPrimary, lineWidth: 1))
                        .offset(y: -18)
                        .transition(.scale.combined(with: .opacity))
                }

                BitchBoyOfficialMascotView(auraColor: engine.currentTheme.accentPrimary) {
                    engine.triggerMascotTap()
                }
                .scaleEffect(engine.mascotScale)
                .animation(.spring(response: 0.25, dampingFraction: 0.5), value: engine.mascotScale)
            }
            .padding(.top, 12)

            Text("3. 3X3 KEYPAD (9 KEYS)")
                .font(.system(size: 8.5, weight: .heavy, design: .monospaced))
                .foregroundColor(engine.currentTheme.headerText)

            // 3x3 Grid
            VStack(spacing: 4) {
                ForEach(0..<3, id: \.self) { r in
                    HStack(spacing: 4) {
                        ForEach(0..<3, id: \.self) { c in
                            let idx = 40 + r * 3 + c
                            HardwareKeyCap(
                                item: engine.controls[idx],
                                isSelected: engine.selectedIndices.contains(idx) || engine.selectedIndex == idx,
                                color: ledColor(engine.controls[idx].currentLED),
                                sectionBorder: engine.currentTheme.sectionOutline,
                                onTap: {
                                    let isShift = NSEvent.modifierFlags.contains(.shift)
                                    engine.handleKeyPressWithModifiers(index: idx, isShiftDown: isShift)
                                }
                            )
                        }
                    }
                }
            }
            .padding(6)
            .background(Color(red: 0.08, green: 0.04, blue: 0.04).opacity(0.8))
            .overlay(
                RoundedRectangle(cornerRadius: 10)
                    .stroke(engine.currentTheme.sectionOutline.opacity(0.85), lineWidth: 1.2)
            )
            .clipShape(RoundedRectangle(cornerRadius: 10))

            // Footer branding
            VStack(spacing: 1) {
                Text("CE 🗑")
                    .font(.system(size: 8, weight: .bold))
                    .foregroundColor(Color(white: 0.5))
                Text("BitchBoy")
                    .font(.system(size: 8, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(white: 0.6))
                Text("BLOCK SYSTEM HARDWARE")
                    .font(.system(size: 6, weight: .bold, design: .monospaced))
                    .foregroundColor(Color(white: 0.4))
            }
        }
    }
}

// ============================================================================
// 5. APP ENTRYPOINT & MENUBAR COMPANION
// ============================================================================

@main
struct BitchBoyNativeApp: App {
    @StateObject private var engine = BitchBoyEngine()

    var body: some Scene {
        WindowGroup("BitchManager") {
            ContentView(engine: engine)
                .frame(minWidth: 1538 * 0.15, minHeight: 760 * 0.15)
        }
        .commands {
            CommandMenu("UI Zoom & Window") {
                Button(engine.isWindowAlwaysOnTop ? "✓ Keep Window Always On Top" : "Keep Window Always On Top") {
                    engine.toggleWindowAlwaysOnTop()
                }
                .keyboardShortcut("t", modifiers: [.command, .option])

                Divider()

                Button("Zoom In (+10%)") {
                    engine.zoomInUI()
                }
                .keyboardShortcut("+", modifiers: [.command])

                Button("Zoom Out (-10%)") {
                    engine.zoomOutUI()
                }
                .keyboardShortcut("-", modifiers: [.command])

                Button("Auto-Fit to Window") {
                    engine.enableAutoFitWindow()
                }
                .keyboardShortcut("0", modifiers: [.command])
            }
        }
        MenuBarExtra {
            Text(engine.isConnected ? "ONLINE: \(engine.deviceName)" : "OFFLINE // UNPLUGGED")
            Divider()
            Button(engine.isWindowAlwaysOnTop ? "📌 Unpin Always On Top" : "📌 Pin Window Always On Top") {
                engine.toggleWindowAlwaysOnTop()
            }
            Button("✨ Trigger Rainbow Refresh") { engine.runRainbowWaveRefresh() }
            Button("🚨 Panic Blackout") { engine.blackoutPanic() }
            Divider()
            ForEach(0..<4, id: \.self) { idx in
                Button("Load \(engine.slots[idx].name)") { engine.selectSlot(idx) }
            }
            Divider()
            Button("Quit BitchManager") { NSApplication.shared.terminate(nil) }
        } label: {
            Image("BitchBoyMenuBarIcon")
                .renderingMode(.template)
        }
    }
}

// ============================================================================
// 6. XCODE CANVAS PREVIEW
// ============================================================================

#Preview("Full Console UI") {
    ContentView(engine: BitchBoyEngine())
        .frame(width: 1538, height: 760)
}
