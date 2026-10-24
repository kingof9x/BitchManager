import { MIDIOutputTarget, MIDIVelocityCurve } from './types';
import { audio } from './audio';

export interface WebMidiPort {
  id: string;
  name?: string;
  state?: string;
  type?: 'input' | 'output';
}

export interface WebMidiInput extends WebMidiPort {
  onmidimessage: ((event: { data: Uint8Array }) => void) | null;
}

export interface WebMidiOutput extends WebMidiPort {
  send: (data: number[] | Uint8Array, timestamp?: number) => void;
}

export interface WebMidiAccess {
  inputs: Map<string, WebMidiInput>;
  outputs: Map<string, WebMidiOutput>;
  onstatechange: (() => void) | null;
}

// Web MIDI Manager supporting real hardware controllers and software MIDI loops
export class MIDIManager {
  private midiAccess: WebMidiAccess | null = null;
  private isAvailable: boolean = false;
  private onMessageCallback: ((status: number, data1: number, data2: number, isFromHardware: boolean) => void) | null = null;
  private onStateChangeCallback: (() => void) | null = null;

  public async init(
    onMessage: (status: number, data1: number, data2: number, isFromHardware: boolean) => void,
    onStateChange: () => void
  ): Promise<boolean> {
    this.onMessageCallback = onMessage;
    this.onStateChangeCallback = onStateChange;

    const nav = typeof navigator !== 'undefined' ? (navigator as unknown as { requestMIDIAccess?: (opts?: { sysex: boolean }) => Promise<WebMidiAccess> }) : null;

    if (nav && typeof nav.requestMIDIAccess === 'function') {
      try {
        const access = await nav.requestMIDIAccess({ sysex: false });
        this.midiAccess = access;
        this.isAvailable = true;

        this.attachInputs();

        this.midiAccess.onstatechange = () => {
          this.attachInputs();
          if (this.onStateChangeCallback) {
            this.onStateChangeCallback();
          }
        };

        return true;
      } catch (err) {
        console.warn('Web MIDI permission denied or unavailable:', err);
        return false;
      }
    }
    return false;
  }

  private attachInputs() {
    if (!this.midiAccess) return;

    this.midiAccess.inputs.forEach((input: WebMidiInput) => {
      input.onmidimessage = (event: { data: Uint8Array }) => {
        if (!event.data || event.data.length < 2) return;
        const status = event.data[0];
        const data1 = event.data[1];
        const data2 = event.data.length > 2 ? event.data[2] : 0;

        if (this.onMessageCallback) {
          this.onMessageCallback(status, data1, data2, true);
        }
      };
    });
  }

  public getAvailableOutputs(): MIDIOutputTarget[] {
    const targets: MIDIOutputTarget[] = [
      {
        id: 'virtual_synth',
        name: 'BitchBoy Internal Web Synth',
        isHardwareBitchBoy: false,
      },
      {
        id: 'virtual_loop',
        name: 'BitchBoy Virtual Web Out',
        isHardwareBitchBoy: false,
      },
    ];

    if (this.midiAccess) {
      this.midiAccess.outputs.forEach((output: WebMidiOutput) => {
        const name = output.name || `MIDI Output (${output.id})`;
        const isBB = name.toLowerCase().includes('bitch') || name.toLowerCase().includes('pico') || name.toLowerCase().includes('rp2040');
        targets.push({
          id: output.id,
          name,
          isHardwareBitchBoy: isBB,
        });
      });
    }

    return targets;
  }

  public sendMIDIToOutputs(
    selectedTargets: Set<string>,
    status: number,
    data1: number,
    data2: number,
    channelRemap: number, // 0 = keep, 1..16 = force 0..15
    transposeSemitones: number,
    velocityCurve: MIDIVelocityCurve,
    virtualOutEnabled: boolean
  ): string {
    const msgType = status & 0xf0;
    const isNote = msgType === 0x90 || msgType === 0x80;
    const isCC = msgType === 0xb0;

    const outChan = channelRemap === 0 ? (status & 0x0f) : (channelRemap - 1) & 0x0f;
    const outStatus = msgType | outChan;

    let outData1 = data1;
    let outData2 = data2;

    if (isNote) {
      outData1 = Math.max(0, Math.min(127, data1 + transposeSemitones));
      if (msgType === 0x90 && data2 > 0) {
        outData2 = this.applyVelocityCurve(data2, velocityCurve);
      }
    }

    const bytes = [outStatus, outData1, outData2];

    // Always play with internal synthesizer if virtual synth is enabled
    if (selectedTargets.has('virtual_synth') && isNote) {
      if (msgType === 0x90 && outData2 > 0) {
        audio.playMIDINote(outData1, outData2, 0.25);
      }
    }

    // Send to Web MIDI destinations
    if (this.midiAccess && virtualOutEnabled) {
      this.midiAccess.outputs.forEach((output: WebMidiOutput) => {
        if (selectedTargets.has(output.name || '') || selectedTargets.has(output.id)) {
          try {
            output.send(bytes);
          } catch (e) {
            console.error('Failed to send MIDI to target', output.name, e);
          }
        }
      });
    }

    const hex = bytes.map((b) => b.toString(16).toUpperCase().padStart(2, '0')).join(' ');
    const chDisplay = outChan + 1;
    let desc = '';
    if (isCC) {
      desc = `CH${chDisplay} CC#${outData1} = ${outData2}`;
    } else if (msgType === 0x90 && outData2 > 0) {
      desc = `CH${chDisplay} NOTE ON #${outData1} V:${outData2}`;
    } else {
      desc = `CH${chDisplay} NOTE OFF #${outData1}`;
    }

    return `[${hex}] ${desc}`;
  }

  private applyVelocityCurve(v: number, curve: MIDIVelocityCurve): number {
    if (v <= 0) return 0;
    const norm = v / 127.0;
    switch (curve) {
      case 'LINEAR':
        return v;
      case 'SOFT':
        return Math.min(127, Math.max(1, Math.round(Math.pow(norm, 1.65) * 127.0)));
      case 'HARD':
        return Math.min(127, Math.max(1, Math.round(Math.pow(norm, 0.55) * 127.0)));
      case 'MAX 127':
        return 127;
    }
  }

  public isConnectedToHardware(): boolean {
    if (!this.midiAccess) return false;
    let found = false;
    this.midiAccess.inputs.forEach((input: WebMidiInput) => {
      const n = (input.name || '').toLowerCase();
      if (n.includes('bitch') || n.includes('rp2040') || n.includes('pico') || n.includes('arduino')) {
        found = true;
      }
    });
    return found;
  }

  public getHardwareDeviceName(): string {
    if (!this.midiAccess) return 'BitchBoy V1 (Simulated)';
    for (const input of this.midiAccess.inputs.values()) {
      const n = input.name || '';
      if (n.toLowerCase().includes('bitch') || n.toLowerCase().includes('rp2040')) {
        return n;
      }
    }
    const first = Array.from(this.midiAccess.inputs.values())[0];
    if (first && first.name) {
      return `${first.name} (Active)`;
    }
    return 'BitchBoy V1 (Simulated)';
  }
}

export const midiManager = new MIDIManager();
