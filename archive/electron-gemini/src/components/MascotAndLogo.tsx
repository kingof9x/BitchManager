import React, { useState } from 'react';
import bitchboyLogo from '../assets/bitchboy.png';

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
    if (onActivate) onActivate();
    setTimeout(() => setIsPressed(false), 200);
  };

  return (
    <button
      onClick={handleClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      className={`relative w-full h-[68px] rounded-xl flex items-center justify-center gap-3 px-3 select-none cursor-pointer transition-all duration-200 bg-black/50 border border-white/10 overflow-hidden ${
        isPressed ? 'scale-95' : isHovered ? 'scale-102 border-amber-400/60' : 'scale-100'
      }`}
      style={{
        boxShadow: isHovered
          ? `0 0 24px ${auraColor}66, inset 0 0 16px ${auraColor}33`
          : `0 0 12px ${auraColor}33`,
      }}
      title="BiTCHBOY Hardware Console"
    >
      <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/5 to-transparent animate-pulse-subtle" />

      {/* Actual BitchBoy Mascot Badge */}
      <img
        src={bitchboyLogo}
        alt="BitchBoy Mascot"
        className="w-11 h-11 object-contain rounded-md shadow-md flex-shrink-0"
        style={{
          imageRendering: 'pixelated',
          filter: isHovered ? `drop-shadow(0 0 8px ${auraColor})` : 'none',
        }}
      />

      <div className="relative flex flex-col items-start">
        <span
          className="font-mono text-2xl font-black tracking-widest text-white drop-shadow-md leading-none"
          style={{
            textShadow: `0 0 10px ${auraColor}, 0 0 20px ${auraColor}`,
          }}
        >
          BiTCHBOY
        </span>
        <span className="font-mono text-[8px] font-bold tracking-[0.25em] text-amber-400 uppercase mt-0.5">
          Video Hardware Console V1
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
    "\"RESOLUME LAYER 1 IS ARMED!\"",
    "\"OBS STREAM SCENE TRANSITION READY!\"",
    "\"VIDEO FADER AT 127 OPACITY!\"",
    "\"PRESS SHIFT FOR MULTI-SELECT!\"",
    "\"RGB LED FEEDBACK MATRIX ACTIVE!\"",
    "\"TOUCHDESIGNER GENERATIVE NODES RUNNING!\"",
    "\"TAYLOR FOSSEN APPROVED! 👑\"",
    "\"STANDING BY FOR VIDEO CUES\"",
    "\"LED PROGRAMMING ENGINE ENGAGED!\"",
    "\"NEON GLOW AT MAXIMUM WATTAGE!\"",
  ];

  const handleClick = () => {
    setIsPressed(true);
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
          className="absolute -top-12 z-50 px-3 py-1.5 rounded-lg bg-black/95 border border-amber-400 text-amber-300 font-mono text-[9px] font-black tracking-wide text-center whitespace-nowrap shadow-xl animate-bounce"
          style={{
            boxShadow: `0 0 16px ${auraColor}88`,
          }}
        >
          {quote}
          <div className="absolute left-1/2 -bottom-1.5 -translate-x-1/2 w-3 h-3 bg-black/95 border-r border-b border-amber-400 rotate-45" />
        </div>
      )}

      {/* Mascot Button */}
      <button
        onClick={handleClick}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        className={`w-[186px] h-[92px] rounded-xl flex items-center justify-center p-1.5 transition-transform duration-200 cursor-pointer bg-black/60 border border-white/10 overflow-hidden relative ${
          isPressed ? 'scale-90' : isHovered ? 'scale-105 border-amber-400/70' : 'scale-100'
        }`}
        style={{
          boxShadow: isHovered
            ? `0 0 24px ${auraColor}aa, inset 0 0 14px ${auraColor}33`
            : `0 0 12px ${auraColor}33`,
        }}
        title="Click the official BitchBoy mascot!"
      >
        {/* Subtle orange neon glow background */}
        <div
          className="absolute inset-0 opacity-20 pointer-events-none transition-opacity"
          style={{
            backgroundColor: auraColor,
            opacity: isHovered ? 0.35 : 0.15,
          }}
        />

        {/* The Actual BitchBoy Mascot Logo Image */}
        <img
          src={bitchboyLogo}
          alt="BitchBoy Official Mascot"
          className="w-full h-full object-contain select-none drop-shadow-lg"
          style={{
            imageRendering: 'pixelated',
            filter: isHovered ? `drop-shadow(0 0 12px ${auraColor})` : 'none',
          }}
        />
      </button>
    </div>
  );
};
