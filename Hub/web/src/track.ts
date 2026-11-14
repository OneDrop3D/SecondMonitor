import type { TrackSegment } from './types';

/** Une zone (virage ou ligne droite) contient-elle cette distance ? Marge en m ; les zones à cheval sur la ligne sont gérées. */
export function inZone(seg: TrackSegment, dist: number, lapLength: number, margin = 0): boolean {
  const L = lapLength > 0 ? lapLength : Math.max(seg.start, seg.end, dist) + 1;
  const from = seg.start - margin;
  const len = ((((seg.end - seg.start) % L) + L) % L) + margin * 2;
  const rel = (((dist - from) % L) + L) % L;
  return rel <= len;
}

/** Zone où se trouve un point du tour : virage d'abord (avec 80 m de marge), sinon ligne droite. */
export function zoneAt(segments: TrackSegment[], dist: number, lapLength: number): TrackSegment | null {
  return segments.find((s) => s.kind === 'corner' && inZone(s, dist, lapLength, 80))
    ?? segments.find((s) => s.kind === 'straight' && inZone(s, dist, lapLength, 0))
    ?? null;
}

/** Distance depuis la ligne : « 850 m », « 2,31 km ». */
export function distanceLabel(m: number): string {
  return m >= 1000 ? `${(m / 1000).toFixed(2).replace('.', ',')} km` : `${Math.round(m)} m`;
}
