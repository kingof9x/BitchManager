// BitchManager Types & Hardware Specification

export type ControlKind = 'key' | 'vfader' | 'hfader' | 'knob';
export type FeedbackMode = 'MOMENTARY' | 'TOGGLE' | 'STATIC';
export type ActionType = 'PASSTHROUGH' | 'QWERTY_MACRO' | 'OSC_UDP' | 'APPLESCRIPT';

export type LEDFXMode =
  | 'STATIC (PRESET)'
  | 'RAINBOW WAVE'
  | 'CYLON SCANNER'
  | 'DIGITAL MATRIX'
  | 'CYBERPUNK PULSE'
  | 'LIVE FADER VU'
  | 'FIRE & EMBERS'
  | 'CENTER RIPPLE'
  | 'TWINKLE STARS'
  | 'STROBE FLASH'
  | 'HEARTBEAT'
  | 'PLASMA FIELD'
  | 'SPIRAL VORTEX'
  | 'EQ DANCE'
  | 'COLOR RUNNER';

export type ChassisTheme =
  | 'PETROL TEAL'
  | 'RETRO MATRIX'
  | 'NEON CYBER'
  | 'ROYAL GOLD'
  | 'STEALTH NOIR';

export type LEDPaletteMode =
  | 'VELOCITY MULTI-COLOR'
  | 'CYBER CYAN'
  | 'MATRIX GREEN'
  | 'SOLAR AMBER'
  | 'SYNTH MAGENTA'
  | 'ARCTIC WHITE';

export type MIDIVelocityCurve = 'LINEAR' | 'SOFT' | 'HARD' | 'MAX 127';

export type CustomFXLoopMode = 'LOOP' | 'BOUNCE' | '1-SHOT';
export type CustomWaveShape = 'SINE WAVE' | 'SAWTOOTH' | 'TRIANGLE' | 'SQUARE PULSE' | 'RADIAL RING' | 'SPIRAL VORTEX' | 'NOISE SPARK';
export type CustomWaveDirection = 'LEFT → RIGHT' | 'TOP → BOTTOM' | 'DIAGONAL 45°' | 'CENTER → OUT' | 'OUTER → CENTER';

export type ReactiveKeyFXMode =
  | 'RIPPLE RING'
  | 'LASER CROSS'
  | 'SUPERNOVA'
  | 'HEAT TRAIL'
  | 'METEOR DROP'
  | 'ARC LIGHTNING'
  | 'OFF';

export interface QwertyStep {
  id: string;
  key: string;
  cmd: boolean;
  shift: boolean;
  alt: boolean;
  ctrl: boolean;
  delayMs: number;
}

export interface ControlItem {
  id: string;
  label: string;
  customName: string;
  kind: ControlKind;
  number: number; // MIDI Note or CC Number
  zone: string;
  idleVelocity: number;
  activeVelocity: number;
  currentLED: number;
  liveValue: number;
  isPressed: boolean;
  toggleLatched: boolean;
  feedbackMode: FeedbackMode;
  actionType: ActionType;
  qwertySteps: QwertyStep[];
  oscHost: string;
  oscPort: number;
  oscAddress: string;
  appleScript: string;
  hasLED: boolean;
}

export interface PresetSlot {
  id: string;
  name: string;
  controls: ControlItem[];
  globalRules: string;
  iconName: string;
  accentVelocity: number;
}

export interface CustomLightEffect {
  id: string;
  name: string;
  isProcedural: boolean;
  frames: number[][]; // Each frame has 49 velocity values (0..127)
  fps: number;
  loopMode: CustomFXLoopMode;
  waveShape: CustomWaveShape;
  waveDirection: CustomWaveDirection;
  waveFrequency: number;
  minVelocity: number;
  maxVelocity: number;
}

export interface ReactivePulseEvent {
  id: string;
  originKeyIndex: number;
  originCol: number;
  originRow: number;
  mode: ReactiveKeyFXMode;
  baseVelocity: number;
  age: number; // 0.0 -> 1.0+
  lightningTargets: number[];
}

