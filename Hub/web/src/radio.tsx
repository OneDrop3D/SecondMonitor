import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import { Recorder, isPlaying, playBase64Wav, stopPlayback } from './audio';

export interface RadioMessage {
  question?: string | null;
  transcript?: string | null;
  answer: string;
  audioBase64?: string | null;
  from: string;
  facts?: string | null;
  milliseconds: number;
  listenMilliseconds?: number | null;
  thinkMilliseconds?: number | null;
  test?: boolean;
  scenario?: string | null;
  usedPreparedAnswer?: boolean;
  kind?: string | null;
  proactive?: boolean;
  awaitingAnswer?: boolean;
  /** Catégorie du carnet : « Stratégie », « Réglages de course », « Réglages de la voiture », « Informations course ». */
  category?: string | null;
  categorySource?: string | null;
  /** Alerte de pilotage (limites de piste, usure brutale…). */
  urgent?: boolean;
  /** Réponse arrivée après « Copy, je reviens vers vous » ; inReplyTo rappelle la question. */
  followUp?: boolean;
  inReplyTo?: string | null;
  /** Spécialiste qui a répondu : « pneus », « stratégie », « voiture », « course ». */
  specialist?: string | null;
  at: string;
}

/** Bouton du volant (ou de la manette) associé à la radio. Mémorisé sur cet appareil uniquement. */
export interface WheelBinding {
  gamepadId: string;
  button: number;
  mode: 'hold' | 'toggle';
}

export interface PadInfo { index: number; id: string; buttons: number; pressed: number[] }

type Phase = 'idle' | 'recording' | 'thinking';

interface RadioApi {
  phase: Phase;
  error: string | null;
  clearError: () => void;
  micAvailable: boolean;
  gamepadAvailable: boolean;
  role: string;
  setRole: (r: string) => void;
  speakHere: boolean;
  setSpeakHere: (v: boolean) => void;
  testMode: boolean;
  setTestMode: (v: boolean) => void;
  /** Réponses du mode test (visibles seulement sur cet écran). */
  testReplies: RadioMessage[];
  startTalk: () => void;
  stopTalk: () => void;
  /** Un appui : le modèle écoute. Un second appui : on coupe et on envoie. */
  toggleTalk: () => void;
  ask: (text: string, opts?: { test?: boolean }) => Promise<void>;
  binding: WheelBinding | null;
  setBinding: (b: WheelBinding | null) => void;
  learning: boolean;
  learn: () => void;
  cancelLearn: () => void;
  pads: PadInfo[];
  /** Vrai tant que le bouton associé est enfoncé (pour le tester). */
  boundPressed: boolean;
}

const RadioContext = createContext<RadioApi | null>(null);

export function useRadio(): RadioApi {
  const ctx = useContext(RadioContext);
  if (!ctx) throw new Error('RadioProvider manquant');
  return ctx;
}

function readPref(key: string, fallback: string) {
  try { return localStorage.getItem(key) ?? fallback; } catch { return fallback; }
}
function writePref(key: string, value: string | null) {
  try { if (value == null) localStorage.removeItem(key); else localStorage.setItem(key, value); } catch { /* stockage indisponible */ }
}

let audioCtx: AudioContext | null = null;
/** Petit bip radio (début / fin d'enregistrement), pour que le pilote sache qu'on l'écoute sans regarder l'écran. */
function beep(freq: number, ms = 90) {
  try {
    audioCtx ??= new AudioContext();
    const o = audioCtx.createOscillator();
    const g = audioCtx.createGain();
    o.frequency.value = freq;
    o.type = 'sine';
    g.gain.setValueAtTime(0.0001, audioCtx.currentTime);
    g.gain.exponentialRampToValueAtTime(0.18, audioCtx.currentTime + 0.01);
    g.gain.exponentialRampToValueAtTime(0.0001, audioCtx.currentTime + ms / 1000);
    o.connect(g).connect(audioCtx.destination);
    o.start();
    o.stop(audioCtx.currentTime + ms / 1000 + 0.02);
  } catch { /* audio indisponible */ }
}

function readPads(): Gamepad[] {
  try {
    return [...(navigator.getGamepads?.() ?? [])].filter((p): p is Gamepad => !!p);
  } catch {
    return [];
  }
}

