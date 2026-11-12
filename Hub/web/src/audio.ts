/** Enregistrement micro → WAV mono 16 kHz (format accepté par la transcription d'Ollama). */
export class Recorder {
  private stream: MediaStream | null = null;
  private recorder: MediaRecorder | null = null;
  private chunks: Blob[] = [];
  private vadCtx: AudioContext | null = null;
  private vadTimer: number | undefined;
  private voiceFrames = 0;
  private noiseFloor = 1;
  readonly startedAt = Date.now();

  /** Vrai dès que le micro a capté de la voix (et pas seulement du bruit de fond). */
  heardVoice(): boolean {
    return this.voiceFrames >= 3;
  }

  /** Détection de voix très simple : énergie du signal comparée au bruit de fond mesuré au début. */
  private startVoiceDetection(stream: MediaStream) {
    try {
      this.vadCtx = new AudioContext();
      const analyser = this.vadCtx.createAnalyser();
      analyser.fftSize = 1024;
      this.vadCtx.createMediaStreamSource(stream).connect(analyser);
      const buf = new Float32Array(analyser.fftSize);
      let frames = 0;
      this.vadTimer = window.setInterval(() => {
        analyser.getFloatTimeDomainData(buf);
        let sum = 0;
        for (const v of buf) sum += v * v;
        const rms = Math.sqrt(sum / buf.length);
        frames++;
        if (frames <= 3) { this.noiseFloor = Math.min(this.noiseFloor, rms); return; }
        if (rms > Math.max(0.015, this.noiseFloor * 3.5)) this.voiceFrames++;
      }, 80);
    } catch {
      this.voiceFrames = 99; // pas d'analyse possible : on considère qu'il y a de la voix
    }
  }

  private stopVoiceDetection() {
    window.clearInterval(this.vadTimer);
    this.vadCtx?.close().catch(() => {});
    this.vadCtx = null;
  }

  /** Abandonne l'enregistrement sans rien envoyer. */
  cancel() {
    this.stopVoiceDetection();
    try { this.recorder?.stop(); } catch { /* déjà arrêté */ }
    this.stream?.getTracks().forEach((t) => t.stop());
  }

  static isAvailable(): boolean {
    return !!(navigator.mediaDevices?.getUserMedia) && window.isSecureContext && typeof MediaRecorder !== 'undefined';
  }

  async start(): Promise<void> {
    const deviceId = audioPref('mic');
    const constraints: MediaTrackConstraints = { echoCancellation: true, noiseSuppression: true, channelCount: 1 };
    if (deviceId) constraints.deviceId = { exact: deviceId };
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: constraints });
    } catch (e) {
      if (!deviceId) throw e;
      // Micro choisi débranché : on reprend le micro par défaut.
      delete constraints.deviceId;
      this.stream = await navigator.mediaDevices.getUserMedia({ audio: constraints });
    }
    this.chunks = [];
    this.startVoiceDetection(this.stream);
    this.recorder = new MediaRecorder(this.stream);
    this.recorder.ondataavailable = (e) => e.data.size > 0 && this.chunks.push(e.data);
    this.recorder.start();
  }

  async stop(): Promise<Blob> {
    const rec = this.recorder;
    if (!rec) throw new Error('Pas d\'enregistrement en cours');
    this.stopVoiceDetection();
    await new Promise<void>((resolve) => { rec.onstop = () => resolve(); rec.stop(); });
    this.stream?.getTracks().forEach((t) => t.stop());
    const blob = new Blob(this.chunks, { type: rec.mimeType });
    return toWav16k(blob);
  }
}

async function toWav16k(blob: Blob): Promise<Blob> {
  const ctx = new AudioContext();
  const decoded = await ctx.decodeAudioData(await blob.arrayBuffer());
  await ctx.close();
  const rate = 16000;
  const offline = new OfflineAudioContext(1, Math.max(1, Math.ceil(decoded.duration * rate)), rate);
  const src = offline.createBufferSource();
  src.buffer = decoded;
  src.connect(offline.destination);
  src.start();
  const rendered = await offline.startRendering();
  return encodeWav(rendered.getChannelData(0), rate);
}

