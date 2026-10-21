// Beyond Nations' sound: the "bn-audio" module the game calls through
// JSImport (game/WebAudioSoundService.cs). Every sound is synthesised with
// WebAudio when it is played -- oscillators and filtered noise -- so the game
// ships no audio files and there are no licences to track.
//
// Browsers only allow sound after the player has interacted with the page,
// so the AudioContext is created on the first key, click or tap. Until then
// cues are dropped and the ambient loop waits, then starts on that gesture.

import { isTouchOnly as touchOnlyDevice } from "./webhost/touch.js";

let ctx = null;
let out = null;
let noiseBuffer = null;
let unavailable = false;

const ambient = { kind: "none", volume: 0, gain: null, filter: null, source: null, timer: 0 };

function makeNoise(context, seconds) {
  const length = Math.floor(context.sampleRate * seconds);
  const buffer = context.createBuffer(1, length, context.sampleRate);
  const data = buffer.getChannelData(0);
  for (let i = 0; i < length; i++) data[i] = Math.random() * 2 - 1;
  return buffer;
}

function unlock() {
  if (unavailable) return;
  try {
    if (!ctx) {
      const Context = window.AudioContext || window.webkitAudioContext;
      if (!Context) { unavailable = true; return; }
      ctx = new Context();
      // A gentle limiter, so a burst of cues at once never clips.
      const limiter = ctx.createDynamicsCompressor();
      limiter.threshold.value = -10;
      limiter.ratio.value = 8;
      limiter.connect(ctx.destination);
      out = limiter;
      noiseBuffer = makeNoise(ctx, 2);
    }
    if (ctx.state === "suspended") ctx.resume().catch(() => {});
    applyAmbient();
  } catch (error) {
    console.warn("audio unavailable:", error);
    unavailable = true;
  }
}

for (const type of ["pointerdown", "keydown", "touchend"]) {
  window.addEventListener(type, unlock, { capture: true, passive: true });
}

// --- building blocks ---

function envelope(gainNode, start, attack, duration, peak) {
  const g = gainNode.gain;
  g.setValueAtTime(0.0001, start);
  g.exponentialRampToValueAtTime(Math.max(0.0002, peak), start + attack);
  g.exponentialRampToValueAtTime(0.0001, start + duration);
}

function tone(at, frequency, duration, { type = "sine", gain = 0.3, to = 0, attack = 0.005 } = {}, volume = 1) {
  const osc = ctx.createOscillator();
  const amp = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(frequency, at);
  if (to > 0) osc.frequency.exponentialRampToValueAtTime(to, at + duration);
  envelope(amp, at, attack, duration, gain * volume);
  osc.connect(amp).connect(out);
  osc.start(at);
  osc.stop(at + duration + 0.05);
}

function noise(at, duration, { filter = "bandpass", frequency = 1000, q = 1, gain = 0.5, to = 0, attack = 0.002 } = {}, volume = 1) {
  const source = ctx.createBufferSource();
  source.buffer = noiseBuffer;
  const shape = ctx.createBiquadFilter();
  shape.type = filter;
  shape.frequency.setValueAtTime(frequency, at);
  if (to > 0) shape.frequency.exponentialRampToValueAtTime(to, at + duration);
  shape.Q.value = q;
  const amp = ctx.createGain();
  envelope(amp, at, attack, duration, gain * volume);
  source.connect(shape).connect(amp).connect(out);
  source.start(at, Math.random() * 1.5);
  source.stop(at + duration + 0.05);
}

// --- the cues (SoundCue in ui/feel/SoundCue.cs) ---