export function RadioProvider({ children }: { children: ReactNode }) {
  const [phase, setPhase] = useState<Phase>('idle');
  const [error, setError] = useState<string | null>(null);
  const [role, setRoleState] = useState(() => readPref('smhub-role', 'ingénieur'));
  const [speakHere, setSpeakHereState] = useState(() => readPref('smhub-speak', 'true') === 'true');
  const [testMode, setTestModeState] = useState(() => readPref('smhub-test', 'false') === 'true');
  const [testReplies, setTestReplies] = useState<RadioMessage[]>([]);
  const [binding, setBindingState] = useState<WheelBinding | null>(() => {
    try { return JSON.parse(readPref('smhub-wheel', 'null')); } catch { return null; }
  });
  const [learning, setLearning] = useState(false);
  const [pads, setPads] = useState<PadInfo[]>([]);
  const [boundPressed, setBoundPressed] = useState(false);

  const recorder = useRef<Recorder | null>(null);
  const phaseRef = useRef<Phase>('idle');
  const stopRequested = useRef(false);
  const state = useRef({ role, testMode, speakHere, binding, learning });
  state.current = { role, testMode, speakHere, binding, learning };
  phaseRef.current = phase;

  const setRole = (r: string) => { setRoleState(r); writePref('smhub-role', r); };
  const setSpeakHere = (v: boolean) => { setSpeakHereState(v); writePref('smhub-speak', String(v)); };
  const setTestMode = (v: boolean) => { setTestModeState(v); writePref('smhub-test', String(v)); };
  const setBinding = (b: WheelBinding | null) => { setBindingState(b); writePref('smhub-wheel', b ? JSON.stringify(b) : null); };

  const handleReply = useCallback((json: RadioMessage & { error?: string }) => {
    if (json.test) {
      setTestReplies((r) => [...r.slice(-19), json]);
      if (state.current.speakHere && json.audioBase64) playBase64Wav(json.audioBase64);
    }
  }, []);

  /** Fait taire la radio : ici tout de suite, et sur tous les écrans via le hub (y compris une réponse en préparation). */
  const silence = useCallback(() => {
    stopPlayback();
    fetch('/api/radio/stop', { method: 'POST' }).catch(() => { /* hub injoignable */ });
  }, []);

  const send = useCallback(async (wav: Blob) => {
    setPhase('thinking');
    try {
      const form = new FormData();
      form.append('audio', wav, 'question.wav');
      form.append('from', state.current.role);
      form.append('test', String(state.current.testMode));
      const res = await fetch('/api/ai/listen', { method: 'POST', body: form });
      const json = await res.json();
      if (!res.ok || json.error) throw new Error(json.error ?? res.statusText);
      if (!json.silent) handleReply(json);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setPhase('idle');
    }
  }, [handleReply]);

  /** Rien n'a été dit (appui bref pour faire taire la radio) : on referme sans rien envoyer ni afficher d'erreur. */
  const cancelSilently = useCallback(() => {
    recorder.current?.cancel();
    recorder.current = null;
    phaseRef.current = 'idle';
    setPhase('idle');
    beep(440, 70);
  }, []);

  const stopTalk = useCallback(async () => {
    if (phaseRef.current !== 'recording') {
      stopRequested.current = true; // relâché avant que le micro ne soit prêt
      return;
    }

    const rec = recorder.current;
    if (!rec) return;
    if (Date.now() - rec.startedAt < 700 || !rec.heardVoice()) {
      cancelSilently();
      return;
    }

    phaseRef.current = 'thinking';
    setPhase('thinking');
    beep(660);
    try {
      const wav = await rec.stop();
      if (wav.size < 44 + 16000 * 2 * 0.25) {
        phaseRef.current = 'idle';
        setPhase('idle');
        return;
      }

      await send(wav);
    } catch (e) {
      setError((e as Error).message);
      setPhase('idle');
    }
  }, [send, cancelSilently]);

  const startTalk = useCallback(async () => {
    // Appuyer pendant que l'ingénieur parle (ou prépare sa réponse) le coupe.
    if (isPlaying() || phaseRef.current === 'thinking') silence();
    if (phaseRef.current !== 'idle') return;
    setError(null);
    stopRequested.current = false;
    if (!Recorder.isAvailable()) {
      setError("Micro indisponible : ouvrez l'interface en HTTPS (adresse dans Réglages) ou sur http://localhost.");
      return;
    }

    try {
      recorder.current = new Recorder();
      await recorder.current.start();
      phaseRef.current = 'recording';
      setPhase('recording');
      beep(880);
      // Personne ne parle pendant 6 s : on referme tout seul (c'était juste pour couper la radio).
      const rec = recorder.current;
      window.setTimeout(() => { if (recorder.current === rec && phaseRef.current === 'recording' && !rec?.heardVoice()) cancelSilently(); }, 6000);
      // Sécurité : envoi automatique après 30 s.
      window.setTimeout(() => { if (recorder.current === rec && phaseRef.current === 'recording') stopTalk(); }, 30000);
      if (stopRequested.current) {
        window.setTimeout(() => stopTalk(), 300);
      }
    } catch (e) {
      setError(`Micro indisponible : ${(e as Error).message}`);
      setPhase('idle');
    }
  }, [stopTalk, silence, cancelSilently]);

  const ask = useCallback(async (text: string, opts?: { test?: boolean }) => {
    if (!text.trim()) return;
    setPhase('thinking');
    setError(null);
    try {
      const res = await fetch('/api/ai/ask', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text, speak: true, from: state.current.role, test: opts?.test ?? state.current.testMode }),
      });
      const json = await res.json();
      if (!res.ok || json.error) throw new Error(json.error ?? res.statusText);
      handleReply(json);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setPhase('idle');
    }
  }, [handleReply]);

  // ───── Appuis venus d'ailleurs (bouton du PC de jeu, ClickDeck) : même effet que le bouton à l'écran ─────
  useEffect(() => {
    const onRemote = () => {
      if (phaseRef.current === 'recording') stopTalk(); else if (phaseRef.current === 'idle') startTalk();
    };
    window.addEventListener('sm-radio-press', onRemote);
    return () => window.removeEventListener('sm-radio-press', onRemote);
  }, [startTalk, stopTalk]);

  // L'état de la radio est signalé au serveur par le poste radio (affiché sur ClickDeck).
  useEffect(() => {
    if (!isRadioStation()) return;
    fetch('/api/radio/state', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ phase }) }).catch(() => undefined);
  }, [phase]);

  // ───── Volant / manette : lecture des boutons (API Gamepad) ─────
  useEffect(() => {
    const previous = new Map<string, boolean[]>();
    let wasPressed = false;
    let lastPadsKey = '';
    const timer = window.setInterval(() => {
      const list = readPads();
      const info: PadInfo[] = list.map((p) => ({ index: p.index, id: p.id, buttons: p.buttons.length, pressed: p.buttons.map((b, i) => (b.pressed ? i : -1)).filter((i) => i >= 0) }));
      const key = info.map((p) => `${p.index}:${p.id}:${p.pressed.join('.')}`).join('|');
      if (key !== lastPadsKey) { lastPadsKey = key; setPads(info); }

      const { binding: b, learning: isLearning } = state.current;
      if (isLearning) {
        for (const p of list) {
          const before = previous.get(p.id) ?? [];
          const idx = p.buttons.findIndex((btn, i) => btn.pressed && !before[i]);
          if (idx >= 0) {
            setBinding({ gamepadId: p.id, button: idx, mode: b?.mode ?? 'toggle' });
            setLearning(false);
            wasPressed = true; // le bouton encore enfoncé ne doit pas déclencher la radio
            beep(1200, 60);
            break;
          }
        }
      } else if (b) {
        const pad = list.find((p) => p.id === b.gamepadId);
        const pressed = !!pad?.buttons[b.button]?.pressed;
        if (pressed !== wasPressed) {
          setBoundPressed(pressed);
          if (b.mode === 'hold') {
            if (pressed) startTalk(); else stopTalk();
          } else if (pressed) {
            if (phaseRef.current === 'recording') stopTalk(); else if (phaseRef.current === 'idle') startTalk();
          }
          wasPressed = pressed;
        }
      }

      for (const p of list) previous.set(p.id, p.buttons.map((btn) => btn.pressed));
    }, 25);
    return () => window.clearInterval(timer);
  }, [startTalk, stopTalk]);

  const api: RadioApi = {
    phase,
    error,
    clearError: () => setError(null),
    micAvailable: Recorder.isAvailable(),
    gamepadAvailable: typeof navigator.getGamepads === 'function' && window.isSecureContext,
    role,
    setRole,
    speakHere,
    setSpeakHere,
    testMode,
    setTestMode,
    testReplies,
    startTalk,
    stopTalk,
    toggleTalk: () => { if (phaseRef.current === 'recording') stopTalk(); else if (phaseRef.current === 'idle') startTalk(); },
    ask,
    binding,
    setBinding,
    learning,
    learn: () => setLearning(true),
    cancelLearn: () => setLearning(false),
    pads,
    boundPressed,
  };

  return <RadioContext.Provider value={api}>{children}</RadioContext.Provider>;
}

/**
 * Poste radio : l'écran qui enregistre le micro quand un bouton du PC de jeu ou de ClickDeck est appuyé.
 * Par défaut la fenêtre de SecondMonitor (ouverte sur l'ordinateur lui-même) ; réglable par écran.
 */
export function isRadioStation(): boolean {
  try {
    const v = localStorage.getItem('smhub-poste-radio');
    if (v === 'oui') return true;
    if (v === 'non') return false;
  } catch {
    /* stockage indisponible */
  }
  return ['localhost', '127.0.0.1'].includes(location.hostname);
}

export function setRadioStation(on: boolean) {
  try { localStorage.setItem('smhub-poste-radio', on ? 'oui' : 'non'); } catch { /* stockage indisponible */ }
}
