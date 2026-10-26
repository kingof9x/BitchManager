import React, { useState } from 'react';
import {
  CustomLightEffect,
  CustomFXLoopMode,
  CustomWaveShape,
  CustomWaveDirection,
  ReactiveKeyFXMode,
  LEDPaletteMode,
  colorForVelocity,
} from '../types';
import {
  Sparkles,
  Film,
  Play,
  Square,
  Plus,
  Copy,
  Trash2,
  RotateCw,
  ArrowRight,
  ArrowLeft,
  ArrowUp,
  ArrowDown,
  Shuffle,
  Sun,
  Flame,
  Zap,
} from 'lucide-react';

export const RightWing: React.FC<{
  customEffects: CustomLightEffect[];
  editingCustomFX: CustomLightEffect;
  editingFrameIndex: number;
  onSelectFrameIndex: (idx: number) => void;
  onAddFrame: () => void;
  onDuplicateFrame: () => void;
  onDeleteFrame: () => void;
  onClearFrame: () => void;
  onShiftFrame: (dx: number, dy: number) => void;
  onInvertFrame: () => void;
  onMirrorFrame: () => void;
  onRandomizeFrame: () => void;
  onApplyRainbowFrame: () => void;
  isPlayingCustomFX: boolean;
  onPlayCustomFX: (fx: CustomLightEffect) => void;
  onStopCustomFX: () => void;
  onChangeFPS: (fps: number) => void;
  onChangeLoopMode: (mode: CustomFXLoopMode) => void;
  customBrushVelocity: number;
  onChangeBrushVelocity: (vel: number) => void;
  paintOnMainChassis: boolean;
  onTogglePaintOnMainChassis: () => void;
  onUpdateWaveParams: (updates: Partial<CustomLightEffect>) => void;
  onBakeWaveToFrames: () => void;
  onSelectEffectForEditing: (fx: CustomLightEffect) => void;
  onExportFXToClipboard: () => void;
  onImportFXFromClipboard: () => void;
  // Reactive Key FX Engine
  reactiveKeyFXMode: ReactiveKeyFXMode;
  onChangeReactiveMode: (mode: ReactiveKeyFXMode) => void;
  reactiveSpeed: number;
  onChangeReactiveSpeed: (spd: number) => void;
  reactiveRainbow: boolean;
  onToggleReactiveRainbow: () => void;
  reactiveColorVelocity: number;
  onChangeReactiveColorVelocity: (vel: number) => void;
  onTriggerDemoReactiveFX: () => void;
  paletteMode: LEDPaletteMode;
  themeAccent: string;
}> = ({
  customEffects,
  editingCustomFX,
  editingFrameIndex,
  onSelectFrameIndex,
  onAddFrame,
  onDuplicateFrame,
  onDeleteFrame,
  onClearFrame,
  onShiftFrame,
  onInvertFrame,
  onMirrorFrame,
  onRandomizeFrame,
  onApplyRainbowFrame,
  isPlayingCustomFX,
  onPlayCustomFX,
  onStopCustomFX,
  onChangeFPS,
  onChangeLoopMode,
  customBrushVelocity,
  onChangeBrushVelocity,
  paintOnMainChassis,
  onTogglePaintOnMainChassis,
  onUpdateWaveParams,
  onBakeWaveToFrames,
  onSelectEffectForEditing,
  onExportFXToClipboard,
  onImportFXFromClipboard,
  reactiveKeyFXMode,
  onChangeReactiveMode,
  reactiveSpeed,
  onChangeReactiveSpeed,
  reactiveRainbow,
  onToggleReactiveRainbow,
  reactiveColorVelocity,
  onChangeReactiveColorVelocity,
  onTriggerDemoReactiveFX,
  paletteMode,
  themeAccent,
}) => {
  const [tab, setTab] = useState<'SEQUENCER' | 'WAVESYNTH' | 'LIBRARY'>('SEQUENCER');

  const paletteVelocities = [0, 5, 14, 24, 38, 52, 68, 84, 98, 112, 122, 127];

  const reactiveModes: Array<{ mode: ReactiveKeyFXMode; label: string }> = [
    { mode: 'RIPPLE RING', label: 'RIPPLE' },
    { mode: 'LASER CROSS', label: 'LASER' },
    { mode: 'SUPERNOVA', label: 'SUPERNOVA' },
    { mode: 'HEAT TRAIL', label: 'HEAT' },
    { mode: 'METEOR DROP', label: 'METEOR' },
    { mode: 'ARC LIGHTNING', label: 'LIGHTNING' },
    { mode: 'OFF', label: 'OFF' },
  ];

  return (
    <div className="flex flex-col gap-2.5 w-[274px] select-none text-zinc-300">
      {/* Top Studio Panel: Custom Lighting FX Studio */}
      <div className="bg-black/55 rounded-xl border border-white/10 p-2.5 flex flex-col gap-2">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5">
            <Sparkles className="w-3.5 h-3.5" style={{ color: themeAccent }} />
            <span className="font-mono text-[9px] font-black tracking-wider uppercase text-zinc-200">
              LIGHTING FX STUDIO
            </span>
          </div>

          <button
            onClick={() => {
              if (isPlayingCustomFX) {
                onStopCustomFX();
              } else {
                onPlayCustomFX(editingCustomFX);
              }
            }}
            className={`flex items-center gap-1 px-2 py-0.5 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              isPlayingCustomFX
                ? 'bg-rose-950 text-rose-300 border border-rose-500/40'
                : 'bg-emerald-950 text-emerald-300 border border-emerald-500/40'
            }`}
          >
            {isPlayingCustomFX ? (
              <>
                <Square className="w-2.5 h-2.5 fill-current" /> STOP SHOW
              </>
            ) : (
              <>
                <Play className="w-2.5 h-2.5 fill-current" /> PLAY SHOW
              </>
            )}
          </button>
        </div>

        {/* 3-Tab Switcher */}
        <div className="grid grid-cols-3 gap-1 bg-white/5 p-0.5 rounded-lg">
          <button
            onClick={() => setTab('SEQUENCER')}
            className={`py-1 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              tab === 'SEQUENCER'
                ? 'bg-zinc-800 text-teal-300 shadow'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            SEQUENCER
          </button>
          <button
            onClick={() => setTab('WAVESYNTH')}
            className={`py-1 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              tab === 'WAVESYNTH'
                ? 'bg-zinc-800 text-teal-300 shadow'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            WAVE SYNTH
          </button>
          <button
            onClick={() => setTab('LIBRARY')}
            className={`py-1 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              tab === 'LIBRARY'
                ? 'bg-zinc-800 text-teal-300 shadow'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            SHOWS ({customEffects.length})
          </button>
        </div>

        {/* Tab 1: SEQUENCER */}
        {tab === 'SEQUENCER' && (
          <div className="flex flex-col gap-2 max-h-[290px] overflow-y-auto pr-1">
            {/* Frame Navigator Bar */}
            <div className="flex items-center justify-between bg-black/40 p-1.5 rounded-lg border border-white/5">
              <span className="font-mono text-[8.5px] font-bold text-teal-300">
                FRAME {editingFrameIndex + 1} / {editingCustomFX.frames.length}
              </span>

              <div className="flex items-center gap-1">
                <button
                  onClick={onAddFrame}
                  className="p-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-300 cursor-pointer"
                  title="Add Blank Frame"
                >
                  <Plus className="w-3 h-3" />
                </button>
                <button
                  onClick={onDuplicateFrame}
                  className="p-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-300 cursor-pointer"
                  title="Duplicate Frame"
                >
                  <Copy className="w-3 h-3" />
                </button>
                <button
                  onClick={onDeleteFrame}
                  disabled={editingCustomFX.frames.length <= 1}
                  className="p-1 rounded bg-zinc-800 hover:bg-zinc-700 text-rose-400 disabled:opacity-30 cursor-pointer"
                  title="Delete Frame"
                >
                  <Trash2 className="w-3 h-3" />
                </button>
              </div>
            </div>

            {/* Frame Timeline Thumbnail Strip */}
            <div className="flex items-center gap-1 overflow-x-auto py-1">
              {editingCustomFX.frames.map((frame, idx) => (
                <button
                  key={idx}
                  onClick={() => onSelectFrameIndex(idx)}
                  className={`w-7 h-5 rounded border flex-shrink-0 cursor-pointer transition-all ${
                    idx === editingFrameIndex
                      ? 'border-teal-400 ring-1 ring-teal-400/60 bg-teal-950/80 scale-105'
                      : 'border-white/20 bg-black/50 hover:border-white/40'
                  }`}
                  title={`Select Frame ${idx + 1}`}
                >
                  <span className="font-mono text-[7px] text-zinc-300">
                    {idx + 1}
                  </span>
                </button>
              ))}
            </div>

            {/* Frame Transform Grid */}
            <div className="grid grid-cols-4 gap-1">
              <button
                onClick={() => onShiftFrame(0, -1)}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold flex items-center justify-center gap-1 cursor-pointer"
                title="Shift Up"
              >
                <ArrowUp className="w-2.5 h-2.5" /> UP
              </button>
              <button
                onClick={() => onShiftFrame(0, 1)}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold flex items-center justify-center gap-1 cursor-pointer"
                title="Shift Down"
              >
                <ArrowDown className="w-2.5 h-2.5" /> DOWN
              </button>
              <button
                onClick={() => onShiftFrame(-1, 0)}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold flex items-center justify-center gap-1 cursor-pointer"
                title="Shift Left"
              >
                <ArrowLeft className="w-2.5 h-2.5" /> LEFT
              </button>
              <button
                onClick={() => onShiftFrame(1, 0)}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold flex items-center justify-center gap-1 cursor-pointer"
                title="Shift Right"
              >
                <ArrowRight className="w-2.5 h-2.5" /> RIGHT
              </button>
            </div>

            {/* Transform Operations */}
            <div className="grid grid-cols-4 gap-1">
              <button
                onClick={onInvertFrame}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold cursor-pointer"
                title="Invert Frame"
              >
                INVERT
              </button>
              <button
                onClick={onMirrorFrame}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold cursor-pointer"
                title="Mirror Horizontal"
              >
                MIRROR
              </button>
              <button
                onClick={onRandomizeFrame}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold cursor-pointer"
                title="Random Sparks"
              >
                RANDOM
              </button>
              <button
                onClick={onApplyRainbowFrame}
                className="py-1 rounded bg-zinc-800/80 hover:bg-zinc-700 text-[7px] font-mono font-bold text-amber-300 cursor-pointer"
                title="Rainbow Gradient Fill"
              >
                RAINBOW
              </button>
            </div>

            {/* Color Palette Brush & Paint Mode */}
            <div className="flex flex-col gap-1.5 bg-black/40 p-2 rounded-lg border border-white/5">
              <div className="flex items-center justify-between">
                <span className="font-mono text-[7.5px] font-bold text-zinc-400 uppercase">
                  Palette Brush Velocity
                </span>
                <label className="flex items-center gap-1 text-[7.5px] font-mono text-teal-300 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={paintOnMainChassis}
                    onChange={onTogglePaintOnMainChassis}
                    className="accent-teal-400 cursor-pointer"
                  />
                  Paint on Chassis
                </label>
              </div>

              {/* Swatches */}
              <div className="grid grid-cols-6 gap-1">
                {paletteVelocities.map((vel) => {
                  const color = colorForVelocity(vel, paletteMode);
                  const isChosen = customBrushVelocity === vel;

                  return (
                    <button
                      key={vel}
                      onClick={() => onChangeBrushVelocity(vel)}
                      className={`h-4 rounded border flex items-center justify-center cursor-pointer transition-transform ${
                        isChosen
                          ? 'ring-1 ring-white scale-110 border-white'
                          : 'border-white/20 hover:scale-105'
                      }`}
                      style={{
                        backgroundColor: vel === 0 ? '#111111' : color,
                      }}
                      title={`Velocity ${vel}`}
                    >
                      <span className="font-mono text-[6.5px] font-bold text-white drop-shadow">
                        {vel}
                      </span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* FPS & Loop Mode */}
            <div className="flex items-center justify-between bg-black/40 p-2 rounded-lg border border-white/5 font-mono text-[8px]">
              <div className="flex flex-col gap-0.5">
                <span className="text-zinc-400">Speed: {editingCustomFX.fps} FPS</span>
                <input
                  type="range"
                  min="2"
                  max="24"
                  value={editingCustomFX.fps}
                  onChange={(e) => onChangeFPS(Number(e.target.value))}
                  className="w-24 accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
                />
              </div>

              <div className="flex flex-col gap-0.5 items-end">
                <span className="text-zinc-400">Loop Mode:</span>
                <select
                  value={editingCustomFX.loopMode}
                  onChange={(e) => onChangeLoopMode(e.target.value as CustomFXLoopMode)}
                  className="bg-zinc-900 border border-white/20 text-zinc-200 rounded px-1 py-0.5 font-mono text-[7.5px]"
                >
                  <option value="LOOP">LOOP</option>
                  <option value="BOUNCE">BOUNCE</option>
                  <option value="1-SHOT">1-SHOT</option>
                </select>
              </div>
            </div>
          </div>
        )}

        {/* Tab 2: WAVE SYNTH */}
        {tab === 'WAVESYNTH' && (
          <div className="flex flex-col gap-2 max-h-[290px] overflow-y-auto pr-1 font-mono text-[8px]">
            <div className="flex flex-col gap-1.5 bg-black/40 p-2 rounded-lg border border-white/5">
              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Wave Shape:</span>
                <select
                  value={editingCustomFX.waveShape}
                  onChange={(e) =>
                    onUpdateWaveParams({ waveShape: e.target.value as CustomWaveShape })
                  }
                  className="bg-zinc-900 border border-white/20 text-teal-300 rounded px-1 py-0.5 text-[7.5px]"
                >
                  <option value="SINE WAVE">SINE WAVE</option>
                  <option value="SAWTOOTH">SAWTOOTH</option>
                  <option value="TRIANGLE">TRIANGLE</option>
                  <option value="SQUARE PULSE">SQUARE PULSE</option>
                  <option value="RADIAL RING">RADIAL RING</option>
                  <option value="SPIRAL VORTEX">SPIRAL VORTEX</option>
                  <option value="NOISE SPARK">NOISE SPARK</option>
                </select>
              </div>

              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Direction:</span>
                <select
                  value={editingCustomFX.waveDirection}
                  onChange={(e) =>
                    onUpdateWaveParams({
                      waveDirection: e.target.value as CustomWaveDirection,
                    })
                  }
                  className="bg-zinc-900 border border-white/20 text-teal-300 rounded px-1 py-0.5 text-[7.5px]"
                >
                  <option value="LEFT → RIGHT">LEFT → RIGHT</option>
                  <option value="TOP → BOTTOM">TOP → BOTTOM</option>
                  <option value="DIAGONAL 45°">DIAGONAL 45°</option>
                  <option value="CENTER → OUT">CENTER → OUT</option>
                  <option value="OUTER → CENTER">OUTER → CENTER</option>
                </select>
              </div>

              <div className="flex flex-col gap-0.5 pt-1">
                <div className="flex justify-between">
                  <span className="text-zinc-400">Frequency:</span>
                  <span className="text-teal-300">{editingCustomFX.waveFrequency.toFixed(1)}x</span>
                </div>
                <input
                  type="range"
                  min="0.4"
                  max="3.0"
                  step="0.1"
                  value={editingCustomFX.waveFrequency}
                  onChange={(e) =>
                    onUpdateWaveParams({ waveFrequency: Number(e.target.value) })
                  }
                  className="w-full accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
                />
              </div>

              <div className="grid grid-cols-2 gap-2 pt-1">
                <div>
                  <span className="text-zinc-400">Min Vel: {editingCustomFX.minVelocity}</span>
                  <input
                    type="range"
                    min="0"
                    max="127"
                    value={editingCustomFX.minVelocity}
                    onChange={(e) =>
                      onUpdateWaveParams({ minVelocity: Number(e.target.value) })
                    }
                    className="w-full accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
                  />
                </div>
                <div>
                  <span className="text-zinc-400">Max Vel: {editingCustomFX.maxVelocity}</span>
                  <input
                    type="range"
                    min="0"
                    max="127"
                    value={editingCustomFX.maxVelocity}
                    onChange={(e) =>
                      onUpdateWaveParams({ maxVelocity: Number(e.target.value) })
                    }
                    className="w-full accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
                  />
                </div>
              </div>
            </div>

            <button
              onClick={onBakeWaveToFrames}
              className="py-1.5 rounded bg-teal-900/60 hover:bg-teal-800 border border-teal-500/40 text-teal-300 font-mono text-[8px] font-bold flex items-center justify-center gap-1 cursor-pointer transition-colors"
            >
              <Zap className="w-3 h-3 text-amber-400" /> BAKE WAVE TO 12 KEYFRAMES
            </button>
          </div>
        )}

        {/* Tab 3: LIBRARY */}
        {tab === 'LIBRARY' && (
          <div className="flex flex-col gap-2 max-h-[290px] overflow-y-auto pr-1 font-mono text-[8px]">
            <div className="flex flex-col gap-1">
              {customEffects.map((cfx) => (
                <div
                  key={cfx.id}
                  className={`flex items-center justify-between p-1.5 rounded-lg border transition-colors ${
                    editingCustomFX.id === cfx.id
                      ? 'bg-teal-950/60 border-teal-400 text-teal-300'
                      : 'bg-black/40 border-white/10 text-zinc-300 hover:border-white/20'
                  }`}
                >
                  <button
                    onClick={() => onSelectEffectForEditing(cfx)}
                    className="flex items-center gap-1.5 flex-1 text-left truncate cursor-pointer"
                  >
                    <Film className="w-3 h-3 text-amber-400 flex-shrink-0" />
                    <span className="truncate font-bold">{cfx.name}</span>
                  </button>

                  <button
                    onClick={() => onPlayCustomFX(cfx)}
                    className="p-1 rounded bg-zinc-800 hover:bg-zinc-700 text-teal-300 cursor-pointer"
                    title="Play Show"
                  >
                    <Play className="w-2.5 h-2.5 fill-current" />
                  </button>
                </div>
              ))}
            </div>

            <div className="grid grid-cols-2 gap-1 pt-1">
              <button
                onClick={onExportFXToClipboard}
                className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-300 font-bold cursor-pointer"
              >
                EXPORT FX
              </button>
              <button
                onClick={onImportFXFromClipboard}
                className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-300 font-bold cursor-pointer"
              >
                IMPORT FX
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Bottom Studio Panel: 2D Reactive Mechanical Keyboard FX Engine */}
      <div className="bg-black/55 rounded-xl border border-white/10 p-2.5 flex flex-col gap-2">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5">
            <Zap className="w-3.5 h-3.5 text-amber-400" />
            <span className="font-mono text-[9px] font-black tracking-wider uppercase text-zinc-200">
              REACTIVE KEY FX ENGINE
            </span>
          </div>

          <button
            onClick={onTriggerDemoReactiveFX}
            className="px-2 py-0.5 rounded bg-amber-500/20 hover:bg-amber-500/30 border border-amber-500/40 text-amber-300 font-mono text-[8px] font-bold cursor-pointer active:scale-95 transition-transform"
          >
            DEMO WAVE
          </button>
        </div>

        {/* Mode Selector Buttons */}
        <div className="grid grid-cols-4 gap-1">
          {reactiveModes.map((item) => (
            <button
              key={item.mode}
              onClick={() => onChangeReactiveMode(item.mode)}
              className={`py-1 rounded font-mono text-[7px] font-bold cursor-pointer transition-colors ${
                reactiveKeyFXMode === item.mode
                  ? 'bg-amber-500 text-black shadow'
                  : 'bg-zinc-800/80 text-zinc-400 hover:text-white'
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>

        {/* Speed & Color Options */}
        <div className="flex flex-col gap-1.5 bg-black/40 p-2 rounded-lg border border-white/5 font-mono text-[8px]">
          <div className="flex items-center justify-between">
            <span className="text-zinc-400">Wave Speed: {reactiveSpeed.toFixed(1)}x</span>
            <input
              type="range"
              min="0.5"
              max="2.5"
              step="0.1"
              value={reactiveSpeed}
              onChange={(e) => onChangeReactiveSpeed(Number(e.target.value))}
              className="w-24 accent-amber-400 cursor-pointer h-1 bg-zinc-800 rounded"
            />
          </div>

          <div className="flex items-center justify-between pt-1">
            <span className="text-zinc-400">Rainbow Dynamic:</span>
            <input
              type="checkbox"
              checked={reactiveRainbow}
              onChange={onToggleReactiveRainbow}
              className="accent-amber-400 cursor-pointer"
            />
          </div>

          {!reactiveRainbow && (
            <div className="flex items-center justify-between pt-1">
              <span className="text-zinc-400">Color Velocity: {reactiveColorVelocity}</span>
              <input
                type="range"
                min="5"
                max="127"
                value={reactiveColorVelocity}
                onChange={(e) => onChangeReactiveColorVelocity(Number(e.target.value))}
                className="w-24 accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
              />
            </div>
          )}
        </div>

        <span className="font-mono text-[7px] text-zinc-400 italic text-center">
          Type on your keyboard or click any key to shoot reactive shockwaves!
        </span>
      </div>
    </div>
  );
};
