import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  ControlItem,
  PresetSlot,
  ChassisTheme,
  LEDPaletteMode,
  SoundPack,
  LEDFXMode,
  MIDIVelocityCurve,
  MIDIOutputTarget,
  CustomLightEffect,
  ReactiveKeyFXMode,
  ReactivePulseEvent,
  BitchBoyKeyMap,
  THEME_CONFIGS,
  buildDefault69Map,
  buildDefault8MemorySlots,
  buildStarterCustomEffects,
} from './types';
import { audio } from './audio';
import { midiManager } from './webmidi';
import { ScrewRivet, GlowingWordLogo, OfficialMascot } from './components/MascotAndLogo';
import {
  HardwareKeyCap,
  HardwareVerticalFader,
  HardwareHorizontalFader,
  HardwareKnob,
} from './components/Controls';
import { LeftWing } from './components/LeftWing';
import { RightWing } from './components/RightWing';
import { CenterHUD } from './components/CenterHUD';
import {
  Sliders,
  Sparkles,
  Volume2,
  Lock,
  Unlock,
  Folder,
  Palette,
  Maximize2,
  ZoomIn,
  ZoomOut,
  StopCircle,
} from 'lucide-react';

export const App: React.FC = () => {
  // --- HARDWARE STATE ---
  const [controls, setControls] = useState<ControlItem[]>(buildDefault69Map);
  const [selectedIndex, setSelectedIndex] = useState<number>(0);
  const [selectedIndices, setSelectedIndices] = useState<Set<number>>(new Set([0]));
  const [slots, setSlots] = useState<PresetSlot[]>(() => {
    const saved = localStorage.getItem('BitchBoy_SavedSlots_v5');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length >= 8) return parsed;
      } catch (e) {
        console.warn('Error reading saved slots:', e);
      }
    }
    return buildDefault8MemorySlots();
  });
  const [activeSlot, setActiveSlot] = useState<number>(0);
  const [isLocked, setIsLocked] = useState<boolean>(false);
  const [lastStreamMessage, setLastStreamMessage] = useState<string>('IDLE // READY');
  const [userNotes, setUserNotes] = useState<string>(
    'Show notes: Slot A configured for live Ableton clip triggers.'
  );

  // --- CUSTOMIZATION & THEMES ---
  const [currentTheme, setCurrentTheme] = useState<ChassisTheme>('PETROL TEAL');
  const [ledPalette, setLedPalette] = useState<LEDPaletteMode>('VELOCITY MULTI-COLOR');
  const [soundPack, setSoundPack] = useState<SoundPack>('8-BIT ARCADE');
  const [currentFXMode, setCurrentFXMode] = useState<LEDFXMode>('STATIC (PRESET)');
  const [fxSpeed, setFxSpeed] = useState<number>(1.0);
  const [isRainbowRunning, setIsRainbowRunning] = useState<boolean>(false);

  // --- MIDI ROUTER STATE ---
  const [midiThruEnabled, setMidiThruEnabled] = useState<boolean>(true);
  const [virtualOutEnabled, setVirtualOutEnabled] = useState<boolean>(true);
  const [forwardNotes, setForwardNotes] = useState<boolean>(true);
  const [forwardCCs, setForwardCCs] = useState<boolean>(true);
  const [channelRemap, setChannelRemap] = useState<number>(0);
  const [transpose, setTranspose] = useState<number>(0);
  const [velocityCurve, setVelocityCurve] = useState<MIDIVelocityCurve>('LINEAR');
  const [outputTargets, setOutputTargets] = useState<MIDIOutputTarget[]>([]);
  const [selectedTargets, setSelectedTargets] = useState<Set<string>>(
    new Set(['virtual_synth', 'BitchBoy Virtual Web Out'])
  );
  const [packetHistory, setPacketHistory] = useState<string[]>([]);
  const [tapTempoBPM, setTapTempoBPM] = useState<number>(120);
  const [lastTapTimes, setLastTapTimes] = useState<number[]>([]);
  const [isConnected, setIsConnected] = useState<boolean>(false);
  const [deviceName, setDeviceName] = useState<string>('BitchBoy V1 (Simulated)');

  // --- CUSTOM LIGHTING FX STUDIO STATE ---
  const [customEffects, setCustomEffects] = useState<CustomLightEffect[]>(() => {
    const saved = localStorage.getItem('BitchBoy_CustomLightFX_v1');
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        if (Array.isArray(parsed) && parsed.length > 0) return parsed;
      } catch (e) {
        console.warn('Error reading custom FX:', e);
      }
    }
    return buildStarterCustomEffects();
  });
  const [editingCustomFX, setEditingCustomFX] = useState<CustomLightEffect>(
    () => customEffects[0] || buildStarterCustomEffects()[0]
  );
  const [editingFrameIndex, setEditingFrameIndex] = useState<number>(0);
  const [customBrushVelocity, setCustomBrushVelocity] = useState<number>(68);
  const [paintOnMainChassis, setPaintOnMainChassis] = useState<boolean>(false);
  const [isPlayingCustomFX, setIsPlayingCustomFX] = useState<boolean>(false);
  const [activeCustomFXName, setActiveCustomFXName] = useState<string>('');

  // --- REACTIVE MECHANICAL KEYBOARD FX STATE ---
  const [reactiveKeyFXMode, setReactiveKeyFXMode] = useState<ReactiveKeyFXMode>('RIPPLE RING');
  const [reactiveSpeed, setReactiveSpeed] = useState<number>(1.4);
  const [reactiveRainbow, setReactiveRainbow] = useState<boolean>(true);
  const [reactiveColorVelocity, setReactiveColorVelocity] = useState<number>(68);
  const [activePulses, setActivePulses] = useState<ReactivePulseEvent[]>([]);

  // --- UI SCALING & HOVER STATE ---
  const [uiScale, setUiScale] = useState<number>(1.0);
  const [autoFit, setAutoFit] = useState<boolean>(true);
  const [isHoveringLoveBadge, setIsHoveringLoveBadge] = useState<boolean>(false);
  const [isHoveringTitleLogo, setIsHoveringTitleLogo] = useState<boolean>(false);

  const containerRef = useRef<HTMLDivElement>(null);
  const theme = THEME_CONFIGS[currentTheme];

  // Sync sound pack with audio engine
  useEffect(() => {
    audio.setSoundPack(soundPack);
  }, [soundPack]);

  // Save presets to localStorage
  const saveCurrentSlot = useCallback(() => {
    setSlots((prev) => {
      const copy = [...prev];
      if (copy[activeSlot]) {
        copy[activeSlot].controls = controls;
        copy[activeSlot].globalRules = userNotes;
      }
      localStorage.setItem('BitchBoy_SavedSlots_v5', JSON.stringify(copy));
      return copy;
    });
  }, [activeSlot, controls, userNotes]);

  // Save custom effects to localStorage
  const saveCustomEffects = useCallback((effects: CustomLightEffect[]) => {
    setCustomEffects(effects);
    localStorage.setItem('BitchBoy_CustomLightFX_v1', JSON.stringify(effects));
  }, []);

  // Initialize Web MIDI API
  useEffect(() => {
    midiManager.init(
      (status, data1, data2, isFromHardware) => {
        handleIncomingMIDI(status, data1, data2, isFromHardware);
      },
      () => {
        setOutputTargets(midiManager.getAvailableOutputs());
        setIsConnected(midiManager.isConnectedToHardware());
        setDeviceName(midiManager.getHardwareDeviceName());
      }
    ).then((available) => {
      setOutputTargets(midiManager.getAvailableOutputs());
      setIsConnected(available && midiManager.isConnectedToHardware());
      setDeviceName(midiManager.getHardwareDeviceName());
    });
  }, []);

  // Window Auto-Fit Resize Observer
  useEffect(() => {
    const handleResize = () => {
      if (!autoFit) return;
      const w = window.innerWidth;
      const h = window.innerHeight;
      const baseW = 1566;
      const baseH = 780;
      const scaleW = (w - 32) / baseW;
      const scaleH = (h - 70) / baseH;
      const calculated = Math.min(2.0, Math.max(0.35, Math.min(scaleW, scaleH)));
      setUiScale(calculated);
    };

    handleResize();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [autoFit]);

  // Route incoming MIDI
  const routeMIDIPacket = (status: number, data1: number, data2: number) => {
    if (!midiThruEnabled) return;
    const packetStr = midiManager.sendMIDIToOutputs(
      selectedTargets,
      status,
      data1,
      data2,
      channelRemap,
      transpose,
      velocityCurve,
      virtualOutEnabled
    );

    setPacketHistory((prev) => [packetStr, ...prev.slice(0, 19)]);
  };

  // Handle Incoming MIDI
  const handleIncomingMIDI = (
    status: number,
    data1: number,
    data2: number,
    isFromHardware: boolean
  ) => {
    routeMIDIPacket(status, data1, data2);

    const msgType = status & 0xf0;
    const isNoteOn = msgType === 0x90 && data2 > 0;
    const isNoteOff = msgType === 0x80 || (msgType === 0x90 && data2 === 0);
    const isCC = msgType === 0xb0;

    if (isNoteOn || isNoteOff) {
      const idx = controls.findIndex((c) => c.kind === 'key' && c.number === data1);
      if (idx !== -1) {
        if (isNoteOn) {
          triggerKeyPress(idx, false);
        } else {
          setControls((prev) => {
            const next = [...prev];
            next[idx].isPressed = false;
            if (next[idx].feedbackMode === 'MOMENTARY') {
              next[idx].currentLED = next[idx].idleVelocity;
            }
            return next;
          });
        }
      }
    } else if (isCC) {
      const idx = controls.findIndex(
        (c) => (c.kind === 'vfader' || c.kind === 'hfader' || c.kind === 'knob') && c.number === data1
      );
      if (idx !== -1) {
        setControls((prev) => {
          const next = [...prev];
          next[idx].liveValue = data2;
          return next;
        });
        setSelectedIndex(idx);
      }
    }
  };

  // Trigger Key Press Simulation
  const triggerKeyPress = (index: number, isShiftDown: boolean = false) => {
    if (paintOnMainChassis && index < 49) {
      paintKey(index, customBrushVelocity);
      setSelectedIndex(index);
      return;
    }

    if (isShiftDown) {
      setSelectedIndices((prev) => {
        const next = new Set(prev);
        if (next.has(index)) next.delete(index);
        else next.add(index);
        return next;
      });
      setSelectedIndex(index);
    } else {
      setSelectedIndices(new Set([index]));
      setSelectedIndex(index);
    }

    const item = controls[index];
    if (!item) return;

    audio.playKeyTap(item.number, item.activeVelocity);
    setLastStreamMessage(`[${item.label}] PRESS #${item.number} V:${item.activeVelocity}`);

    // Update LED state based on feedback mode
    let nextLED = item.activeVelocity;
    let nextToggle = item.toggleLatched;

    if (item.feedbackMode === 'TOGGLE') {
      nextToggle = !item.toggleLatched;
      nextLED = nextToggle ? item.activeVelocity : item.idleVelocity;
    } else if (item.feedbackMode === 'STATIC') {
      nextLED = item.idleVelocity;
    }

    setControls((prev) => {
      const next = [...prev];
      next[index] = {
        ...next[index],
        isPressed: true,
        toggleLatched: nextToggle,
        currentLED: nextLED,
      };
      return next;
    });

    // Send MIDI out
    routeMIDIPacket(0x90, item.number, item.activeVelocity);

    // Trigger reactive spatial FX on 49 keys
    if (index < 49 && reactiveKeyFXMode !== 'OFF') {
      triggerReactivePulse(index);
    }

    // Release after 120ms
    setTimeout(() => {
      setControls((prev) => {
        const next = [...prev];
        if (next[index]) {
          next[index] = {
            ...next[index],
            isPressed: false,
            currentLED:
              next[index].feedbackMode === 'MOMENTARY'
                ? next[index].idleVelocity
                : next[index].currentLED,
          };
        }
        return next;
      });
      routeMIDIPacket(0x80, item.number, 0);
    }, 120);
  };

  // Trigger 2D Spatial Reactive Pulse
  const triggerReactivePulse = (keyIndex: number) => {
    const pos = BitchBoyKeyMap.keyPositions[keyIndex];
    if (!pos) return;

    const palette = [5, 14, 24, 38, 52, 68, 84, 98, 112, 127];
    const baseVel = reactiveRainbow
      ? palette[(keyIndex * 3 + activePulses.length * 2) % palette.length]
      : reactiveColorVelocity;

    const newPulse: ReactivePulseEvent = {
      id: Math.random().toString(),
      originKeyIndex: keyIndex,
      originCol: pos.col,
      originRow: pos.row,
      mode: reactiveKeyFXMode,
      baseVelocity: baseVel,
      age: 0.0,
      lightningTargets:
        reactiveKeyFXMode === 'ARC LIGHTNING'
          ? Array.from({ length: 8 }, () => Math.floor(Math.random() * 49))
          : [],
    };

    setActivePulses((prev) => [...prev.slice(-10), newPulse]);
  };

  // 2D Reactive Key FX Animation Loop
  useEffect(() => {
    if (activePulses.length === 0) return;

    const interval = setInterval(() => {
      const dt = 0.045 * Math.max(0.3, reactiveSpeed);

      setActivePulses((prev) => {
        const updated = prev
          .map((p) => ({ ...p, age: p.age + dt }))
          .filter((p) => p.age <= 1.05);

        if (updated.length === 0) {
          // Restore idle velocities
          setControls((cPrev) => {
            const next = [...cPrev];
            for (let k = 0; k < 49; k++) {
              if (!next[k].isPressed) {
                next[k].currentLED =
                  next[k].feedbackMode === 'TOGGLE' && next[k].toggleLatched
                    ? next[k].activeVelocity
                    : next[k].idleVelocity;
              }
            }
            return next;
          });
          return [];
        }

        // Apply spatial math to 49 keys
        setControls((cPrev) => {
          const next = [...cPrev];
          const palette = [5, 14, 24, 38, 52, 68, 84, 98, 112, 127];

          for (let k = 0; k < 49; k++) {
            const kPos = BitchBoyKeyMap.keyPositions[k];
            const kc = kPos.col;
            const kr = kPos.row;

            let maxIntensity = 0;
            let effectVel = 0;

            for (const pulse of updated) {
              const dx = kc - pulse.originCol;
              const dy = kr - pulse.originRow;
              const dist = Math.sqrt(dx * dx + dy * dy);
              let intensity = 0;

              switch (pulse.mode) {
                case 'RIPPLE RING': {
                  const radius = pulse.age * 11.5;
                  const diff = Math.abs(dist - radius);
                  if (diff < 1.45) {
                    intensity = (1.0 - diff / 1.45) * Math.max(0.0, 1.0 - pulse.age * 0.75);
                  }
                  break;
                }
                case 'LASER CROSS': {
                  const beamPos = pulse.age * 11.0;
                  if (Math.round(kr) === Math.round(pulse.originRow)) {
                    const d = Math.abs(Math.abs(dx) - beamPos);
                    if (d < 1.6) intensity = (1.0 - d / 1.6) * (1.0 - pulse.age * 0.65);
                  }
                  if (Math.round(kc) === Math.round(pulse.originCol)) {
                    const d = Math.abs(Math.abs(dy) - beamPos * 0.65);
                    if (d < 1.3)
                      intensity = Math.max(intensity, (1.0 - d / 1.3) * (1.0 - pulse.age * 0.65));
                  }
                  break;
                }
                case 'SUPERNOVA': {
                  const maxR = 6.5 * Math.sin(pulse.age * Math.PI);
                  if (dist <= maxR) {
                    intensity = (1.0 - dist / Math.max(0.5, maxR)) * (1.0 - pulse.age * 0.5);
                  }
                  break;
                }
                case 'HEAT TRAIL': {
                  if (k === pulse.originKeyIndex) {
                    intensity = Math.max(0, 1.0 - pulse.age);
                  } else if (dist <= 1.5) {
                    intensity = Math.max(0, (0.65 - pulse.age * 0.85) * (1.0 - dist / 1.6));
                  }
                  break;
                }
                case 'METEOR DROP': {
                  if (Math.round(kc) === Math.round(pulse.originCol)) {
                    const dropRow = (pulse.originRow + pulse.age * 7.5) % 5;
                    const d = Math.abs(kr - dropRow);
                    if (d < 1.35) intensity = (1.0 - d / 1.35) * (1.0 - pulse.age * 0.65);
                  }
                  break;
                }
                case 'ARC LIGHTNING': {
                  if (k === pulse.originKeyIndex || pulse.lightningTargets.includes(k)) {
                    const flicker = Math.sin(pulse.age * 42.0 + k) > -0.2 ? 1.0 : 0.35;
                    intensity = (1.0 - pulse.age * 0.85) * flicker;
                  }
                  break;
                }
                case 'OFF':
                  break;
              }

              if (intensity > maxIntensity) {
                maxIntensity = intensity;
                if (reactiveRainbow) {
                  const pIdx =
                    Math.abs(Math.round(dist + pulse.age * 8.0) + pulse.originKeyIndex) %
                    palette.length;
                  effectVel = intensity > 0.72 ? 127 : palette[pIdx];
                } else {
                  effectVel = intensity > 0.82 ? 127 : pulse.baseVelocity;
                }
              }
            }

            const baseV =
              next[k].feedbackMode === 'TOGGLE' && next[k].toggleLatched
                ? next[k].activeVelocity
                : next[k].idleVelocity;

            next[k].currentLED = maxIntensity > 0.14 ? effectVel : baseV;
          }
          return next;
        });

        return updated;
      });
    }, 35);

    return () => clearInterval(interval);
  }, [activePulses, reactiveSpeed, reactiveRainbow, reactiveColorVelocity]);

  // Physical Computer Keyboard Typing Listener
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't intercept typing when in text input
      if (
        document.activeElement?.tagName === 'INPUT' ||
        document.activeElement?.tagName === 'TEXTAREA'
      ) {
        return;
      }

      // Map common typing keys to random or specific 49 keys
      const charCode = e.key.charCodeAt(0) || 0;
      const keyIdx = Math.abs(charCode * 7 + 13) % 49;
      triggerKeyPress(keyIdx, e.shiftKey);
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [controls, paintOnMainChassis, customBrushVelocity, reactiveKeyFXMode]);

  // Paint a single key on current custom FX frame
  const paintKey = (keyIndex: number, vel: number) => {
    if (editingCustomFX.frames.length === 0) return;
    const safeIdx = Math.min(editingFrameIndex, editingCustomFX.frames.length - 1);
    const updatedFrames = [...editingCustomFX.frames];
    const currentV = updatedFrames[safeIdx][keyIndex];
    const newV = currentV === vel && vel > 0 ? 0 : vel;
    updatedFrames[safeIdx][keyIndex] = newV;

    setEditingCustomFX({ ...editingCustomFX, frames: updatedFrames });
    setControls((prev) => {
      const next = [...prev];
      next[keyIndex].currentLED = newV;
      return next;
    });
    audio.playKeyTap(controls[keyIndex]?.number, newV);
  };

  // Continuous Custom Light Show Player
  useEffect(() => {
    if (!isPlayingCustomFX) return;

    let step = 0;
    let direction = 1;
    const intervalMs = Math.max(35, 1000 / (editingCustomFX.fps * fxSpeed));

    const timer = setInterval(() => {
      if (editingCustomFX.isProcedural) {
        // Procedural Wave evaluation
        step += 0.06 * fxSpeed;
        setControls((prev) => {
          const next = [...prev];
          for (let k = 0; k < 49; k++) {
            const pos = BitchBoyKeyMap.keyPositions[k];
            const x = pos.col / 10.0;
            const y = pos.row / 4.0;
            const cx = x - 0.45;
            const cy = y - 0.5;

            let coord = x;
            if (editingCustomFX.waveDirection === 'TOP → BOTTOM') coord = y;
            if (editingCustomFX.waveDirection === 'DIAGONAL 45°') coord = (x + y) * 0.5;
            if (editingCustomFX.waveDirection === 'CENTER → OUT')
              coord = Math.sqrt(cx * cx + cy * cy) * 1.4;
            if (editingCustomFX.waveDirection === 'OUTER → CENTER')
              coord = 1.0 - Math.min(1.0, Math.sqrt(cx * cx + cy * cy) * 1.4);

            const arg = (coord * editingCustomFX.waveFrequency - step) * 2.0 * Math.PI;
            const norm = (Math.sin(arg) + 1.0) * 0.5;
            const lo = Math.min(editingCustomFX.minVelocity, editingCustomFX.maxVelocity);
            const hi = Math.max(editingCustomFX.minVelocity, editingCustomFX.maxVelocity);
            next[k].currentLED = Math.round(lo + norm * (hi - lo));
          }
          return next;
        });
      } else {
        const count = editingCustomFX.frames.length;
        if (count === 0) return;
        const currentFrame = editingCustomFX.frames[step];

        if (currentFrame) {
          setEditingFrameIndex(step);
          setControls((prev) => {
            const next = [...prev];
            for (let k = 0; k < 49; k++) {
              next[k].currentLED = currentFrame[k] || 0;
            }
            return next;
          });
        }

        if (count > 1) {
          if (editingCustomFX.loopMode === 'LOOP') {
            step = (step + 1) % count;
          } else if (editingCustomFX.loopMode === 'BOUNCE') {
            const nextStep = step + direction;
            if (nextStep >= count) {
              direction = -1;
              step = Math.max(0, count - 2);
            } else if (nextStep < 0) {
              direction = 1;
              step = Math.min(count - 1, 1);
            } else {
              step = nextStep;
            }
          } else {
            // 1-Shot
            if (step + 1 < count) step += 1;
            else setIsPlayingCustomFX(false);
          }
        }
      }
    }, intervalMs);

    return () => clearInterval(timer);
  }, [isPlayingCustomFX, editingCustomFX, fxSpeed]);

  // Procedural 15 Shows Engine
  useEffect(() => {
    if (currentFXMode === 'STATIC (PRESET)' || isPlayingCustomFX) return;

    let step = 0;
    const intervalMs = Math.max(40, 70 / Math.max(0.2, fxSpeed));

    const timer = setInterval(() => {
      step += 1;

      setControls((prev) => {
        const next = [...prev];

        switch (currentFXMode) {
          case 'RAINBOW WAVE': {
            const rainbow = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127];
            for (let k = 0; k < 49; k++) {
              const col = BitchBoyKeyMap.keyPositions[k].col;
              next[k].currentLED = rainbow[(step + col) % rainbow.length];
            }
            break;
          }
          case 'CYLON SCANNER': {
            const sweep = (step % 20 <= 10 ? step % 20 : 20 - (step % 20));
            for (let k = 0; k < 49; k++) {
              const col = BitchBoyKeyMap.keyPositions[k].col;
              const dist = Math.abs(col - sweep);
              next[k].currentLED = dist === 0 ? 127 : dist === 1 ? 55 : dist === 2 ? 18 : 0;
            }
            break;
          }
          case 'DIGITAL MATRIX': {
            for (let k = 0; k < 49; k++) {
              const col = BitchBoyKeyMap.keyPositions[k].col;
              const row = BitchBoyKeyMap.keyPositions[k].row;
              const dropRow = (step + col * 3) % 7;
              next[k].currentLED =
                row === dropRow
                  ? 127
                  : row === (dropRow - 1 + 7) % 7
                  ? 60
                  : row === (dropRow - 2 + 7) % 7
                  ? 22
                  : 0;
            }
            break;
          }
          case 'CYBERPUNK PULSE': {
            const phase = step * 0.12;
            const norm = (Math.sin(phase) + 1.0) * 0.5;
            const vel = Math.round(norm * 115.0 + 12.0);
            for (let k = 0; k < 49; k++) next[k].currentLED = vel;
            break;
          }
          case 'LIVE FADER VU': {
            for (let c = 0; c < 8; c++) {
              const faderVal = next[49 + c].liveValue;
              const colKeyIndices = [32 + c, 24 + c, 16 + c, 8 + c, 0 + c];
              const thresholds = [15, 40, 65, 90, 115];
              const colors = [45, 55, 75, 100, 127];
              for (let seg = 0; seg < 5; seg++) {
                const kIdx = colKeyIndices[seg];
                next[kIdx].currentLED = faderVal >= thresholds[seg] ? colors[seg] : 0;
              }
            }
            const padWave = Math.round(((Math.sin(step * 0.2) + 1.0) * 0.5) * 40.0 + 10.0);
            for (let p = 40; p < 49; p++) next[p].currentLED = padWave;
            break;
          }
          case 'FIRE & EMBERS': {
            for (let k = 0; k < 49; k++) {
              const row = BitchBoyKeyMap.keyPositions[k].row;
              const baseHeat = Math.max(0, 5 - row) * 22;
              const flicker = (Math.random() * 36 - 18);
              next[k].currentLED = Math.max(0, Math.min(127, Math.round(baseHeat + flicker)));
            }
            break;
          }
          case 'CENTER RIPPLE': {
            const phase = step * 0.25;
            for (let k = 0; k < 49; k++) {
              const col = BitchBoyKeyMap.keyPositions[k].col;
              const row = BitchBoyKeyMap.keyPositions[k].row;
              const dist = Math.sqrt(Math.pow(col - 3.5, 2) + Math.pow(row - 2.0, 2));
              const wave = Math.sin(dist * 1.6 - phase);
              next[k].currentLED = Math.round(Math.max(0, wave) * 127.0);
            }
            break;
          }
          case 'TWINKLE STARS': {
            for (let k = 0; k < 49; k++) {
              next[k].currentLED = Math.random() > 0.85 ? Math.round(Math.random() * 60 + 67) : 0;
            }
            break;
          }
          case 'STROBE FLASH': {
            const vel = step % 3 === 0 ? 127 : 0;
            for (let k = 0; k < 49; k++) next[k].currentLED = vel;
            break;
          }
          case 'HEARTBEAT': {
            const s = step % 24;
            const vel = s === 0 ? 127 : s === 1 ? 80 : s === 2 ? 20 : s === 4 ? 110 : s === 5 ? 60 : 0;
            for (let k = 0; k < 49; k++) next[k].currentLED = vel;
            break;
          }
          case 'PLASMA FIELD': {
            const t = step * 0.16;
            for (let k = 0; k < 49; k++) {
              const c = BitchBoyKeyMap.keyPositions[k].col;
              const r = BitchBoyKeyMap.keyPositions[k].row;
              const v1 = Math.sin(c * 0.7 + t);
              const v2 = Math.cos(r * 0.8 + t * 0.9);
              const v3 = Math.sin((c + r) * 0.5 + t * 1.1);
              const norm = (v1 + v2 + v3 + 3.0) / 6.0;
              next[k].currentLED = Math.round(norm * 127.0);
            }
            break;
          }
          case 'SPIRAL VORTEX': {
            const angle = step * 0.32;
            for (let k = 0; k < 49; k++) {
              const dx = BitchBoyKeyMap.keyPositions[k].col - 4.0;
              const dy = BitchBoyKeyMap.keyPositions[k].row - 2.0;
              const keyAngle = Math.atan2(dy, dx);
              const diff = (keyAngle - angle + Math.PI * 4) % (Math.PI * 2);
              next[k].currentLED = Math.round(Math.max(0, 1.0 - diff / (Math.PI * 2)) * 127.0);
            }
            break;
          }
          case 'EQ DANCE': {
            for (let k = 0; k < 49; k++) {
              const col = BitchBoyKeyMap.keyPositions[k].col;
              const row = BitchBoyKeyMap.keyPositions[k].row;
              if (col < 8) {
                const height = Math.abs(Math.sin(step * 0.3 + col * 0.8)) * 5;
                const active = 4 - row <= height;
                next[k].currentLED = active ? 40 + (4 - row) * 20 : 0;
              } else {
                next[k].currentLED = Math.round(Math.random() * 50 + 20);
              }
            }
            break;
          }
          case 'COLOR RUNNER': {
            const head = step % 49;
            for (let k = 0; k < 49; k++) {
              const dist = (head - k + 49) % 49;
              next[k].currentLED = dist < 7 ? (7 - dist) * 18 : 0;
            }
            break;
          }
        }

        return next;
      });
    }, intervalMs);

    return () => clearInterval(timer);
  }, [currentFXMode, isPlayingCustomFX, fxSpeed]);

  // Rainbow Wave Refresh Sweep
  const runRainbowWaveRefresh = () => {
    if (isRainbowRunning) return;
    setIsRainbowRunning(true);
    audio.playPresetSwitch();

    const rainbowVelocities = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127];
    let step = 0;
    const totalSteps = 22;

    const timer = setInterval(() => {
      step += 1;
      setControls((prev) => {
        const next = [...prev];
        for (let k = 0; k < 49; k++) {
          const col = BitchBoyKeyMap.keyPositions[k].col;
          const wavePos = step - col;
          if (wavePos >= 0 && wavePos < rainbowVelocities.length) {
            next[k].currentLED = rainbowVelocities[wavePos];
          } else if (wavePos >= rainbowVelocities.length) {
            next[k].currentLED = next[k].idleVelocity;
          }
        }
        return next;
      });

      if (step >= totalSteps) {
        clearInterval(timer);
        setIsRainbowRunning(false);
      }
    }, 45);
  };

  // Blackout Panic
  const blackoutPanic = () => {
    setCurrentFXMode('STATIC (PRESET)');
    setIsPlayingCustomFX(false);
    setControls((prev) => {
      const next = [...prev];
      for (let k = 0; k < 49; k++) {
        next[k].idleVelocity = 0;
        next[k].currentLED = 0;
      }
      return next;
    });
    setLastStreamMessage('🚨 BLACKOUT / PANIC ALL LEDS OFF');
  };

  // Tap Tempo BPM handler
  const handleTapTempo = () => {
    const now = Date.now();
    const newTaps = [...lastTapTimes.filter((t) => now - t < 4000), now];
    setLastTapTimes(newTaps);

    if (newTaps.length >= 2) {
      const intervals = [];
      for (let i = 1; i < newTaps.length; i++) {
        intervals.push((newTaps[i] - newTaps[i - 1]) / 1000);
      }
      const avg = intervals.reduce((a, b) => a + b, 0) / intervals.length;
      if (avg > 0.15) {
        const bpm = Math.min(240, Math.max(40, Math.round(60 / avg)));
        setTapTempoBPM(bpm);
        setFxSpeed(Math.min(2.5, Math.max(0.4, Math.round((bpm / 120) * 10) / 10)));
      }
    }
    audio.playKnobTick();
  };

  const selectedControl = controls[selectedIndex] || controls[0];

  return (
    <div className="flex flex-col min-h-screen bg-[#040807] text-white overflow-x-hidden selection:bg-teal-500 selection:text-black">
      {/* TOP NAVIGATION BAR */}
      <header className="h-[54px] bg-[#071310] border-b border-white/10 px-4 flex items-center justify-between z-40 select-none">
        {/* Logo & Subtitle */}
        <div
          onMouseEnter={() => setIsHoveringTitleLogo(true)}
          onMouseLeave={() => setIsHoveringTitleLogo(false)}
          className="flex items-center gap-3 cursor-pointer"
        >
          <div
            className="w-8 h-8 rounded-lg flex items-center justify-center font-mono font-black text-sm bg-gradient-to-br from-teal-400 to-emerald-600 text-black shadow-md transition-transform"
            style={{
              boxShadow: isHoveringTitleLogo ? `0 0 16px ${theme.accentPrimary}` : 'none',
              transform: isHoveringTitleLogo ? 'scale(1.08)' : 'scale(1)',
            }}
          >
            BB
          </div>

          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <span
                className="font-mono text-sm font-black tracking-wider text-white"
                style={{
                  textShadow: `0 0 10px ${theme.accentPrimary}aa`,
                }}
              >
                BITCHMANAGER
              </span>
              <span
                className={`font-mono text-[7px] font-bold px-1.5 py-0.5 rounded-full ${
                  isLocked ? 'bg-amber-400 text-black' : 'bg-teal-400 text-black'
                }`}
              >
                {isLocked ? '69-MAP LOCKED' : '69-MAP UNLOCKED'}
              </span>
            </div>
            <span
              className="font-mono text-[7.5px] font-semibold tracking-wider -mt-0.5"
              style={{ color: theme.headerText }}
            >
              BLOCK SYSTEM HARDWARE // LED & ACTION ENGINE
            </span>
          </div>
        </div>

        {/* Online / Ready Status Pill */}
        <div className="flex items-center gap-2 bg-black/60 px-3 py-1 rounded-full border border-emerald-500/40">
          <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          <div className="flex flex-col">
            <span className="font-mono text-[8px] font-black text-emerald-400 leading-tight">
              {isConnected ? 'ONLINE // HARDWARE CONNECTED' : 'ONLINE // ENGINE READY'}
            </span>
            <span className="font-mono text-[7px] text-zinc-400 leading-tight">
              {deviceName}
            </span>
          </div>
        </div>

        {/* Right Nav Action Controls */}
        <div className="flex items-center gap-2">
          {/* Made with Love signature with glowing hover */}
          <div
            onMouseEnter={() => setIsHoveringLoveBadge(true)}
            onMouseLeave={() => setIsHoveringLoveBadge(false)}
            className="px-2.5 py-1 rounded-full border border-pink-500/40 bg-black/40 font-mono text-[8px] font-bold text-pink-300 transition-all cursor-pointer"
            style={{
              boxShadow: isHoveringLoveBadge
                ? '0 0 16px rgba(244, 63, 94, 0.8), inset 0 0 8px rgba(244, 63, 94, 0.4)'
                : 'none',
              transform: isHoveringLoveBadge ? 'scale(1.05)' : 'scale(1)',
              borderColor: isHoveringLoveBadge ? '#f43f5e' : 'rgba(244, 63, 94, 0.4)',
            }}
          >
            ❤️ Made with love - Taylor Fossen
          </div>

          {/* Scale / Zoom */}
          <div className="flex items-center gap-1 bg-black/60 p-0.5 rounded-lg border border-white/10 font-mono text-[8px]">
            <button
              onClick={() => {
                setAutoFit(false);
                setUiScale((s) => Math.max(0.4, Math.round((s - 0.1) * 10) / 10));
              }}
              className="p-1 hover:bg-white/10 rounded text-zinc-300 cursor-pointer"
              title="Zoom Out"
            >
              <ZoomOut className="w-3 h-3" />
            </button>
            <button
              onClick={() => setAutoFit(true)}
              className={`px-1.5 py-0.5 rounded font-bold cursor-pointer ${
                autoFit ? 'bg-teal-500/30 text-teal-300' : 'text-zinc-400 hover:text-white'
              }`}
              title="Auto-Fit to Window"
            >
              {autoFit ? 'AUTO-FIT' : `${Math.round(uiScale * 100)}%`}
            </button>
            <button
              onClick={() => {
                setAutoFit(false);
                setUiScale((s) => Math.min(2.0, Math.round((s + 0.1) * 10) / 10));
              }}
              className="p-1 hover:bg-white/10 rounded text-zinc-300 cursor-pointer"
              title="Zoom In"
            >
              <ZoomIn className="w-3 h-3" />
            </button>
          </div>

          {/* Memory Slot Dropdown */}
          <select
            value={activeSlot}
            onChange={(e) => {
              saveCurrentSlot();
              const idx = Number(e.target.value);
              setActiveSlot(idx);
              setControls(slots[idx].controls);
              setUserNotes(slots[idx].globalRules);
              audio.playPresetSwitch();
              runRainbowWaveRefresh();
            }}
            className="bg-black/60 border border-white/20 text-teal-300 rounded-lg px-2 py-1 font-mono text-[8px] font-bold cursor-pointer"
          >
            {slots.map((s, i) => (
              <option key={s.id} value={i}>
                {s.name}
              </option>
            ))}
          </select>

          {/* Theme Selector */}
          <select
            value={currentTheme}
            onChange={(e) => {
              setCurrentTheme(e.target.value as ChassisTheme);
              audio.playPresetSwitch();
            }}
            className="bg-black/60 border border-white/20 text-teal-300 rounded-lg px-2 py-1 font-mono text-[8px] font-bold cursor-pointer"
          >
            <option value="PETROL TEAL">🎨 Petrol Teal</option>
            <option value="RETRO MATRIX">🎨 Retro Matrix</option>
            <option value="NEON CYBER">🎨 Neon Cyber</option>
            <option value="ROYAL GOLD">🎨 Royal Gold</option>
            <option value="STEALTH NOIR">🎨 Stealth Noir</option>
          </select>

          {/* Sound Pack Selector */}
          <select
            value={soundPack}
            onChange={(e) => {
              setSoundPack(e.target.value as SoundPack);
              audio.playPresetSwitch();
            }}
            className="bg-black/60 border border-white/20 text-zinc-200 rounded-lg px-2 py-1 font-mono text-[8px] font-bold cursor-pointer"
          >
            <option value="8-BIT ARCADE">🔊 8-Bit Arcade</option>
            <option value="CHERRY MX BLUE">🔊 Cherry MX Blue</option>
            <option value="SCI-FI SYNTH">🔊 Sci-Fi Synth</option>
            <option value="MUTE SOUNDS">🔇 Mute Sounds</option>
          </select>

          {/* Rainbow Refresh Button */}
          <button
            onClick={runRainbowWaveRefresh}
            disabled={isRainbowRunning}
            className="px-2.5 py-1 rounded-lg bg-gradient-to-r from-purple-600 to-teal-500 hover:from-purple-500 hover:to-teal-400 text-white font-mono text-[8px] font-black tracking-wider flex items-center gap-1 cursor-pointer disabled:opacity-50 transition-all active:scale-95"
          >
            ✨ RAINBOW REFRESH
          </button>

          {/* 15 Shows Menu */}
          <select
            value={currentFXMode}
            onChange={(e) => {
              setIsPlayingCustomFX(false);
              setCurrentFXMode(e.target.value as LEDFXMode);
            }}
            className="bg-teal-950 border border-teal-500/60 text-teal-300 rounded-lg px-2 py-1 font-mono text-[8px] font-bold cursor-pointer"
          >
            <option value="STATIC (PRESET)">FX: Static Preset</option>
            <option value="RAINBOW WAVE">FX: Rainbow Wave</option>
            <option value="CYLON SCANNER">FX: Cylon Scanner</option>
            <option value="DIGITAL MATRIX">FX: Digital Matrix</option>
            <option value="CYBERPUNK PULSE">FX: Cyberpunk Pulse</option>
            <option value="LIVE FADER VU">FX: Live Fader VU</option>
            <option value="FIRE & EMBERS">FX: Fire & Embers</option>
            <option value="CENTER RIPPLE">FX: Center Ripple</option>
            <option value="TWINKLE STARS">FX: Twinkle Stars</option>
            <option value="STROBE FLASH">FX: Strobe Flash</option>
            <option value="HEARTBEAT">FX: Heartbeat</option>
            <option value="PLASMA FIELD">FX: Plasma Field</option>
            <option value="SPIRAL VORTEX">FX: Spiral Vortex</option>
            <option value="EQ DANCE">FX: EQ Dance</option>
            <option value="COLOR RUNNER">FX: Color Runner</option>
          </select>

          {/* Stop FX */}
          {(currentFXMode !== 'STATIC (PRESET)' || isPlayingCustomFX) && (
            <button
              onClick={() => {
                setIsPlayingCustomFX(false);
                setCurrentFXMode('STATIC (PRESET)');
                runRainbowWaveRefresh();
              }}
              className="px-2 py-1 rounded-lg bg-rose-600 hover:bg-rose-500 text-white font-mono text-[8px] font-black flex items-center gap-1 cursor-pointer"
            >
              <StopCircle className="w-3 h-3" /> STOP
            </button>
          )}

          {/* Lock Button */}
          <button
            onClick={() => setIsLocked(!isLocked)}
            className={`p-1.5 rounded-lg border cursor-pointer transition-colors ${
              isLocked
                ? 'bg-amber-500 text-black border-amber-400'
                : 'bg-black/40 text-teal-300 border-white/20 hover:border-white/40'
            }`}
            title={isLocked ? 'Unlock 69-Map' : 'Lock 69-Map'}
          >
            {isLocked ? <Lock className="w-3 h-3" /> : <Unlock className="w-3 h-3" />}
          </button>
        </div>
      </header>

      {/* MAIN HARDWARE CONSOLE VIEWPORT */}
      <main className="flex-1 flex items-center justify-center p-4 overflow-auto">
        <div
          ref={containerRef}
          style={{
            transform: `scale(${uiScale})`,
            transformOrigin: 'top center',
            width: '1566px',
            height: '780px',
          }}
          className="flex-shrink-0 relative transition-transform duration-150"
        >
          {/* Main Chassis PCB Surface */}
          <div
            className="w-full h-full rounded-[24px] relative p-5 shadow-2xl flex flex-col justify-between"
            style={{
              backgroundColor: theme.pcbBackground,
              border: `5.5px solid ${theme.outerBezelColors[0]}`,
              boxShadow: `0 25px 60px rgba(0,0,0,0.95), 0 0 20px ${theme.accentPrimary}22`,
            }}
          >
            {/* Metallic Screw Rivets in Bezel Corners & Centers */}
            <div className="absolute top-2 left-2 pointer-events-none">
              <ScrewRivet size={13} />
            </div>
            <div className="absolute top-2 left-1/2 -translate-x-1/2 pointer-events-none">
              <ScrewRivet size={13} />
            </div>
            <div className="absolute top-2 right-2 pointer-events-none">
              <ScrewRivet size={13} />
            </div>
            <div className="absolute bottom-2 left-2 pointer-events-none">
              <ScrewRivet size={13} />
            </div>
            <div className="absolute bottom-2 left-1/2 -translate-x-1/2 pointer-events-none">
              <ScrewRivet size={13} />
            </div>
            <div className="absolute bottom-2 right-2 pointer-events-none">
              <ScrewRivet size={13} />
            </div>

            {/* THREE SYMMETRIC PANELS: LEFT WING + CENTER CONSOLE + RIGHT WING */}
            <div className="flex items-start justify-between gap-5 h-full">
              {/* LEFT WING: MIDI Router, 20-CC Scope & Telemetry, Memory Bank */}
              <LeftWing
                controls={controls}
                slots={slots}
                activeSlot={activeSlot}
                onSelectSlot={(idx) => {
                  saveCurrentSlot();
                  setActiveSlot(idx);
                  setControls(slots[idx].controls);
                  setUserNotes(slots[idx].globalRules);
                  audio.playPresetSwitch();
                  runRainbowWaveRefresh();
                }}
                onSaveSlot={() => {
                  saveCurrentSlot();
                  audio.playPresetSwitch();
                  setLastStreamMessage('💾 PRESETS SAVED TO LOCAL STORAGE');
                }}
                onCloneSlot={() => {
                  const targetIdx = (activeSlot + 1) % 8;
                  const copy = [...slots];
                  copy[targetIdx] = {
                    ...slots[activeSlot],
                    id: Math.random().toString(),
                    name: `SLOT ${['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'][targetIdx]} // CLONE`,
                  };
                  setSlots(copy);
                  localStorage.setItem('BitchBoy_SavedSlots_v5', JSON.stringify(copy));
                  audio.playPresetSwitch();
                }}
                onResetSlot={() => {
                  const defaults = buildDefault8MemorySlots();
                  const copy = [...slots];
                  copy[activeSlot] = defaults[activeSlot];
                  setSlots(copy);
                  setControls(defaults[activeSlot].controls);
                  localStorage.setItem('BitchBoy_SavedSlots_v5', JSON.stringify(copy));
                  audio.playPresetSwitch();
                }}
                onExportSlot={() => {
                  const jsonStr = JSON.stringify(slots[activeSlot], null, 2);
                  navigator.clipboard.writeText(jsonStr);
                  setLastStreamMessage('⤓ EXPORTED PRESET JSON TO CLIPBOARD');
                }}
                onImportSlot={() => {
                  navigator.clipboard.readText().then((clip) => {
                    try {
                      const parsed = JSON.parse(clip);
                      if (parsed && parsed.controls) {
                        const copy = [...slots];
                        copy[activeSlot] = parsed;
                        setSlots(copy);
                        setControls(parsed.controls);
                        saveCurrentSlot();
                        setLastStreamMessage('⤒ IMPORTED PRESET JSON');
                      }
                    } catch (e) {
                      setLastStreamMessage('❌ INVALID JSON IN CLIPBOARD');
                    }
                  });
                }}
                midiThruEnabled={midiThruEnabled}
                onToggleMidiThru={() => setMidiThruEnabled(!midiThruEnabled)}
                virtualOutEnabled={virtualOutEnabled}
                onToggleVirtualOut={() => setVirtualOutEnabled(!virtualOutEnabled)}
                forwardNotes={forwardNotes}
                onToggleForwardNotes={() => setForwardNotes(!forwardNotes)}
                forwardCCs={forwardCCs}
                onToggleForwardCCs={() => setForwardCCs(!forwardCCs)}
                channelRemap={channelRemap}
                onChangeChannelRemap={setChannelRemap}
                transpose={transpose}
                onChangeTranspose={setTranspose}
                velocityCurve={velocityCurve}
                onChangeVelocityCurve={setVelocityCurve}
                outputTargets={outputTargets}
                selectedTargets={selectedTargets}
                onToggleTarget={(name) => {
                  setSelectedTargets((prev) => {
                    const next = new Set(prev);
                    if (next.has(name)) next.delete(name);
                    else next.add(name);
                    return next;
                  });
                }}
                packetHistory={packetHistory}
                tapTempoBPM={tapTempoBPM}
                onRegisterTapTempo={handleTapTempo}
                onSendTestChord={() => {
                  [60, 64, 67, 72].forEach((note) => {
                    routeMIDIPacket(0x90, note, 110);
                    setTimeout(() => routeMIDIPacket(0x80, note, 0), 350);
                  });
                  audio.playPresetSwitch();
                }}
                onSendPanicAllNotesOff={() => {
                  for (let ch = 0; ch < 16; ch++) {
                    routeMIDIPacket(0xb0 | ch, 123, 0);
                  }
                  blackoutPanic();
                }}
                themeAccent={theme.accentPrimary}
              />

              {/* CENTER CONSOLE: The Authentic 69-Control BitchBoy Layout */}
              <div className="flex items-start gap-4">
                {/* Left Section: 8x4 Grid (32 keys) + 1x8 Strip (8 keys) + 8 Vertical Faders + In-Chassis Notes */}
                <div className="flex flex-col gap-2 w-[414px]">
                  {/* 1. 8x4 Grid (32 Keys) */}
                  <div
                    className="p-2 rounded-xl border flex flex-col gap-1.5 shadow-inner"
                    style={{
                      backgroundColor: 'rgba(0,0,0,0.5)',
                      borderColor: theme.sectionOutline,
                    }}
                  >
                    <div className="flex items-center justify-between font-mono text-[8px] font-bold text-zinc-400">
                      <span>8×4 KEY GRID (32 KEYS)</span>
                      <span className="text-[7px] text-zinc-500">NOTES 52..99</span>
                    </div>

                    <div className="grid grid-cols-8 gap-1.5">
                      {controls.slice(0, 32).map((item, idx) => (
                        <HardwareKeyCap
                          key={item.id}
                          item={item}
                          isSelected={selectedIndices.has(idx)}
                          paletteMode={ledPalette}
                          sectionBorder={theme.sectionOutline}
                          onTap={(e) => triggerKeyPress(idx, e.shiftKey)}
                        />
                      ))}
                    </div>
                  </div>

                  {/* 2. 1x8 Key Strip (8 Keys) */}
                  <div
                    className="p-2 rounded-xl border flex flex-col gap-1.5 shadow-inner"
                    style={{
                      backgroundColor: 'rgba(0,0,0,0.5)',
                      borderColor: theme.sectionOutline,
                    }}
                  >
                    <div className="flex items-center justify-between font-mono text-[8px] font-bold text-zinc-400">
                      <span>1×8 KEY STRIP (8 KEYS)</span>
                      <span className="text-[7px] text-zinc-500">NOTES 48..83</span>
                    </div>

                    <div className="grid grid-cols-8 gap-1.5">
                      {controls.slice(32, 40).map((item, idx) => {
                        const realIdx = 32 + idx;
                        return (
                          <HardwareKeyCap
                            key={item.id}
                            item={item}
                            isSelected={selectedIndices.has(realIdx)}
                            paletteMode={ledPalette}
                            sectionBorder={theme.sectionOutline}
                            onTap={(e) => triggerKeyPress(realIdx, e.shiftKey)}
                          />
                        );
                      })}
                    </div>
                  </div>

                  {/* 3. 8 Vertical Faders (CC 0..7) */}
                  <div
                    className="p-2 rounded-xl border flex flex-col gap-1 shadow-inner"
                    style={{
                      backgroundColor: 'rgba(0,0,0,0.5)',
                      borderColor: theme.sectionOutline,
                    }}
                  >
                    <div className="flex items-center justify-between font-mono text-[8px] font-bold text-zinc-400">
                      <span>8 VERTICAL FADERS (CC 0..7)</span>
                      <span className="text-[7px] text-zinc-500">ANALOG VALUE 0..127</span>
                    </div>

                    <div className="grid grid-cols-8 gap-1 items-center justify-items-center">
                      {controls.slice(49, 57).map((item, idx) => {
                        const realIdx = 49 + idx;
                        return (
                          <HardwareVerticalFader
                            key={item.id}
                            item={item}
                            isSelected={selectedIndex === realIdx}
                            onSelect={() => setSelectedIndex(realIdx)}
                            onValueChange={(val) => {
                              setControls((prev) => {
                                const next = [...prev];
                                next[realIdx].liveValue = val;
                                return next;
                              });
                              routeMIDIPacket(0xb0, item.number, val);
                              audio.playFaderSlide();
                            }}
                          />
                        );
                      })}
                    </div>
                  </div>

                  {/* In-Chassis Drawer: User Notes & Rules */}
                  <div className="bg-black/50 p-2 rounded-xl border border-white/10 flex flex-col gap-1">
                    <span className="font-mono text-[7.5px] font-bold text-zinc-400 uppercase">
                      In-Chassis Notes & Rules
                    </span>
                    <textarea
                      value={userNotes}
                      onChange={(e) => setUserNotes(e.target.value)}
                      placeholder="Show rules: Slot configured for live set..."
                      rows={2}
                      className="w-full bg-zinc-950/70 border border-white/10 rounded p-1.5 font-mono text-[7.5px] text-zinc-300 resize-none focus:outline-none focus:border-teal-400"
                    />
                  </div>
                </div>

                {/* Middle Section: 4 Horizontal Faders + Wordmark + Inspector HUD */}
                <div className="flex flex-col gap-2.5 w-[254px]">
                  {/* 4 Horizontal Crossfaders (CC 8..11) */}
                  <div
                    className="p-2 rounded-xl border flex flex-col gap-1.5 shadow-inner"
                    style={{
                      backgroundColor: 'rgba(0,0,0,0.5)',
                      borderColor: theme.sectionOutline,
                    }}
                  >
                    <div className="flex items-center justify-between font-mono text-[8px] font-bold text-zinc-400">
                      <span>4 HORIZONTAL CROSSFADERS</span>
                      <span className="text-[7px] text-zinc-500">CC 8..11</span>
                    </div>

                    <div className="flex flex-col gap-1">
                      {controls.slice(57, 61).map((item, idx) => {
                        const realIdx = 57 + idx;
                        return (
                          <HardwareHorizontalFader
                            key={item.id}
                            item={item}
                            isSelected={selectedIndex === realIdx}
                            onSelect={() => setSelectedIndex(realIdx)}
                            onValueChange={(val) => {
                              setControls((prev) => {
                                const next = [...prev];
                                next[realIdx].liveValue = val;
                                return next;
                              });
                              routeMIDIPacket(0xb0, item.number, val);
                              audio.playFaderSlide();
                            }}
                          />
                        );
                      })}
                    </div>
                  </div>

                  {/* Glowing BiTCHBOY Logo */}
                  <GlowingWordLogo auraColor={theme.accentPrimary} />

                  {/* Center Inspector HUD */}
                  <CenterHUD
                    selectedControl={selectedControl}
                    selectedIndices={selectedIndices}
                    onUpdateControl={(updates) => {
                      setControls((prev) => {
                        const next = [...prev];
                        // Apply to all multi-selected items if applicable
                        selectedIndices.forEach((idx) => {
                          next[idx] = { ...next[idx], ...updates };
                        });
                        return next;
                      });
                    }}
                    onBatchSetLED={(velocity) => {
                      setControls((prev) => {
                        const next = [...prev];
                        selectedIndices.forEach((idx) => {
                          if (next[idx].hasLED) {
                            next[idx].idleVelocity = velocity;
                            next[idx].currentLED = velocity;
                          }
                        });
                        return next;
                      });
                      audio.playKeyTap();
                    }}
                    paletteMode={ledPalette}
                    themeAccent={theme.accentPrimary}
                  />
                </div>

                {/* Right Section: 8 Knobs (CC 20..27) + Official Mascot + 3x3 Keypad */}
                <div className="flex flex-col gap-2.5 w-[194px] items-center">
                  {/* 2x4 Rotary Knobs (CC 20..27) */}
                  <div
                    className="p-2 rounded-xl border flex flex-col gap-1.5 w-full shadow-inner"
                    style={{
                      backgroundColor: 'rgba(0,0,0,0.5)',
                      borderColor: theme.sectionOutline,
                    }}
                  >
                    <div className="flex items-center justify-between font-mono text-[8px] font-bold text-zinc-400">
                      <span>8 ROTARY KNOBS</span>
                      <span className="text-[7px] text-zinc-500">CC 20..27</span>
                    </div>

                    <div className="grid grid-cols-4 gap-1.5 justify-items-center">
                      {controls.slice(61, 69).map((item, idx) => {
                        const realIdx = 61 + idx;
                        return (
                          <HardwareKnob
                            key={item.id}
                            item={item}
                            isSelected={selectedIndex === realIdx}
                            onSelect={() => setSelectedIndex(realIdx)}
                            onValueChange={(val) => {
                              setControls((prev) => {
                                const next = [...prev];
                                next[realIdx].liveValue = val;
                                return next;
                              });
                              routeMIDIPacket(0xb0, item.number, val);
                              audio.playKnobTick();
                            }}
                          />
                        );
                      })}
                    </div>
                  </div>

                  {/* Official Mascot */}
                  <OfficialMascot auraColor={theme.accentPrimary} />

                  {/* 3x3 Keypad (9 Keys) */}
                  <div
                    className="p-2 rounded-xl border flex flex-col gap-1.5 w-full shadow-inner"
                    style={{
                      backgroundColor: 'rgba(0,0,0,0.5)',
                      borderColor: theme.sectionOutline,
                    }}
                  >
                    <div className="flex items-center justify-between font-mono text-[8px] font-bold text-zinc-400">
                      <span>3×3 KEYPAD (9 KEYS)</span>
                      <span className="text-[7px] text-zinc-500">NOTES 36..46</span>
                    </div>

                    <div className="grid grid-cols-3 gap-1.5 justify-items-center">
                      {controls.slice(40, 49).map((item, idx) => {
                        const realIdx = 40 + idx;
                        return (
                          <HardwareKeyCap
                            key={item.id}
                            item={item}
                            isSelected={selectedIndices.has(realIdx)}
                            paletteMode={ledPalette}
                            sectionBorder={theme.sectionOutline}
                            onTap={(e) => triggerKeyPress(realIdx, e.shiftKey)}
                          />
                        );
                      })}
                    </div>
                  </div>
                </div>
              </div>

              {/* RIGHT WING: Custom Lighting FX Studio + Reactive Key FX Engine */}
              <RightWing
                customEffects={customEffects}
                editingCustomFX={editingCustomFX}
                editingFrameIndex={editingFrameIndex}
                onSelectFrameIndex={(idx) => {
                  setEditingFrameIndex(idx);
                  if (editingCustomFX.frames[idx]) {
                    setControls((prev) => {
                      const next = [...prev];
                      for (let k = 0; k < 49; k++) {
                        next[k].currentLED = editingCustomFX.frames[idx][k] || 0;
                      }
                      return next;
                    });
                  }
                }}
                onAddFrame={() => {
                  const newFrames = [
                    ...editingCustomFX.frames,
                    Array(49).fill(0),
                  ];
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setEditingFrameIndex(newFrames.length - 1);
                  audio.playKnobTick();
                }}
                onDuplicateFrame={() => {
                  const current = editingCustomFX.frames[editingFrameIndex] || Array(49).fill(0);
                  const newFrames = [...editingCustomFX.frames];
                  newFrames.splice(editingFrameIndex + 1, 0, [...current]);
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setEditingFrameIndex(editingFrameIndex + 1);
                  audio.playKnobTick();
                }}
                onDeleteFrame={() => {
                  if (editingCustomFX.frames.length <= 1) return;
                  const newFrames = editingCustomFX.frames.filter((_, i) => i !== editingFrameIndex);
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setEditingFrameIndex(Math.max(0, editingFrameIndex - 1));
                }}
                onClearFrame={() => {
                  const newFrames = [...editingCustomFX.frames];
                  newFrames[editingFrameIndex] = Array(49).fill(0);
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setControls((prev) => {
                    const next = [...prev];
                    for (let k = 0; k < 49; k++) next[k].currentLED = 0;
                    return next;
                  });
                }}
                onShiftFrame={(dx, dy) => {
                  const current = editingCustomFX.frames[editingFrameIndex] || Array(49).fill(0);
                  const shifted = Array(49).fill(0);
                  for (let k = 0; k < 49; k++) {
                    const pos = BitchBoyKeyMap.keyPositions[k];
                    const srcCol = (pos.col - dx + 11) % 11;
                    const srcRow = (pos.row - dy + 5) % 5;
                    const srcIdx = BitchBoyKeyMap.keyPositions.findIndex(
                      (p) => p.col === srcCol && p.row === srcRow
                    );
                    if (srcIdx !== -1) shifted[k] = current[srcIdx];
                  }
                  const newFrames = [...editingCustomFX.frames];
                  newFrames[editingFrameIndex] = shifted;
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setControls((prev) => {
                    const next = [...prev];
                    for (let k = 0; k < 49; k++) next[k].currentLED = shifted[k];
                    return next;
                  });
                  audio.playKnobTick();
                }}
                onInvertFrame={() => {
                  const current = editingCustomFX.frames[editingFrameIndex] || Array(49).fill(0);
                  const inverted = current.map((v) => (v === 0 ? customBrushVelocity : 0));
                  const newFrames = [...editingCustomFX.frames];
                  newFrames[editingFrameIndex] = inverted;
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setControls((prev) => {
                    const next = [...prev];
                    for (let k = 0; k < 49; k++) next[k].currentLED = inverted[k];
                    return next;
                  });
                }}
                onMirrorFrame={() => {
                  const current = editingCustomFX.frames[editingFrameIndex] || Array(49).fill(0);
                  const mirrored = [...current];
                  for (let k = 0; k < 49; k++) {
                    const pos = BitchBoyKeyMap.keyPositions[k];
                    const mirrorCol = pos.col < 8 ? 7 - pos.col : 18 - pos.col;
                    const srcIdx = BitchBoyKeyMap.keyPositions.findIndex(
                      (p) => p.col === mirrorCol && p.row === pos.row
                    );
                    if (srcIdx !== -1) mirrored[k] = current[srcIdx];
                  }
                  const newFrames = [...editingCustomFX.frames];
                  newFrames[editingFrameIndex] = mirrored;
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setControls((prev) => {
                    const next = [...prev];
                    for (let k = 0; k < 49; k++) next[k].currentLED = mirrored[k];
                    return next;
                  });
                }}
                onRandomizeFrame={() => {
                  const pal = [0, 0, 5, 14, 24, 38, 52, 68, 84, 98, 112, 127];
                  const randomF = Array.from({ length: 49 }, () => pal[Math.floor(Math.random() * pal.length)]);
                  const newFrames = [...editingCustomFX.frames];
                  newFrames[editingFrameIndex] = randomF;
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setControls((prev) => {
                    const next = [...prev];
                    for (let k = 0; k < 49; k++) next[k].currentLED = randomF[k];
                    return next;
                  });
                }}
                onApplyRainbowFrame={() => {
                  const rainbow = [5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127];
                  const rainF = Array.from({ length: 49 }, (_, k) => {
                    const col = BitchBoyKeyMap.keyPositions[k].col;
                    return rainbow[(col + editingFrameIndex) % rainbow.length];
                  });
                  const newFrames = [...editingCustomFX.frames];
                  newFrames[editingFrameIndex] = rainF;
                  setEditingCustomFX({ ...editingCustomFX, frames: newFrames });
                  setControls((prev) => {
                    const next = [...prev];
                    for (let k = 0; k < 49; k++) next[k].currentLED = rainF[k];
                    return next;
                  });
                }}
                isPlayingCustomFX={isPlayingCustomFX}
                onPlayCustomFX={(fx) => {
                  setEditingCustomFX(fx);
                  setIsPlayingCustomFX(true);
                  setActiveCustomFXName(fx.name);
                  audio.playPresetSwitch();
                }}
                onStopCustomFX={() => {
                  setIsPlayingCustomFX(false);
                  setActiveCustomFXName('');
                  runRainbowWaveRefresh();
                }}
                onChangeFPS={(fps) =>
                  setEditingCustomFX({ ...editingCustomFX, fps })
                }
                onChangeLoopMode={(loopMode) =>
                  setEditingCustomFX({ ...editingCustomFX, loopMode })
                }
                customBrushVelocity={customBrushVelocity}
                onChangeBrushVelocity={setCustomBrushVelocity}
                paintOnMainChassis={paintOnMainChassis}
                onTogglePaintOnMainChassis={() =>
                  setPaintOnMainChassis(!paintOnMainChassis)
                }
                onUpdateWaveParams={(updates) =>
                  setEditingCustomFX({ ...editingCustomFX, ...updates })
                }
                onBakeWaveToFrames={() => {
                  const baked = [];
                  for (let fIdx = 0; fIdx < 12; fIdx++) {
                    const phase = fIdx / 12.0;
                    const frame = [];
                    for (let k = 0; k < 49; k++) {
                      const pos = BitchBoyKeyMap.keyPositions[k];
                      const x = pos.col / 10.0;
                      const y = pos.row / 4.0;
                      const arg = (x * editingCustomFX.waveFrequency - phase) * 2.0 * Math.PI;
                      const norm = (Math.sin(arg) + 1.0) * 0.5;
                      const lo = Math.min(editingCustomFX.minVelocity, editingCustomFX.maxVelocity);
                      const hi = Math.max(editingCustomFX.minVelocity, editingCustomFX.maxVelocity);
                      frame.push(Math.round(lo + norm * (hi - lo)));
                    }
                    baked.push(frame);
                  }
                  setEditingCustomFX({
                    ...editingCustomFX,
                    isProcedural: false,
                    frames: baked,
                  });
                  setEditingFrameIndex(0);
                  audio.playPresetSwitch();
                }}
                onSelectEffectForEditing={(fx) => {
                  setEditingCustomFX(fx);
                  setEditingFrameIndex(0);
                  audio.playKnobTick();
                }}
                onExportFXToClipboard={() => {
                  navigator.clipboard.writeText(JSON.stringify(editingCustomFX, null, 2));
                  setLastStreamMessage(`⤓ EXPORTED CUSTOM SHOW: ${editingCustomFX.name}`);
                }}
                onImportFXFromClipboard={() => {
                  navigator.clipboard.readText().then((clip) => {
                    try {
                      const parsed = JSON.parse(clip);
                      if (parsed && parsed.name && parsed.frames) {
                        const nextList = [parsed, ...customEffects];
                        saveCustomEffects(nextList);
                        setEditingCustomFX(parsed);
                        setEditingFrameIndex(0);
                        setLastStreamMessage(`⤒ IMPORTED SHOW: ${parsed.name}`);
                      }
                    } catch (e) {
                      setLastStreamMessage('❌ INVALID SHOW JSON');
                    }
                  });
                }}
                reactiveKeyFXMode={reactiveKeyFXMode}
                onChangeReactiveMode={setReactiveKeyFXMode}
                reactiveSpeed={reactiveSpeed}
                onChangeReactiveSpeed={setReactiveSpeed}
                reactiveRainbow={reactiveRainbow}
                onToggleReactiveRainbow={() => setReactiveRainbow(!reactiveRainbow)}
                reactiveColorVelocity={reactiveColorVelocity}
                onChangeReactiveColorVelocity={setReactiveColorVelocity}
                onTriggerDemoReactiveFX={() => {
                  const randomKey = [11, 12, 19, 20, 18, 21, 44][Math.floor(Math.random() * 7)];
                  triggerReactivePulse(randomKey);
                  audio.playKeyTap();
                }}
                paletteMode={ledPalette}
                themeAccent={theme.accentPrimary}
              />
            </div>
          </div>
        </div>
      </main>

      {/* FOOTER STATUS BAR */}
      <footer className="h-6 bg-[#071310] border-t border-white/10 px-4 flex items-center justify-between font-mono text-[8px] text-zinc-400 select-none">
        <div className="flex items-center gap-3">
          <span className="text-teal-400 font-bold">{lastStreamMessage}</span>
          <span className="text-zinc-600">|</span>
          <span>Theme: {currentTheme}</span>
          <span className="text-zinc-600">|</span>
          <span>Sound: {soundPack}</span>
        </div>

        <div className="flex items-center gap-3">
          <span>Active Slot: {slots[activeSlot]?.name}</span>
          <span className="text-zinc-600">|</span>
          <span>BitchBoy V1 69-Control Hardware Matrix</span>
        </div>
      </footer>
    </div>
  );
};
