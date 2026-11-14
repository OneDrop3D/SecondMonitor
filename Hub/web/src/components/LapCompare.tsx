import { useMemo, useState } from 'react';
import { usePoll } from '../live';
import { delta, kph, lapTime } from '../format';
import type { CarControls, LapComparison, LapDetail, RaceSnapshot, ShiftPoint, TrackSegment } from '../types';
import { LineChart, type Series } from '../charts/LineChart';
import { useStoredState } from '../prefs';
import { distanceLabel, zoneAt } from '../track';
import { TyreBadge } from './Common';

const COLORS = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-7)'];
const MAX_LAPS = 4;

interface Prefs {
  /** Tours comparés : « best », « last », « current » ou un numéro ; le premier sert de référence. */
  laps: string[];
  /** Zone affichée dans les graphiques (id d'un virage ou d'une ligne droite, « __lap » = tout le tour). */
  zone: string;
  shifts: 'all' | 'up' | 'down';
}

const DEFAULTS: Prefs = { laps: ['best', 'last'], zone: '__lap', shifts: 'all' };

const CONTROL_ORDER = ['bias', 'tc', 'tcCut', 'tcSlip', 'abs', 'map', 'migration', 'arbFront', 'arbRear'] as const;
const CONTROL_LABEL: Record<string, string> = {
  bias: 'Répartition de freinage (avant)',
  tc: 'Antipatinage (TC)',
  tcCut: 'TC coupure',
  tcSlip: 'TC glissement',
  abs: 'ABS',
  map: 'Cartographie moteur',
  migration: 'Migration de freinage',
  arbFront: 'Barre antiroulis avant',
  arbRear: 'Barre antiroulis arrière',
};
const CONTROL_MAX: Record<string, keyof CarControls> = {
  tc: 'tcMax', tcCut: 'tcCutMax', tcSlip: 'tcSlipMax', abs: 'absMax', map: 'motorMapMax', migration: 'migrationMax', arbFront: 'frontAntiRollMax', arbRear: 'rearAntiRollMax',
};

const signed = (v: number, digits = 0) => `${v > 0 ? '+' : v < 0 ? '−' : '±'}${Math.abs(v).toFixed(digits)}`;

/**
 * Comparaison de vos tours (essais surtout) : vitesse, écart cumulé, rapport engagé, pédales et régime le long du tour,
 * points de passage des rapports et réglages en roulant appliqués pendant chaque tour.
 */
