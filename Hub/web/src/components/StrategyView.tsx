import { useEffect, useMemo, useRef, useState } from 'react';
import { compoundInfo, lapTime } from '../format';
import type { DriverRow, RaceSnapshot, StintDto } from '../types';
import { ClassSwatch, Confidence, TyreBadge } from './Common';
import { PitPlanCard } from './RaceExtras';

const ROW_H = 26;
const BAR_H = 16;
const LABEL_W = 150;
const TOP = 26;

function useWidth<T extends HTMLElement>() {
  const ref = useRef<T>(null);
  const [width, setWidth] = useState(800);
  useEffect(() => {
    if (!ref.current) return;
    const ro = new ResizeObserver((entries) => setWidth(entries[0].contentRect.width));
    ro.observe(ref.current);
    return () => ro.disconnect();
  }, []);
  return { ref, width };
}

const FILL: Record<string, string> = {
  soft: 'var(--tyre-soft)', medium: 'var(--tyre-medium)', hard: 'var(--tyre-hard)', inter: 'var(--tyre-inter)',
  wet: 'var(--tyre-wet)', dry: 'var(--tyre-other)', other: 'var(--tyre-other)', unknown: 'var(--tyre-other)',
};
const INK: Record<string, string> = { medium: '#0b0b0b', hard: 'var(--tyre-hard-ink)' };

interface Props {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (d: DriverRow) => void;
}

interface Tip {
  x: number;
  y: number;
  d: DriverRow;
  s?: StintDto;
  projected?: boolean;
}

