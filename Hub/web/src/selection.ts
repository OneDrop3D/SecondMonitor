import { useCallback, useEffect, useRef, useState } from 'react';
import type { DriverRow, RaceSnapshot } from './types';

export const MAX_SELECTED = 6;

export interface Selection {
  ids: string[];
  /** Couleur stable d'un pilote comparé : vous = série 1, les autres gardent leur couleur tant qu'ils restent sélectionnés. */
  colorOf: (id: string) => string;
  toggle: (id: string) => void;
  set: (ids: string[]) => void;
  neighbours: () => string[];
  classLeaders: () => string[];
}

/** Pilotes à comparer dans les graphiques (partagés entre les onglets). Par défaut : vous, vos voisins de catégorie et le leader. */
export function useSelection(snapshot: RaceSnapshot | null): Selection {
  const [ids, setIds] = useState<string[]>([]);
  const slots = useRef(new Map<string, number>());
  const snapRef = useRef(snapshot);
  snapRef.current = snapshot;

  const neighbours = useCallback(() => {
    const s = snapRef.current;
    const me = s?.drivers.find((d) => d.isPlayer);
    if (!s || !me) return s?.drivers.slice(0, 3).map((d) => d.id) ?? [];
    const cls = s.drivers.filter((d) => d.classId === me.classId);
    const i = cls.indexOf(me);
    const list: DriverRow[] = [me];
    if (i > 0) list.push(cls[i - 1]);
    if (i < cls.length - 1) list.push(cls[i + 1]);
    if (i > 1) list.push(cls[0]);
    return list.map((d) => d.id);
  }, []);

  const classLeaders = useCallback(() => {
    const s = snapRef.current;
    if (!s) return [];
    const seen = new Set<string>();
    const out: string[] = [];
    const me = s.drivers.find((d) => d.isPlayer);
    if (me) out.push(me.id);
    for (const d of s.drivers) if (!seen.has(d.classId)) { seen.add(d.classId); if (!out.includes(d.id)) out.push(d.id); }
    return out.slice(0, MAX_SELECTED);
  }, []);

  // Sélection par défaut, et nettoyage quand la session change (pilotes disparus).
  const known = snapshot?.drivers.map((d) => d.id).join(',') ?? '';
  useEffect(() => {
    if (!snapshot?.hasData) return;
    setIds((current) => {
      const alive = current.filter((id) => snapshot.drivers.some((d) => d.id === id));
      return alive.length > 0 ? (alive.length === current.length ? current : alive) : neighbours();
    });
  }, [known, snapshot?.hasData, neighbours]);

  const playerId = snapshot?.drivers.find((d) => d.isPlayer)?.id;
  const colorOf = useCallback((id: string) => {
    if (id === playerId) return 'var(--series-1)';
    const map = slots.current;
    if (!map.has(id)) {
      const used = new Set([...map.entries()].filter(([k]) => ids.includes(k)).map(([, v]) => v));
      let slot = 2;
      while (used.has(slot) && slot < 8) slot++;
      map.set(id, slot);
    }
    return `var(--series-${map.get(id)})`;
  }, [ids, playerId]);

  const set = (next: string[]) => {
    const keep = next.slice(0, MAX_SELECTED);
    for (const k of [...slots.current.keys()]) if (!keep.includes(k)) slots.current.delete(k);
    setIds(keep);
  };

  const toggle = (id: string) => set(ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]);

  return { ids, colorOf, toggle, set, neighbours, classLeaders };
}
