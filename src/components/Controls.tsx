import React, { useRef } from 'react';
import { ControlItem, LEDPaletteMode, colorForVelocity } from '../types';
import { Plus, Command, Radio, Terminal } from 'lucide-react';

export const HardwareKeyCap: React.FC<{
  item: ControlItem;
  isSelected: boolean;
  paletteMode: LEDPaletteMode;
  sectionBorder: string;
  onTap: (e: React.MouseEvent) => void;
}> = ({ item, isSelected, paletteMode, sectionBorder, onTap }) => {
  const isLit = item.currentLED > 0;
  const ledGlow = isLit ? colorForVelocity(item.currentLED, paletteMode) : '#1a1a1a';

  const renderActionIcon = () => {
    switch (item.actionType) {
      case 'PASSTHROUGH':
        return <Plus className="w-2.5 h-2.5" />;
      case 'QWERTY_MACRO':
        return <Command className="w-2.5 h-2.5" />;
      case 'OSC_UDP':
        return <Radio className="w-2.5 h-2.5" />;
      case 'APPLESCRIPT':
        return <Terminal className="w-2.5 h-2.5" />;
    }
  };

  return (
    <button
      onClick={onTap}
      className={`relative w-[44px] h-[40px] rounded-md flex flex-col items-center justify-between p-1 select-none transition-transform duration-75 cursor-pointer ${
        item.isPressed ? 'scale-90 shadow-inner' : 'scale-100'
      }`}
      style={{
        backgroundColor: isLit ? `${ledGlow}44` : '#181010',
        borderColor: isSelected ? '#38f4d9' : sectionBorder,
        borderWidth: isSelected ? '2px' : '1px',
        boxShadow: isSelected
          ? '0 0 10px #38f4d9aa, inset 0 0 6px #38f4d966'
          : isLit
          ? `0 0 10px ${ledGlow}88, inset 0 0 8px ${ledGlow}44`
          : '0 2px 4px rgba(0,0,0,0.6)',
      }}
      title={`${item.label} (Note #${item.number}) - Click to trigger, Shift+Click to multi-select`}
    >
      {/* Top Label */}
      <span className="font-mono text-[7.5px] font-bold text-zinc-300 leading-none truncate max-w-full">
        {item.customName || item.label}
      </span>

      {/* Center Action Icon / Badge */}
      <div
        className="w-4 h-4 rounded-full flex items-center justify-center border transition-colors"
        style={{
          borderColor: isLit ? ledGlow : 'rgba(255,255,255,0.2)',
          color: isLit ? '#ffffff' : 'rgba(255,255,255,0.6)',
          backgroundColor: isLit ? `${ledGlow}33` : 'transparent',
        }}
      >
        {renderActionIcon()}
      </div>

      {/* Bottom Note */}
      <span className="font-mono text-[6.5px] font-bold text-zinc-400 leading-none">
        N{item.number}
      </span>
    </button>
  );
};

export const HardwareVerticalFader: React.FC<{
  item: ControlItem;
  isSelected: boolean;
  onSelect: () => void;
  onValueChange: (val: number) => void;
}> = ({ item, isSelected, onSelect, onValueChange }) => {
  const trackRef = useRef<HTMLDivElement>(null);

  const handlePointerDown = (e: React.PointerEvent) => {
    e.preventDefault();
    onSelect();
    const track = trackRef.current;
    if (!track) return;

    const update = (clientY: number) => {
      const rect = track.getBoundingClientRect();
      const relativeY = clientY - rect.top;
      const normalized = Math.max(0, Math.min(1, 1 - relativeY / rect.height));
      onValueChange(Math.round(normalized * 127));
    };

    update(e.clientY);

    const onPointerMove = (moveEvent: PointerEvent) => {
      update(moveEvent.clientY);
    };

    const onPointerUp = () => {
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
    };

    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
  };

  const capPercentage = (item.liveValue / 127) * 100;

  return (
    <div
      onClick={onSelect}
      className={`flex flex-col items-center justify-between w-[46px] h-[150px] py-1 px-0.5 rounded cursor-pointer transition-colors ${
        isSelected ? 'bg-white/10 ring-1 ring-teal-400' : 'bg-transparent hover:bg-white/5'
      }`}
    >
      <span className="font-mono text-[8px] font-bold text-zinc-300">
        {item.label}
      </span>

      {/* Track */}
      <div
        ref={trackRef}
        onPointerDown={handlePointerDown}
        className="relative w-2 h-[106px] bg-[#050b09] rounded-full border border-white/15 cursor-ns-resize"
      >
        {/* Recessed fill level */}
        <div
          className="absolute bottom-0 w-full bg-teal-500/30 rounded-full"
          style={{ height: `${capPercentage}%` }}
        />

        {/* Coral Fader Cap */}
        <div
          className="absolute left-1/2 -translate-x-1/2 w-6 h-[18px] rounded-sm bg-gradient-to-b from-[#fa8470] to-[#d64a38] border border-white/40 shadow-lg flex items-center justify-center pointer-events-none"
          style={{
            bottom: `calc(${capPercentage}% - 9px)`,
          }}
        >
          <div className="w-3.5 h-[1.5px] bg-white/90" />
        </div>
      </div>

      <span className="font-mono text-[7px] font-bold text-amber-400">
        CC{item.number}
      </span>
    </div>
  );
};

