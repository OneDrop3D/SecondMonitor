import { useEffect, useId, useMemo, useRef, useState, type ReactNode } from 'react';
import { niceTicks, useWidth } from './useSize';

export interface Point { x: number; y: number | null }

export interface Series {
  id: string;
  label: string;
  color: string;
  points: Point[];
  /** Projection / prévision : tracé en pointillés. */
  dashed?: boolean;
  width?: number;
  /** Repères ponctuels (ex. arrêts au stand). */
  markers?: { x: number; y: number; title?: string }[];
  /** Série d'accompagnement (projection d'une autre série) : absente de la légende et de l'infobulle. */
  companion?: boolean;
  /** Valeurs en paliers (rapport engagé) : pas de pente entre deux points. */
  step?: boolean;
  /** Motif de pointillés propre (ex. « 2 3 ») : distingue des tours de même couleur. */
  dash?: string;
  opacity?: number;
}

/** Fenêtre zoomée sur l'axe X (null : tout). */
export type XZoom = [number, number] | null;

interface Props {
  series: Series[];
  height?: number;
  xFormat?: (x: number) => string;
  yFormat?: (y: number) => string;
  /** Format des valeurs dans l'infobulle (par défaut yFormat). */
  valueFormat?: (y: number) => string;
  xTitle?: (x: number) => string;
  yDomain?: [number, number];
  xDomain?: [number, number];
  /** Zone de référence (ex. plage idéale de température). */
  band?: { y0: number; y1: number; label: string };
  refLines?: { x?: number; y?: number; label?: string }[];
  emptyText?: string;
  legend?: boolean;
  ariaLabel: string;
  footer?: ReactNode;
  /** Légende des séries en pointillés (par défaut « projection » ; null : rien). */
  dashedLabel?: string | null;
  /**
   * Survol partagé entre plusieurs graphiques (même axe X) : position survolée et rappel quand elle change.
   * Le réticule suit partout ; l'infobulle ne s'affiche que sur le graphique sous le pointeur.
   */
  hoverX?: number | null;
  onHoverX?: (x: number | null) => void;
  /** Graduations imposées sur l'axe Y (ex. rapports 1 à 7). */
  yTicks?: number[];
  /** Zoom partagé entre plusieurs graphiques (même axe X) ; sans ces deux props, chaque graphique a le sien. */
  zoom?: XZoom;
  onZoom?: (z: XZoom) => void;
  /** Moyenne de chaque courbe sur la partie affichée, sous le graphique. */
  means?: boolean;
}

const M = { l: 54, r: 14, t: 10, b: 26 };

/**
 * Graphique en courbes : un seul axe Y, réticule + infobulle au survol, légende dès 2 séries,
 * pointillés pour les projections. Dessiné à la largeur réelle du conteneur.
 */
