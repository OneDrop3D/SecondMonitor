import { useMemo, useState } from 'react';
import { usePoll } from '../live';
import { kph, lapTime } from '../format';
import type { DriverRow, RaceSnapshot, SpeedAnalysis, SpeedTraces, Trajectories, TrackSegment } from '../types';
import type { Selection } from '../selection';
import { LineChart, type Series } from '../charts/LineChart';
import { useWidth } from '../charts/useSize';
import { DriverPicker } from './DriverPicker';
import { ClassSwatch } from './Common';

interface Props {
  snapshot: RaceSnapshot;
  selection: Selection;
  classColor: (classId: string) => string;
}

/** Vmax en ligne droite, Vmin en virage, profils de vitesse et trajectoires de tous les pilotes. */
export function SpeedView({ snapshot, selection, classColor }: Props) {
  const [kind, setKind] = useState<'best' | 'last'>('best');
  const { data } = usePoll<SpeedAnalysis>('/api/analysis/speeds', 3000);
  const idsKey = selection.ids.join(',');
  const { data: traces } = usePoll<SpeedTraces>(idsKey ? `/api/analysis/speedtrace?ids=${encodeURIComponent(idsKey)}&kind=${kind}` : null, 4000);
  const lowRate = snapshot.capabilities.includes('lowRateOpponents');

  return (
    <div className="stack">
      {lowRate && (
        <div className="callout warn">
          <span aria-hidden>ℹ️</span>
          <div>
            <b>Données des adversaires reçues {snapshot.dataRate.opponentsHz.toFixed(1)} fois par seconde</b> : leurs Vmax, Vmin et trajectoires sont approximatives.
            Pour des mesures précises, sur le PC de jeu : SecondMonitor → Plugins → <i>Data Broadcast Server</i> → <i>Network Conservation Settings</i>,
            réglez « Non-Player Info Rate (ms) » sur <span className="code">100</span> (ou décochez « Enable »). Le réseau transporte un peu plus de données, sans effet sur le jeu.
          </div>
        </div>
      )}

      <section className="card">
        <div className="card-head">
          <span className="card-title">Vitesses par zone</span>
          <span className="muted hide-xs">Vmax en ligne droite · Vmin (vitesse de passage) en virage</span>
          <span className="spacer" />
          <KindSwitch kind={kind} setKind={setKind} />
        </div>
        <SpeedTable snapshot={snapshot} data={data} kind={kind} classColor={classColor} selection={selection} />
      </section>

      <section className="card">
        <div className="card-head">
          <span className="card-title">Profil de vitesse</span>
          <span className="muted hide-xs">{kind === 'best' ? 'meilleur tour' : 'dernier tour'} de chaque pilote, sur un tour</span>
        </div>
        <div className="card-body stack-sm">
          <DriverPicker snapshot={snapshot} selection={selection} />
          <SpeedTraceChart traces={traces} selection={selection} snapshot={snapshot} />
        </div>
      </section>

      <section className="card">
        <div className="card-head">
          <span className="card-title">Trajectoires</span>
          <span className="muted hide-xs">positions réelles transmises par le jeu</span>
        </div>
        <div className="card-body">
          {snapshot.track.worldPositionsValid ? (
            <TrajectoryPanel segments={data?.segments ?? []} selection={selection} snapshot={snapshot} kind={kind} />
          ) : (
            <div className="muted">Ce jeu ne transmet pas la position des voitures : trajectoires indisponibles.</div>
          )}
        </div>
      </section>
    </div>
  );
}

function KindSwitch({ kind, setKind }: { kind: 'best' | 'last'; setKind: (k: 'best' | 'last') => void }) {
  return (
    <div className="seg" role="group" aria-label="Tour de référence">
      <button className={kind === 'best' ? 'on' : ''} onClick={() => setKind('best')}>Meilleur</button>
      <button className={kind === 'last' ? 'on' : ''} onClick={() => setKind('last')}>Dernier tour</button>
    </div>
  );
}

function driverLabel(d: DriverRow) {
  return d.isPlayer ? 'Vous' : `#${d.number ?? '–'} ${d.shortName}`;
}