export const HardwareHorizontalFader: React.FC<{
  item: ControlItem;
  isSelected: boolean;
  onSelect: () => void;
  onValueChange: (val: number) => void;
}> = ({ item, isSelected, onSelect, onValueChange }) => {
  const trackRef = useRef<HTMLDivElement>(null);

  const handlePointerDown = (e: React.PointerEvent) => {
    e.preventDefault();
    onSelect();
    const track = trackRef.current;
    if (!track) return;

    const update = (clientX: number) => {
      const rect = track.getBoundingClientRect();
      const relativeX = clientX - rect.left;
      const normalized = Math.max(0, Math.min(1, relativeX / rect.width));
      onValueChange(Math.round(normalized * 127));
    };

    update(e.clientX);

    const onPointerMove = (moveEvent: PointerEvent) => {
      update(moveEvent.clientX);
    };

    const onPointerUp = () => {
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
    };

    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
  };

  const capPercentage = (item.liveValue / 127) * 100;

  return (
    <div
      onClick={onSelect}
      className={`flex items-center justify-between w-full h-[26px] px-2 rounded cursor-pointer transition-colors ${
        isSelected ? 'bg-white/10 ring-1 ring-teal-400' : 'bg-transparent hover:bg-white/5'
      }`}
    >
      <span className="font-mono text-[8.5px] font-bold text-zinc-300 w-7 text-left">
        {item.label}
      </span>

      {/* Track */}
      <div
        ref={trackRef}
        onPointerDown={handlePointerDown}
        className="relative flex-1 h-2 mx-2 bg-[#050b09] rounded-full border border-white/15 cursor-ew-resize"
      >
        {/* Recessed fill level */}
        <div
          className="absolute left-0 top-0 h-full bg-teal-500/30 rounded-full"
          style={{ width: `${capPercentage}%` }}
        />

        {/* Coral Fader Cap */}
        <div
          className="absolute top-1/2 -translate-y-1/2 w-[18px] h-6 rounded-sm bg-gradient-to-b from-[#fa8470] to-[#d64a38] border border-white/40 shadow-lg flex items-center justify-center pointer-events-none"
          style={{
            left: `calc(${capPercentage}% - 9px)`,
          }}
        >
          <div className="h-3.5 w-[1.5px] bg-zinc-900" />
        </div>
      </div>

      <span className="font-mono text-[7.5px] font-bold text-amber-400 w-8 text-right">
        CC{item.number}
      </span>
    </div>
  );
};

export const HardwareKnob: React.FC<{
  item: ControlItem;
  isSelected: boolean;
  onSelect: () => void;
  onValueChange: (val: number) => void;
}> = ({ item, isSelected, onSelect, onValueChange }) => {
  const handlePointerDown = (e: React.PointerEvent) => {
    e.preventDefault();
    onSelect();
    const startY = e.clientY;
    const startVal = item.liveValue;

    const onPointerMove = (moveEvent: PointerEvent) => {
      const deltaY = startY - moveEvent.clientY;
      const newVal = Math.max(0, Math.min(127, Math.round(startVal + deltaY * 1.2)));
      onValueChange(newVal);
    };

    const onPointerUp = () => {
      window.removeEventListener('pointermove', onPointerMove);
      window.removeEventListener('pointerup', onPointerUp);
    };

    window.addEventListener('pointermove', onPointerMove);
    window.addEventListener('pointerup', onPointerUp);
  };

  const angle = (item.liveValue / 127) * 270 - 135;

  return (
    <div
      onClick={onSelect}
      className={`flex flex-col items-center justify-center w-[46px] p-1 rounded cursor-pointer transition-colors ${
        isSelected ? 'bg-white/10 ring-1 ring-teal-400' : 'bg-transparent hover:bg-white/5'
      }`}
    >
      {/* Dial Body */}
      <div
        onPointerDown={handlePointerDown}
        className="relative w-8 h-8 rounded-full bg-gradient-to-br from-zinc-700 via-zinc-800 to-zinc-950 border border-white/25 shadow-md flex items-center justify-center cursor-ns-resize"
        style={{
          boxShadow: isSelected ? '0 0 10px #38f4d988' : '0 2px 6px rgba(0,0,0,0.8)',
        }}
      >
        {/* Indicator Line */}
        <div
          className="absolute w-0.5 h-3 bg-white rounded-full origin-bottom"
          style={{
            bottom: '50%',
            transform: `rotate(${angle}deg)`,
          }}
        />
        {/* Center screw point */}
        <div className="w-1.5 h-1.5 rounded-full bg-zinc-900 border border-zinc-600" />
      </div>

      <div className="flex items-center gap-1 mt-1">
        <span className="font-mono text-[7px] font-bold text-teal-300">
          {item.label}
        </span>
        <span className="font-mono text-[6.5px] font-bold text-amber-400">
          CC{item.number}
        </span>
      </div>
    </div>
  );
};
