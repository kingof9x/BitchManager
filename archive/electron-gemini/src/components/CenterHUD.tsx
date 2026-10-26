import React from 'react';
import {
  ControlItem,
  FeedbackMode,
  ActionType,
  LEDPaletteMode,
  colorForVelocity,
  QwertyStep,
} from '../types';
import { Sliders, Plus, Trash2, Radio, Terminal, Command } from 'lucide-react';

export const CenterHUD: React.FC<{
  selectedControl: ControlItem;
  selectedIndices: Set<number>;
  onUpdateControl: (updates: Partial<ControlItem>) => void;
  onBatchSetLED: (velocity: number) => void;
  paletteMode: LEDPaletteMode;
  themeAccent: string;
}> = ({
  selectedControl,
  selectedIndices,
  onUpdateControl,
  onBatchSetLED,
  paletteMode,
  themeAccent,
}) => {
  const isKey = selectedControl.kind === 'key';
  const idleColor = colorForVelocity(selectedControl.idleVelocity, paletteMode);
  const activeColor = colorForVelocity(selectedControl.activeVelocity, paletteMode);

  const addQwertyStep = () => {
    const newStep: QwertyStep = {
      id: Math.random().toString(),
      key: 'space',
      cmd: false,
      shift: false,
      alt: false,
      ctrl: false,
      delayMs: 10,
    };
    onUpdateControl({
      qwertySteps: [...selectedControl.qwertySteps, newStep],
    });
  };

  const removeQwertyStep = (id: string) => {
    onUpdateControl({
      qwertySteps: selectedControl.qwertySteps.filter((s) => s.id !== id),
    });
  };

  const updateQwertyStep = (id: string, updates: Partial<QwertyStep>) => {
    onUpdateControl({
      qwertySteps: selectedControl.qwertySteps.map((s) =>
        s.id === id ? { ...s, ...updates } : s
      ),
    });
  };

  return (
    <div className="flex flex-col w-[254px] h-[256px] bg-black/60 rounded-xl border border-white/10 p-2.5 overflow-y-auto select-none font-mono text-[8px] text-zinc-300">
      {/* Header */}
      <div className="flex items-center justify-between pb-1.5 border-b border-white/10">
        <div className="flex items-center gap-1.5">
          <Sliders className="w-3.5 h-3.5" style={{ color: themeAccent }} />
          <span className="text-[9px] font-black uppercase text-zinc-100">
            {selectedControl.label} INSPECTOR
          </span>
        </div>

        <span className="px-1.5 py-0.5 rounded bg-zinc-800 text-[7px] text-teal-300 font-bold">
          {isKey
            ? `NOTE #${selectedControl.number}`
            : `CC #${selectedControl.number}`}
        </span>
      </div>

      {selectedIndices.size > 1 && (
        <div className="mt-1.5 p-1 rounded bg-teal-950/60 border border-teal-500/40 text-[7px] text-teal-300 font-bold text-center">
          ⚡ Multi-Selected: {selectedIndices.size} controls
        </div>
      )}

      {/* Custom Name */}
      <div className="flex items-center justify-between mt-2">
        <span className="text-zinc-400">Custom Label:</span>
        <input
          type="text"
          value={selectedControl.customName}
          onChange={(e) => onUpdateControl({ customName: e.target.value })}
          placeholder={selectedControl.label}
          className="w-28 bg-zinc-900 border border-white/20 rounded px-1.5 py-0.5 text-zinc-200 text-[7.5px] font-mono focus:outline-none focus:border-teal-400"
        />
      </div>

      {isKey ? (
        <div className="flex flex-col gap-2 mt-2">
          {/* Idle Velocity */}
          <div className="flex flex-col gap-0.5">
            <div className="flex items-center justify-between">
              <span className="text-zinc-400">Idle LED (Vel):</span>
              <div className="flex items-center gap-1">
                <div
                  className="w-2.5 h-2.5 rounded-full border border-white/20"
                  style={{ backgroundColor: idleColor }}
                />
                <span className="text-teal-300 font-bold">{selectedControl.idleVelocity}</span>
              </div>
            </div>
            <input
              type="range"
              min="0"
              max="127"
              value={selectedControl.idleVelocity}
              onChange={(e) => {
                const vel = Number(e.target.value);
                onUpdateControl({ idleVelocity: vel, currentLED: vel });
              }}
              className="w-full accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
            />
          </div>

          {/* Active Velocity */}
          <div className="flex flex-col gap-0.5">
            <div className="flex items-center justify-between">
              <span className="text-zinc-400">Active LED (Vel):</span>
              <div className="flex items-center gap-1">
                <div
                  className="w-2.5 h-2.5 rounded-full border border-white/20"
                  style={{ backgroundColor: activeColor }}
                />
                <span className="text-teal-300 font-bold">{selectedControl.activeVelocity}</span>
              </div>
            </div>
            <input
              type="range"
              min="0"
              max="127"
              value={selectedControl.activeVelocity}
              onChange={(e) =>
                onUpdateControl({ activeVelocity: Number(e.target.value) })
              }
              className="w-full accent-teal-400 cursor-pointer h-1 bg-zinc-800 rounded"
            />
          </div>

          {/* Feedback Mode */}
          <div className="flex items-center justify-between">
            <span className="text-zinc-400">Feedback:</span>
            <select
              value={selectedControl.feedbackMode}
              onChange={(e) =>
                onUpdateControl({ feedbackMode: e.target.value as FeedbackMode })
              }
              className="bg-zinc-900 border border-white/20 text-zinc-200 rounded px-1 py-0.5 text-[7.5px]"
            >
              <option value="MOMENTARY">Momentary</option>
              <option value="TOGGLE">Toggle Latched</option>
              <option value="STATIC">Static LED</option>
            </select>
          </div>

          {/* Action Type */}
          <div className="flex items-center justify-between">
            <span className="text-zinc-400">Action:</span>
            <select
              value={selectedControl.actionType}
              onChange={(e) =>
                onUpdateControl({ actionType: e.target.value as ActionType })
              }
              className="bg-zinc-900 border border-white/20 text-zinc-200 rounded px-1 py-0.5 text-[7.5px]"
            >
              <option value="PASSTHROUGH">MIDI Passthrough</option>
              <option value="QWERTY_MACRO">QWERTY Macro</option>
              <option value="OSC_UDP">OSC Packet</option>
              <option value="APPLESCRIPT">AppleScript</option>
            </select>
          </div>

          {/* Sub-configs */}
          {selectedControl.actionType === 'QWERTY_MACRO' && (
            <div className="flex flex-col gap-1 p-1.5 rounded bg-zinc-900/60 border border-white/10">
              <div className="flex items-center justify-between">
                <span className="text-zinc-400 font-bold">Keystrokes</span>
                <button
                  onClick={addQwertyStep}
                  className="px-1.5 py-0.5 rounded bg-teal-900 hover:bg-teal-800 text-teal-300 text-[6.5px] cursor-pointer"
                >
                  + Add Step
                </button>
              </div>

              {selectedControl.qwertySteps.map((step) => (
                <div
                  key={step.id}
                  className="flex items-center gap-1 bg-black/40 p-1 rounded border border-white/5"
                >
                  <input
                    type="text"
                    value={step.key}
                    onChange={(e) => updateQwertyStep(step.id, { key: e.target.value })}
                    className="w-10 bg-zinc-800 text-zinc-200 text-[7px] px-1 rounded"
                  />
                  <label className="flex items-center gap-0.5 text-[6.5px]">
                    <input
                      type="checkbox"
                      checked={step.cmd}
                      onChange={(e) => updateQwertyStep(step.id, { cmd: e.target.checked })}
                    />
                    Cmd
                  </label>
                  <label className="flex items-center gap-0.5 text-[6.5px]">
                    <input
                      type="checkbox"
                      checked={step.shift}
                      onChange={(e) => updateQwertyStep(step.id, { shift: e.target.checked })}
                    />
                    Sh
                  </label>
                  <button
                    onClick={() => removeQwertyStep(step.id)}
                    className="text-rose-400 p-0.5 hover:text-rose-300 ml-auto"
                  >
                    <Trash2 className="w-2.5 h-2.5" />
                  </button>
                </div>
              ))}
            </div>
          )}

          {selectedControl.actionType === 'OSC_UDP' && (
            <div className="flex flex-col gap-1 p-1.5 rounded bg-zinc-900/60 border border-white/10">
              <input
                type="text"
                value={selectedControl.oscAddress}
                onChange={(e) => onUpdateControl({ oscAddress: e.target.value })}
                placeholder="/bitchboy/trigger"
                className="w-full bg-zinc-800 text-zinc-200 text-[7px] px-1.5 py-0.5 rounded"
              />
              <div className="flex gap-1">
                <input
                  type="text"
                  value={selectedControl.oscHost}
                  onChange={(e) => onUpdateControl({ oscHost: e.target.value })}
                  placeholder="127.0.0.1"
                  className="flex-1 bg-zinc-800 text-zinc-200 text-[7px] px-1.5 py-0.5 rounded"
                />
                <input
                  type="number"
                  value={selectedControl.oscPort}
                  onChange={(e) => onUpdateControl({ oscPort: Number(e.target.value) })}
                  className="w-12 bg-zinc-800 text-zinc-200 text-[7px] px-1 rounded"
                />
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="flex flex-col gap-2 mt-2">
          <div className="flex items-center justify-between">
            <span className="text-zinc-400">Analog Value:</span>
            <span className="text-teal-300 font-bold">{selectedControl.liveValue}</span>
          </div>
          <div className="w-full bg-zinc-900 h-2 rounded overflow-hidden">
            <div
              className="bg-teal-400 h-full"
              style={{ width: `${(selectedControl.liveValue / 127) * 100}%` }}
            />
          </div>
          <span className="text-zinc-500 italic mt-2">
            Continuous controller sends MIDI CC #{selectedControl.number} packets in real-time.
          </span>
        </div>
      )}
    </div>
  );
};
