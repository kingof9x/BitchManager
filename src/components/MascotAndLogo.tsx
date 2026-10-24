import React, { useState } from 'react';
import { audio } from '../audio';

export const ScrewRivet: React.FC<{ size?: number }> = ({ size = 12 }) => {
  return (
    <div
      style={{ width: size, height: size }}
      className="rounded-full bg-gradient-to-br from-zinc-200 via-zinc-400 to-zinc-700 shadow-md flex items-center justify-center relative select-none flex-shrink-0 border border-zinc-600/40"
    >
      <div className="absolute w-[65%] h-[1.5px] bg-zinc-800 rounded-sm" />
      <div className="absolute h-[65%] w-[1.5px] bg-zinc-800 rounded-sm" />
    </div>
  );
};

export const GlowingWordLogo: React.FC<{
  auraColor: string;
  onActivate?: () => void;
}> = ({ auraColor, onActivate }) => {
  const [isHovered, setIsHovered] = useState(false);
  const [isPressed, setIsPressed] = useState(false);

  const handleClick = () => {
    setIsPressed(true);
    audio.playMascotSqueak();
    if (onActivate) onActivate();
    setTimeout(() => setIsPressed(false), 200);
  };

  return (
    <button
      onClick={handleClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className={`relative w-full h-[68px] rounded-xl flex items-center justify-center select-none cursor-pointer transition-all duration-200 bg-black/40 border border-white/10 overflow-hidden ${
        isPressed ? 'scale-95' : isHovered ? 'scale-102 border-teal-400/50' : 'scale-100'
      }`}
      style={{
        boxShadow: isHovered
          ? `0 0 24px ${auraColor}66, inset 0 0 16px ${auraColor}33`
          : `0 0 12px ${auraColor}33`,
      }}
    >
      <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/5 to-transparent animate-pulse-subtle" />
      <div className="relative flex flex-col items-center">
        <span
          className="font-mono text-2xl font-black tracking-widest text-white drop-shadow-md"
          style={{
            textShadow: `0 0 10px ${auraColor}, 0 0 20px ${auraColor}`,
          }}
        >
          BiTCHBOY
        </span>
        <span className="font-mono text-[8px] font-bold tracking-[0.25em] text-zinc-400 uppercase -mt-1">
          Hardware System V1
        </span>
      </div>
    </button>
  );
};

export const OfficialMascot: React.FC<{
  auraColor: string;
  onTap?: () => void;
}> = ({ auraColor, onTap }) => {
  const [isHovered, setIsHovered] = useState(false);
  const [isPressed, setIsPressed] = useState(false);
  const [quote, setQuote] = useState('ALL 69 HARDWARE CONTROLS LIVE!');
  const [showSpeech, setShowSpeech] = useState(false);

  const quotes = [
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
    "\"NEON GLOW AT MAXIMUM WATTAGE!\"",
  ];

  const handleClick = () => {
    setIsPressed(true);
    audio.playMascotSqueak();
    const nextQuote = quotes[Math.floor(Math.random() * quotes.length)];
    setQuote(nextQuote);
    setShowSpeech(true);
    if (onTap) onTap();

    setTimeout(() => setIsPressed(false), 200);
    setTimeout(() => setShowSpeech(false), 4500);
  };

  return (
    <div className="relative flex flex-col items-center justify-center">
      {/* Speech Bubble */}
      {showSpeech && (
        <div
          className="absolute -top-12 z-50 px-3 py-1.5 rounded-lg bg-black/90 border border-teal-400 text-teal-300 font-mono text-[9px] font-black tracking-wide text-center whitespace-nowrap shadow-xl animate-bounce"
          style={{
            boxShadow: `0 0 14px ${auraColor}88`,
          }}
        >
          {quote}
          <div className="absolute left-1/2 -bottom-1.5 -translate-x-1/2 w-3 h-3 bg-black/90 border-r border-b border-teal-400 rotate-45" />
        </div>
      )}

      {/* Mascot Button */}
      <button
        onClick={handleClick}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        className={`w-[186px] h-[92px] rounded-xl flex items-center justify-center p-1.5 transition-transform duration-200 cursor-pointer bg-black/40 border border-white/10 ${
          isPressed ? 'scale-90' : isHovered ? 'scale-105 border-pink-400/50' : 'scale-100'
        }`}
        style={{
          boxShadow: isHovered
            ? `0 0 22px ${auraColor}88, inset 0 0 14px ${auraColor}33`
            : `0 0 10px ${auraColor}33`,
        }}
        title="Click the BitchBoy Mascot!"
      >
        {/* Crisp Pixel-Art Robot Gameboy Mascot SVG */}
        <svg
          viewBox="0 0 160 80"
          className="w-full h-full drop-shadow-md select-none"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          {/* Outer Headphones */}
          <rect x="18" y="22" width="10" height="34" rx="5" fill="#f43f5e" />
          <rect x="132" y="22" width="10" height="34" rx="5" fill="#f43f5e" />
          <path
            d="M 23 24 C 23 4, 137 4, 137 24"
            stroke="#f43f5e"
            strokeWidth="5"
            strokeLinecap="round"
          />

          {/* Main Chassis Head */}
          <rect
            x="28"
            y="12"
            width="104"
            height="56"
            rx="12"
            fill="#0f172a"
            stroke={auraColor}
            strokeWidth="2.5"
          />

          {/* CRT Screen Visor */}
          <rect
            x="36"
            y="18"
            width="88"
            height="32"
            rx="6"
            fill="#022c22"
            stroke="#059669"
            strokeWidth="1.5"
          />

          {/* Glowing Eyes */}
          <rect x="50" y="26" width="14" height="14" rx="3" fill={auraColor} />
          <rect x="96" y="26" width="14" height="14" rx="3" fill={auraColor} />

          {/* Pupils */}
          <circle cx="58" cy="33" r="3.5" fill="#000000" />
          <circle cx="104" cy="33" r="3.5" fill="#000000" />
          <circle cx="60" cy="31" r="1.5" fill="#ffffff" />
          <circle cx="106" cy="31" r="1.5" fill="#ffffff" />

          {/* Cute Smile / Mouth */}
          <path
            d="M 72 40 Q 80 46 88 40"
            stroke={auraColor}
            strokeWidth="2.5"
            strokeLinecap="round"
          />

          {/* Blushing cheeks */}
          <circle cx="44" cy="42" r="3" fill="#f43f5e" opacity="0.8" />
          <circle cx="116" cy="42" r="3" fill="#f43f5e" opacity="0.8" />

          {/* Controller D-Pad & Buttons on lower chin */}
          <rect x="42" y="54" width="14" height="4" rx="1" fill="#475569" />
          <rect x="47" y="49" width="4" height="14" rx="1" fill="#475569" />
          <circle cx="106" cy="56" r="3.5" fill="#38bdf8" />
          <circle cx="116" cy="56" r="3.5" fill="#f43f5e" />

          {/* LED Bar */}
          <rect x="66" y="55" width="28" height="3" rx="1.5" fill={auraColor} />
        </svg>
      </button>
    </div>
  );
};
