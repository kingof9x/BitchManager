import { SoundPack } from './types';

// Web Audio API Synthesizer Engine for BitchBoy Sound Effects & MIDI Tone Playback
class AudioEngine {
  private ctx: AudioContext | null = null;
  private soundPack: SoundPack = '8-BIT ARCADE';
  private masterGain: GainNode | null = null;

  private initCtx() {
    if (!this.ctx) {
      const AudioCtx = window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext;
      this.ctx = new AudioCtx();
      this.masterGain = this.ctx.createGain();
      this.masterGain.gain.value = 0.35;
      this.masterGain.connect(this.ctx.destination);
    }
    if (this.ctx.state === 'suspended') {
      this.ctx.resume();
    }
  }

  public setSoundPack(pack: SoundPack) {
    this.soundPack = pack;
  }

  // Key tap sound effect
  public playKeyTap(noteNumber?: number, velocity: number = 100) {
    if (this.soundPack === 'MUTE SOUNDS') return;
    this.initCtx();
    if (!this.ctx || !this.masterGain) return;

    const t = this.ctx.currentTime;

    switch (this.soundPack) {
      case '8-BIT ARCADE': {
        // Crisp 8-bit square wave blip
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'square';
        const baseFreq = noteNumber ? 440 * Math.pow(2, (noteNumber - 69) / 12) : 520;
        osc.frequency.setValueAtTime(baseFreq, t);
        osc.frequency.exponentialRampToValueAtTime(baseFreq * 1.5, t + 0.04);

        gain.gain.setValueAtTime(0.25 * (velocity / 127), t);
        gain.gain.exponentialRampToValueAtTime(0.001, t + 0.08);

        osc.connect(gain);
        gain.connect(this.masterGain);
        osc.start(t);
        osc.stop(t + 0.08);
        break;
      }
      case 'CHERRY MX BLUE': {
        // High-tactile mechanical snap and click
        const bufferSize = this.ctx.sampleRate * 0.035;
        const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
        const data = buffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
          data[i] = (Math.random() * 2 - 1) * Math.exp(-i / (this.ctx.sampleRate * 0.005));
        }
        const noise = this.ctx.createBufferSource();
        noise.buffer = buffer;

        const filter = this.ctx.createBiquadFilter();
        filter.type = 'highpass';
        filter.frequency.value = 2800;

        const gain = this.ctx.createGain();
        gain.gain.setValueAtTime(0.4 * (velocity / 127), t);
        gain.gain.exponentialRampToValueAtTime(0.001, t + 0.04);

        noise.connect(filter);
        filter.connect(gain);
        gain.connect(this.masterGain);
        noise.start(t);
        break;
      }
      case 'SCI-FI SYNTH': {
        // Futuristic FM sweep
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'sine';
        const baseFreq = noteNumber ? 440 * Math.pow(2, (noteNumber - 69) / 12) : 660;
        osc.frequency.setValueAtTime(baseFreq * 2.2, t);
        osc.frequency.exponentialRampToValueAtTime(baseFreq * 0.7, t + 0.1);

        gain.gain.setValueAtTime(0.28 * (velocity / 127), t);
        gain.gain.exponentialRampToValueAtTime(0.001, t + 0.1);

        osc.connect(gain);
        gain.connect(this.masterGain);
        osc.start(t);
        osc.stop(t + 0.1);
        break;
      }
    }
  }

  // Fader movement tick
  public playFaderSlide() {
    if (this.soundPack === 'MUTE SOUNDS') return;
    this.initCtx();
    if (!this.ctx || !this.masterGain) return;

    const t = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = 'triangle';
    osc.frequency.setValueAtTime(320, t);
    osc.frequency.linearRampToValueAtTime(180, t + 0.03);

    gain.gain.setValueAtTime(0.08, t);
    gain.gain.exponentialRampToValueAtTime(0.001, t + 0.03);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(t);
    osc.stop(t + 0.03);
  }

  // Rotary knob tick
  public playKnobTick() {
    if (this.soundPack === 'MUTE SOUNDS') return;
    this.initCtx();
    if (!this.ctx || !this.masterGain) return;

    const t = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = 'sine';
    osc.frequency.setValueAtTime(800, t);

    gain.gain.setValueAtTime(0.12, t);
    gain.gain.exponentialRampToValueAtTime(0.001, t + 0.02);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(t);
    osc.stop(t + 0.02);
  }

  // Mascot squeak
  public playMascotSqueak() {
    this.initCtx();
    if (!this.ctx || !this.masterGain) return;

    const t = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = 'sine';
    osc.frequency.setValueAtTime(450, t);
    osc.frequency.exponentialRampToValueAtTime(1300, t + 0.12);
    osc.frequency.exponentialRampToValueAtTime(750, t + 0.22);

    gain.gain.setValueAtTime(0.35, t);
    gain.gain.exponentialRampToValueAtTime(0.001, t + 0.25);

    osc.connect(gain);
    gain.connect(this.masterGain);
    osc.start(t);
    osc.stop(t + 0.25);
  }

  // Preset switch / slot change sound
  public playPresetSwitch() {
    if (this.soundPack === 'MUTE SOUNDS') return;
    this.initCtx();
    if (!this.ctx || !this.masterGain) return;

    const t = this.ctx.currentTime;
    const notes = [523.25, 659.25, 783.99, 1046.5]; // C5, E5, G5, C6 arpeggio
    notes.forEach((freq, idx) => {
      const osc = this.ctx!.createOscillator();
      const gain = this.ctx!.createGain();
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(freq, t + idx * 0.035);

      gain.gain.setValueAtTime(0.18, t + idx * 0.035);
      gain.gain.exponentialRampToValueAtTime(0.001, t + idx * 0.035 + 0.1);

      osc.connect(gain);
      gain.connect(this.masterGain!);
      osc.start(t + idx * 0.035);
      osc.stop(t + idx * 0.035 + 0.1);
    });
  }

  // Play musical polyphonic notes for MIDI feedback
  public playMIDINote(note: number, velocity: number = 100, durationSec: number = 0.2) {
    this.initCtx();
    if (!this.ctx || !this.masterGain) return;

    const t = this.ctx.currentTime;
    const freq = 440 * Math.pow(2, (note - 69) / 12);

    const osc = this.ctx.createOscillator();
    const filter = this.ctx.createBiquadFilter();
    const gain = this.ctx.createGain();

    osc.type = 'sawtooth';
    osc.frequency.setValueAtTime(freq, t);

    filter.type = 'lowpass';
    filter.frequency.setValueAtTime(freq * 3, t);
    filter.frequency.exponentialRampToValueAtTime(freq * 1.2, t + durationSec);

    const amp = (velocity / 127) * 0.25;
    gain.gain.setValueAtTime(amp, t);
    gain.gain.exponentialRampToValueAtTime(0.001, t + durationSec);

    osc.connect(filter);
    filter.connect(gain);
    gain.connect(this.masterGain);

    osc.start(t);
    osc.stop(t + durationSec);
  }
}

export const audio = new AudioEngine();
