import { useMemo, useState, type ReactNode } from 'react';
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
}

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
}

const M = { l: 54, r: 14, t: 10, b: 26 };

/**
 * Graphique en courbes : un seul axe Y, réticule + infobulle au survol, légende dès 2 séries,
 * pointillés pour les projections. Dessiné à la largeur réelle du conteneur.
 */
export function LineChart({ series, height = 240, xFormat = (x) => String(x), yFormat = (y) => String(y), valueFormat, xTitle, yDomain, xDomain, band, refLines, emptyText = 'Pas encore de données.', legend, ariaLabel, footer }: Props) {
  const { ref, width } = useWidth<HTMLDivElement>();
  const [hoverX, setHoverX] = useState<number | null>(null);
  const H = Math.max(140, Math.min(height, Math.round(width * 0.75)));
  const fmtValue = valueFormat ?? yFormat;

  const scale = useMemo(() => {
    const pts = series.flatMap((s) => s.points.filter((p) => p.y != null && isFinite(p.y)));
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
    const yt = niceTicks(y0, y1, Math.max(2, Math.floor(plotH / 40)));
    const allX = [...new Set(series.filter((s) => !s.companion).flatMap((s) => s.points.filter((p) => p.y != null).map((p) => p.x)))].sort((a, b) => a - b);
    return { x0, x1, y0, y1, sx, sy, xt, yt, plotW, plotH, allX };
  }, [series, width, H, yDomain, xDomain, band, refLines]);

  const paths = useMemo(() => {
    if (!scale) return [];
    return series.map((s) => {
      // Décimation : au plus ~2 points par pixel.
      const stride = Math.max(1, Math.floor(s.points.length / (scale.plotW * 2)));
      let d = '';
      let pen = false;
      s.points.forEach((p, i) => {
        if (p.y == null || !isFinite(p.y)) { pen = false; return; }
        if (stride > 1 && i % stride !== 0 && i !== s.points.length - 1) return;
        d += `${pen ? 'L' : 'M'}${scale.sx(p.x).toFixed(1)},${scale.sy(p.y).toFixed(1)}`;
        pen = true;
      });
      return { s, d };
    });
  }, [series, scale]);

  const onMove = (e: React.PointerEvent<SVGSVGElement>) => {
    if (!scale || scale.allX.length === 0) return;
    const rect = e.currentTarget.getBoundingClientRect();
    const px = e.clientX - rect.left;
    const xVal = scale.x0 + ((px - M.l) / scale.plotW) * (scale.x1 - scale.x0);
    let lo = 0, hi = scale.allX.length - 1;
    while (hi - lo > 1) { const mid = (lo + hi) >> 1; if (scale.allX[mid] < xVal) lo = mid; else hi = mid; }
    setHoverX(Math.abs(scale.allX[lo] - xVal) < Math.abs(scale.allX[hi] - xVal) ? scale.allX[lo] : scale.allX[hi]);
  };

  const hover = useMemo(() => {
    if (hoverX == null || !scale) return null;
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
              <span className={`legend-line ${s.dashed ? 'dashed' : ''}`} style={{ borderColor: s.color }} />
              {s.label}
            </span>
          ))}
          {series.some((s) => s.companion || s.dashed) && !series.every((s) => s.dashed) && (
            <span className="legend-item muted"><span className="legend-line dashed" style={{ borderColor: 'var(--text-muted)' }} />projection</span>
          )}
          {band && <span className="legend-item muted"><span className="legend-band" />{band.label}</span>}
        </div>
      )}
      <div ref={ref} className="chart-area">
        {!scale ? (
          <div className="chart-empty" style={{ height: Math.min(H, 160) }}>{emptyText}</div>
        ) : (
          <svg width={width} height={H} role="img" aria-label={ariaLabel} onPointerMove={onMove} onPointerLeave={() => setHoverX(null)} style={{ touchAction: 'pan-y' }}>
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
            {paths.map(({ s, d }) => (
              <path key={s.id} d={d} fill="none" stroke={s.color} strokeWidth={s.width ?? 2} strokeLinejoin="round" strokeLinecap="round" strokeDasharray={s.dashed ? '5 4' : undefined} opacity={s.companion ? 0.85 : 1} />
            ))}
            {series.flatMap((s) => (s.markers ?? []).filter((m) => isFinite(m.y) && isFinite(m.x)).map((m, i) => (
              <circle key={`${s.id}m${i}`} cx={scale.sx(m.x)} cy={scale.sy(m.y)} r={4.5} fill={s.color} stroke="var(--surface-1)" strokeWidth={2}><title>{m.title}</title></circle>
            )))}
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
        {hover && scale && (
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
      </div>
      {footer}
    </div>
  );
}