export function LineChart({ series, height = 240, xFormat = (x) => String(x), yFormat = (y) => String(y), valueFormat, xTitle, yDomain, xDomain: baseDomain, band, refLines, emptyText = 'Pas encore de données.', legend, ariaLabel, footer, dashedLabel = 'projection', hoverX: sharedHoverX, onHoverX, yTicks, zoom: sharedZoom, onZoom, means }: Props) {
  const { ref, width } = useWidth<HTMLDivElement>();
  const [localZoom, setLocalZoom] = useState<XZoom>(null);
  const zoomShared = onZoom !== undefined;
  // Étendue entière de l'axe X : la fenêtre imposée (zone du circuit), sinon toutes les données.
  const full = useMemo<[number, number] | null>(() => {
    if (baseDomain) return baseDomain;
    let lo = Infinity, hi = -Infinity;
    for (const s of series) for (const p of s.points) if (p.y != null) { if (p.x < lo) lo = p.x; if (p.x > hi) hi = p.x; }
    return hi > lo ? [lo, hi] : null;
  }, [series, baseDomain]);
  const rawZoom = zoomShared ? sharedZoom ?? null : localZoom;
  const zoomed = full && rawZoom ? clampZoom(rawZoom, full) : null;
  const xDomain = zoomed ?? baseDomain;
  const setZoom = (z: XZoom) => {
    const c = z && full ? clampZoom(z, full) : null;
    const next = c && full && c[1] - c[0] < (full[1] - full[0]) * 0.995 ? c : null;
    if (zoomShared) onZoom!(next); else setLocalZoom(next);
  };
  const [brush, setBrush] = useState<{ a: number; b: number } | null>(null);
  const [localHoverX, setLocalHoverX] = useState<number | null>(null);
  const [pointerInside, setPointerInside] = useState(false);
  const shared = onHoverX !== undefined;
  const hoverX = shared ? sharedHoverX ?? null : localHoverX;
  const setHoverX = (x: number | null) => (shared ? onHoverX!(x) : setLocalHoverX(x));
  const clipId = `clip${useId().replace(/[^a-zA-Z0-9]/g, '')}`;
  const H = Math.max(140, Math.min(height, Math.round(width * 0.75)));
  const fmtValue = valueFormat ?? yFormat;

  const scale = useMemo(() => {
    // Avec une fenêtre en X (zoom sur une zone), l'échelle verticale ne tient compte que des points visibles.
    const inX = (x: number) => !xDomain || (x >= xDomain[0] && x <= xDomain[1]);
    const pts = series.flatMap((s) => s.points.filter((p) => p.y != null && isFinite(p.y) && inX(p.x)));
    if (pts.length < 2) return null;
    const xs = pts.map((p) => p.x);
    const ys = pts.map((p) => p.y as number);
    let [x0, x1] = xDomain ?? [Math.min(...xs), Math.max(...xs)];
    if (x1 - x0 < 1e-9) x1 = x0 + 1;
    let [y0, y1] = yDomain ?? [Math.min(...ys), Math.max(...ys)];
    if (band && !yDomain) { y0 = Math.min(y0, band.y0); y1 = Math.max(y1, band.y1); }
    for (const r of refLines ?? []) if (r.y != null && !yDomain) { y0 = Math.min(y0, r.y); y1 = Math.max(y1, r.y); }
    if (!yDomain) {
      const pad = (y1 - y0) * 0.08 || Math.abs(y1) * 0.05 || 1;
      y0 -= pad; y1 += pad;
    }
    const plotW = Math.max(60, width - M.l - M.r);
    const plotH = H - M.t - M.b;
    const sx = (x: number) => M.l + ((x - x0) / (x1 - x0)) * plotW;
    const sy = (y: number) => M.t + (1 - (Math.min(Math.max(y, y0), y1) - y0) / (y1 - y0)) * plotH;
    const xt = niceTicks(x0, x1, Math.max(2, Math.floor(plotW / 90)));
    const yt = yTicks ? yTicks.filter((t) => t >= y0 && t <= y1) : niceTicks(y0, y1, Math.max(2, Math.floor(plotH / 40)));
    const allX = [...new Set(series.filter((s) => !s.companion).flatMap((s) => s.points.filter((p) => p.y != null).map((p) => p.x)))].sort((a, b) => a - b);
    return { x0, x1, y0, y1, sx, sy, xt, yt, plotW, plotH, allX };
  }, [series, width, H, yDomain, xDomain, band, refLines, yTicks]);

  const paths = useMemo(() => {
    if (!scale) return [];
    return series.map((s) => {
      // Décimation : au plus ~2 points par pixel visible (rien de sauté pour les paliers, chaque changement compte).
      const span = (s.points.at(-1)?.x ?? 1) - (s.points[0]?.x ?? 0);
      const visible = xDomain && span > 0 ? Math.min(1, (scale.x1 - scale.x0) / span) : 1;
      const stride = s.step ? 1 : Math.max(1, Math.floor((s.points.length * visible) / (scale.plotW * 2)));
      let d = '';
      let pen = false;
      s.points.forEach((p, i) => {
        if (p.y == null || !isFinite(p.y)) { pen = false; return; }
        if (stride > 1 && i % stride !== 0 && i !== s.points.length - 1) return;
        const x = scale.sx(p.x).toFixed(1), y = scale.sy(p.y).toFixed(1);
        d += !pen ? `M${x},${y}` : s.step ? `H${x}V${y}` : `L${x},${y}`;
        pen = true;
      });
      return { s, d };
    });
  }, [series, scale, xDomain]);

  // Zoom : Ctrl/⌘ + molette ou pincement autour du pointeur ; molette horizontale ou Maj + molette pour défiler.
  const live = useRef({ scale, full, view: xDomain, setZoom });
  live.current = { scale, full, view: xDomain, setZoom };
  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const zoomAt = (clientX: number, factor: number) => {
      const { scale: sc, full: f, view, setZoom: set } = live.current;
      if (!sc || !f) return;
      const [v0, v1] = view ?? f;
      const at = sc.x0 + ((clientX - el.getBoundingClientRect().left - M.l) / sc.plotW) * (sc.x1 - sc.x0);
      const c = Math.min(v1, Math.max(v0, at));
      set([c - (c - v0) * factor, c + (v1 - c) * factor]);
    };
    const onWheel = (e: WheelEvent) => {
      const { full: f, view, setZoom: set } = live.current;
      if (!f) return;
      if (e.ctrlKey || e.metaKey) {
        e.preventDefault();
        zoomAt(e.clientX, Math.exp(Math.max(-0.5, Math.min(0.5, e.deltaY * 0.01))));
        return;
      }
      const dx = Math.abs(e.deltaX) > Math.abs(e.deltaY) ? e.deltaX : e.shiftKey ? e.deltaY : 0;
      if (!view || dx === 0 || view === f) return; // pas zoomé : la page défile normalement
      e.preventDefault();
      const shift = (dx / Math.max(60, el.clientWidth)) * (view[1] - view[0]);
      set([view[0] + shift, view[1] + shift]);
    };
    // Safari et la fenêtre Mac : le pincement du trackpad arrive en « gesture ».
    let g0 = 1;
    const onGestureStart = (e: Event) => { e.preventDefault(); g0 = 1; };
    const onGestureChange = (e: Event) => {
      e.preventDefault();
      const ge = e as Event & { scale: number; clientX: number };
      zoomAt(ge.clientX, g0 / ge.scale);
      g0 = ge.scale;
    };
    el.addEventListener('wheel', onWheel, { passive: false });
    el.addEventListener('gesturestart', onGestureStart);
    el.addEventListener('gesturechange', onGestureChange);
    return () => {
      el.removeEventListener('wheel', onWheel);
      el.removeEventListener('gesturestart', onGestureStart);
      el.removeEventListener('gesturechange', onGestureChange);
    };
  }, [ref]);

  const xAtPointer = (e: React.PointerEvent<Element>) => {
    if (!scale) return null;
    const rect = e.currentTarget.getBoundingClientRect();
    return scale.x0 + ((e.clientX - rect.left - M.l) / scale.plotW) * (scale.x1 - scale.x0);
  };
  // Glisser sur le graphique : la zone choisie est agrandie.
  const onDown = (e: React.PointerEvent<SVGSVGElement>) => {
    const x = xAtPointer(e);
    if (x == null || e.button !== 0) return;
    e.currentTarget.setPointerCapture(e.pointerId);
    setBrush({ a: x, b: x });
  };
  const onUp = () => {
    if (!brush || !scale) { setBrush(null); return; }
    const lo = Math.min(brush.a, brush.b), hi = Math.max(brush.a, brush.b);
    setBrush(null);
    if (((hi - lo) / (scale.x1 - scale.x0)) * scale.plotW >= 8) setZoom([lo, hi]);
  };

  const onMove = (e: React.PointerEvent<SVGSVGElement>) => {
    if (!scale || scale.allX.length === 0) return;
    const xVal = xAtPointer(e)!;
    if (brush) setBrush({ a: brush.a, b: xVal });
    setPointerInside(true);
    let lo = 0, hi = scale.allX.length - 1;
    while (hi - lo > 1) { const mid = (lo + hi) >> 1; if (scale.allX[mid] < xVal) lo = mid; else hi = mid; }
    setHoverX(Math.abs(scale.allX[lo] - xVal) < Math.abs(scale.allX[hi] - xVal) ? scale.allX[lo] : scale.allX[hi]);
  };

  const hover = useMemo(() => {
    if (hoverX == null || !scale || hoverX < scale.x0 || hoverX > scale.x1) return null;
    const tol = (scale.x1 - scale.x0) / Math.max(20, scale.plotW / 3);
    const rows = series.filter((s) => !s.companion).map((s) => {
      let best: Point | null = null, bd = Infinity;
      for (const p of s.points) { if (p.y == null) continue; const d = Math.abs(p.x - hoverX); if (d < bd) { bd = d; best = p; } }
      return best && bd <= tol ? { s, p: best } : null;
    }).filter((r): r is { s: Series; p: Point } => r != null);
    return rows.length ? { x: hoverX, rows } : null;
  }, [hoverX, series, scale]);

  const showLegend = legend ?? series.filter((s) => !s.companion).length >= 2;

  return (
    <div className="chart">
      {showLegend && (
        <div className="legend chart-legend">
          {series.filter((s) => !s.companion).map((s) => (
            <span key={s.id} className="legend-item">
              <span className={`legend-line ${s.dashed || s.dash ? 'dashed' : ''}`} style={{ borderColor: s.color, opacity: s.opacity }} />
              {s.label}
            </span>
          ))}
          {dashedLabel && series.some((s) => s.companion || s.dashed) && !series.every((s) => s.dashed) && (
            <span className="legend-item muted"><span className="legend-line dashed" style={{ borderColor: 'var(--text-muted)' }} />{dashedLabel}</span>
          )}
          {band && <span className="legend-item muted"><span className="legend-band" />{band.label}</span>}
        </div>
      )}
      <div ref={ref} className="chart-area">
        {!scale ? (
          <div className="chart-empty" style={{ height: Math.min(H, 160) }}>{emptyText}</div>
        ) : (
          <svg width={width} height={H} role="img" aria-label={ariaLabel} onPointerMove={onMove} onPointerDown={onDown} onPointerUp={onUp} onPointerCancel={() => setBrush(null)} onDoubleClick={() => setZoom(null)} onPointerLeave={() => { setPointerInside(false); setHoverX(null); }} style={{ touchAction: 'pan-y', cursor: brush ? 'col-resize' : 'crosshair' }}>
            <defs>
              <clipPath id={clipId}><rect x={M.l} y={0} width={scale.plotW} height={H - M.b} /></clipPath>
            </defs>
            {band && (
              <rect x={M.l} width={scale.plotW} y={scale.sy(band.y1)} height={Math.max(0, scale.sy(band.y0) - scale.sy(band.y1))} fill="var(--band)" />
            )}
            {scale.yt.map((t) => (
              <g key={`y${t}`}>
                <line x1={M.l} x2={M.l + scale.plotW} y1={scale.sy(t)} y2={scale.sy(t)} stroke="var(--grid)" />
                <text x={M.l - 8} y={scale.sy(t) + 4} textAnchor="end" className="axis-text">{yFormat(t)}</text>
              </g>
            ))}
            <line x1={M.l} x2={M.l + scale.plotW} y1={M.t + scale.plotH} y2={M.t + scale.plotH} stroke="var(--axis)" />
            {scale.xt.map((t) => (
              <text key={`x${t}`} x={scale.sx(t)} y={H - 7} textAnchor="middle" className="axis-text">{xFormat(t)}</text>
            ))}
            {refLines?.map((r, i) => r.x != null && r.x >= scale.x0 && r.x <= scale.x1 ? (
              <g key={`r${i}`}>
                <line x1={scale.sx(r.x)} x2={scale.sx(r.x)} y1={M.t} y2={M.t + scale.plotH} stroke="var(--text-muted)" strokeDasharray="3 3" />
                {r.label && <text x={scale.sx(r.x) + 4} y={M.t + 11} className="axis-text">{r.label}</text>}
              </g>
            ) : r.y != null ? (
              <g key={`r${i}`}>
                <line x1={M.l} x2={M.l + scale.plotW} y1={scale.sy(r.y)} y2={scale.sy(r.y)} stroke="var(--text-muted)" strokeDasharray="3 3" />
                {r.label && <text x={M.l + scale.plotW - 4} y={scale.sy(r.y) - 4} textAnchor="end" className="axis-text">{r.label}</text>}
              </g>
            ) : null)}
            <g clipPath={`url(#${clipId})`}>
              {paths.map(({ s, d }) => (
                <path key={s.id} d={d} fill="none" stroke={s.color} strokeWidth={s.width ?? 2} strokeLinejoin="round" strokeLinecap="round" strokeDasharray={s.dash ?? (s.dashed ? '5 4' : undefined)} opacity={s.opacity ?? (s.companion ? 0.85 : 1)} />
              ))}
              {series.flatMap((s) => (s.markers ?? []).filter((m) => isFinite(m.y) && isFinite(m.x)).map((m, i) => (
                <circle key={`${s.id}m${i}`} cx={scale.sx(m.x)} cy={scale.sy(m.y)} r={4.5} fill={s.color} stroke="var(--surface-1)" strokeWidth={2}><title>{m.title}</title></circle>
              )))}
            </g>
            {brush && (
              <rect pointerEvents="none" x={scale.sx(Math.min(brush.a, brush.b))} y={M.t} width={Math.abs(scale.sx(brush.b) - scale.sx(brush.a))} height={scale.plotH} fill="var(--text-muted)" opacity={0.15} />
            )}
            {hover && (
              <g pointerEvents="none">
                <line x1={scale.sx(hover.x)} x2={scale.sx(hover.x)} y1={M.t} y2={M.t + scale.plotH} stroke="var(--text-muted)" />
                {hover.rows.map(({ s, p }) => (
                  <circle key={s.id} cx={scale.sx(p.x)} cy={scale.sy(p.y as number)} r={4} fill={s.color} stroke="var(--surface-1)" strokeWidth={2} />
                ))}
              </g>
            )}
          </svg>
        )}
        {hover && scale && (!shared || pointerInside) && (
          <div className="tooltip chart-tip" style={scale.sx(hover.x) > width * 0.6 ? { right: width - scale.sx(hover.x) + 12 } : { left: scale.sx(hover.x) + 12 }}>
            <div className="secondary">{xTitle ? xTitle(hover.x) : xFormat(hover.x)}</div>
            {hover.rows.map(({ s, p }) => (
              <div key={s.id} className="tip-row">
                <span className="legend-line" style={{ borderColor: s.color }} />
                <span className="tip-label">{s.label}</span>
                <b className="tnum">{fmtValue(p.y as number)}</b>
              </div>
            ))}
          </div>
        )}
        {zoomed && full && scale && <ZoomBar full={full} view={zoomed} left={M.l} width={scale.plotW} onChange={setZoom} />}
      </div>
      {means && scale && <Means series={series} domain={[scale.x0, scale.x1]} format={fmtValue} />}
      {footer}
    </div>
  );
}