function encodeWav(samples: Float32Array, rate: number): Blob {
  const buffer = new ArrayBuffer(44 + samples.length * 2);
  const v = new DataView(buffer);
  const str = (o: number, s: string) => { for (let i = 0; i < s.length; i++) v.setUint8(o + i, s.charCodeAt(i)); };
  str(0, 'RIFF'); v.setUint32(4, 36 + samples.length * 2, true); str(8, 'WAVE'); str(12, 'fmt ');
  v.setUint32(16, 16, true); v.setUint16(20, 1, true); v.setUint16(22, 1, true); v.setUint32(24, rate, true);
  v.setUint32(28, rate * 2, true); v.setUint16(32, 2, true); v.setUint16(34, 16, true); str(36, 'data');
  v.setUint32(40, samples.length * 2, true);
  for (let i = 0; i < samples.length; i++) {
    const s = Math.max(-1, Math.min(1, samples[i]));
    v.setInt16(44 + i * 2, s < 0 ? s * 0x8000 : s * 0x7fff, true);
  }
  return new Blob([buffer], { type: 'audio/wav' });
}

let current: HTMLAudioElement | null = null;

/** La radio est-elle en train de parler sur cet écran ? */
export function isPlaying(): boolean {
  return !!current && !current.paused && !current.ended;
}

/** Coupe immédiatement la voix de l'ingénieur sur cet écran. */
export function stopPlayback() {
  if (current) {
    current.pause();
    current.currentTime = 0;
  }
}

/** Joue un WAV base64 (coupe la lecture précédente : la radio ne se superpose pas). */
export function playBase64Wav(b64: string) {
  current?.pause();
  const audio = new Audio(`data:audio/wav;base64,${b64}`);
  current = audio;
  const sink = audioPref('speaker');
  const withSink = audio as HTMLAudioElement & { setSinkId?: (id: string) => Promise<void> };
  const ready = sink && withSink.setSinkId ? withSink.setSinkId(sink).catch(() => { /* sortie indisponible : sortie par défaut */ }) : Promise.resolve();
  ready.then(() => audio.play()).catch(() => { /* lecture bloquée tant que l'utilisateur n'a pas interagi avec la page */ });
}

// ───── Choix du micro et du haut-parleur de cet écran (mémorisé sur l'appareil) ─────

export function audioPref(kind: 'mic' | 'speaker'): string {
  try { return localStorage.getItem(`smhub-audio-${kind}`) ?? ''; } catch { return ''; }
}

export function setAudioPref(kind: 'mic' | 'speaker', id: string) {
  try { if (id) localStorage.setItem(`smhub-audio-${kind}`, id); else localStorage.removeItem(`smhub-audio-${kind}`); } catch { /* stockage indisponible */ }
}

export function canChooseSpeaker(): boolean {
  return typeof (HTMLMediaElement.prototype as unknown as { setSinkId?: unknown }).setSinkId === 'function';
}

/** Micros et sorties audio de cet appareil. Les noms n'apparaissent qu'après avoir autorisé le micro une fois. */
export async function listAudioDevices(askPermission = false): Promise<{ inputs: MediaDeviceInfo[]; outputs: MediaDeviceInfo[]; named: boolean }> {
  if (!navigator.mediaDevices?.enumerateDevices) return { inputs: [], outputs: [], named: false };
  if (askPermission) {
    try {
      const s = await navigator.mediaDevices.getUserMedia({ audio: true });
      s.getTracks().forEach((t) => t.stop());
    } catch { /* refusé */ }
  }
  const all = await navigator.mediaDevices.enumerateDevices();
  const inputs = all.filter((d) => d.kind === 'audioinput');
  const outputs = all.filter((d) => d.kind === 'audiooutput');
  return { inputs, outputs, named: inputs.some((d) => d.label) };
}

/** Petit son de test sur la sortie choisie. */
export async function testSpeaker() {
  const ctx = new AudioContext();
  const dest = ctx.createMediaStreamDestination();
  const o = ctx.createOscillator();
  const g = ctx.createGain();
  o.frequency.value = 660;
  g.gain.value = 0.15;
  o.connect(g).connect(dest);
  const el = new Audio() as HTMLAudioElement & { setSinkId?: (id: string) => Promise<void> };
  el.srcObject = dest.stream;
  const sink = audioPref('speaker');
  if (sink && el.setSinkId) await el.setSinkId(sink).catch(() => {});
  o.start();
  await el.play().catch(() => {});
  window.setTimeout(() => { o.stop(); el.pause(); ctx.close(); }, 450);
}