export interface MIDIOutputTarget {
  id: string;
  name: string;
  isHardwareBitchBoy: boolean;
}

// 49 Key Coordinate Positions:
// 0..31: 8x4 Grid (Row 0..3, Col 0..7)
// 32..39: 1x8 Strip (Row 4, Col 0..7)
// 40..48: 3x3 Keypad (Row 0..2, Col 8..10)
export const BitchBoyKeyMap = {
  keyPositions: (() => {
    const pos: Array<{ col: number; row: number }> = [];
    // 0..31: 8x4 Grid
    for (let r = 0; r < 4; r++) {
      for (let c = 0; c < 8; c++) {
        pos.push({ col: c, row: r });
      }
    }
    // 32..39: 1x8 Strip
    for (let c = 0; c < 8; c++) {
      pos.push({ col: c, row: 4 });
    }
    // 40..48: 3x3 Keypad
    for (let r = 0; r < 3; r++) {
      for (let c = 0; c < 3; c++) {
        pos.push({ col: 8 + c, row: r });
      }
    }
    return pos;
  })(),
};

export const THEME_CONFIGS: Record<ChassisTheme, {
  pcbBackground: string;
  outerBezelColors: [string, string, string];
  accentPrimary: string;
  sectionOutline: string;
  headerText: string;
}> = {
  'PETROL TEAL': {
    pcbBackground: '#052a24',
    outerBezelColors: ['#f0f0f0', '#bfbfbf', '#8c8c8c'],
    accentPrimary: '#38f4d9',
    sectionOutline: '#f58440',
    headerText: '#fae6a6',
  },
  'RETRO MATRIX': {
    pcbBackground: '#041f10',
    outerBezelColors: ['#268c40', '#144d1f', '#0a2610'],
    accentPrimary: '#2ef259',
    sectionOutline: '#33d973',
    headerText: '#a6faa6',
  },
  'NEON CYBER': {
    pcbBackground: '#14052e',
    outerBezelColors: ['#f238d9', '#591a1a', '#2d0f59'],
    accentPrimary: '#ff40e0',
    sectionOutline: '#33ebfa',
    headerText: '#f2bffa',
  },
  'ROYAL GOLD': {
    pcbBackground: '#2e1f0a',
    outerBezelColors: ['#fad973', '#a67326', '#4d3310'],
    accentPrimary: '#fad159',
    sectionOutline: '#ebbf40',
    headerText: '#faebb3',
  },
  'STEALTH NOIR': {
    pcbBackground: '#0f0f12',
    outerBezelColors: ['#595959', '#262626', '#141414'],
    accentPrimary: '#d9d9d9',
    sectionOutline: '#737373',
    headerText: '#cccccc',
  },
};

export function colorForVelocity(vel: number, paletteMode: LEDPaletteMode): string {
  if (vel === 0) return '#222222';

  switch (paletteMode) {
    case 'VELOCITY MULTI-COLOR':
      if (vel < 16) return '#ff3b30'; // Red
      if (vel < 32) return '#ff9500'; // Orange
      if (vel < 48) return '#ffd60a'; // Amber
      if (vel < 64) return '#30d158'; // Green
      if (vel < 85) return '#00d4ff'; // Cyan
      if (vel < 108) return '#b052de'; // Purple
      return '#e0faff'; // Pure white/cyan glow
    case 'CYBER CYAN':
      return '#38f4d9';
    case 'MATRIX GREEN':
      return '#2ef259';
    case 'SOLAR AMBER':
      return '#ffb81a';
    case 'SYNTH MAGENTA':
      return '#f240d9';
    case 'ARCTIC WHITE':
      return '#f5f5f5';
  }
}

