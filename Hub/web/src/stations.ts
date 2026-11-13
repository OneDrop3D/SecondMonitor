import { useSyncExternalStore } from 'react';

/** Poste audio du PC de jeu : le collecteur joue la voix de l'ingénieur et/ou enregistre son micro. */
export interface AudioStation {
  name: string;
  voice: boolean;
  mic: boolean;
  address?: string | null;
  since: string;
}

/** Où se fait entendre la voix quand le PC de jeu la demande : le PC seul, les écrans seuls, ou les deux. */
export type VoiceOutput = 'pc' | 'ecrans' | 'deux';

export interface StationsState {
  stations: AudioStation[];
  voiceOutput: VoiceOutput;
}

let state: StationsState = { stations: [], voiceOutput: 'pc' };
const listeners = new Set<() => void>();

/** Message « stations » du hub (à la connexion et à chaque changement). */
export function setStations(data: Partial<StationsState> | null | undefined) {
  state = { stations: data?.stations ?? [], voiceOutput: data?.voiceOutput ?? 'pc' };
  listeners.forEach((l) => l());
}

export function useStations(): StationsState {
  return useSyncExternalStore(
    (l) => { listeners.add(l); return () => listeners.delete(l); },
    () => state,
  );
}

/** Le PC de jeu fait entendre la voix de l'ingénieur. */
export function voiceStation(): AudioStation | undefined {
  return state.voiceOutput === 'ecrans' ? undefined : state.stations.find((s) => s.voice);
}

/** Les écrans jouent-ils la voix ? Non quand le PC de jeu la joue seul (réglage « Voix de l'ingénieur sur »). */
export function screensPlayVoice(): boolean {
  return !(state.voiceOutput === 'pc' && state.stations.some((s) => s.voice));
}
