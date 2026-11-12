import { useEffect, useRef, useState } from 'react';
import { api } from './live';
import type { TelemetryHistory } from './types';

export interface TelemetryStore {
  t: number[];
  series: Record<string, (number | null)[]>;
  version: number;
}

/**
 * Historique de télémétrie de la voiture du joueur (1 mesure/s), chargé une fois puis complété toutes les 2 s
 * (seules les nouvelles mesures transitent). Repart de zéro à chaque nouvelle session.
 */
export function useTelemetry(intervalMs = 2000): TelemetryStore {
  const store = useRef<TelemetryStore & { sessionId: number }>({ t: [], series: {}, version: 0, sessionId: -1 });
  const [, force] = useState(0);

  useEffect(() => {
    let alive = true;
    let timer: number | undefined;
    const load = async () => {
      try {
        const s = store.current;
        const since = s.t.length ? s.t[s.t.length - 1] : -1e9;
        const r = await api<TelemetryHistory>(`/api/telemetry?since=${since}`);
        if (!alive) return;
        if (r.sessionId !== s.sessionId) {
          s.t = []; s.series = {}; s.sessionId = r.sessionId;
          if (since > -1e9) { timer = window.setTimeout(load, 50); return; }
        }
        if (r.t.length) {
          const before = s.t.length;
          s.t.push(...r.t);
          for (const [k, v] of Object.entries(r.series)) {
            const arr = s.series[k] ?? (s.series[k] = new Array(before).fill(null));
            arr.push(...v);
          }
          for (const [k, arr] of Object.entries(s.series)) if (!(k in r.series)) arr.push(...new Array(r.t.length).fill(null));
          s.version++;
          force((x) => x + 1);
        }
      } catch {
        /* nouvel essai au prochain tour */
      }
      if (alive) timer = window.setTimeout(load, document.visibilityState === 'visible' ? intervalMs : intervalMs * 3);
    };
    load();
    return () => { alive = false; window.clearTimeout(timer); };
  }, [intervalMs]);

  return store.current;
}

/** Points (x = temps de session, y = valeur) d'une série, limités aux `windowSec` dernières secondes. */
export function seriesPoints(store: TelemetryStore, key: string, windowSec: number | null, xKey: 'time' | 'lap' = 'time') {
  const t = store.t;
  const ys = store.series[key];
  if (!ys || t.length === 0) return [];
  const from = windowSec == null ? -Infinity : t[t.length - 1] - windowSec;
  const laps = store.series.lap;
  const out: { x: number; y: number | null }[] = [];
  for (let i = 0; i < t.length; i++) {
    if (t[i] < from) continue;
    const x = xKey === 'lap' ? laps?.[i] : t[i];
    if (x == null) continue;
    out.push({ x, y: ys[i] });
  }
  return out;
}

export function latest(store: TelemetryStore, key: string): number | null {
  const ys = store.series[key];
  if (!ys) return null;
  for (let i = ys.length - 1; i >= 0; i--) if (ys[i] != null) return ys[i];
  return null;
}