function SpeedTable({ snapshot, data, kind, classColor, selection }: { snapshot: RaceSnapshot; data: SpeedAnalysis | null; kind: 'best' | 'last'; classColor: (c: string) => string; selection: Selection }) {
  const [classFilter, setClassFilter] = useState<string | null>(() => snapshot.drivers.find((d) => d.isPlayer)?.classId ?? null);
  const classes = [...new Map(snapshot.drivers.map((d) => [d.classId, d.className ?? d.classId])).entries()];
  const segments = data?.segments ?? [];
  const rows = snapshot.drivers.filter((d) => !classFilter || d.classId === classFilter);
  const stat = (id: string) => data?.drivers.find((x) => x.id === id);

  // Échelle par colonne (catégorie affichée) : intensité de fond proportionnelle à la vitesse.
  const ranges = useMemo(() => {
    const r = new Map<string, [number, number]>();
    for (const s of [...segments.map((s) => s.id), '__top']) {
      const vals = rows.map((d) => {
        const st = stat(d.id);
        return s === '__top' ? (kind === 'best' ? st?.topSpeedBest : st?.topSpeedLast) : st?.[kind][s];
      }).filter((v): v is number => v != null);
      if (vals.length) r.set(s, [Math.min(...vals), Math.max(...vals)]);
    }
    return r;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, kind, classFilter, snapshot.drivers.length]);

  const cell = (segId: string, v: number | null | undefined) => {
    if (v == null) return <td key={segId} className="r muted">—</td>;
    const [lo, hi] = ranges.get(segId) ?? [v, v];
    const f = hi - lo < 0.05 ? 1 : (v - lo) / (hi - lo);
    const isBest = Math.abs(v - hi) < 0.05 && rows.length > 1;
    return (
      <td key={segId} className={`r tnum speed-cell ${isBest ? 'best' : ''}`} style={{ background: `color-mix(in oklab, var(--accent) ${Math.round(4 + f * 30)}%, transparent)` }} title={isBest ? 'Meilleure valeur de la catégorie' : undefined}>
        {v.toFixed(0)}{isBest ? ' ◆' : ''}
      </td>
    );
  };

  return (
    <div>
      {classes.length > 1 && (
        <div className="row card-toolbar" role="group" aria-label="Catégorie">
          <button className={`chip ${classFilter === null ? 'on' : ''}`} onClick={() => setClassFilter(null)}>Toutes</button>
          {classes.map(([id, name]) => (
            <button key={id} className={`chip ${classFilter === id ? 'on' : ''}`} onClick={() => setClassFilter(id)}><ClassSwatch color={classColor(id)} />{name}</button>
          ))}
        </div>
      )}
      {segments.length === 0 ? (
        <div className="card-body muted">{data?.message ?? 'Chargement…'}</div>
      ) : (
        <div className="table-scroll">
          <table className="lb speed-table">
            <thead>
              <tr>
                <th className="sticky-col">Pilote</th>
                <th className="r" title="Vitesse maximale sur le tour">Vmax tour</th>
                {segments.map((s) => (
                  <th key={s.id} className="r" title={`${s.name}${s.description ? ` (${s.description})` : ''} — ${s.kind === 'corner' ? 'vitesse minimale' : 'vitesse maximale'}`}>
                    {s.kind === 'corner' ? `${s.id} ↓` : `${s.id} ↑`}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((d) => {
                const st = stat(d.id);
                return (
                  <tr key={d.id} className={`${d.isPlayer ? 'player' : ''} ${selection.ids.includes(d.id) ? 'selected' : ''}`} onClick={() => selection.toggle(d.id)} title="Cliquer pour comparer ce pilote dans les graphiques">
                    <td className="sticky-col">
                      <span className="row nowrap" style={{ gap: 6 }}>
                        {selection.ids.includes(d.id) ? <span className="legend-line" style={{ borderColor: selection.colorOf(d.id) }} /> : <span className="legend-line" style={{ borderColor: 'transparent' }} />}
                        <span className="muted tnum">P{classFilter && snapshot.isMultiClass ? d.classPosition : d.position}</span> {driverLabel(d)}
                        {st && st.sampleHz > 0 && st.sampleHz < 3 && <span className="muted" title={`Données reçues ${st.sampleHz} fois/s : mesure approximative`}>≈</span>}
                      </span>
                    </td>
                    {cell('__top', kind === 'best' ? st?.topSpeedBest : st?.topSpeedLast)}
                    {segments.map((s) => cell(s.id, st?.[kind][s.id]))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      {segments.length > 0 && (
        <div className="chart-note muted card-foot">
          Zones détectées automatiquement d'après le profil de vitesse{data?.reference ? ` de ${data.reference}` : ''} · ↑ = Vmax de la ligne droite · ↓ = Vmin du virage · ◆ meilleure valeur · km/h · cliquer une ligne pour l'ajouter aux graphiques.
        </div>
      )}
    </div>
  );
}

function SpeedTraceChart({ traces, selection, snapshot }: { traces: SpeedTraces | null; selection: Selection; snapshot: RaceSnapshot }) {
  const byId = new Map(snapshot.drivers.map((d) => [d.id, d]));
  const series: Series[] = useMemo(() => (traces?.drivers ?? []).filter((t) => selection.ids.includes(t.id)).map((t) => {
    const d = byId.get(t.id);
    return {
      id: t.id,
      label: `${d ? driverLabel(d) : t.id} · T${t.lap}${t.time ? ` ${lapTime(t.time)}` : ''}`,
      color: selection.colorOf(t.id),
      width: 1.75,
      points: t.speed.map((v, i) => ({ x: (i + 0.5) * traces!.binSize, y: v })),
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }), [traces, selection.ids.join(',')]);
  const refs = (traces?.segments ?? []).filter((s) => s.kind === 'corner').map((s) => ({ x: s.apex, label: s.id }));
  return (
    <LineChart
      series={series}
      height={300}
      xFormat={(x) => (x >= 1000 ? `${(x / 1000).toFixed(1)} km` : `${Math.round(x)} m`)}
      xTitle={(x) => `${Math.round(x)} m depuis la ligne`}
      yFormat={(y) => `${Math.round(y)}`}
      valueFormat={(y) => kph(y)}
      refLines={refs}
      ariaLabel="Vitesse le long du tour"
      emptyText="Les profils apparaissent après un tour complet de chaque pilote."
    />
  );
}

function TrajectoryPanel({ segments, selection, snapshot, kind }: { segments: TrackSegment[]; selection: Selection; snapshot: RaceSnapshot; kind: 'best' | 'last' }) {
  const [segId, setSegId] = useState<string | null>(null);
  const current = segId ?? segments.find((s) => s.kind === 'corner')?.id ?? null;
  const idsKey = selection.ids.join(',');
  const { data } = usePoll<Trajectories>(idsKey ? `/api/analysis/trajectories?ids=${encodeURIComponent(idsKey)}&kind=${kind}${current && current !== '__lap' ? `&seg=${current}` : ''}` : null, 4000);
  const corners = segments.filter((s) => s.kind === 'corner');
  const straights = segments.filter((s) => s.kind === 'straight');
  return (
    <div className="stack-sm">
      <div className="row" role="group" aria-label="Zone affichée">
        <button className={`chip ${current === '__lap' ? 'on' : ''}`} onClick={() => setSegId('__lap')}>Tour complet</button>
        {corners.map((s) => <button key={s.id} className={`chip ${current === s.id ? 'on' : ''}`} onClick={() => setSegId(s.id)} title={s.name}>{s.id}</button>)}
        {straights.map((s) => <button key={s.id} className={`chip ${current === s.id ? 'on' : ''}`} onClick={() => setSegId(s.id)} title={s.name}>{s.id}</button>)}
      </div>
      <DriverPicker snapshot={snapshot} selection={selection} />
      <TrajectoryPlot data={data} selection={selection} snapshot={snapshot} full={current === '__lap'} />
    </div>
  );
}

function TrajectoryPlot({ data, selection, snapshot, full }: { data: Trajectories | null; selection: Selection; snapshot: RaceSnapshot; full: boolean }) {
  const { ref, width } = useWidth<HTMLDivElement>();
  const [hover, setHover] = useState<{ x: number; y: number } | null>(null);
  const byId = new Map(snapshot.drivers.map((d) => [d.id, d]));
  const drivers = (data?.drivers ?? []).filter((d) => selection.ids.includes(d.id) && d.points.length > 1);
  const H = Math.max(260, Math.min(520, Math.round(width * 0.62)));

  const frame = useMemo(() => {
    const pts = drivers.flatMap((d) => d.points);
    if (pts.length < 2) return null;
    const xs = pts.map((p) => p[0]), zs = pts.map((p) => p[1]);
    const minX = Math.min(...xs), maxX = Math.max(...xs), minZ = Math.min(...zs), maxZ = Math.max(...zs);
    const pad = 24;
    const scale = Math.min((width - pad * 2) / Math.max(1, maxX - minX), (H - pad * 2) / Math.max(1, maxZ - minZ));
    const ox = (width - (maxX - minX) * scale) / 2;
    const oz = (H - (maxZ - minZ) * scale) / 2;
    return { scale, tx: (x: number) => ox + (x - minX) * scale, ty: (z: number) => oz + (z - minZ) * scale, metersPerPx: 1 / scale };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, width, H, selection.ids.join(',')]);

  // Au survol : la vitesse de chaque pilote au point le plus proche.
  const readout = useMemo(() => {
    if (!hover || !frame) return null;
    return drivers.map((d) => {
      let best = d.points[0], bd = Infinity;
      for (const p of d.points) { const dd = (frame.tx(p[0]) - hover.x) ** 2 + (frame.ty(p[1]) - hover.y) ** 2; if (dd < bd) { bd = dd; best = p; } }
      return { d, p: best, close: Math.sqrt(bd) < 40 };
    }).filter((r) => r.close);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hover, frame, data]);

  const seg = data?.segment;
  return (
    <div ref={ref} className="chart-area">
      {!frame ? (
        <div className="chart-empty" style={{ height: 200 }}>Les trajectoires apparaissent après un tour complet des pilotes sélectionnés.</div>
      ) : (
        <>
          <svg width={width} height={H} className="traj" role="img" aria-label="Trajectoires des pilotes"
            onPointerMove={(e) => { const r = e.currentTarget.getBoundingClientRect(); setHover({ x: e.clientX - r.left, y: e.clientY - r.top }); }}
            onPointerLeave={() => setHover(null)}>
            {drivers.map((d) => {
              const path = d.points.map((p, i) => `${i ? 'L' : 'M'}${frame.tx(p[0]).toFixed(1)},${frame.ty(p[1]).toFixed(1)}`).join(' ');
              return <path key={`bg${d.id}`} d={path} fill="none" stroke="var(--track)" strokeWidth={Math.max(6, Math.min(26, 12 / frame.metersPerPx))} strokeLinecap="round" strokeLinejoin="round" />;
            })}
            {drivers.map((d) => {
              const path = d.points.map((p, i) => `${i ? 'L' : 'M'}${frame.tx(p[0]).toFixed(1)},${frame.ty(p[1]).toFixed(1)}`).join(' ');
              return <path key={d.id} d={path} fill="none" stroke={selection.colorOf(d.id)} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />;
            })}
            {!full && drivers.map((d) => {
              // Point de vitesse minimale (virage) ou maximale (ligne droite) de chaque pilote.
              const pick = d.points.reduce((a, b) => (seg?.kind === 'straight' ? (b[2] > a[2] ? b : a) : (b[2] < a[2] ? b : a)));
              return <circle key={`m${d.id}`} cx={frame.tx(pick[0])} cy={frame.ty(pick[1])} r={5} fill={selection.colorOf(d.id)} stroke="var(--surface-1)" strokeWidth={2}><title>{`${byId.get(d.id)?.shortName ?? ''} ${seg?.kind === 'straight' ? 'Vmax' : 'Vmin'} ${kph(pick[2])}`}</title></circle>;
            })}
            {readout?.map((r) => <circle key={`h${r.d.id}`} cx={frame.tx(r.p[0])} cy={frame.ty(r.p[1])} r={4} fill="none" stroke={selection.colorOf(r.d.id)} strokeWidth={2} />)}
            <g className="axis-text">
              <line x1={12} x2={12 + 50 / frame.metersPerPx} y1={H - 14} y2={H - 14} stroke="var(--text-muted)" strokeWidth={2} />
              <text x={12} y={H - 20}>50 m</text>
            </g>
          </svg>
          {readout && readout.length > 0 && hover && (
            <div className="tooltip chart-tip" style={hover.x > width * 0.6 ? { right: width - hover.x + 12, top: hover.y } : { left: hover.x + 12, top: hover.y }}>
              {readout.map((r) => {
                const d = byId.get(r.d.id);
                return (
                  <div key={r.d.id} className="tip-row">
                    <span className="legend-line" style={{ borderColor: selection.colorOf(r.d.id) }} />
                    <span className="tip-label">{d ? driverLabel(d) : r.d.id}</span>
                    <b className="tnum">{kph(r.p[2])}</b>
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}
      {frame && (
        <div className="legend chart-legend" style={{ marginTop: 8 }}>
          {drivers.map((d) => {
            const dr = byId.get(d.id);
            const pick = d.points.reduce((a, b) => (seg?.kind === 'straight' ? (b[2] > a[2] ? b : a) : (b[2] < a[2] ? b : a)));
            return (
              <span key={d.id} className="legend-item">
                <span className="legend-line" style={{ borderColor: selection.colorOf(d.id) }} />
                {dr ? driverLabel(dr) : d.id} · T{d.lap}
                {!full && <b className="tnum">&nbsp;{seg?.kind === 'straight' ? 'Vmax' : 'Vmin'} {kph(pick[2])}</b>}
                {d.sampleHz > 0 && d.sampleHz < 3 && <span className="muted" title="Données peu fréquentes : trajectoire lissée">&nbsp;≈</span>}
              </span>
            );
          })}
        </div>
      )}
    </div>
  );
}
