// Musique Deezer : types et appels de /api/music/*, et priorité à la radio (la musique s'efface quand l'ingénieur parle).
import { isRadioStation } from '../radio';

export type MusicKind = 'playlist' | 'artist' | 'album' | 'track';

export interface MusicSettings {
  enabled: boolean;
  player: 'app' | 'web';
  sessionPolicy: 'toujours' | 'pasEnCourse' | 'essaisSeulement';
  duckMode: 'pause' | 'volume' | 'aucun';
  duckVolumePct: number;
  volumeStepPct: number;
  mediaKeys: boolean;
  accessibilityAsked: boolean;
}

export interface MusicPlayerState {
  playing: boolean | null;
  title: string | null;
  artist: string | null;
  album: string | null;
  source: string | null;
  volume: number | null;
  via: string | null;
}

export interface MusicStatus {
  enabled: boolean;
  settings: MusicSettings;
  platform: 'mac' | 'windows' | 'linux';
  appInstalled: boolean;
  appRunning: boolean;
  state: MusicPlayerState | null;
  canSetVolume: boolean;
  permission: { needed: boolean; granted: boolean; name: string | null; why: string | null };
  ducked: boolean;
  sessionType: string | null;
  sessionAllowed: boolean;
  sessionReason: string | null;
  lastOpened: string | null;
  lastMessage: string | null;
}

export interface MusicItem {
  kind: string;
  id: number;
  title: string;
  subtitle: string | null;
  picture: string | null;
  count: number;
}

async function call<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, { ...init, headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) } });
  const json = await res.json().catch(() => ({}));
  if (!res.ok) throw new Error((json as { error?: string }).error ?? `${res.status} ${res.statusText}`);
  return json as T;
}

export const musicApi = {
  status: () => call<MusicStatus>('/api/music'),
  saveSettings: (s: MusicSettings) => call<MusicSettings>('/api/music/settings', { method: 'PUT', body: JSON.stringify(s) }),
  command: (action: 'play' | 'pause' | 'next' | 'previous' | 'volumeUp' | 'volumeDown' | 'flow' | 'nowPlaying') =>
    call<{ message: string | null }>('/api/music/command', { method: 'POST', body: JSON.stringify({ action }) }),
  search: (q: string, kind: MusicKind) => call<MusicItem[]>(`/api/music/search?q=${encodeURIComponent(q)}&kind=${kind}`),
  open: (item: MusicItem, kind: MusicKind) =>
    call<{ message: string }>('/api/music/open', { method: 'POST', body: JSON.stringify({ kind, id: item.id, title: item.title }) }),
  askPermission: () => call<{ granted: boolean }>('/api/music/permission', { method: 'POST' }),
  openPermissionSettings: () => fetch('/api/music/permission?action=reglages', { method: 'POST' }),
};

// ───── Priorité à la radio ─────
// audio.ts signale « sm-voix » (true au début de la voix de l'ingénieur, false à la fin). Seul le poste radio
// (l'ordinateur où le casque du pilote est branché) prévient le hub, qui met Deezer en pause ou le baisse.
let installed = false;
let lastSent: boolean | null = null;

export function installMusicDucking() {
  if (installed || typeof window === 'undefined') return;
  installed = true;
  window.addEventListener('sm-voix', (e) => {
    const playing = !!(e as CustomEvent<boolean>).detail;
    if (!isRadioStation()) return;
    if (!playing && lastSent === false) return;
    lastSent = playing;
    fetch('/api/music/voix', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ playing }) }).catch(() => undefined);
  });
}

installMusicDucking();
