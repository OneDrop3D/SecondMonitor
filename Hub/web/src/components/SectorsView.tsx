import { useMemo, useState } from 'react';
import { usePoll } from '../live';
import { delta, lapTime } from '../format';
import type { DriverRow, MicroSectorReport, RaceSnapshot } from '../types';
import { useWidth } from '../charts/useSize';
import { ClassSwatch } from './Common';

type Mode = 'current' | 'last' | 'best';
type Status = 'overall' | 'personal' | 'slower' | 'pit' | 'none' | 'live';

interface Props {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (d: DriverRow) => void;
}

const STATUS_LABEL: Record<Status, string> = {
  overall: 'Meilleur de la catégorie',
  personal: 'Meilleur personnel',
  slower: 'Plus lent que son meilleur',
  pit: 'Passage au stand',
  none: 'Pas de mesure',
  live: 'En cours',
};

/** Micro-secteurs : le tour est découpé en tronçons égaux, chronométrés par le hub pour tous les pilotes. */
export function SectorsView({ snapshot, classColor, onSelect }: Props) {
  const { data } = usePoll<MicroSectorReport>('/api/analysis/microsectors', 1000);
  const [mode, setMode] = useState<Mode>('last');
  const [showTimes, setShowTimes] = useState(false);
  const [classFilter, setClassFilter] = useState<string | null>(() => snapshot.drivers.find((d) => d.isPlayer)?.classId ?? null);
  const classes = [...new Map(snapshot.drivers.map((d) => [d.classId, d.className ?? d.classId])).entries()];
  const rows = snapshot.drivers.filter((d) => !classFilter || d.classId === classFilter);
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const mine = data?.drivers.find((x) => x.id === me?.id);

  const status = (classId: string, best: (number | null)[], v: number | null, i: number, pit: boolean): Status => {
    if (pit) return 'pit';
    if (v == null) return 'none';
    const cb = data?.classBest[classId]?.[i];
    if (cb != null && v <= cb + 0.0005) return 'overall';
    const pb = best[i];
    if (pb != null && v <= pb + 0.0005) return 'personal';
    return 'slower';
  };

  // Delta en direct du joueur : tour en cours comparé à ses meilleurs micro-secteurs et à son dernier tour.
  const live = useMemo(() => {
    if (!mine || mine.currentIndex <= 0) return null;
    let cur = 0, best = 0, last = 0, okBest = true, okLast = true;
    for (let i = 0; i < mine.currentIndex; i++) {
      const c = mine.current[i];
      if (c == null) return null;
      cur += c;
      if (mine.best[i] == null) okBest = false; else best += mine.best[i]!;
      if (mine.last[i] == null) okLast = false; else last += mine.last[i]!;
    }
    return { vsBest: okBest ? cur - best : null, vsLast: okLast ? cur - last : null, sector: mine.currentIndex };
  }, [mine]);

  return (
    <div className="stack">
      <div className="grid-2 grid-2-wide-left">
        <section className="card">
          <div className="card-head">
            <span className="card-title">Mon tour en direct</span>
            <span className="muted">{data ? `${data.count} micro-secteurs de ${Math.round(data.lapLength / Math.max(1, data.count))} m` : ''}</span>
          </div>
          <div className="card-body">
            <div className="tiles">
              <div className={`tile ${live?.vsBest != null ? (live.vsBest <= 0 ? 'good' : 'warn') : ''}`}>
                <div className="tile-label">Delta vs meilleurs secteurs</div>
                <div className="tile-value tnum">{live?.vsBest != null ? `${delta(live.vsBest, 2)} s` : '—'}</div>
                <div className="tile-sub">{live ? `après ${live.sector} micro-secteurs` : 'en attente du prochain repère'}</div>
              </div>
              <div className={`tile ${live?.vsLast != null ? (live.vsLast <= 0 ? 'good' : 'warn') : ''}`}>
                <div className="tile-label">Delta vs dernier tour</div>
                <div className="tile-value tnum">{live?.vsLast != null ? `${delta(live.vsLast, 2)} s` : '—'}</div>
                <div className="tile-sub">même point du tour</div>
              </div>
              <div className="tile">
                <div className="tile-label">Meilleur tour théorique</div>
                <div className="tile-value tnum">{lapTime(mine?.theoreticalBest)}</div>
                <div className="tile-sub tnum">
                  {mine?.theoreticalBest != null && me?.bestLap != null ? `${delta(mine.theoreticalBest - me.bestLap, 3)} s vs votre meilleur` : 'somme des meilleurs micro-secteurs'}
                </div>
              </div>
            </div>
          </div>
        </section>
        <section className="card">
          <div className="card-head"><span className="card-title">Mon dernier tour sur la piste</span></div>
          <div className="card-body">
            <MicroSectorMap snapshot={snapshot} data={data} me={me} status={status} />
          </div>
        </section>
      </div>

      <section className="card">
        <div className="card-head">
          <span className="card-title">Micro-secteurs du plateau</span>
          <span className="spacer" />
          <div className="seg" role="group" aria-label="Tour affiché">
            <button className={mode === 'current' ? 'on' : ''} onClick={() => setMode('current')}>En cours</button>
            <button className={mode === 'last' ? 'on' : ''} onClick={() => setMode('last')}>Dernier tour</button>
            <button className={mode === 'best' ? 'on' : ''} onClick={() => setMode('best')}>Meilleurs</button>
          </div>
          <label className="row secondary hide-xs" style={{ gap: 6 }}>
            <input type="checkbox" checked={showTimes} onChange={(e) => setShowTimes(e.target.checked)} /> Temps
          </label>
        </div>
        {classes.length > 1 && (
          <div className="row card-toolbar" role="group" aria-label="Catégorie">
            <button className={`chip ${classFilter === null ? 'on' : ''}`} onClick={() => setClassFilter(null)}>Toutes</button>
            {classes.map(([id, name]) => (
              <button key={id} className={`chip ${classFilter === id ? 'on' : ''}`} onClick={() => setClassFilter(id)}><ClassSwatch color={classColor(id)} />{name}</button>
            ))}
          </div>
        )}
        <div className="table-scroll">
          <table className={`lb ms-table ${showTimes ? 'with-times' : ''}`}>
            <thead>
              <tr>
                <th className="sticky-col">Pilote</th>
                {Array.from({ length: data?.count ?? 0 }, (_, i) => <th key={i} className="c ms-head">{i + 1}</th>)}
                <th className="r">Tour</th>
                <th className="r hide-sm" title="Somme de ses meilleurs micro-secteurs">Théorique</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((d) => {
                const m = data?.drivers.find((x) => x.id === d.id);
                const values = !m ? [] : mode === 'current' ? m.current : mode === 'last' ? m.last : m.best;
                return (
                  <tr key={d.id} className={`lb-row ${d.isPlayer ? 'player' : ''}`} onClick={() => onSelect(d)}>
                    <td className="sticky-col nowrap"><span className="muted tnum">P{classFilter && snapshot.isMultiClass ? d.classPosition : d.position}</span> {d.isPlayer ? <b>Vous</b> : `#${d.number ?? '–'} ${d.shortName}`}</td>
                    {Array.from({ length: data?.count ?? 0 }, (_, i) => {
                      const v = values[i] ?? null;
                      const isLive = mode === 'current' && m?.currentIndex === i;
                      const st: Status = isLive ? 'live' : m ? status(d.classId, m.best, v, i, mode === 'last' && m.lastPit[i]) : 'none';
                      const cb = data?.classBest[d.classId]?.[i];
                      return (
                        <td key={i} className="c ms-td">
                          <span className={`ms ${st}`} title={`Micro-secteur ${i + 1} · ${v != null ? `${v.toFixed(3)} s` : '—'}${v != null && cb != null ? ` (${delta(v - cb, 3)} vs meilleur cat.)` : ''} · ${STATUS_LABEL[st]}`}>
                            {showTimes && v != null ? v.toFixed(1) : ''}
                          </span>
                        </td>
                      );
                    })}
                    <td className="r tnum">{lapTime(mode === 'best' ? d.bestLap : d.lastLap)}</td>
                    <td className="r tnum hide-sm">{lapTime(m?.theoreticalBest)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div className="legend card-foot">
          <span className="legend-item"><span className="ms overall" /> Meilleur de la catégorie</span>
          <span className="legend-item"><span className="ms personal" /> Meilleur personnel</span>
          <span className="legend-item"><span className="ms slower" /> Plus lent</span>
          <span className="legend-item"><span className="ms pit" /> Stand</span>
          <span className="legend-item"><span className="ms live" /> En cours</span>
          {snapshot.capabilities.includes('lowRateOpponents') && <span className="legend-item muted">Adversaires reçus {snapshot.dataRate.opponentsHz.toFixed(1)}×/s : temps interpolés (±0,2 s).</span>}
        </div>
      </section>
    </div>
  );
}

function MicroSectorMap({ snapshot, data, me, status }: {
  snapshot: RaceSnapshot;
  data: MicroSectorReport | null;
  me: DriverRow | undefined;
  status: (classId: string, best: (number | null)[], v: number | null, i: number, pit: boolean) => Status;
}) {
  const { ref, width } = useWidth<HTMLDivElement>(360);
  const outline = snapshot.track.outline;
  const mine = data?.drivers.find((x) => x.id === me?.id);
  const H = Math.round(Math.min(320, width * 0.7));
  const frame = useMemo(() => {
    if (!outline || outline.length < 10) return null;
    const xs = outline.map((p) => p[0]), zs = outline.map((p) => p[1]);
    const minX = Math.min(...xs), maxX = Math.max(...xs), minZ = Math.min(...zs), maxZ = Math.max(...zs);
    const pad = 16;
    const scale = Math.min((width - pad * 2) / Math.max(1, maxX - minX), (H - pad * 2) / Math.max(1, maxZ - minZ));
    const ox = (width - (maxX - minX) * scale) / 2, oz = (H - (maxZ - minZ) * scale) / 2;
    return { tx: (x: number) => ox + (x - minX) * scale, ty: (z: number) => oz + (z - minZ) * scale };
  }, [outline, width, H]);

  return (
    <div ref={ref}>
      {!frame || !data || !mine || !me ? (
        <div className="chart-empty" style={{ height: 160 }}>La carte se colore après un tour complet (tracé appris).</div>
      ) : (
        <svg width={width} height={H} role="img" aria-label="Micro-secteurs de votre dernier tour sur le tracé">
          {Array.from({ length: data.count }, (_, i) => {
            const n = outline!.length;
            const a = Math.floor((i / data.count) * (n - 1));
            const b = Math.floor(((i + 1) / data.count) * (n - 1));
            const pts = outline!.slice(a, b + 1);
            const d = pts.map((p, k) => `${k ? 'L' : 'M'}${frame.tx(p[0]).toFixed(1)},${frame.ty(p[1]).toFixed(1)}`).join(' ');
            const st = status(me.classId, mine.best, mine.last[i], i, mine.lastPit[i]);
            return (
              <path key={i} d={d} fill="none" stroke={`var(--ms-${st})`} strokeWidth={8} strokeLinecap="butt">
                <title>{`Micro-secteur ${i + 1} : ${mine.last[i] != null ? `${mine.last[i]!.toFixed(3)} s` : '—'} · ${STATUS_LABEL[st]}`}</title>
              </path>
            );
          })}
          <rect x={frame.tx(outline![0][0]) - 2} y={frame.ty(outline![0][1]) - 9} width={4} height={18} fill="var(--text-primary)" rx={1} />
        </svg>
      )}
    </div>
  );
}
