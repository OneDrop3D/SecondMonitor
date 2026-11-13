import { allBlocks, getBlock } from './registry';

/** Un bloc placé : largeur en colonnes, hauteur en rangées. L'ordre de la liste est l'ordre d'affichage. */
export interface LayoutItem {
  type: string;
  w: number;
  h: number;
  hidden?: boolean;
}

/** Grand écran (12 colonnes) et tablette (6 colonnes) ont chacun leur disposition ; le téléphone suit celle de la tablette sur une colonne. */
export type LayoutMode = 'large' | 'medium';

interface Stored {
  v: 1;
  large?: LayoutItem[];
  medium?: LayoutItem[];
}

const KEY = 'smhub-accueil-disposition';
export const ROW = 36;
export const GAP = 4;
export const MAX_ROWS = 24;

export function colsFor(width: number): { cols: number; mode: LayoutMode; phone: boolean } {
  if (width >= 1100) return { cols: 12, mode: 'large', phone: false };
  if (width >= 640) return { cols: 6, mode: 'medium', phone: false };
  return { cols: 1, mode: 'medium', phone: true };
}

export function defaultLayout(mode: LayoutMode): LayoutItem[] {
  return allBlocks().map((b) => {
    const s = mode === 'large' ? b.size : b.medium ?? { w: Math.min(6, Math.max(2, Math.round(b.size.w / 2))), h: b.size.h };
    return { type: b.type, w: s.w, h: s.h, hidden: !b.addByDefault };
  });
}

function read(): Stored {
  try {
    const raw = localStorage.getItem(KEY);
    if (raw) {
      const s = JSON.parse(raw) as Stored;
      if (s && s.v === 1) return s;
    }
  } catch { /* stockage indisponible */ }
  return { v: 1 };
}

export function loadLayout(mode: LayoutMode): LayoutItem[] {
  const saved = read()[mode];
  if (!saved || !Array.isArray(saved)) return defaultLayout(mode);
  const max = mode === 'large' ? 12 : 6;
  const clean = saved
    .filter((i) => i && typeof i.type === 'string')
    .map((i) => ({ type: i.type, w: clamp(Math.round(i.w) || 4, 1, max), h: clamp(Math.round(i.h) || 4, 2, MAX_ROWS), hidden: !!i.hidden }));
  // Blocs inconnus conservés (un module peut s'enregistrer plus tard), nouveaux blocs ajoutés masqués à la fin.
  const known = new Set(clean.map((i) => i.type));
  for (const d of defaultLayout(mode)) if (!known.has(d.type)) clean.push({ ...d, hidden: true });
  return clean;
}

export function saveLayout(mode: LayoutMode, items: LayoutItem[]) {
  const s = read();
  s[mode] = items;
  try { localStorage.setItem(KEY, JSON.stringify(s)); } catch { /* stockage indisponible */ }
}

export function resetLayout(mode: LayoutMode): LayoutItem[] {
  const s = read();
  delete s[mode];
  try { localStorage.setItem(KEY, JSON.stringify(s)); } catch { /* stockage indisponible */ }
  return defaultLayout(mode);
}

export function clamp(v: number, min: number, max: number) {
  return Math.max(min, Math.min(max, v));
}

export function limits(type: string, cols: number) {
  const b = getBlock(type);
  return { minW: Math.min(cols, b?.minW ?? 2), minH: b?.minH ?? 2, maxW: cols, maxH: MAX_ROWS };
}
