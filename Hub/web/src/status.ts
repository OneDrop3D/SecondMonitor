import { useEffect, useState } from 'react';
import { api } from './live';
import type { HubStatus, RaceSnapshot } from './types';

/** État du serveur (connexion au collecteur, jeu, débit), relu toutes les 2 s. */
export function useHubStatus(intervalMs = 2000) {
  const [status, setStatus] = useState<HubStatus | null>(null);
  useEffect(() => {
    let stop = false;
    const load = () => api<HubStatus>('/api/status').then((s) => { if (!stop) setStatus(s); }).catch(() => undefined);
    load();
    const t = window.setInterval(load, intervalMs);
    return () => { stop = true; window.clearInterval(t); };
  }, [intervalMs]);
  return status;
}

export type LinkStage = 'demo' | 'replay' | 'no-collector' | 'no-game' | 'no-data' | 'ok';

/** Où en est la chaîne PC de jeu → collecteur → SecondMonitor, en une étape claire. */
export function linkStage(status: HubStatus | null, snapshot: RaceSnapshot | null): LinkStage {
  const src = status?.source;
  if (src?.replay) return 'replay';
  if (src?.mode === 'demo') return 'demo';
  const rx = src?.receiver;
  if (!rx?.isConnected) return snapshot?.hasData ? 'ok' : 'no-collector';
  const dataFresh = rx.secondsSinceLastData != null && rx.secondsSinceLastData < 5;
  if (dataFresh || snapshot?.hasData) return 'ok';
  return rx.game ? 'no-data' : 'no-game';
}

export function collectorAddress(status: HubStatus | null): string | null {
  const peer = status?.source.receiver?.peers[0];
  return peer ? peer.replace(/:\d+$/, '') : null;
}

/** Conseil propre au jeu quand le collecteur le voit mais ne reçoit rien. */
export function gameHint(game: string | null | undefined): string {
  const g = (game ?? '').toLowerCase();
  if (g.includes('f1')) return 'Dans le jeu : Options › Paramètres › Télémétrie. Télémétrie UDP Activée, mode diffusion Désactivé, IP 127.0.0.1, port 20777, format UDP de l\'année du jeu (2024 pour F1 24).';
  if (g.includes('assetto corsa') && !g.includes('competizione')) return 'Assetto Corsa a besoin du plugin SecondMonitor : la fenêtre du collecteur propose de l\'installer, puis activez l\'application Python « SecondMonitor » et relancez la session.';
  if (g.includes('mans') || g.includes('rfactor 2') || g.includes('rf2')) return 'Le plugin de mémoire partagée du jeu doit être activé. Les données arrivent une fois la session chargée.';
  return 'Les données arrivent une fois en piste (pas dans les menus ou au chargement). La fenêtre du collecteur indique ce qui manque.';
}
