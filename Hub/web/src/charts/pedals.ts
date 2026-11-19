import type { Series } from './LineChart';

/** Un tour (ou une moyenne de tours) : on distingue les tours par le trait, les couleurs restent celles des pédales. */
const DASHES = [undefined, '7 4', '2 3', '12 3 2 3'];

/** Accélérateur en vert, frein en rouge : un trait par tour (plein, tirets, points…). */
export function pedalSeries(items: { id: string; label: string; throttle: (number | null)[]; brake: (number | null)[] }[], x: (j: number) => number): Series[] {
  return items.flatMap((t, i) => {
    const dash = DASHES[i % DASHES.length];
    const common = { width: i === 0 ? 2 : 1.6, dash, opacity: i === 0 ? 1 : 0.85 };
    return [
      { id: `t${t.id}`, label: `${t.label} accélérateur`, color: 'var(--good)', ...common, points: t.throttle.map((v, j) => ({ x: x(j), y: v })) },
      { id: `b${t.id}`, label: `${t.label} frein`, color: 'var(--critical)', ...common, points: t.brake.map((v, j) => ({ x: x(j), y: v })) },
    ];
  });
}