export function LapCompare({ snapshot, segments }: { snapshot: RaceSnapshot; segments: TrackSegment[] }) {
  const [prefs, setPrefs] = useStoredState<Prefs>('smhub-mes-tours', DEFAULTS);
  const tokens = (prefs.laps.length > 0 ? prefs.laps : DEFAULTS.laps).slice(0, MAX_LAPS);
  const live = tokens.includes('current');
  const { data } = usePoll<LapComparison>(`/api/analysis/lapcompare?laps=${encodeURIComponent(tokens.join(','))}`, live ? 2000 : 5000);
  const [hoverX, setHoverX] = useState<number | null>(null);

  const laps = data?.laps ?? [];
  const traces = data?.traces ?? [];
  const step = data?.step ?? 10;
  const lapLength = data?.lapLength || snapshot.track.length;
  const ref = traces[0] ?? null;
  const bestLap = laps.find((l) => l.best)?.lap;
  const lastLap = [...laps].reverse().find((l) => l.complete)?.lap;
  const currentLap = laps.find((l) => l.current)?.lap;
  const resolve = (t: string) => (t === 'best' ? bestLap : t === 'last' ? lastLap : t === 'current' ? currentLap : Number(t));
  const colorOf = (lap: number) => COLORS[Math.max(0, traces.findIndex((t) => t.lap === lap)) % COLORS.length];

  const toggleLap = (lap: number) => {
    if (tokens.some((t) => resolve(t) === lap)) {
      const next = tokens.filter((t) => resolve(t) !== lap);
      setPrefs({ laps: next.length > 0 ? next : DEFAULTS.laps });
    } else {
      setPrefs({ laps: tokens.length >= MAX_LAPS ? [...tokens.slice(0, MAX_LAPS - 1), String(lap)] : [...tokens, String(lap)] });
    }
  };
  const makeReference = (lap: number) => {
    const i = tokens.findIndex((t) => resolve(t) === lap);
    if (i > 0) setPrefs({ laps: [tokens[i], ...tokens.filter((_, k) => k !== i)] });
  };
  const lastComplete = laps.filter((l) => l.complete).map((l) => l.lap).slice(-3).reverse();

  const zone = segments.find((s) => s.id === prefs.zone) ?? null;
  const xDomain: [number, number] | undefined = zone && zone.start < zone.end ? [Math.max(0, zone.start - 150), Math.min(lapLength, zone.end + 150)] : undefined;

  const label = (t: LapDetail) => `T${t.lap}${t.lap === bestLap ? ' ★' : ''}${t.current ? ' (en cours)' : ''}${t.time ? ` ${lapTime(t.time)}` : ''}`;
  const x = (i: number) => (i + 0.5) * step;

  const charts = useMemo(() => {
    const speed: Series[] = traces.map((t, i) => ({ id: `v${t.lap}`, label: label(t), color: COLORS[i], width: i === 0 ? 2 : 1.6, points: t.speed.map((v, j) => ({ x: x(j), y: v })) }));
    const gaps: Series[] = ref ? traces.slice(1).map((t, i) => ({
      id: `d${t.lap}`,
      label: `${label(t)} vs T${ref.lap}`,
      color: COLORS[i + 1],
      points: t.elapsed.map((e, j) => ({ x: x(j), y: e != null && ref.elapsed[j] != null ? e - ref.elapsed[j]! : null })),
    })) : [];
    const gears: Series[] = traces.map((t, i) => ({
      id: `g${t.lap}`, label: label(t), color: COLORS[i], width: i === 0 ? 2 : 1.6, step: true,
      points: t.gear.map((g, j) => ({ x: x(j), y: g != null && g >= 1 ? g : null })),
      markers: t.shifts.map((s) => ({ x: s.distance, y: s.to, title: `T${t.lap} : ${s.from} → ${s.to} à ${distanceLabel(s.distance)}, ${kph(s.speed)}, ${s.to > s.from ? s.rpmBefore : s.rpmAfter} tr/min` })),
    }));
    const pedals: Series[] = traces.flatMap((t, i) => [
      { id: `t${t.lap}`, label: `${label(t)} accélérateur`, color: COLORS[i], width: 1.6, points: t.throttle.map((v, j) => ({ x: x(j), y: v })) },
      { id: `b${t.lap}`, label: `${label(t)} frein`, color: COLORS[i], width: 1.6, dashed: true, points: t.brake.map((v, j) => ({ x: x(j), y: v })) },
    ]);
    const rpm: Series[] = traces.map((t, i) => ({ id: `r${t.lap}`, label: label(t), color: COLORS[i], width: 1.4, points: t.rpm.map((v, j) => ({ x: x(j), y: v && v > 0 ? v : null })) }));
    return { speed, gaps, gears, pedals, rpm, hasGaps: gaps.some((s) => s.points.some((p) => p.y != null)), hasGears: gears.some((s) => s.points.some((p) => p.y != null)), hasRpm: rpm.some((s) => s.points.some((p) => p.y != null)) };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [data, bestLap]);

  // Écart cumulé : échelle sur 94 % des points, pour qu'un arrêt ou une sortie de piste n'écrase pas tout le reste.
  const gapScale = useMemo(() => {
    const vals = charts.gaps.flatMap((s) => s.points.filter((p) => p.y != null && (!xDomain || (p.x >= xDomain[0] && p.x <= xDomain[1]))).map((p) => p.y as number)).sort((a, b) => a - b);
    if (vals.length < 20) return null;
    const q = (f: number) => vals[Math.round(f * (vals.length - 1))];
    const lo = Math.min(0, q(0.03)), hi = Math.max(0, q(0.97));
    const span = Math.max(0.5, hi - lo);
    const min = Math.min(0, vals[0]), max = Math.max(0, vals[vals.length - 1]);
    if (max - min <= span * 1.6) return null;
    return { domain: [lo - span * 0.12, hi + span * 0.12] as [number, number], min, max };
  }, [charts, xDomain]);
  const maxGear = Math.max(1, ...traces.flatMap((t) => t.gear.filter((g): g is number => g != null)));

  const corners = segments.filter((s) => s.kind === 'corner');
  const straights = segments.filter((s) => s.kind === 'straight' && s.start < s.end);
  const refLines = corners.map((s) => ({ x: s.apex, label: s.id }));
  const shared = { hoverX, onHoverX: setHoverX, xDomain, xFormat: (v: number) => distanceLabel(v), xTitle: (v: number) => `${Math.round(v)} m depuis la ligne${zoneAt(segments, v, lapLength) ? ` · ${zoneAt(segments, v, lapLength)!.id}` : ''}` };

  const hz = snapshot.dataRate.playerHz;
  const precision = hz > 0 ? Math.round(250 / 3.6 / hz / 2) : null;

  if (data && laps.length === 0) {
    return (
      <section className="card">
        <div className="card-head"><span className="card-title">Mes tours comparés</span></div>
        <div className="card-body muted">Vos tours apparaissent ici dès le premier tour complet (hors stands) : vitesses, rapports, pédales et réglages de chacun.</div>
      </section>
    );
  }

  return (
    <div className="stack">
      <section className="card">
        <div className="card-head">
          <span className="card-title">Mes tours comparés</span>
          <span className="muted hide-xs">jusqu'à {MAX_LAPS} tours · le premier sert de référence</span>
          <span className="spacer" />
          <span className="row" role="group" aria-label="Choix rapides">
            <button className="chip" onClick={() => setPrefs({ laps: ['best', 'last'] })}>Meilleur + dernier</button>
            <button className="chip" onClick={() => setPrefs({ laps: ['best', 'current'] })}>Meilleur + en cours</button>
            {lastComplete.length >= 2 && <button className="chip" onClick={() => setPrefs({ laps: lastComplete.map(String) })}>{lastComplete.length} derniers tours</button>}
          </span>
        </div>
        <div className="card-body stack-sm">
          <div className="lap-picker" role="group" aria-label="Tours comparés">
            {[...laps].reverse().map((l) => {
              const on = traces.some((t) => t.lap === l.lap);
              const flags = [l.best && 'meilleur', !l.valid && 'invalide', l.pit && 'stand', l.outLap && 'sortie', l.current && 'en cours', !l.complete && !l.current && 'incomplet'].filter(Boolean).join(' · ');
              return (
                <button key={l.lap} className={`chip lap-chip ${on ? 'on' : ''} ${!l.valid || l.pit || l.outLap ? 'dim' : ''}`} onClick={() => toggleLap(l.lap)} aria-pressed={on}
                  title={`Tour ${l.lap}${flags ? ` (${flags})` : ''}${on ? ' — cliquer pour le retirer' : ' — cliquer pour le comparer'}`}>
                  {on && <span className="legend-line" style={{ borderColor: colorOf(l.lap) }} />}
                  <b>T{l.lap}</b>
                  <span className="tnum">{l.current ? 'en cours' : lapTime(l.time)}</span>
                  {l.best && <span className="best-overall" aria-label="meilleur tour">★</span>}
                  {(!l.valid || l.pit || l.outLap) && <span className="muted small">{!l.valid ? 'inv.' : l.pit ? 'stand' : 'sortie'}</span>}
                  {l.compound && <span className="lap-chip-tyre"><TyreBadge compound={l.compound} /></span>}
                </button>
              );
            })}
          </div>
          {traces.length > 0 && (
            <div className="table-scroll">
              <table className="mini-table cmp-table">
                <thead>
                  <tr>
                    <th>Tour</th>
                    <th className="r">Temps</th>
                    <th className="r" title="Écart au tour de référence">Écart</th>
                    <th className="r">Vmax</th>
                    <th className="r hide-sm">Régime max</th>
                    <th className="r" title="Montées de rapport (régime moyen au passage)">Montées</th>
                    <th className="r" title="Descentes de rapport">Descentes</th>
                    <th>Pneus</th>
                    <th className="r hide-sm">Essence</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {traces.map((t, i) => {
                    const ups = t.shifts.filter((s) => s.to > s.from);
                    const downs = t.shifts.filter((s) => s.to < s.from);
                    const upRpm = ups.length ? Math.round(ups.reduce((a, s) => a + s.rpmBefore, 0) / ups.length) : null;
                    return (
                      <tr key={t.lap}>
                        <td className="nowrap"><span className="legend-line" style={{ borderColor: COLORS[i] }} /> <b>{label(t).split(' ')[0]}</b>{t.lap === bestLap ? <span className="best-overall"> ★</span> : null}{i === 0 && <span className="pill small-pill">référence</span>}{t.current && <span className="pill small-pill">en cours</span>}{!t.valid && <span className="pill small-pill">invalide</span>}</td>
                        <td className="r tnum">{t.current ? '—' : lapTime(t.time)}</td>
                        <td className="r tnum">{i > 0 && t.time && ref?.time ? `${delta(t.time - ref.time, 3)} s` : '—'}</td>
                        <td className="r tnum">{kph(t.topSpeed)}</td>
                        <td className="r tnum hide-sm">{t.maxRpm ? `${t.maxRpm} tr/min` : '—'}</td>
                        <td className="r tnum">{ups.length}{upRpm ? <span className="muted"> · {upRpm} tr/min</span> : ''}</td>
                        <td className="r tnum">{downs.length}</td>
                        <td className="nowrap">{t.compound ? <TyreBadge compound={t.compound} laps={t.tyreLaps ?? undefined} /> : '—'}</td>
                        <td className="r tnum hide-sm">{t.fuel ? `${t.fuel.toFixed(2)} L` : '—'}</td>
                        <td className="r">{i > 0 && <button className="chip tiny" onClick={() => makeReference(t.lap)} title="Comparer les autres tours à celui-ci">Référence</button>}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>

      <section className="card">
        <div className="card-head">
          <span className="card-title">Le long du tour</span>
          <span className="muted hide-xs">survolez un graphique : le même point s'affiche sur tous</span>
        </div>
        <div className="card-body stack-sm">
          <div className="row" role="group" aria-label="Zone affichée">
            <button className={`chip ${!zone ? 'on' : ''}`} onClick={() => setPrefs({ zone: '__lap' })}>Tour complet</button>
            {corners.filter((s) => s.start < s.end).map((s) => (
              <button key={s.id} className={`chip ${zone?.id === s.id ? 'on' : ''}`} onClick={() => setPrefs({ zone: s.id })} title={s.name}>{s.id}</button>
            ))}
            {straights.length > 0 && (
              <select className={`select compact ${zone?.kind === 'straight' ? 'on' : ''}`} value={zone?.kind === 'straight' ? zone.id : ''} aria-label="Ligne droite affichée"
                onChange={(e) => e.target.value && setPrefs({ zone: e.target.value })}>
                <option value="">Lignes droites…</option>
                {straights.map((s) => <option key={s.id} value={s.id}>{s.id} · {s.name}{s.description ? ` (${s.description})` : ''}</option>)}
              </select>
            )}
          </div>
          <div className="chart-title">Vitesse</div>
          <LineChart series={charts.speed} height={260} {...shared} refLines={refLines} yFormat={(y) => `${Math.round(y)}`} valueFormat={(y) => kph(y)} ariaLabel="Vitesse de chaque tour le long du tour" emptyText="Choisissez au moins un tour complet." />
          {traces.length >= 2 && (
            <>
              <div className="chart-title">Écart cumulé par rapport à T{ref?.lap} <span className="muted small">(au-dessus de 0 : plus lent à cet endroit)</span></div>
              {charts.hasGaps ? (
                <>
                  <LineChart series={charts.gaps} height={180} {...shared} legend={false} yDomain={gapScale?.domain} refLines={gapScale ? undefined : [{ y: 0 }]} yFormat={(y) => y.toFixed(1)} valueFormat={(y) => `${delta(y, 3)} s`} ariaLabel="Écart de temps cumulé le long du tour" />
                  {gapScale && <div className="chart-note muted">Échelle resserrée : l'écart va de {delta(gapScale.min, 1)} à {delta(gapScale.max, 1)} s sur la zone (arrêt, sortie de piste ou passage au stand) ; la courbe est coupée au bord.</div>}
                </>
              ) : (
                <div className="chart-empty" style={{ height: 80 }}>Écart indisponible : le passage sur la ligne d'un de ces tours n'a pas été vu (sortie des stands, données coupées).</div>
              )}
            </>
          )}
          {charts.hasGears && (
            <>
              <div className="chart-title">Rapport engagé <span className="muted small">(points : passages de rapports)</span></div>
              <LineChart series={charts.gears} height={Math.max(150, 26 * maxGear)} {...shared} legend={false} yTicks={Array.from({ length: maxGear }, (_, i) => i + 1)} yFormat={(y) => String(y)} valueFormat={(y) => `${y}e`} ariaLabel="Rapport engagé le long du tour" />
            </>
          )}
          <div className="chart-title">Accélérateur et frein <span className="muted small">(trait plein : accélérateur · pointillés : frein)</span></div>
          <LineChart series={charts.pedals} height={170} {...shared} legend={false} dashedLabel={null} yDomain={[0, 100]} yFormat={(y) => `${Math.round(y)}`} valueFormat={(y) => `${Math.round(y)} %`} ariaLabel="Accélérateur et frein le long du tour" />
          {charts.hasRpm && (
            <>
              <div className="chart-title">Régime moteur</div>
              <LineChart series={charts.rpm} height={170} {...shared} legend={false} yFormat={(y) => `${Math.round(y / 100) / 10}k`} valueFormat={(y) => `${Math.round(y)} tr/min`} ariaLabel="Régime moteur le long du tour" />
            </>
          )}
        </div>
      </section>

      <ShiftTable traces={traces} segments={segments} lapLength={lapLength} filter={prefs.shifts} setFilter={(f) => setPrefs({ shifts: f })} xDomain={xDomain} precision={precision} hz={hz} />
      <ControlsTable traces={traces} controls={snapshot.player?.controls ?? null} />
    </div>
  );
}

interface ShiftRow { key: string; from: number; to: number; at: number; cells: (ShiftPoint | null)[] }

/**
 * Aligne les passages de rapports des tours comparés sur ceux du tour de référence :
 * même passage (ex. 3 → 4), le plus proche à moins de 250 m. Ceux qui n'ont pas d'équivalent ont leur propre ligne.
 */
function matchShifts(traces: LapDetail[], lapLength: number): ShiftRow[] {
  if (traces.length === 0) return [];
  const circ = (a: number, b: number) => { const d = Math.abs(a - b); return lapLength > 0 ? Math.min(d, lapLength - d) : d; };
  const used = traces.map(() => new Set<number>());
  const rows: ShiftRow[] = traces[0].shifts.map((s, i) => ({
    key: `r${i}`,
    from: s.from,
    to: s.to,
    at: s.distance,
    cells: traces.map((t, ti) => {
      if (ti === 0) return s;
      let best = -1, bd = Infinity;
      t.shifts.forEach((o, oi) => {
        if (used[ti].has(oi) || o.from !== s.from || o.to !== s.to) return;
        const d = circ(o.distance, s.distance);
        if (d < bd) { bd = d; best = oi; }
      });
      if (best < 0 || bd > 250) return null;
      used[ti].add(best);
      return t.shifts[best];
    }),
  }));
  traces.forEach((t, ti) => {
    if (ti === 0) return;
    t.shifts.forEach((o, oi) => {
      if (!used[ti].has(oi)) rows.push({ key: `x${ti}-${oi}`, from: o.from, to: o.to, at: o.distance, cells: traces.map((_, k) => (k === ti ? o : null)) });
    });
  });
  return rows.sort((a, b) => a.at - b.at);
}

function ShiftTable({ traces, segments, lapLength, filter, setFilter, xDomain, precision, hz }: {
  traces: LapDetail[];
  segments: TrackSegment[];
  lapLength: number;
  filter: 'all' | 'up' | 'down';
  setFilter: (f: 'all' | 'up' | 'down') => void;
  xDomain?: [number, number];
  precision: number | null;
  hz: number;
}) {
  const rows = useMemo(() => matchShifts(traces, lapLength), [traces, lapLength])
    .filter((r) => (filter === 'all' || (filter === 'up' ? r.to > r.from : r.to < r.from)) && (!xDomain || (r.at >= xDomain[0] && r.at <= xDomain[1])));
  const anyShift = traces.some((t) => t.shifts.length > 0);
  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Passages de rapports</span>
        <span className="muted hide-xs">où, à quelle vitesse et à quel régime · écart avec la référence</span>
        <span className="spacer" />
        <div className="seg" role="group" aria-label="Passages affichés">
          <button className={filter === 'all' ? 'on' : ''} onClick={() => setFilter('all')}>Tous</button>
          <button className={filter === 'up' ? 'on' : ''} onClick={() => setFilter('up')}>Montées</button>
          <button className={filter === 'down' ? 'on' : ''} onClick={() => setFilter('down')}>Descentes</button>
        </div>
      </div>
      {!anyShift ? (
        <div className="card-body muted">{traces.length === 0 ? 'Choisissez au moins un tour.' : 'Aucun passage de rapport enregistré pour ces tours (le jeu ne donne pas le rapport engagé, ou tours trop courts).'}</div>
      ) : (
        <div className="table-scroll shift-scroll">
          <table className="mini-table shift-table">
            <thead>
              <tr>
                <th>Passage</th>
                <th>Lieu</th>
                {traces.map((t, i) => <th key={t.lap}><span className="legend-line" style={{ borderColor: COLORS[i] }} /> T{t.lap}{i === 0 ? ' (réf.)' : ''}</th>)}
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => {
                const up = r.to > r.from;
                const refCell = r.cells[0];
                const place = zoneAt(segments, r.at, lapLength);
                return (
                  <tr key={r.key}>
                    <td className="nowrap"><span className={`shift-dir ${up ? 'up' : 'down'}`} aria-hidden>{up ? '▲' : '▼'}</span> {r.from} → {r.to}</td>
                    <td className="nowrap muted" title={place?.name}>{place?.id ?? '—'}</td>
                    {r.cells.map((c, i) => (
                      <td key={i} className="tnum shift-cell">
                        {c ? (
                          <>
                            <div><b>{distanceLabel(c.distance)}</b> · {kph(c.speed)} · {up ? c.rpmBefore : c.rpmAfter} tr/min</div>
                            {i > 0 && refCell && (
                              <div className="small muted" title="Écart avec la référence : + = plus loin sur le tour / plus vite / plus haut en régime">
                                {signed(c.distance - refCell.distance)} m · {signed(c.speed - refCell.speed)} km/h · {signed((up ? c.rpmBefore : c.rpmAfter) - (up ? refCell.rpmBefore : refCell.rpmAfter))} tr/min
                              </div>
                            )}
                          </>
                        ) : <span className="muted">{i === 0 ? 'pas sur la référence' : '—'}</span>}
                      </td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
      <div className="card-foot muted">
        Montée : régime juste avant le passage · descente : régime juste après.
        {precision != null && ` Voiture reçue ${hz.toFixed(hz < 10 ? 1 : 0)} fois par seconde : passages situés à ±${precision} m près à 250 km/h.`}
      </div>
    </section>
  );
}

function ControlsTable({ traces, controls }: { traces: LapDetail[]; controls: CarControls | null }) {
  const keys = CONTROL_ORDER.filter((k) => traces.some((t) => t.controls[k] != null || t.controlChanges.some((c) => c.key === k)));
  const fmt = (key: string, v: number) => {
    if (key === 'bias') return `${v.toFixed(1)} %`;
    const max = CONTROL_MAX[key] && controls ? (controls[CONTROL_MAX[key]] as number | null) : null;
    return max ? `${v} / ${max}` : String(v);
  };
  const ref = traces[0];
  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Réglages en roulant</span>
        <span className="muted hide-xs">au départ de chaque tour, et ceux changés pendant le tour</span>
      </div>
      {traces.length === 0 ? (
        <div className="card-body muted">Choisissez au moins un tour.</div>
      ) : keys.length === 0 ? (
        <div className="card-body muted">Le jeu n'envoie pas les réglages en roulant (répartition de freinage, TC, ABS, cartographie) : rien à comparer.</div>
      ) : (
        <div className="table-scroll">
          <table className="mini-table controls-table">
            <thead>
              <tr>
                <th>Réglage</th>
                {traces.map((t, i) => <th key={t.lap}><span className="legend-line" style={{ borderColor: COLORS[i] }} /> T{t.lap}{i === 0 ? ' (réf.)' : ''}</th>)}
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k}>
                  <td className="nowrap">{CONTROL_LABEL[k]}</td>
                  {traces.map((t, i) => {
                    const start = t.controls[k];
                    const changes = t.controlChanges.filter((c) => c.key === k);
                    const differs = i > 0 && ref && (start !== ref.controls[k] || changes.length > 0 || ref.controlChanges.some((c) => c.key === k));
                    return (
                      <td key={t.lap} className={`tnum ${differs ? 'diff' : ''} ${i === 0 && changes.length ? 'diff' : ''}`}>
                        {start != null ? fmt(k, start) : '—'}
                        {changes.map((c, n) => (
                          <div key={n} className="small">→ {fmt(k, c.to)} <span className="muted">à {distanceLabel(c.distance)}</span></div>
                        ))}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