export function buildDefault69Map(): ControlItem[] {
  const items: ControlItem[] = [];

  // 1. 8x4 Grid (32 Mechanical Keys)
  const gridNotes: number[][] = [
    [64, 65, 66, 67, 96, 97, 98, 99],
    [60, 61, 62, 63, 92, 93, 94, 95],
    [56, 57, 58, 59, 88, 89, 90, 91],
    [52, 53, 54, 55, 84, 85, 86, 87],
  ];

  for (let r = 0; r < 4; r++) {
    for (let c = 0; c < 8; c++) {
      const lbl = `R${r + 1}C${c + 1}`;
      items.push({
        id: `key_${items.length}`,
        label: lbl,
        customName: lbl,
        kind: 'key',
        number: gridNotes[r][c],
        zone: `8X4 GRID - ROW ${r + 1}, COL ${c + 1}`,
        idleVelocity: 0,
        activeVelocity: 127,
        currentLED: 0,
        liveValue: 0,
        isPressed: false,
        toggleLatched: false,
        feedbackMode: 'MOMENTARY',
        actionType: 'PASSTHROUGH',
        qwertySteps: [],
        oscHost: '127.0.0.1',
        oscPort: 8000,
        oscAddress: '/bitchboy/trigger',
        appleScript: '',
        hasLED: true,
      });
    }
  }

  // 2. 1x8 Key Strip (8 Keys)
  const stripNotes = [48, 49, 50, 51, 80, 81, 82, 83];
  for (let i = 0; i < 8; i++) {
    const lbl = `S${i + 1}`;
    items.push({
      id: `key_${items.length}`,
      label: lbl,
      customName: lbl,
      kind: 'key',
      number: stripNotes[i],
      zone: `1X8 KEY STRIP - KEY ${i + 1}`,
      idleVelocity: 0,
      activeVelocity: 127,
      currentLED: 0,
      liveValue: 0,
      isPressed: false,
      toggleLatched: false,
      feedbackMode: 'MOMENTARY',
      actionType: 'PASSTHROUGH',
      qwertySteps: [],
      oscHost: '127.0.0.1',
      oscPort: 8000,
      oscAddress: '/bitchboy/trigger',
      appleScript: '',
      hasLED: true,
    });
  }

  // 3. 3x3 Keypad (9 Keys)
  const padNotes = [44, 45, 46, 40, 41, 42, 36, 37, 38];
  for (let i = 0; i < 9; i++) {
    const lbl = `P${i + 1}`;
    items.push({
      id: `key_${items.length}`,
      label: lbl,
      customName: lbl,
      kind: 'key',
      number: padNotes[i],
      zone: `3X3 KEYPAD - KEY ${i + 1}`,
      idleVelocity: 0,
      activeVelocity: 127,
      currentLED: 0,
      liveValue: 0,
      isPressed: false,
      toggleLatched: false,
      feedbackMode: 'MOMENTARY',
      actionType: 'PASSTHROUGH',
      qwertySteps: [],
      oscHost: '127.0.0.1',
      oscPort: 8000,
      oscAddress: '/bitchboy/trigger',
      appleScript: '',
      hasLED: true,
    });
  }

  // 4. 8 Vertical Faders (CC 0..7)
  for (let i = 0; i < 8; i++) {
    const lbl = `VF${i + 1}`;
    items.push({
      id: `vfader_${i}`,
      label: lbl,
      customName: lbl,
      kind: 'vfader',
      number: i,
      zone: `VERTICAL FADER ${i + 1} (ANALOG CC)`,
      idleVelocity: 0,
      activeVelocity: 0,
      currentLED: 0,
      liveValue: i * 16,
      isPressed: false,
      toggleLatched: false,
      feedbackMode: 'MOMENTARY',
      actionType: 'PASSTHROUGH',
      qwertySteps: [],
      oscHost: '127.0.0.1',
      oscPort: 8000,
      oscAddress: '/bitchboy/trigger',
      appleScript: '',
      hasLED: false,
    });
  }

  // 5. 4 Horizontal Crossfaders (CC 8..11)
  for (let i = 0; i < 4; i++) {
    const lbl = `HF${i + 1}`;
    items.push({
      id: `hfader_${i}`,
      label: lbl,
      customName: lbl,
      kind: 'hfader',
      number: 8 + i,
      zone: `HORIZONTAL FADER ${i + 1} (ANALOG CC)`,
      idleVelocity: 0,
      activeVelocity: 0,
      currentLED: 0,
      liveValue: 32 + i * 24,
      isPressed: false,
      toggleLatched: false,
      feedbackMode: 'MOMENTARY',
      actionType: 'PASSTHROUGH',
      qwertySteps: [],
      oscHost: '127.0.0.1',
      oscPort: 8000,
      oscAddress: '/bitchboy/trigger',
      appleScript: '',
      hasLED: false,
    });
  }

  // 6. 8 Rotary Knobs (CC 20..27)
  for (let i = 0; i < 8; i++) {
    const lbl = `K${i + 1}`;
    items.push({
      id: `knob_${i}`,
      label: lbl,
      customName: lbl,
      kind: 'knob',
      number: 20 + i,
      zone: `ROTARY KNOB ${i + 1} (ANALOG CC)`,
      idleVelocity: 0,
      activeVelocity: 0,
      currentLED: 0,
      liveValue: 40 + i * 10,
      isPressed: false,
      toggleLatched: false,
      feedbackMode: 'MOMENTARY',
      actionType: 'PASSTHROUGH',
      qwertySteps: [],
      oscHost: '127.0.0.1',
      oscPort: 8000,
      oscAddress: '/bitchboy/trigger',
      appleScript: '',
      hasLED: false,
    });
  }

  return items;
}