/** Frise des relais de tous les pilotes : gommes, arrêts, et projection du prochain arrêt estimé. */
export function StrategyView({ snapshot, classColor, onSelect }: Props) {
  const { ref, width } = useWidth<HTMLDivElement>();
  const [classFilter, setClassFilter] = useState<string | null>(() => (snapshot.isMultiClass ? snapshot.drivers.find((d) => d.isPlayer)?.classId ?? null : null));
  const [tip, setTip] = useState<Tip | null>(null);
  const drivers = classFilter ? snapshot.drivers.filter((d) => d.classId === classFilter) : snapshot.drivers;
  const classes = [...new Map(snapshot.drivers.map((d) => [d.classId, d.className ?? d.classId])).entries()];

  const leaderLaps = snapshot.drivers[0]?.laps ?? 0;
  const projectedEnd = leaderLaps + (snapshot.player?.lapsRemaining ?? 5);
  const maxLap = Math.max(
    5,
    Math.ceil(projectedEnd),
    ...snapshot.drivers.map((d) => d.nextStop?.lap ?? 0),
    ...snapshot.drivers.map((d) => d.laps + 1),
  );

  const plotW = Math.max(200, width - LABEL_W - 16);
  const x = (lap: number) => LABEL_W + (Math.max(0, lap) / maxLap) * plotW;
  const height = TOP + drivers.length * ROW_H + 8;
  const ticks = useMemo(() => {
    const step = maxLap > 120 ? 20 : maxLap > 60 ? 10 : maxLap > 25 ? 5 : 2;
    const list: number[] = [];
    for (let l = 0; l <= maxLap; l += step) list.push(l);
    return list;
  }, [maxLap]);

  const compounds = [...new Set(snapshot.drivers.flatMap((d) => d.stints.map((s) => compoundInfo(s.compound).kind)))];
  const soon = snapshot.drivers
    .filter((d) => d.nextStop && !d.inPits)
    .sort((a, b) => a.nextStop!.lapsUntil - b.nextStop!.lapsUntil)
    .slice(0, 8);

  return (
    <div className="stack">
      {snapshot.player?.pitPlan && (
        <div className="card">
          <div className="card-head"><span className="card-title">Simulation d'arrêt</span><span className="muted">si vous rentrez maintenant · undercut · overcut</span></div>
          <div className="card-body"><PitPlanCard player={snapshot.player} /></div>
        </div>
      )}
      <div className="card">
        <div className="card-head">
          <span className="card-title">Stratégies du plateau</span>
          <span className="muted">relais, gommes et arrêts estimés</span>
          <span className="spacer" />
          {classes.length > 1 && (
            <div className="row">
              <button className={`chip ${classFilter === null ? 'on' : ''}`} onClick={() => setClassFilter(null)}>Toutes</button>
              {classes.map(([id, name]) => (
                <button key={id} className={`chip ${classFilter === id ? 'on' : ''}`} onClick={() => setClassFilter(classFilter === id ? null : id)}>
                  <ClassSwatch color={classColor(id)} />{name}
                </button>
              ))}
            </div>
          )}
        </div>
        <div className="card-body">
          <div className="legend" style={{ marginBottom: 8 }}>
            {compounds.map((k) => (
              <span key={k} className="row" style={{ gap: 6 }}>
                <span className="swatch" style={{ background: FILL[k] }} />
                {k === 'soft' ? 'Tendre (S)' : k === 'medium' ? 'Medium (M)' : k === 'hard' ? 'Dur (H)' : k === 'inter' ? 'Intermédiaire (I)' : k === 'wet' ? 'Pluie (W)' : 'Autre'}
              </span>
            ))}
            <span className="row" style={{ gap: 6 }}>
              <span className="swatch" style={{ background: 'transparent', border: '1.5px dashed var(--text-muted)' }} />
              Projection jusqu'au prochain arrêt estimé
            </span>
          </div>
          <div ref={ref} style={{ width: '100%' }}>
            <svg width={width} height={height} role="img" aria-label="Frise des relais de chaque pilote">
              {ticks.map((t) => (
                <g key={t}>
                  <line x1={x(t)} x2={x(t)} y1={TOP - 6} y2={height - 4} stroke="var(--grid)" strokeWidth={1} />
                  <text className="gantt-axis" x={x(t)} y={TOP - 10} textAnchor="middle">T{t}</text>
                </g>
              ))}
              <line x1={x(leaderLaps)} x2={x(leaderLaps)} y1={TOP - 6} y2={height - 4} stroke="var(--text-secondary)" strokeWidth={1.5} />

              {drivers.map((d, row) => {
                const y = TOP + row * ROW_H + (ROW_H - BAR_H) / 2;
                return (
                  <g key={d.id}>
                    {d.isPlayer && <rect x={0} y={TOP + row * ROW_H} width={width} height={ROW_H} fill="var(--player-wash)" />}
                    <rect x={4} y={y + 2} width={3} height={BAR_H - 4} rx={1.5} fill={classColor(d.classId)} />
                    <text className="gantt-row-label" x={12} y={y + BAR_H - 4} style={{ cursor: 'pointer', fontWeight: d.isPlayer ? 700 : 400 }} onClick={() => onSelect(d)}>
                      {`P${d.position}  ${d.number ?? ''} ${d.shortName}`}
                    </text>
                    {d.stints.map((s) => {
                      const end = s.endLap ?? d.laps + d.lapFraction;
                      const x0 = x(s.startLap) + 1;
                      const x1 = x(end) - 1;
                      const w = Math.max(2, x1 - x0);
                      const kind = compoundInfo(s.compound).kind;
                      const letter = compoundInfo(s.compound).letter;
                      return (
                        <g key={s.index} onPointerMove={(e) => setTip({ x: e.clientX, y: e.clientY, d, s })} onPointerLeave={() => setTip(null)}>
                          <rect x={x0} y={y} width={w} height={BAR_H} rx={4} fill={FILL[kind]} />
                          {w > 22 && (
                            <text x={x0 + 6} y={y + BAR_H - 4} fontSize={11} fontWeight={700} fill={INK[kind] ?? '#fff'}>
                              {letter}{w > 48 ? ` ${s.laps}` : ''}
                            </text>
                          )}
                        </g>
                      );
                    })}
                    {d.nextStop && !d.inPits && d.nextStop.lap > d.laps && (
                      <g onPointerMove={(e) => setTip({ x: e.clientX, y: e.clientY, d, projected: true })} onPointerLeave={() => setTip(null)}>
                        <rect
                          x={x(d.laps + d.lapFraction) + 1}
                          y={y + 1}
                          width={Math.max(2, x(d.nextStop.lap) - x(d.laps + d.lapFraction) - 2)}
                          height={BAR_H - 2}
                          rx={4}
                          fill="transparent"
                          stroke="var(--text-muted)"
                          strokeDasharray="4 3"
                        />
                        <path d={`M${x(d.nextStop.lap)},${y - 1} l4,-5 h-8 z`} fill="var(--text-secondary)" />
                      </g>
                    )}
                  </g>
                );
              })}
            </svg>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-head"><span className="card-title">Prochains arrêts attendus</span><span className="muted">estimations</span></div>
        <div className="card-body">
          {soon.length === 0 ? (
            <div className="muted">Pas encore assez de données : les estimations apparaissent après les premiers relais.</div>
          ) : (
            <table className="mini-table">
              <thead>
                <tr><th>Pos</th><th>Pilote</th><th>Pneus</th><th>Arrêt estimé</th><th>Confiance</th><th>Basé sur</th></tr>
              </thead>
              <tbody>
                {soon.map((d) => (
                  <tr key={d.id} onClick={() => onSelect(d)} style={{ cursor: 'pointer' }}>
                    <td>P{d.position}</td>
                    <td>{d.number != null ? `#${d.number} ` : ''}{d.name}</td>
                    <td><TyreBadge compound={d.tyres.compound} laps={d.tyres.stintLaps} /></td>
                    <td className="tnum">T{d.nextStop!.lap} <span className="muted">({d.nextStop!.lapsUntil === 0 ? 'imminent' : `dans ${d.nextStop!.lapsUntil}`})</span></td>
                    <td><Confidence level={d.nextStop!.confidence} /></td>
                    <td className="secondary">{d.nextStop!.reason}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {tip && (
        <div className="tooltip" style={{ left: tip.x + 14, top: tip.y + 10 }}>
          {tip.s ? (
            <>
              <div className="tv">{tip.s.laps} tours · {compoundInfo(tip.s.compound).label}</div>
              <div className="secondary">Relais {tip.s.index} de {tip.d.name}</div>
              <div className="tnum">T{tip.s.startLap} → {tip.s.endLap != null ? `T${tip.s.endLap}` : 'en cours'}</div>
              <div className="tnum">Moyenne {lapTime(tip.s.averageLap)} · meilleur {lapTime(tip.s.bestLap)}</div>
            </>
          ) : tip.d.nextStop ? (
            <>
              <div className="tv">Arrêt estimé T{tip.d.nextStop.lap}</div>
              <div className="secondary">{tip.d.name}</div>
              <div>D'après {tip.d.nextStop.reason}</div>
            </>
          ) : null}
        </div>
      )}
    </div>
  );
}
