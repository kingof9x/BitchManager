import React, { useState } from 'react';
import {
  ControlItem,
  PresetSlot,
  MIDIVelocityCurve,
  MIDIOutputTarget,
} from '../types';
import {
  Layers,
  Activity,
  Sliders,
  Copy,
  Save,
  RotateCcw,
  Download,
  Upload,
  Volume2,
  AlertTriangle,
  Music,
} from 'lucide-react';

export const LeftWing: React.FC<{
  controls: ControlItem[];
  slots: PresetSlot[];
  activeSlot: number;
  onSelectSlot: (idx: number) => void;
  onSaveSlot: () => void;
  onCloneSlot: () => void;
  onResetSlot: () => void;
  onExportSlot: () => void;
  onImportSlot: () => void;
  midiThruEnabled: boolean;
  onToggleMidiThru: () => void;
  virtualOutEnabled: boolean;
  onToggleVirtualOut: () => void;
  forwardNotes: boolean;
  onToggleForwardNotes: () => void;
  forwardCCs: boolean;
  onToggleForwardCCs: () => void;
  channelRemap: number;
  onChangeChannelRemap: (ch: number) => void;
  transpose: number;
  onChangeTranspose: (semitones: number) => void;
  velocityCurve: MIDIVelocityCurve;
  onChangeVelocityCurve: (curve: MIDIVelocityCurve) => void;
  outputTargets: MIDIOutputTarget[];
  selectedTargets: Set<string>;
  onToggleTarget: (id: string) => void;
  packetHistory: string[];
  tapTempoBPM: number;
  onRegisterTapTempo: () => void;
  onSendTestChord: () => void;
  onSendPanicAllNotesOff: () => void;
  themeAccent: string;
}> = ({
  controls,
  slots,
  activeSlot,
  onSelectSlot,
  onSaveSlot,
  onCloneSlot,
  onResetSlot,
  onExportSlot,
  onImportSlot,
  midiThruEnabled,
  onToggleMidiThru,
  virtualOutEnabled,
  onToggleVirtualOut,
  forwardNotes,
  onToggleForwardNotes,
  forwardCCs,
  onToggleForwardCCs,
  channelRemap,
  onChangeChannelRemap,
  transpose,
  onChangeTranspose,
  velocityCurve,
  onChangeVelocityCurve,
  outputTargets,
  selectedTargets,
  onToggleTarget,
  packetHistory,
  tapTempoBPM,
  onRegisterTapTempo,
  onSendTestChord,
  onSendPanicAllNotesOff,
  themeAccent,
}) => {
  const [tab, setTab] = useState<'ROUTER' | 'TELEMETRY' | 'MACROS'>('ROUTER');
  const [activeBank, setActiveBank] = useState<0 | 1>(activeSlot < 4 ? 0 : 1);

  // CC items for telemetry
  const vfaders = controls.filter((c) => c.kind === 'vfader');
  const hfaders = controls.filter((c) => c.kind === 'hfader');
  const knobs = controls.filter((c) => c.kind === 'knob');

  const bankSlots = activeBank === 0 ? slots.slice(0, 4) : slots.slice(4, 8);
  const bankOffset = activeBank === 0 ? 0 : 4;
  const slotLetters = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'];

  return (
    <div className="flex flex-col gap-2.5 w-[266px] select-none text-zinc-300">
      {/* Top Studio Panel */}
      <div className="bg-black/55 rounded-xl border border-white/10 p-2.5 flex flex-col gap-2">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5">
            <Activity className="w-3.5 h-3.5" style={{ color: themeAccent }} />
            <span className="font-mono text-[9px] font-black tracking-wider uppercase text-zinc-200">
              MIDI ROUTER // OUT
            </span>
          </div>
          <button
            onClick={onToggleMidiThru}
            className={`flex items-center gap-1 px-1.5 py-0.5 rounded text-[7.5px] font-mono font-bold cursor-pointer transition-colors ${
              midiThruEnabled
                ? 'bg-emerald-950 text-emerald-400 border border-emerald-500/40'
                : 'bg-amber-950 text-amber-400 border border-amber-500/40'
            }`}
          >
            <div
              className={`w-1.5 h-1.5 rounded-full ${
                midiThruEnabled ? 'bg-emerald-400' : 'bg-amber-400'
              }`}
            />
            {midiThruEnabled ? 'THRU ON' : 'MUTED'}
          </button>
        </div>

        {/* 3-Tab Switcher */}
        <div className="grid grid-cols-3 gap-1 bg-white/5 p-0.5 rounded-lg">
          <button
            onClick={() => setTab('ROUTER')}
            className={`py-1 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              tab === 'ROUTER'
                ? 'bg-zinc-800 text-teal-300 shadow'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            ROUTER
          </button>
          <button
            onClick={() => setTab('TELEMETRY')}
            className={`py-1 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              tab === 'TELEMETRY'
                ? 'bg-zinc-800 text-teal-300 shadow'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            20-CC SCOPE
          </button>
          <button
            onClick={() => setTab('MACROS')}
            className={`py-1 rounded text-[8px] font-mono font-bold cursor-pointer transition-colors ${
              tab === 'MACROS'
                ? 'bg-zinc-800 text-teal-300 shadow'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            ACTION LOG
          </button>
        </div>

        {/* Tab 1: ROUTER */}
        {tab === 'ROUTER' && (
          <div className="flex flex-col gap-2 max-h-[290px] overflow-y-auto pr-1">
            {/* Quick Actions Bar */}
            <div className="grid grid-cols-2 gap-1.5">
              <button
                onClick={onSendTestChord}
                className="py-1 px-2 rounded bg-teal-900/40 hover:bg-teal-800/60 border border-teal-500/40 text-teal-300 font-mono text-[8px] font-bold flex items-center justify-center gap-1 cursor-pointer transition-colors"
              >
                <Music className="w-2.5 h-2.5" /> TEST C-CHORD
              </button>
              <button
                onClick={onSendPanicAllNotesOff}
                className="py-1 px-2 rounded bg-rose-950/40 hover:bg-rose-900/60 border border-rose-500/40 text-rose-300 font-mono text-[8px] font-bold flex items-center justify-center gap-1 cursor-pointer transition-colors"
              >
                <AlertTriangle className="w-2.5 h-2.5" /> ALL NOTES OFF
              </button>
            </div>

            {/* Tap Tempo */}
            <div className="flex items-center justify-between bg-black/40 p-1.5 rounded-lg border border-white/5">
              <div className="flex flex-col">
                <span className="font-mono text-[7.5px] text-zinc-400">TAP TEMPO</span>
                <span className="font-mono text-sm font-black text-amber-400">
                  {tapTempoBPM} <span className="text-[8px] text-zinc-400">BPM</span>
                </span>
              </div>
              <button
                onClick={onRegisterTapTempo}
                className="px-3 py-1.5 rounded bg-amber-500/20 hover:bg-amber-500/30 border border-amber-500/50 text-amber-300 font-mono text-[8.5px] font-bold cursor-pointer active:scale-95 transition-transform"
              >
                TAP BEAT
              </button>
            </div>

            {/* Settings */}
            <div className="flex flex-col gap-1.5 bg-black/40 p-2 rounded-lg border border-white/5 text-[8px] font-mono">
              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Virtual Web Out:</span>
                <input
                  type="checkbox"
                  checked={virtualOutEnabled}
                  onChange={onToggleVirtualOut}
                  className="accent-teal-400 cursor-pointer"
                />
              </div>

              <div className="flex items-center justify-between">
                <span className="text-zinc-400">Forward Notes / CC:</span>
                <div className="flex items-center gap-2">
                  <label className="flex items-center gap-1 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={forwardNotes}
                      onChange={onToggleForwardNotes}
                      className="accent-teal-400"
                    />
                    N
                  </label>
                  <label className="flex items-center gap-1 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={forwardCCs}
                      onChange={onToggleForwardCCs}
                      className="accent-teal-400"
                    />
                    CC
                  </label>
                </div>
              </div>

              <div className="flex items-center justify-between pt-1">
                <span className="text-zinc-400">Channel Remap:</span>
                <select
                  value={channelRemap}
                  onChange={(e) => onChangeChannelRemap(Number(e.target.value))}
                  className="bg-zinc-900 border border-white/20 text-zinc-200 rounded px-1 py-0.5 font-mono text-[7.5px]"
                >
                  <option value={0}>Pass-thru (Original)</option>
                  {Array.from({ length: 16 }, (_, i) => (
                    <option key={i + 1} value={i + 1}>
                      Channel {i + 1}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex flex-col gap-0.5 pt-1">
                <div className="flex items-center justify-between">
                  <span className="text-zinc-400">Transpose:</span>
                  <span className="text-teal-300">{transpose > 0 ? `+${transpose}` : transpose} st</span>
                </div>
                <input
                  type="range"
                  min="-24"
                  max="24"
                  value={transpose}
                  onChange={(e) => onChangeTranspose(Number(e.target.value))}
                  className="w-full accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
                />
              </div>

              <div className="flex items-center justify-between pt-1">
                <span className="text-zinc-400">Velocity Curve:</span>
                <select
                  value={velocityCurve}
                  onChange={(e) => onChangeVelocityCurve(e.target.value as MIDIVelocityCurve)}
                  className="bg-zinc-900 border border-white/20 text-zinc-200 rounded px-1 py-0.5 font-mono text-[7.5px]"
                >
                  <option value="LINEAR">Linear</option>
                  <option value="SOFT">Soft Curve</option>
                  <option value="HARD">Hard Curve</option>
                  <option value="MAX 127">Fixed 127</option>
                </select>
              </div>
            </div>

            {/* Output Targets */}
            <div className="flex flex-col gap-1 bg-black/40 p-2 rounded-lg border border-white/5">
              <span className="font-mono text-[7.5px] font-bold text-zinc-400 uppercase">
                Active MIDI Targets ({selectedTargets.size})
              </span>
              <div className="flex flex-col gap-1 max-h-[85px] overflow-y-auto">
                {outputTargets.map((tgt) => (
                  <label
                    key={tgt.id}
                    className="flex items-center gap-1.5 text-[8px] font-mono text-zinc-300 hover:text-white cursor-pointer truncate"
                  >
                    <input
                      type="checkbox"
                      checked={selectedTargets.has(tgt.name) || selectedTargets.has(tgt.id)}
                      onChange={() => onToggleTarget(tgt.name)}
                      className="accent-teal-400 cursor-pointer"
                    />
                    <span className="truncate">{tgt.name}</span>
                  </label>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Tab 2: 20-CC SCOPE */}
        {tab === 'TELEMETRY' && (
          <div className="flex flex-col gap-2 max-h-[290px] overflow-y-auto pr-1 text-[7.5px] font-mono">
            <span className="font-bold text-zinc-400 uppercase">
              8 Vertical Faders (CC 0..7)
            </span>
            <div className="grid grid-cols-4 gap-1">
              {vfaders.map((vf) => (
                <div key={vf.id} className="bg-black/40 p-1 rounded border border-white/5 flex flex-col">
                  <div className="flex justify-between text-zinc-400">
                    <span>{vf.label}</span>
                    <span className="text-teal-300 font-bold">{vf.liveValue}</span>
                  </div>
                  <div className="w-full bg-zinc-900 h-1 rounded overflow-hidden mt-1">
                    <div
                      className="bg-teal-400 h-full"
                      style={{ width: `${(vf.liveValue / 127) * 100}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>

            <span className="font-bold text-zinc-400 uppercase pt-1">
              4 Horizontal Crossfaders (CC 8..11)
            </span>
            <div className="grid grid-cols-2 gap-1">
              {hfaders.map((hf) => (
                <div key={hf.id} className="bg-black/40 p-1 rounded border border-white/5 flex flex-col">
                  <div className="flex justify-between text-zinc-400">
                    <span>{hf.label}</span>
                    <span className="text-amber-300 font-bold">{hf.liveValue}</span>
                  </div>
                  <div className="w-full bg-zinc-900 h-1 rounded overflow-hidden mt-1">
                    <div
                      className="bg-amber-400 h-full"
                      style={{ width: `${(hf.liveValue / 127) * 100}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>

            <span className="font-bold text-zinc-400 uppercase pt-1">
              8 Rotary Knobs (CC 20..27)
            </span>
            <div className="grid grid-cols-4 gap-1">
              {knobs.map((kn) => (
                <div key={kn.id} className="bg-black/40 p-1 rounded border border-white/5 flex flex-col">
                  <div className="flex justify-between text-zinc-400">
                    <span>{kn.label}</span>
                    <span className="text-pink-300 font-bold">{kn.liveValue}</span>
                  </div>
                  <div className="w-full bg-zinc-900 h-1 rounded overflow-hidden mt-1">
                    <div
                      className="bg-pink-400 h-full"
                      style={{ width: `${(kn.liveValue / 127) * 100}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Tab 3: ACTION LOG */}
        {tab === 'MACROS' && (
          <div className="flex flex-col gap-1 max-h-[290px] overflow-y-auto pr-1">
            <span className="font-mono text-[7.5px] font-bold text-zinc-400 uppercase">
              Live Routed Stream Packet Log
            </span>
            <div className="flex flex-col gap-1 font-mono text-[7px] text-zinc-400">
              {packetHistory.length === 0 ? (
                <div className="text-zinc-500 italic p-2 text-center">
                  Press keys or move faders to generate packets...
                </div>
              ) : (
                packetHistory.map((line, i) => (
                  <div
                    key={i}
                    className="p-1 rounded bg-black/40 border border-white/5 truncate font-mono text-teal-300"
                  >
                    {line}
                  </div>
                ))
              )}
            </div>
          </div>
        )}
      </div>

      {/* Bottom Memory Bank Panel */}
      <div className="bg-black/55 rounded-xl border border-white/10 p-2.5 flex flex-col gap-2">
        {/* Header with Bank 1 vs Bank 2 */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-1.5">
            <Layers className="w-3.5 h-3.5 text-amber-400" />
            <span className="font-mono text-[9px] font-black tracking-wider uppercase text-zinc-200">
              MEMORY BANK
            </span>
          </div>

          <div className="flex items-center gap-1">
            <button
              onClick={() => setActiveBank(0)}
              className={`px-1.5 py-0.5 rounded font-mono text-[7.5px] font-bold cursor-pointer transition-colors ${
                activeBank === 0
                  ? 'bg-amber-500 text-black'
                  : 'bg-zinc-800 text-zinc-400 hover:text-white'
              }`}
            >
              A–D
            </button>
            <button
              onClick={() => setActiveBank(1)}
              className={`px-1.5 py-0.5 rounded font-mono text-[7.5px] font-bold cursor-pointer transition-colors ${
                activeBank === 1
                  ? 'bg-amber-500 text-black'
                  : 'bg-zinc-800 text-zinc-400 hover:text-white'
              }`}
            >
              E–H
            </button>
          </div>
        </div>

        {/* 4 Bank Slots */}
        <div className="grid grid-cols-2 gap-1.5">
          {bankSlots.map((slot, index) => {
            const realIdx = bankOffset + index;
            const isCurrent = activeSlot === realIdx;
            const letter = slotLetters[realIdx];

            return (
              <button
                key={slot.id}
                onClick={() => onSelectSlot(realIdx)}
                className={`flex flex-col items-start p-1.5 rounded-lg border text-left cursor-pointer transition-all ${
                  isCurrent
                    ? 'bg-teal-950/60 border-teal-400 ring-1 ring-teal-400/50 shadow-md'
                    : 'bg-black/40 border-white/10 hover:border-white/25 hover:bg-black/60'
                }`}
              >
                <div className="flex items-center justify-between w-full">
                  <span
                    className={`font-mono text-[8.5px] font-black ${
                      isCurrent ? 'text-teal-300' : 'text-zinc-300'
                    }`}
                  >
                    SLOT {letter}
                  </span>
                  <div
                    className="w-2 h-2 rounded-full"
                    style={{
                      backgroundColor: isCurrent ? themeAccent : 'rgba(255,255,255,0.2)',
                    }}
                  />
                </div>
                <span className="font-mono text-[7px] text-zinc-400 truncate max-w-full">
                  {slot.name.replace(/^SLOT [A-H] \/\/ /, '')}
                </span>
              </button>
            );
          })}
        </div>

        {/* Slot Operations Bar */}
        <div className="grid grid-cols-5 gap-1 pt-0.5">
          <button
            onClick={onSaveSlot}
            className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-200 flex items-center justify-center cursor-pointer"
            title="Save Slot to Internal Memory"
          >
            <Save className="w-3 h-3" />
          </button>
          <button
            onClick={onCloneSlot}
            className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-200 flex items-center justify-center cursor-pointer"
            title="Clone Current Slot"
          >
            <Copy className="w-3 h-3" />
          </button>
          <button
            onClick={onResetSlot}
            className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-200 flex items-center justify-center cursor-pointer"
            title="Reset Slot to Defaults"
          >
            <RotateCcw className="w-3 h-3" />
          </button>
          <button
            onClick={onExportSlot}
            className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-200 flex items-center justify-center cursor-pointer"
            title="Export Slot JSON to Clipboard"
          >
            <Download className="w-3 h-3" />
          </button>
          <button
            onClick={onImportSlot}
            className="py-1 rounded bg-zinc-800 hover:bg-zinc-700 text-zinc-200 flex items-center justify-center cursor-pointer"
            title="Import Slot JSON from Clipboard"
          >
            <Upload className="w-3 h-3" />
          </button>
        </div>
      </div>
    </div>
  );
};
