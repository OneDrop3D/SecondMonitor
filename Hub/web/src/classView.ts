import type { DriverRow, RaceSnapshot } from './types';

/** « mine » : ma catégorie + le trafic proche ; « all » : tout le plateau ; sinon l'identifiant d'une catégorie. */
export type ClassView = 'mine' | 'all' | string;

/** Distance (s) sous laquelle une voiture d'une autre catégorie est affichée comme « trafic ». */
export const TRAFFIC_SECONDS = 5;

/**
 * Voitures à afficher. En course multi-catégories, les autres catégories n'apparaissent que si elles sont
 * à moins de 5 s de vous sur la piste (trafic à doubler ou qui arrive), sauf si on demande « Toutes ».
 */
export function visibleDrivers(snapshot: RaceSnapshot, view: ClassView): { rows: DriverRow[]; traffic: Set<string> } {
  const traffic = new Set<string>();
  const me = snapshot.drivers.find((d) => d.isPlayer);
  if (!snapshot.isMultiClass && new Set(snapshot.drivers.map((d) => d.classId)).size < 2) return { rows: snapshot.drivers, traffic };
  if (view === 'all') return { rows: snapshot.drivers, traffic };
  const cls = view === 'mine' ? me?.classId : view;
  if (cls == null) return { rows: snapshot.drivers, traffic };
  const rows = snapshot.drivers.filter((d) => {
    if (d.classId === cls || d.isPlayer) return true;
    const near = view === 'mine' && !d.inPits && d.gapToPlayer != null && Math.abs(d.gapToPlayer) <= TRAFFIC_SECONDS;
    if (near) traffic.add(d.id);
    return near;
  });
  return { rows, traffic };
}

/** Intervalle avec la voiture précédente de la même catégorie (s), à partir des écarts au leader de catégorie. */
export function classInterval(d: DriverRow, rows: DriverRow[]): { seconds: number | null; laps: number } {
  const same = rows.filter((x) => x.classId === d.classId);
  const i = same.indexOf(d);
  if (i <= 0) return { seconds: null, laps: 0 };
  const prev = same[i - 1];
  if (d.gapToClassLeaderLaps > prev.gapToClassLeaderLaps) return { seconds: null, laps: d.gapToClassLeaderLaps - prev.gapToClassLeaderLaps };
  if (d.gapToClassLeader == null || prev.gapToClassLeader == null) return { seconds: d.interval, laps: d.intervalLaps };
  return { seconds: Math.max(0, d.gapToClassLeader - prev.gapToClassLeader), laps: 0 };
}