export function buildDefault8MemorySlots(): PresetSlot[] {
  return [
    {
      id: 'slot_a',
      name: 'SLOT A // RESOLUME ARENA',
      controls: buildDefault69Map(),
      globalRules: 'Resolume Arena VJ clip triggers, column launches, and layer opacity faders.',
      iconName: 'tv',
      accentVelocity: 68,
    },
    {
      id: 'slot_b',
      name: 'SLOT B // OBS STUDIO',
      controls: buildDefault69Map(),
      globalRules: 'OBS Studio scene transitions, lower-thirds toggles, and live stream camera feeds.',
      iconName: 'video',
      accentVelocity: 52,
    },
    {
      id: 'slot_c',
      name: 'SLOT C // TOUCHDESIGNER',
      controls: buildDefault69Map(),
      globalRules: 'TouchDesigner real-time generative visual parameters and geometry transforms.',
      iconName: 'sparkles',
      accentVelocity: 112,
    },
    {
      id: 'slot_d',
      name: 'SLOT D // VMIX SWITCHER',
      controls: buildDefault69Map(),
      globalRules: 'vMix live production input buses, cut/fade transitions, and overlay channels.',
      iconName: 'layers',
      accentVelocity: 24,
    },
    {
      id: 'slot_e',
      name: 'SLOT E // QLAB VIDEO CUES',
      controls: buildDefault69Map(),
      globalRules: 'QLab theater video projections, screen shutter blackout, and cue GO/HALT triggers.',
      iconName: 'play',
      accentVelocity: 84,
    },
    {
      id: 'slot_f',
      name: 'SLOT F // DAVINCI RESOLVE',
      controls: buildDefault69Map(),
      globalRules: 'DaVinci Resolve & Premiere timeline playhead scrubbing, node bypass, and color wheels.',
      iconName: 'film',
      accentVelocity: 14,
    },
    {
      id: 'slot_g',
      name: 'SLOT G // GRANDMA3 VIDEO/DMX',
      controls: buildDefault69Map(),
      globalRules: 'GrandMA3 video wall mapping, ArtNet dimmer curves, and pixel fixture chases.',
      iconName: 'cpu',
      accentVelocity: 98,
    },
    {
      id: 'slot_h',
      name: 'SLOT H // MASTER VIDEO RIG',
      controls: buildDefault69Map(),
      globalRules: 'Master video controller surface with customized LED response and hardware routing.',
      iconName: 'sliders',
      accentVelocity: 38,
    },
  ];
}