function clampZoom([a, b]: [number, number], [f0, f1]: [number, number]): [number, number] {
  const minSpan = (f1 - f0) / 400;
  let lo = Math.min(a, b), hi = Math.max(a, b);
  if (hi - lo < minSpan) { const c = (lo + hi) / 2; lo = c - minSpan / 2; hi = c + minSpan / 2; }
  const span = Math.min(hi - lo, f1 - f0);
  lo = Math.min(Math.max(lo, f0), f1 - span);
  return [lo, lo + span];
}

/** Barre de défilement sous un graphique zoomé : glisser la poignée, cliquer ailleurs pour y aller, « Tout » pour revenir. */
function ZoomBar({ full: [f0, f1], view: [v0, v1], left, width, onChange }: { full: [number, number]; view: [number, number]; left: number; width: number; onChange: (z: XZoom) => void }) {
  const drag = useRef<{ x: number; v0: number; k: number } | null>(null);
  const span = v1 - v0;
  const pct = (v: number) => `${(v / (f1 - f0)) * 100}%`;
  return (
    <div className="zoom-bar" style={{ marginLeft: left, width }}>
      <div className="zoom-track"
        onPointerDown={(e) => {
          const rect = e.currentTarget.getBoundingClientRect();
          const k = rect.width / (f1 - f0);
          const at = f0 + (e.clientX - rect.left) / k;
          const start = at < v0 || at > v1 ? at - span / 2 : v0;
          if (start !== v0) onChange([start, start + span]);
          drag.current = { x: e.clientX, v0: start, k };
          e.currentTarget.setPointerCapture(e.pointerId);
        }}
        onPointerMove={(e) => { const d = drag.current; if (d) { const s0 = d.v0 + (e.clientX - d.x) / d.k; onChange([s0, s0 + span]); } }}
        onPointerUp={() => { drag.current = null; }}>
        <div className="zoom-thumb" style={{ left: pct(v0 - f0), width: pct(span), minWidth: 12 }} />
      </div>
      <button className="chip tiny" onClick={() => onChange(null)} title="Revenir à la vue entière (ou double-clic sur le graphique)">Tout</button>
    </div>
  );
}

/** Moyenne de chaque courbe sur la partie affichée (le zoom compris). */
function Means({ series, domain: [x0, x1], format }: { series: Series[]; domain: [number, number]; format: (y: number) => string }) {
  const rows = series.filter((s) => !s.companion).map((s) => {
    let sum = 0, n = 0;
    for (const p of s.points) if (p.y != null && isFinite(p.y) && p.x >= x0 && p.x <= x1) { sum += p.y; n++; }
    return n ? { s, v: sum / n } : null;
  }).filter((r): r is { s: Series; v: number } => r != null);
  if (rows.length === 0) return null;
  return (
    <div className="legend chart-means" aria-label="Moyennes sur la partie affichée">
      <span className="muted">Moyenne affichée :</span>
      {rows.map(({ s, v }) => (
        <span key={s.id} className="legend-item"><span className={`legend-line ${s.dashed || s.dash ? 'dashed' : ''}`} style={{ borderColor: s.color }} />{rows.length > 1 ? `${s.label} ` : ''}<b className="tnum">{format(v)}</b></span>
      ))}
    </div>
  );
}
