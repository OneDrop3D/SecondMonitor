/** 83.456 → « 1:23.456 » */
export function lapTime(s: number | null | undefined): string {
  if (s == null || !isFinite(s) || s <= 0) return '—';
  const m = Math.floor(s / 60);
  const rest = s - m * 60;
  return m > 0 ? `${m}:${rest.toFixed(3).padStart(6, '0')}` : rest.toFixed(3);
}

/** Écart : « +1.234 », « +1 T », « — » */
export function gap(seconds: number | null | undefined, laps = 0, signed = true): string {
  if (laps > 0) return `+${laps} T`;
  if (seconds == null || !isFinite(seconds)) return '—';
  if (Math.abs(seconds) < 0.0005) return signed ? '—' : '0.000';
  const sign = seconds > 0 ? '+' : '−';
  const abs = Math.abs(seconds);
  if (abs >= 60) {
    const m = Math.floor(abs / 60);
    return `${sign}${m}:${(abs - m * 60).toFixed(1).padStart(4, '0')}`;
  }
  return `${sign}${abs.toFixed(abs >= 10 ? 1 : 3)}`;
}

/** Durée de session : « 1:02:33 » */
export function clock(s: number | null | undefined): string {
  if (s == null || !isFinite(s) || s < 0) return '—';
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = Math.floor(s % 60);
  return h > 0 ? `${h}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}` : `${m}:${String(sec).padStart(2, '0')}`;
}

export function num(v: number | null | undefined, digits = 1, unit = ''): string {
  if (v == null || !isFinite(v)) return '—';
  return `${v.toFixed(digits)}${unit}`;
}

/** Libellé + lettre + teinte conventionnelle d'une gomme (les jeux utilisent des noms très variés). */
export function compoundInfo(name: string | null | undefined): { letter: string; label: string; kind: string } {
  if (!name) return { letter: '?', label: 'Inconnu', kind: 'unknown' };
  const n = name.toLowerCase();
  if (n.includes('inter')) return { letter: 'I', label: name, kind: 'inter' };
  if (n.includes('wet') || n.includes('rain') || n.includes('pluie')) return { letter: 'W', label: name, kind: 'wet' };
  if (n.includes('hyper') || n.includes('ultra') || n.includes('supersoft') || n.includes('super soft')) return { letter: 'S', label: name, kind: 'soft' };
  if (n.includes('soft') || n.includes('tendre') || n === 's' || /c[45]/.test(n)) return { letter: 'S', label: name, kind: 'soft' };
  if (n.includes('med') || n === 'm' || /c3/.test(n)) return { letter: 'M', label: name, kind: 'medium' };
  if (n.includes('hard') || n.includes('dur') || n === 'h' || /c[12]/.test(n)) return { letter: 'H', label: name, kind: 'hard' };
  if (n.includes('dry') || n.includes('slick')) return { letter: 'D', label: name, kind: 'dry' };
  return { letter: name.trim().charAt(0).toUpperCase(), label: name, kind: 'other' };
}

export function sessionLabel(type: string | null): string {
  switch (type) {
    case 'Race': return 'Course';
    case 'Qualification': return 'Qualification';
    case 'Practice': return 'Essais';
    case 'WarmUp': return 'Warm-up';
    default: return 'Session';
  }
}

export function flagLabel(f: string): string {
  const map: Record<string, string> = {
    YellowSector1: 'Jaune S1', YellowSector2: 'Jaune S2', YellowSector3: 'Jaune S3',
    FullCourseYellow: 'Full Course Yellow', VirtualSafetyCar: 'VSC', SafetyCar: 'Safety car',
    BlackFlag: 'Drapeau noir', BlueFlag: 'Drapeau bleu', WhiteFlag: 'Dernier tour', CheckeredFlag: 'Arrivée',
  };
  return map[f] ?? f;
}

/** Couleur de catégorie : palette catégorielle validée, attribuée dans l'ordre d'apparition (jamais selon le rang). */
export function classColorVar(index: number): string {
  return `var(--series-${(index % 8) + 1})`;
}

/** Vitesse « 231 km/h » */
export function kph(v: number | null | undefined, digits = 0): string {
  if (v == null || !isFinite(v)) return '—';
  return `${v.toFixed(digits)} km/h`;
}

/** Delta signé court : « +0.12 », « −0.30 » */
export function delta(v: number | null | undefined, digits = 2): string {
  if (v == null || !isFinite(v)) return '—';
  if (Math.abs(v) < 0.5 * 10 ** -digits) return (0).toFixed(digits);
  return `${v > 0 ? '+' : '−'}${Math.abs(v).toFixed(digits)}`;
}

export const WHEELS = ['FL', 'FR', 'RL', 'RR'] as const;
export const WHEEL_LABEL: Record<string, string> = { FL: 'Avant gauche', FR: 'Avant droit', RL: 'Arrière gauche', RR: 'Arrière droit' };
export const WHEEL_SHORT: Record<string, string> = { FL: 'AvG', FR: 'AvD', RL: 'ArG', RR: 'ArD' };

/** Direction du vent (d'où il vient) en abrégé : 240 → « OSO » */
export function compass(deg: number | null | undefined): string {
  if (deg == null || !isFinite(deg)) return '—';
  const dirs = ['N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE', 'S', 'SSO', 'SO', 'OSO', 'O', 'ONO', 'NO', 'NNO'];
  return dirs[Math.round((((deg % 360) + 360) % 360) / 22.5) % 16];
}

/**
 * Position d'une valeur par rapport à sa plage idéale (idéal ± fenêtre) :
 * −1 (très froid) … 0 (dans la plage = bon) … +1 (très chaud).
 */
export function thermal(value: number | null | undefined, ideal: number | null | undefined, window: number | null | undefined): number | null {
  if (value == null || ideal == null) return null;
  const w = Math.max(1, window ?? 10);
  const d = value - ideal;
  if (Math.abs(d) <= w) return 0;
  return Math.max(-1, Math.min(1, (d - Math.sign(d) * w) / (w * 1.5)));
}

/** Couleur d'une température : bleu = froid, vert = bon, rouge = trop chaud (le chiffre est toujours affiché). */
export function thermalColor(t: number | null): string {
  if (t == null) return 'var(--surface-3)';
  if (t === 0) return 'var(--temp-ok)';
  const pct = Math.round(45 + Math.abs(t) * 55);
  return `color-mix(in oklab, ${t > 0 ? 'var(--temp-hot)' : 'var(--temp-cold)'} ${pct}%, var(--surface-3))`;
}