const cues = {
  chop(t, v) {
    noise(t, 0.13, { frequency: 900, q: 1.4, gain: 0.9 }, v);
    tone(t, 150, 0.15, { type: "triangle", gain: 0.55, to: 70 }, v);
  },
  mine(t, v) {
    noise(t, 0.05, { filter: "highpass", frequency: 3000, gain: 0.45 }, v);
    tone(t, 1900, 0.2, { gain: 0.22 }, v);
    tone(t, 2870, 0.14, { gain: 0.14 }, v);
  },
  pickup(t, v) {
    tone(t, 660, 0.12, { gain: 0.3, to: 1320 }, v);
  },
  craft(t, v) {
    noise(t, 0.05, { frequency: 2200, q: 2, gain: 0.5 }, v);
    noise(t + 0.09, 0.05, { frequency: 2600, q: 2, gain: 0.45 }, v);
    tone(t + 0.16, 880, 0.3, { type: "triangle", gain: 0.25 }, v);
  },
  build(t, v) {
    [523.25, 659.25, 783.99, 1046.5].forEach((f, i) => tone(t + i * 0.09, f, 0.26, { type: "triangle", gain: 0.28 }, v));
  },
  coin(t, v) {
    tone(t, 1318.5, 0.09, { type: "square", gain: 0.09 }, v);
    tone(t + 0.07, 1760, 0.3, { type: "square", gain: 0.08 }, v);
  },
  hunger(t, v) {
    tone(t, 330, 0.22, { type: "triangle", gain: 0.35 }, v);
    tone(t + 0.2, 247, 0.4, { type: "triangle", gain: 0.35 }, v);
  },
  hit(t, v) {
    noise(t, 0.16, { filter: "lowpass", frequency: 700, gain: 0.9 }, v);
    tone(t, 170, 0.2, { gain: 0.8, to: 50 }, v);
  },
  click(t, v) {
    tone(t, 1250, 0.035, { gain: 0.14 }, v);
  },
  objective(t, v) {
    [523.25, 659.25, 783.99].forEach((f, i) => tone(t + i * 0.1, f, 0.22, { type: "triangle", gain: 0.26 }, v));
    tone(t + 0.3, 1046.5, 0.7, { type: "triangle", gain: 0.24 }, v);
    tone(t + 0.3, 1318.5, 0.7, { type: "triangle", gain: 0.16 }, v);
  },
};

// --- the ambient loop: wind and birds by day, a lower wind and crickets by night ---

function stopAmbient() {
  clearTimeout(ambient.timer);
  ambient.timer = 0;
  if (ambient.source) {
    const { source, gain } = ambient;
    gain.gain.setTargetAtTime(0.0001, ctx.currentTime, 0.4);
    setTimeout(() => { try { source.stop(); } catch (_) { /* already stopped */ } }, 2000);
  }
  ambient.source = null;
  ambient.gain = null;
  ambient.filter = null;
}

function scheduleCritters() {
  const night = ambient.kind === "night";
  ambient.timer = setTimeout(() => {
    if (!ctx || !ambient.source) return;
    const t = ctx.currentTime + 0.02;
    const v = ambient.volume;
    if (night) {
      for (let i = 0; i < 3; i++) tone(t + i * 0.07, 4400, 0.04, { gain: 0.05 }, v);
    } else {
      const base = 2400 + Math.random() * 1200;
      const notes = 2 + Math.floor(Math.random() * 3);
      for (let i = 0; i < notes; i++) tone(t + i * 0.11, base, 0.08, { gain: 0.06, to: base * 1.4 }, v);
    }
    scheduleCritters();
  }, night ? 700 + Math.random() * 1500 : 1800 + Math.random() * 3500);
}

function applyAmbient() {
  if (!ctx) return;
  const wanted = ambient.kind;
  const level = wanted === "night" ? 0.08 : 0.11;
  if (wanted === "none" || ambient.volume <= 0.001) {
    stopAmbient();
    return;
  }
  if (!ambient.source || ambient.playing !== wanted) {
    stopAmbient();
    const source = ctx.createBufferSource();
    source.buffer = noiseBuffer;
    source.loop = true;
    const filter = ctx.createBiquadFilter();
    filter.type = "lowpass";
    filter.frequency.value = wanted === "night" ? 260 : 480;
    const gain = ctx.createGain();
    gain.gain.value = 0.0001;
    source.connect(filter).connect(gain).connect(out);
    source.start();
    Object.assign(ambient, { source, filter, gain, playing: wanted });
    scheduleCritters();
  }
  ambient.gain.gain.setTargetAtTime(level * ambient.volume, ctx.currentTime, 0.6);
}

// --- the JSImport surface ---

export function play(cue, volume) {
  if (!ctx || ctx.state !== "running") return;
  const make = cues[cue];
  if (!make) return;
  try { make(ctx.currentTime + 0.005, Math.max(0, Math.min(1, volume))); } catch (error) { console.warn("audio:", error); }
}

export function setAmbient(kind, volume) {
  ambient.kind = kind;
  ambient.volume = Math.max(0, Math.min(1, volume));
  try { applyAmbient(); } catch (error) { console.warn("audio:", error); }
}

export function state() {
  if (unavailable) return "unavailable";
  if (!ctx) return "locked";
  return ctx.state === "running" ? "running" : "locked";
}

export function isTouchOnly() {
  return touchOnlyDevice();
}
