import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { usePoll } from '../live';
import { kph, lapTime } from '../format';
import type { DriverRow, RaceSnapshot, SpeedAnalysis, SpeedTraces, Trajectories, TrackSegment } from '../types';
import type { Selection } from '../selection';
import { LineChart, type Series } from '../charts/LineChart';
import { useViewportHeight, useWidth } from '../charts/useSize';
import { niceScale, useZoomPan } from '../charts/useZoomPan';
import { useStoredChoice } from '../prefs';
import { DriverPicker } from './DriverPicker';
import { ClassSwatch } from './Common';
import { LapCompare } from './LapCompare';
import { distanceLabel, inZone } from '../track';

interface Props {
  snapshot: RaceSnapshot;
  selection: Selection;
  classColor: (classId: string) => string;
}

type SpeedTab = 'zones' | 'traj' | 'mylaps';
const TABS: readonly SpeedTab[] = ['zones', 'traj', 'mylaps'];

/** Vmax en ligne droite, Vmin en virage, profils de vitesse, trajectoires de tous les pilotes et comparaison de vos tours. */
export function SpeedView({ snapshot, selection, classColor }: Props) {
  const [view, setView] = useStoredChoice<SpeedTab>('smhub-vitesses-vue', 'zones', TABS);
  const [kind, setKind] = useStoredChoice<'best' | 'last'>('smhub-vitesses-tour', 'best', ['best', 'last']);
  const { data } = usePoll<SpeedAnalysis>('/api/analysis/speeds', view === 'mylaps' ? 10000 : 3000);
  const idsKey = selection.ids.join(',');
  const { data: traces } = usePoll<SpeedTraces>(view === 'zones' && idsKey ? `/api/analysis/speedtrace?ids=${encodeURIComponent(idsKey)}&kind=${kind}` : null, 4000);
  const lowRate = snapshot.capabilities.includes('lowRateOpponents');

  return (
    <div className="stack">
      <div className="row view-tabs-row">
        <div className="seg view-tabs" role="tablist" aria-label="Affichage">
          <button role="tab" aria-selected={view === 'zones'} className={view === 'zones' ? 'on' : ''} onClick={() => setView('zones')}>Zones et profils</button>
          <button role="tab" aria-selected={view === 'traj'} className={view === 'traj' ? 'on' : ''} onClick={() => setView('traj')}>Trajectoires</button>
          <button role="tab" aria-selected={view === 'mylaps'} className={view === 'mylaps' ? 'on' : ''} onClick={() => setView('mylaps')}>Mes tours comparés</button>
        </div>
        <span className="spacer" />
        {view !== 'mylaps' && <KindSwitch kind={kind} setKind={setKind} />}
      </div>

      {lowRate && view !== 'mylaps' && (
        <div className="callout warn">
          <span aria-hidden>ℹ️</span>
          <div>
            <b>Données des adversaires reçues {snapshot.dataRate.opponentsHz.toFixed(1)} fois par seconde</b> : leurs Vmax, Vmin et trajectoires sont approximatives.
            Pour des mesures précises, sur le PC de jeu : SecondMonitor → Plugins → <i>Data Broadcast Server</i> → <i>Network Conservation Settings</i>,
            réglez « Non-Player Info Rate (ms) » sur <span className="code">100</span> (ou décochez « Enable »). Le réseau transporte un peu plus de données, sans effet sur le jeu.
          </div>
        </div>
      )}

      {view === 'zones' && (
        <>
          <section className="card">
            <div className="card-head">
              <span className="card-title">Vitesses par zone</span>
              <span className="muted hide-xs">Vmax en ligne droite · Vmin (vitesse de passage) en virage</span>
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
        </>
      )}

      {view === 'traj' && (
        <section className="card">
          <div className="card-head">
            <span className="card-title">Trajectoires</span>
            <span className="muted hide-xs">positions réelles transmises par le jeu · {kind === 'best' ? 'meilleur tour' : 'dernier tour'} de chaque pilote</span>
          </div>
          <div className="card-body">
            {snapshot.track.worldPositionsValid ? (
              <TrajectoryPanel segments={data?.segments ?? []} selection={selection} snapshot={snapshot} kind={kind} />
            ) : (
              <div className="muted">Ce jeu ne transmet pas la position des voitures : trajectoires indisponibles.</div>
            )}
          </div>
        </section>
      )}

      {view === 'mylaps' && <LapCompare snapshot={snapshot} segments={data?.segments ?? []} />}
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
  // Zone mise en évidence sur la carte et dans le tableau : survolée, sinon choisie d'un clic.
  const [pinned, setPinned] = useState<string | null>(null);
  const [hovered, setHovered] = useState<string | null>(null);
  const active = hovered ?? pinned;
  const scroller = useRef<HTMLDivElement>(null);
  const pick = (id: string) => setPinned((p) => (p === id ? null : id));
  // Choisie sur la carte : sa colonne vient au milieu (au Mans, le tableau est bien plus large que l'écran).
  useEffect(() => {
    const box = scroller.current;
    const th = pinned ? box?.querySelector<HTMLElement>(`th[data-zone="${pinned}"]`) : null;
    // Centrée dans ce qui reste visible à droite de la colonne « Pilote », qui ne défile pas.
    const fixed = box?.querySelector<HTMLElement>('th.sticky-col')?.offsetWidth ?? 0;
    if (box && th) box.scrollTo({ left: th.offsetLeft - fixed - (box.clientWidth - fixed - th.offsetWidth) / 2, behavior: 'smooth' });
  }, [pinned]);

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

  const cell = (segId: string, v: number | null | undefined, zone = '') => {
    if (v == null) return <td key={segId} data-zone={zone ? segId : undefined} className={`r muted ${zone}`}>—</td>;
    const [lo, hi] = ranges.get(segId) ?? [v, v];
    const f = hi - lo < 0.05 ? 1 : (v - lo) / (hi - lo);
    const isBest = Math.abs(v - hi) < 0.05 && rows.length > 1;
    return (
      <td key={segId} data-zone={zone ? segId : undefined} className={`r tnum speed-cell ${isBest ? 'best' : ''} ${zone}`} style={{ background: `color-mix(in oklab, var(--accent) ${Math.round(4 + f * 30)}%, transparent)` }} title={isBest ? 'Meilleure valeur de la catégorie' : undefined}>
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
        <>
        <ZoneMap snapshot={snapshot} segments={segments} data={data} kind={kind} selection={selection} active={active} pinned={pinned} onHover={setHovered} onPick={pick} />
        <div className="table-scroll" ref={scroller}>
          <table className="lb speed-table" onMouseOver={(e) => setHovered((e.target as HTMLElement).closest<HTMLElement>('[data-zone]')?.dataset.zone ?? null)} onMouseLeave={() => setHovered(null)}>
            <thead>
              <tr>
                <th className="sticky-col">Pilote</th>
                <th className="r" title="Vitesse maximale sur le tour">Vmax tour</th>
                {segments.map((s) => (
                  <th key={s.id} data-zone={s.id} className={`r zone-th ${s.kind} ${active === s.id ? 'zone-on' : ''}`} onClick={() => pick(s.id)}
                    title={`${s.place ? `${s.place} — ` : ''}${s.name}${s.description ? ` (${s.description})` : ''} — ${s.kind === 'corner' ? 'vitesse minimale' : 'vitesse maximale'} · cliquer pour la garder en évidence`}>
                    {s.kind === 'corner' ? `${s.id} ↓` : `${s.id} ↑`}
                    {s.place && <span className="zone-place">{s.place}</span>}
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
                    {segments.map((s) => cell(s.id, st?.[kind][s.id], `zone-col ${s.kind} ${active === s.id ? 'zone-on' : ''}`))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        </>
      )}
      {segments.length > 0 && (
        <div className="chart-note muted card-foot">
          Zones détectées automatiquement d'après le profil de vitesse{data?.reference ? ` de ${data.reference}` : ''} · ↑ = Vmax de la ligne droite (vert) · ↓ = Vmin du virage (orange) · ◆ meilleure valeur · km/h · survoler une zone, sur la carte ou dans le tableau, la montre des deux côtés ; un clic la garde · cliquer une ligne pour ajouter le pilote aux graphiques.
        </div>
      )}
    </div>
  );
}

/** Carte des zones sur le tracé appris : lignes droites et virages en deux couleurs, reliée au tableau ; à côté, la valeur de la zone pour les pilotes comparés. */
function ZoneMap({ snapshot, segments, data, kind, selection, active, pinned, onHover, onPick }: {
  snapshot: RaceSnapshot;
  segments: TrackSegment[];
  data: SpeedAnalysis | null;
  kind: 'best' | 'last';
  selection: Selection;
  active: string | null;
  pinned: string | null;
  onHover: (id: string | null) => void;
  onPick: (id: string) => void;
}) {
  const { ref, width } = useWidth<HTMLDivElement>(320);
  const outline = snapshot.track.outline;
  const L = snapshot.track.length;
  const segKey = segments.map((s) => `${s.id}:${s.start}:${s.end}:${s.apex}`).join(',');

  const drawn = useMemo(() => {
    if (!outline || outline.length < 10 || !(L > 0)) return null;
    const pad = 22, maxH = 420, n = outline.length;
    const xs = outline.map((p) => p[0]), zs = outline.map((p) => p[1]);
    const minX = Math.min(...xs), maxX = Math.max(...xs), minZ = Math.min(...zs), maxZ = Math.max(...zs);
    const scale = Math.min((width - pad * 2) / Math.max(1, maxX - minX), (maxH - pad * 2) / Math.max(1, maxZ - minZ));
    const h = Math.round((maxZ - minZ) * scale + pad * 2);
    const ox = (width - (maxX - minX) * scale) / 2;
    // Le tracé appris est régulier en distance (comme pour placer les voitures sur la carte de l'accueil).
    const at = (m: number): [number, number] => {
      const f = ((((m / L) % 1) + 1) % 1) * (n - 1);
      const i = Math.floor(f), t = f - i, a = outline[i], b = outline[Math.min(n - 1, i + 1)];
      return [ox + (a[0] + (b[0] - a[0]) * t - minX) * scale, pad + (a[1] + (b[1] - a[1]) * t - minZ) * scale];
    };
    const line = (pts: [number, number][]) => pts.map((p, i) => `${i ? 'L' : 'M'}${p[0].toFixed(1)},${p[1].toFixed(1)}`).join('');
    const step = L / (n - 1);
    const placed: [number, number][] = [];
    const zones = segments.map((s) => {
      const len = (((s.end - s.start) % L) + L) % L;
      const k = Math.max(2, Math.ceil(len / step) + 1);
      const d = line(Array.from({ length: k + 1 }, (_, i) => at(s.start + (len * i) / k)));
      // Étiquette au point de corde (virage) ou au milieu (ligne droite), poussée vers l'extérieur du circuit.
      const [x, y] = at(s.kind === 'corner' ? s.apex : s.start + len / 2);
      const [x2, y2] = at((s.kind === 'corner' ? s.apex : s.start + len / 2) + step);
      let nx = y - y2, ny = x2 - x;
      const nl = Math.hypot(nx, ny) || 1;
      nx /= nl; ny /= nl;
      if (nx * (x - width / 2) + ny * (y - h / 2) < 0) { nx = -nx; ny = -ny; }
      const lx = Math.min(width - 18, Math.max(18, x + nx * 18)), ly = Math.min(h - 10, Math.max(10, y + ny * 18));
      return { s, d, lx, ly };
    });
    // Étiquettes qui se chevaucheraient (enchaînements serrés) : d'abord les virages les plus lents (Arnage, Mulsanne…),
    // puis les lignes droites les plus rapides ; les autres apparaissent au survol.
    const shown = new Set<string>();
    const rank = (s: TrackSegment) => (s.kind === 'corner' ? s.referenceSpeed : 1000 - s.referenceSpeed);
    for (const z of [...zones].sort((a, b) => rank(a.s) - rank(b.s))) {
      if (placed.some(([px, py]) => Math.abs(px - z.lx) < 32 && Math.abs(py - z.ly) < 18)) continue;
      placed.push([z.lx, z.ly]);
      shown.add(z.s.id);
    }
    return { h, track: line(outline.map((_, i) => at(i * step))) + 'Z', zones, shown, start: at(0) };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [outline, L, width, segKey]);

  const seg = segments.find((s) => s.id === active) ?? null;
  const byId = new Map(snapshot.drivers.map((d) => [d.id, d]));
  const values = seg ? selection.ids.map((id) => ({ id, v: data?.drivers.find((x) => x.id === id)?.[kind][seg.id] })).filter((r): r is { id: string; v: number } => r.v != null).sort((a, b) => b.v - a.v) : [];
  const zoneTitle = (s: TrackSegment) => `${s.id}${s.place ? ` · ${s.place}` : ''} — ${s.kind === 'corner' ? 'virage, vitesse minimale' : 'ligne droite, vitesse maximale'}`;
  const bind = (id: string) => ({ onPointerEnter: () => onHover(id), onPointerLeave: () => onHover(null), onClick: () => onPick(id) });
  const tag = (z: { s: TrackSegment; lx: number; ly: number }, copy: boolean) => (
    <g key={copy ? 'copie' : z.s.id} className={`zone-tag ${z.s.kind} ${active === z.s.id ? 'on' : active ? 'dim' : ''}`} transform={`translate(${z.lx.toFixed(1)},${z.ly.toFixed(1)})`}
      {...(copy ? { pointerEvents: 'none' } : bind(z.s.id))}>
      {!copy && <title>{zoneTitle(z.s)}</title>}
      <rect x={-15} y={-8} width={30} height={16} rx={4} />
      <text x={0} y={3.5} textAnchor="middle">{z.s.id}</text>
    </g>
  );

  return (
    <div className="zone-wrap">
      <div className="zone-grid">
        <div ref={ref}>
          {!drawn ? (
            <div className="chart-empty" style={{ height: 120 }}>La carte des zones apparaît une fois le tracé appris (un tour complet).</div>
          ) : (
            <svg width={width} height={drawn.h} className="zone-svg" role="img" aria-label="Carte des zones : lignes droites et virages">
              <path d={drawn.track} className="zone-track" />
              {drawn.zones.map((z) => (
                <g key={z.s.id} className={`zone ${z.s.kind} ${active === z.s.id ? 'on' : active ? 'dim' : ''}`} {...bind(z.s.id)}>
                  <title>{zoneTitle(z.s)}</title>
                  <path d={z.d} className="zone-hit" />
                  <path d={z.d} className="zone-line" />
                </g>
              ))}
              <rect x={drawn.start[0] - 2} y={drawn.start[1] - 9} width={4} height={18} fill="var(--text-primary)" rx={1} pointerEvents="none" />
              {drawn.zones.filter((z) => drawn.shown.has(z.s.id)).map((z) => tag(z, false))}
              {/* La zone en évidence repasse devant en copie inerte : déplacer l'étiquette sous le doigt ferait perdre le clic. */}
              {drawn.zones.filter((z) => z.s.id === active).map((z) => tag(z, true))}
            </svg>
          )}
        </div>
        <div className="zone-panel">
          {seg ? (
            <>
              <div className="zone-panel-title"><span className={`zone-key ${seg.kind}`} /><b>{seg.id}</b><span>{seg.place ?? seg.name}</span></div>
              <div className="muted small">{seg.kind === 'corner' ? 'Virage : vitesse minimale (Vmin)' : 'Ligne droite : vitesse maximale (Vmax)'} · à {distanceLabel(seg.apex)} de la ligne</div>
              {values.length === 0 ? (
                <div className="muted small">Aucune valeur pour les pilotes comparés : cliquez une ligne du tableau pour en ajouter.</div>
              ) : values.map((r, i) => {
                const d = byId.get(r.id);
                return (
                  <div key={r.id} className="tip-row">
                    <span className="legend-line" style={{ borderColor: selection.colorOf(r.id) }} />
                    <span className="tip-label">{d ? driverLabel(d) : r.id}</span>
                    <b className="tnum">{kph(r.v)}{i === 0 && values.length > 1 ? ' ◆' : ''}</b>
                  </div>
                );
              })}
              {pinned === seg.id && <button className="chip tiny zone-clear" onClick={() => onPick(seg.id)}>Ne plus garder</button>}
            </>
          ) : (
            <div className="muted small">Survolez ou touchez une zone, sur la carte ou dans le tableau, pour voir sa valeur pour les pilotes comparés ({kind === 'best' ? 'meilleure de la session' : 'dernier tour'}).</div>
          )}
          <div className="legend">
            <span className="legend-item"><span className="zone-key straight" />Ligne droite ↑ Vmax</span>
            <span className="legend-item"><span className="zone-key corner" />Virage ↓ Vmin</span>
          </div>
        </div>
      </div>
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
  // Chaque choix de zone recadre, même s'il s'agit de la même (après un déplacement à la main).
  const [focusState, setFocusState] = useState<{ id: string; n: number }>({ id: '__lap', n: 0 });
  const setFocus = (id: string) => setFocusState((f) => ({ id, n: f.n + 1 }));
  const focus = focusState.id;
  const idsKey = selection.ids.join(',');
  // Tour complet à pleine résolution : le zoom se fait ici, sans redemander de données.
  const { data } = usePoll<Trajectories>(idsKey ? `/api/analysis/trajectories?ids=${encodeURIComponent(idsKey)}&kind=${kind}` : null, 5000);
  const corners = segments.filter((s) => s.kind === 'corner');
  const straights = segments.filter((s) => s.kind === 'straight');
  const current = focus === '__lap' || segments.some((s) => s.id === focus) ? focus : '__lap';
  // « Grand écran » : la carte prend toute la fenêtre (Échap pour revenir).
  const [expanded, setExpanded] = useState(false);
  useEffect(() => {
    if (!expanded) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setExpanded(false); };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [expanded]);
  return (
    <div className={`stack-sm ${expanded ? 'traj-expanded' : ''}`}>
      <div className="row" role="group" aria-label="Zoomer sur une zone">
        <button className={`chip ${current === '__lap' ? 'on' : ''}`} onClick={() => setFocus('__lap')} title="Vue entière du circuit">Tour complet</button>
        {corners.map((s) => <button key={s.id} className={`chip ${current === s.id ? 'on' : ''}`} onClick={() => setFocus(s.id)} title={`Zoomer sur le ${s.name.toLowerCase()}`}>{s.id}</button>)}
        {straights.length > 0 && (
          <select className={`select compact ${straights.some((s) => s.id === current) ? 'on' : ''}`} value={straights.some((s) => s.id === current) ? current : ''} aria-label="Zoomer sur une ligne droite"
            onChange={(e) => e.target.value && setFocus(e.target.value)}>
            <option value="">Lignes droites…</option>
            {straights.map((s) => <option key={s.id} value={s.id}>{s.id} · {s.name}{s.description ? ` (${s.description})` : ''}</option>)}
          </select>
        )}
      </div>
      <DriverPicker snapshot={snapshot} selection={selection} />
      <TrajectoryPlot data={data} selection={selection} snapshot={snapshot} segments={segments} focus={current} focusRequest={focusState.n} onFocus={setFocus} expanded={expanded} onExpand={setExpanded} />
    </div>
  );
}

function TrajectoryPlot({ data, selection, snapshot, segments, focus, focusRequest, onFocus, expanded, onExpand }: {
  data: Trajectories | null;
  selection: Selection;
  snapshot: RaceSnapshot;
  segments: TrackSegment[];
  focus: string;
  focusRequest: number;
  onFocus: (id: string) => void;
  expanded: boolean;
  onExpand: (v: boolean) => void;
}) {
  const { ref, width } = useWidth<HTMLDivElement>();
  const viewport = useViewportHeight();
  const [hover, setHover] = useState<{ x: number; y: number } | null>(null);
  const byId = new Map(snapshot.drivers.map((d) => [d.id, d]));
  const idsKey = selection.ids.join(',');
  const drivers = useMemo(() => (data?.drivers ?? []).filter((d) => selection.ids.includes(d.id) && d.points.length > 1)
    // eslint-disable-next-line react-hooks/exhaustive-deps
    .sort((a, b) => selection.ids.indexOf(a.id) - selection.ids.indexOf(b.id)), [data, idsKey]);
  // Le dessin prend toute la hauteur qui reste sous les choix (sans faire défiler la page) ; en grand écran, toute la fenêtre.
  const [top, setTop] = useState(320);
  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) return;
    const scroller = el.closest('.content, .traj-expanded');
    setTop(el.getBoundingClientRect().top + (scroller?.scrollTop ?? 0));
  }, [viewport, width, expanded, ref]);
  const H = Math.round(Math.max(380, Math.min(viewport - top - (expanded ? 56 : 84), width * 1.2, 1600)));
  const lapLength = snapshot.track.length;
  const seg = focus === '__lap' ? null : segments.find((s) => s.id === focus) ?? null;

  // Cadre stable (bornes arrondies à 50 m) : une nouvelle donnée ne fait pas sauter la vue zoomée.
  const bounds = useMemo(() => {
    const pts = drivers.flatMap((d) => d.points);
    if (pts.length < 2) return null;
    let minX = Infinity, maxX = -Infinity, minZ = Infinity, maxZ = -Infinity;
    for (const p of pts) {
      if (p[0] < minX) minX = p[0];
      if (p[0] > maxX) maxX = p[0];
      if (p[1] < minZ) minZ = p[1];
      if (p[1] > maxZ) maxZ = p[1];
    }
    const r = (v: number, up: boolean) => (up ? Math.ceil(v / 50) : Math.floor(v / 50)) * 50;
    return { minX: r(minX, false), maxX: r(maxX, true), minZ: r(minZ, false), maxZ: r(maxZ, true) };
  }, [drivers]);
  const boundsKey = bounds ? `${bounds.minX},${bounds.maxX},${bounds.minZ},${bounds.maxZ}` : '';

  const frame = useMemo(() => {
    if (!bounds) return null;
    const { minX, maxX, minZ, maxZ } = bounds;
    const pad = 16;
    const scale = Math.min((width - pad * 2) / Math.max(1, maxX - minX), (H - pad * 2) / Math.max(1, maxZ - minZ));
    const ox = (width - (maxX - minX) * scale) / 2;
    const oz = (H - (maxZ - minZ) * scale) / 2;
    return { scale, tx: (x: number) => ox + (x - minX) * scale, ty: (z: number) => oz + (z - minZ) * scale, metersPerPx: 1 / scale };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [boundsKey, width, H]);

  const zp = useZoomPan<SVGSVGElement>({ width, height: H, maxK: 80 });
  const v = zp.view;

  const paths = useMemo(() => {
    if (!frame) return [];
    return drivers.map((d) => ({
      id: d.id,
      d: d.points.map((p, i) => `${i ? 'L' : 'M'}${frame.tx(p[0]).toFixed(2)},${frame.ty(p[1]).toFixed(2)}`).join(''),
    }));
  }, [drivers, frame]);

  // Zoom sur la zone choisie (puce ou étiquette de virage) ; « Tour complet » = vue entière.
  // Une fois par choix (et quand la carte change de taille : grand écran, fenêtre) : de nouvelles données
  // ne recadrent pas une vue que l'utilisateur a pu déplacer.
  const fitted = useRef<string | null>(null);
  useEffect(() => {
    if (!frame) return;
    const key = `${focus}#${focusRequest}#${Math.round(width)}x${H}`;
    if (fitted.current === key) return;
    fitted.current = key;
    if (!seg) { zp.reset(); return; }
    let x0 = Infinity, y0 = Infinity, x1 = -Infinity, y1 = -Infinity;
    for (const d of drivers) for (const p of d.points) {
      if (!inZone(seg, p[3], lapLength, 70)) continue;
      const x = frame.tx(p[0]), y = frame.ty(p[1]);
      x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y);
    }
    if (isFinite(x0)) zp.fitBox(x0, y0, x1, y1, 56);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [focus, focusRequest, frame]);

  const toScreen = (bx: number, by: number) => ({ x: bx * v.k + v.x, y: by * v.k + v.y });

  // Point de vitesse minimale (virage) ou maximale (ligne droite) de chaque pilote dans la zone choisie.
  const picks = useMemo(() => {
    if (!seg) return new Map<string, [number, number, number, number]>();
    const m = new Map<string, [number, number, number, number]>();
    for (const d of drivers) {
      const inside = d.points.filter((p) => inZone(seg, p[3], lapLength, 0));
      if (inside.length === 0) continue;
      m.set(d.id, inside.reduce((a, b) => (seg.kind === 'straight' ? (b[2] > a[2] ? b : a) : (b[2] < a[2] ? b : a))));
    }
    return m;
  }, [drivers, seg, lapLength]);

  // Étiquettes des virages (au point de corde du premier pilote comparé) : un clic zoome dessus.
  const reference = drivers[0];
  const cornerTags = useMemo(() => {
    if (!frame || !reference) return [];
    return segments.filter((s) => s.kind === 'corner').map((s) => {
      let best = reference.points[0], bd = Infinity;
      for (const p of reference.points) {
        const dd = Math.abs(p[3] - s.apex);
        const wrapped = lapLength > 0 ? Math.min(dd, lapLength - dd) : dd;
        if (wrapped < bd) { bd = wrapped; best = p; }
      }
      return { seg: s, bx: frame.tx(best[0]), by: frame.ty(best[1]), ok: bd < 40 };
    }).filter((t) => t.ok);
  }, [frame, reference, segments, lapLength]);

  // Au survol : la vitesse de chaque pilote au point le plus proche.
  const readout = useMemo(() => {
    if (!hover || !frame || zp.dragging) return null;
    const bx = (hover.x - v.x) / v.k, by = (hover.y - v.y) / v.k;
    const limit = 36 / v.k;
    return drivers.map((d) => {
      let best = d.points[0], bd = Infinity;
      for (const p of d.points) { const dd = (frame.tx(p[0]) - bx) ** 2 + (frame.ty(p[1]) - by) ** 2; if (dd < bd) { bd = dd; best = p; } }
      return { d, p: best, close: Math.sqrt(bd) < limit };
    }).filter((r) => r.close);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hover, frame, data, v.k, v.x, v.y, zp.dragging]);

  // Échelle : distance ronde à la taille actuelle.
  const mpp = frame ? frame.metersPerPx / v.k : 1;
  const scaleMeters = niceScale(mpp, 90);
  const start = reference && frame ? toScreen(frame.tx(reference.points[0][0]), frame.ty(reference.points[0][1])) : null;

  return (
    <div className="stack-sm">
      <div ref={ref} className="chart-area traj-area">
        {!frame ? (
          <div className="chart-empty" style={{ height: 200 }}>Les trajectoires apparaissent après un tour complet des pilotes sélectionnés.</div>
        ) : (
          <>
            <svg
              ref={zp.ref}
              width={width}
              height={H}
              className={`traj ${zp.isZoomed ? 'zoomed' : ''} ${zp.dragging ? 'dragging' : ''}`}
              style={{ touchAction: zp.isZoomed ? 'none' : 'pan-y' }}
              role="img"
              aria-label="Trajectoires des pilotes (molette ou pincement pour zoomer, glisser pour se déplacer)"
              {...zp.bind}
              onPointerMove={(e) => {
                zp.bind.onPointerMove(e);
                const r = e.currentTarget.getBoundingClientRect();
                setHover({ x: e.clientX - r.left, y: e.clientY - r.top });
              }}
              onPointerLeave={() => setHover(null)}
            >
              <rect width={width} height={H} fill="transparent" />
              <g transform={`translate(${v.x.toFixed(2)},${v.y.toFixed(2)}) scale(${v.k.toFixed(4)})`}>
                {/* Largeur de piste approximative (12 m) : à l'échelle, et au moins quelques pixels quand on voit tout le tour. */}
                {paths.map((p) => (
                  <path key={`bg${p.id}`} d={p.d} fill="none" stroke="var(--track)" strokeWidth={Math.max(6 / v.k, 12 * frame.scale)} strokeLinecap="round" strokeLinejoin="round" />
                ))}
                {paths.map((p) => (
                  <path key={p.id} d={p.d} fill="none" stroke={selection.colorOf(p.id)} strokeWidth={2} vectorEffect="non-scaling-stroke" strokeLinecap="round" strokeLinejoin="round" />
                ))}
              </g>
              {start && (
                <g className="axis-text" pointerEvents="none">
                  <rect x={start.x - 2} y={start.y - 9} width={4} height={18} fill="var(--text-primary)" rx={1} />
                </g>
              )}
              {cornerTags.map((t) => {
                const s = toScreen(t.bx, t.by);
                const on = focus === t.seg.id;
                if (s.x < -20 || s.y < -20 || s.x > width + 20 || s.y > H + 20) return null;
                const ty = s.y - 16 < 12 ? s.y + 18 : s.y - 16; // tout en haut : étiquette sous le virage
                return (
                  <g key={t.seg.id} className={`corner-tag ${on ? 'on' : ''}`} transform={`translate(${s.x.toFixed(1)},${ty.toFixed(1)})`}
                    onClick={() => { if (!zp.justDragged()) onFocus(on ? '__lap' : t.seg.id); }}>
                    <title>{on ? 'Revenir au tour complet' : `Zoomer sur le ${t.seg.name.toLowerCase()} (Vmin de référence ${kph(t.seg.referenceSpeed)})`}</title>
                    <rect x={-15} y={-10} width={30} height={18} rx={4} />
                    <text x={0} y={3} textAnchor="middle">{t.seg.id}</text>
                  </g>
                );
              })}
              {[...picks.entries()].map(([id, p]) => {
                const s = toScreen(frame.tx(p[0]), frame.ty(p[1]));
                return <circle key={`m${id}`} cx={s.x} cy={s.y} r={5} fill={selection.colorOf(id)} stroke="var(--surface-1)" strokeWidth={2} pointerEvents="none" />;
              })}
              {readout?.map((r) => {
                const s = toScreen(frame.tx(r.p[0]), frame.ty(r.p[1]));
                return <circle key={`h${r.d.id}`} cx={s.x} cy={s.y} r={4} fill="none" stroke={selection.colorOf(r.d.id)} strokeWidth={2} pointerEvents="none" />;
              })}
              {/* Échelle en haut à gauche (en bas à gauche, le bouton de la radio la cache). */}
              <g className="axis-text" pointerEvents="none">
                <line x1={12} x2={12 + scaleMeters / mpp} y1={24} y2={24} stroke="var(--text-muted)" strokeWidth={2} />
                <text x={12} y={18}>{scaleMeters >= 1000 ? `${scaleMeters / 1000} km` : `${scaleMeters} m`}</text>
              </g>
            </svg>
            <div className="zoom-ctl" role="group" aria-label="Zoom">
              <button className="btn" onClick={() => zp.zoomBy(1.6)} title="Zoomer" aria-label="Zoomer">+</button>
              <button className="btn" onClick={() => zp.zoomBy(1 / 1.6)} title="Dézoomer" aria-label="Dézoomer" disabled={!zp.isZoomed}>−</button>
              <button className="btn" onClick={() => { onFocus('__lap'); zp.reset(); }} title="Vue entière (double-clic sur la carte)" disabled={!zp.isZoomed && focus === '__lap'}>Vue entière</button>
              <button className="btn" onClick={() => onExpand(!expanded)} title={expanded ? 'Revenir à la page (Échap)' : 'La carte prend toute la fenêtre'} aria-pressed={expanded}>{expanded ? 'Réduire' : 'Grand écran'}</button>
              {zp.isZoomed && <span className="zoom-level tnum">×{v.k < 10 ? v.k.toFixed(1) : Math.round(v.k)}</span>}
            </div>
            <div className="zoom-hint muted">Molette ou pincement : zoom · glisser : se déplacer · double-clic : vue entière · clic sur un virage : zoom dessus</div>
            {readout && readout.length > 0 && hover && (
              <div className="tooltip chart-tip" style={hover.x > width * 0.6 ? { right: width - hover.x + 12, top: hover.y } : { left: hover.x + 12, top: hover.y }}>
                <div className="secondary">{Math.round(readout[0].p[3])} m depuis la ligne</div>
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
      </div>
      {frame && (
        <div className="legend chart-legend">
          {drivers.map((d) => {
            const dr = byId.get(d.id);
            const pick = picks.get(d.id);
            return (
              <span key={d.id} className="legend-item">
                <span className="legend-line" style={{ borderColor: selection.colorOf(d.id) }} />
                {dr ? driverLabel(dr) : d.id} · T{d.lap}{d.time ? ` ${lapTime(d.time)}` : ''}
                {seg && pick && <b className="tnum">&nbsp;{seg.id} {seg.kind === 'straight' ? 'Vmax' : 'Vmin'} {kph(pick[2])}</b>}
                {d.sampleHz > 0 && d.sampleHz < 3 && <span className="muted" title="Données peu fréquentes : trajectoire lissée">&nbsp;≈</span>}
              </span>
            );
          })}
        </div>
      )}
    </div>
  );
}