export function buildStarterCustomEffects(): CustomLightEffect[] {
  const emptyFrame = (fill = 0) => Array(49).fill(fill);

  // 1. Neon Heart Pulse
  const heartF1 = emptyFrame(0);
  const heartF2 = emptyFrame(0);
  const heartCoords = [
    [2, 0], [3, 0], [5, 0], [6, 0],
    [1, 1], [2, 1], [3, 1], [4, 1], [5, 1], [6, 1], [7, 1],
    [2, 2], [3, 2], [4, 2], [5, 2], [6, 2],
    [3, 3], [4, 3], [5, 3],
    [4, 4],
  ];

  BitchBoyKeyMap.keyPositions.forEach((pos, k) => {
    if (heartCoords.some(([c, r]) => c === pos.col && r === pos.row)) {
      heartF1[k] = 112; // Pink
      heartF2[k] = 5;   // Red
    }
  });

  const heartEffect: CustomLightEffect = {
    id: 'starter_heart',
    name: 'NEON HEART PULSE',
    isProcedural: false,
    frames: [heartF1, heartF2, heartF1, emptyFrame(14)],
    fps: 5.0,
    loopMode: 'LOOP',
    waveShape: 'SINE WAVE',
    waveDirection: 'LEFT → RIGHT',
    waveFrequency: 1.0,
    minVelocity: 0,
    maxVelocity: 122,
  };

  // 2. Cyber Cross Chase
  const xFrames: number[][] = [];
  const pal = [68, 84, 98, 112, 24, 52];
  for (let step = 0; step < 6; step++) {
    const f = emptyFrame(0);
    for (let k = 0; k < 49; k++) {
      const p = BitchBoyKeyMap.keyPositions[k];
      if ((p.col + p.row + step) % 3 === 0 || (p.col - p.row + 10 + step) % 3 === 0) {
        f[k] = pal[(step + p.col) % pal.length];
      }
    }
    xFrames.push(f);
  }

  const xEffect: CustomLightEffect = {
    id: 'starter_cross',
    name: 'CYBER CROSS CHASE',
    isProcedural: false,
    frames: xFrames,
    fps: 8.0,
    loopMode: 'BOUNCE',
    waveShape: 'SINE WAVE',
    waveDirection: 'LEFT → RIGHT',
    waveFrequency: 1.0,
    minVelocity: 0,
    maxVelocity: 122,
  };

  // 3. 11-Col Laser Sweep
  const barFrames: number[][] = [];
  const barColors = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127];
  for (let c = 0; c < 11; c++) {
    const f = emptyFrame(0);
    for (let k = 0; k < 49; k++) {
      const col = BitchBoyKeyMap.keyPositions[k].col;
      if (col === c) {
        f[k] = barColors[c];
      } else if (Math.abs(col - c) === 1) {
        f[k] = 14;
      }
    }
    barFrames.push(f);
  }

  const barEffect: CustomLightEffect = {
    id: 'starter_sweep',
    name: '11-COL LASER SWEEP',
    isProcedural: false,
    frames: barFrames,
    fps: 14.0,
    loopMode: 'BOUNCE',
    waveShape: 'SINE WAVE',
    waveDirection: 'LEFT → RIGHT',
    waveFrequency: 1.0,
    minVelocity: 0,
    maxVelocity: 122,
  };

  // 4. Radial Hyperspace
  const synthEffect: CustomLightEffect = {
    id: 'starter_radial',
    name: 'RADIAL HYPERSPACE',
    isProcedural: true,
    frames: [emptyFrame(68)],
    fps: 15.0,
    loopMode: 'LOOP',
    waveShape: 'RADIAL RING',
    waveDirection: 'CENTER → OUT',
    waveFrequency: 1.4,
    minVelocity: 14,
    maxVelocity: 112,
  };

  return [heartEffect, xEffect, barEffect, synthEffect];
}
