import { useEffect, useMemo, useState } from 'react';
import { api, usePoll } from '../live';
import { delta, kph, lapTime, WHEELS, WHEEL_SHORT } from '../format';
import { LineChart, type Series, type XZoom } from '../charts/LineChart';
import { pedalSeries } from '../charts/pedals';
import { distanceLabel } from '../track';
import { readPref, writePref } from '../prefs';
import type { LapDetail } from '../types';
import { BilanCarnet, dateTime, sessionName, type CarnetSummary } from './CarnetView';
import { TyreBadge } from './Common';

const COLORS = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-7)', 'var(--series-4)', 'var(--series-5)'];
const MAX_COMPARE = 6;

type Mode = 'compare' | 'average' | 'stints';

interface BilanTour { lap: number; time?: number | null; pit: boolean; valid: boolean; fuel?: number | null; energy?: number | null; wearDelta?: number[] | null }
interface Stint { relais: number; gomme?: string | null; premier: number; dernier: number }
interface Bilan { tours: BilanTour[]; exclus: number[]; relais: Stint[] }

/** Une courbe affichée : un tour, la moyenne de la sélection ou la moyenne d'un relais. */
interface Group { id: string; label: string; laps: number[]; trace: Trace }
interface Trace { speed: (number | null)[]; throttle: (number | null)[]; brake: (number | null)[]; gear: (number | null)[]; rpm: (number | null)[]; elapsed: (number | null)[] }

/** Moyenne point par point (tous les 10 m) de plusieurs tours ; un point manquant d'un tour est ignoré. */
export function averageTraces(laps: Trace[]): Trace {
  const avg = (pick: (t: Trace) => (number | null)[]) => {
    const n = Math.max(0, ...laps.map((t) => pick(t).length));
    return Array.from({ length: n }, (_, j) => {
      let sum = 0, k = 0;
      for (const t of laps) { const v = pick(t)[j]; if (v != null) { sum += v; k++; } }
      return k ? sum / k : null;
    });
  };
  return { speed: avg((t) => t.speed), throttle: avg((t) => t.throttle), brake: avg((t) => t.brake), gear: avg((t) => t.gear), rpm: avg((t) => t.rpm), elapsed: avg((t) => t.elapsed) };
}

const mean = (vals: (number | null | undefined)[]) => {
  const v = vals.filter((x): x is number => x != null && isFinite(x));
  return v.length ? v.reduce((a, b) => a + b, 0) / v.length : null;
};

/**
 * Analyse après la course (marche sans PC de jeu) : choix d'une course enregistrée, tours et relais à comparer ou à moyenner,
 * vitesse, écart, accélérateur (vert) et frein (rouge), rapports et régime le long du tour, puis le bilan essence / énergie / pneus.
 */
export function DebriefView() {
  const { data: list } = usePoll<CarnetSummary[]>('/api/carnets', 15000);
  const [selected, setSelected] = useState<string | null>(() => readPref('smhub-analyse', '') || null);
  const id = selected && list?.some((s) => s.id === selected) ? selected : list?.[0]?.id ?? null;
  useEffect(() => { if (id) writePref('smhub-analyse', id); }, [id]);
  const live = list?.find((s) => s.id === id)?.current ?? false;

  return (
    <div className="stack">
      <section className="card">
        <div className="card-head">
          <span className="card-title">Analyse de course</span>
          <span className="muted hide-xs">vos courses enregistrées, même sans le PC de jeu</span>
          <span className="spacer" />
          {list && list.length > 0 && (
            <select className="select" value={id ?? ''} onChange={(e) => setSelected(e.target.value)} aria-label="Course analysée">
              {list.map((s) => (
                <option key={s.id} value={s.id}>
                  {sessionName(s.sessionType)} · {s.track ?? 'circuit inconnu'} · {dateTime(s.startedAt)}{s.current ? ' (en cours)' : ''}{s.result ? ` · ${s.result}` : ''}
                </option>
              ))}
            </select>
          )}
        </div>
        {list?.length === 0 && <div className="card-body muted">Aucune course enregistrée pour l'instant : chaque session est gardée automatiquement pendant que vous roulez.</div>}
      </section>
      {id && <TraceAnalysis key={id} id={id} live={live} />}
      {id && <BilanCarnet id={id} live={live} />}
    </div>
  );
}

function TraceAnalysis({ id, live }: { id: string; live: boolean }) {
  const base = `/api/carnets/${encodeURIComponent(id)}`;
  const { data: index } = usePoll<{ step: number; laps: number[] }>(`${base}/traces`, live ? 10000 : 600000);
  const { data: bilan } = usePoll<Bilan>(`${base}/bilan`, live ? 10000 : 600000);
  const [cache, setCache] = useState<Record<number, LapDetail>>({});
  const [mode, setMode] = useState<Mode>(() => readPref('smhub-analyse-mode', 'stints') as Mode);
  const [chosen, setChosen] = useState<number[] | null>(null); // null : tous les tours comptés
  const [hoverX, setHoverX] = useState<number | null>(null);
  const [zoom, setZoom] = useState<XZoom>(null);

  const withTrace = useMemo(() => new Set(index?.laps ?? []), [index]);
  const excluded = useMemo(() => new Set(bilan?.exclus ?? []), [bilan]);
  const tours = useMemo(() => {
    const byLap = new Map((bilan?.tours ?? []).map((t) => [t.lap, t]));
    for (const l of withTrace) if (!byLap.has(l)) byLap.set(l, { lap: l, pit: false, valid: true });
    return [...byLap.values()].sort((a, b) => a.lap - b.lap);
  }, [bilan, withTrace]);
  const stintOf = (lap: number) => bilan?.relais.find((r) => lap >= r.premier && lap <= r.dernier) ?? null;
  const counted = tours.filter((t) => withTrace.has(t.lap) && !excluded.has(t.lap)).map((t) => t.lap);
  const selection = (chosen ?? counted).filter((l) => withTrace.has(l));
  const sel = new Set(selection);
  const best = tours.filter((t) => withTrace.has(t.lap) && !excluded.has(t.lap) && t.time).sort((a, b) => a.time! - b.time!)[0]?.lap;

  // Tracés chargés à la demande, gardés pour les choix suivants.
  const missing = selection.filter((l) => !cache[l]);
  const missingKey = missing.join(',');
  useEffect(() => {
    if (!missingKey) return;
    let alive = true;
    api<LapDetail[]>(`${base}/traces?laps=${missingKey}`).then((r) => {
      if (alive) setCache((c) => ({ ...c, ...Object.fromEntries(r.map((l) => [l.lap, l])) }));
    }).catch(() => undefined);
    return () => { alive = false; };
  }, [base, missingKey]);

  const step = index?.step ?? 10;
  const groups = useMemo<Group[]>(() => {
    const loaded = selection.filter((l) => cache[l]);
    const make = (gid: string, label: string, laps: number[]): Group | null => {
      const traces = laps.map((l) => cache[l]).filter(Boolean);
      return traces.length ? { id: gid, label, laps, trace: traces.length === 1 ? traces[0] : averageTraces(traces) } : null;
    };
    if (mode === 'compare') return loaded.slice(0, MAX_COMPARE).map((l) => make(String(l), `T${l}`, [l])!).filter(Boolean);
    if (mode === 'average') return [make('moy', `Moyenne de ${loaded.length} tour${loaded.length > 1 ? 's' : ''}`, loaded)].filter((g): g is Group => g != null);
    const byStint = new Map<number, number[]>();
    for (const l of loaded) { const r = stintOf(l)?.relais ?? 0; byStint.set(r, [...(byStint.get(r) ?? []), l]); }
    return [...byStint.entries()].sort((a, b) => a[0] - b[0])
      .map(([r, laps]) => make(`r${r}`, `${r ? `Relais ${r}` : 'Hors relais'} (${laps.length} t.)`, laps)).filter((g): g is Group => g != null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cache, mode, selection.join(','), bilan]);

  const x = (j: number) => (j + 0.5) * step;
  const charts = useMemo(() => {
    const line = (pick: (t: Trace) => (number | null)[], width = 1.8) => groups.map((g, i): Series => ({ id: g.id, label: g.label, color: COLORS[i % COLORS.length], width: i === 0 ? width + 0.3 : width, points: pick(g.trace).map((v, j) => ({ x: x(j), y: v })) }));
    const ref = groups[0]?.trace.elapsed;
    const gaps: Series[] = ref ? groups.slice(1).map((g, i) => ({
      id: `d${g.id}`, label: `${g.label} vs ${groups[0].label}`, color: COLORS[(i + 1) % COLORS.length],
      points: g.trace.elapsed.map((e, j) => ({ x: x(j), y: e != null && ref[j] != null ? e - ref[j]! : null })),
    })) : [];
    return {
      speed: line((t) => t.speed),
      pedals: pedalSeries(groups.map((g) => ({ id: g.id, label: g.label, throttle: g.trace.throttle, brake: g.trace.brake })), x),
      gear: line((t) => t.gear.map((v) => (v != null && v >= 1 ? v : null))).map((s) => ({ ...s, step: true })),
      rpm: line((t) => t.rpm.map((v) => (v && v > 0 ? v : null)), 1.4),
      gaps,
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groups, step]);

  if (!index || !bilan) return <section className="card"><div className="card-body muted">Chargement…</div></section>;
  if (index.laps.length === 0) {
    return (
      <section className="card">
        <div className="card-head"><span className="card-title">Le long du tour</span></div>
        <div className="card-body muted">
          Pas de tracé enregistré pour cette session : vitesse, accélérateur, frein et rapports de chaque tour sont gardés avec la course
          à partir de cette version{live ? ' (le premier apparaît à la fin du tour en cours)' : ''}. Le bilan par tour ci-dessous reste disponible.
        </div>
      </section>
    );
  }

  const pickMode = (m: Mode) => { setMode(m); writePref('smhub-analyse-mode', m); };
  const toggle = (laps: number[]) => {
    const on = laps.every((l) => sel.has(l));
    setChosen(on ? selection.filter((l) => !laps.includes(l)) : [...new Set([...selection, ...laps])].sort((a, b) => a - b));
  };
  // Rangées du choix : un relais par rangée, puis les tours qu'aucun relais ne couvre (tracé sans bilan, carnet coupé…).
  const inStint = (r: Stint) => tours.filter((t) => t.lap >= r.premier && t.lap <= r.dernier);
  const loose = tours.filter((t) => !stintOf(t.lap));
  const rows: { key: string; label: string; gomme?: string | null; laps: BilanTour[] }[] = [
    ...bilan.relais.map((r) => ({ key: `r${r.relais}`, label: `Relais ${r.relais}`, gomme: r.gomme, laps: inStint(r) })),
    ...(loose.length ? [{ key: 'autres', label: bilan.relais.length ? 'Autres tours' : 'Tours', laps: loose }] : []),
  ];
  const usable = (lap: number) => withTrace.has(lap);
  const lapInfo = (laps: number[]) => {
    const ts = laps.map((l) => tours.find((t) => t.lap === l)).filter((t): t is BilanTour => t != null);
    return {
      time: mean(laps.map((l) => tours.find((t) => t.lap === l)?.time ?? cache[l]?.time)),
      fuel: mean(ts.map((t) => t.fuel)),
      energy: mean(ts.map((t) => t.energy)),
      wear: WHEELS.map((_, w) => mean(ts.map((t) => t.wearDelta?.[w]))),
    };
  };
  const maxGear = Math.max(1, ...groups.flatMap((g) => g.trace.gear.filter((v): v is number => v != null)));
  const shared = { hoverX, onHoverX: setHoverX, zoom, onZoom: setZoom, xFormat: distanceLabel, xTitle: (v: number) => `${Math.round(v)} m depuis la ligne` };
  const tooMany = mode === 'compare' && selection.length > MAX_COMPARE;

  return (
    <>
      <section className="card">
        <div className="card-head">
          <span className="card-title">Tours et relais</span>
          <span className="muted hide-xs">{selection.length} tour{selection.length > 1 ? 's' : ''} choisi{selection.length > 1 ? 's' : ''} sur {index.laps.length} enregistrés</span>
          <span className="spacer" />
          <div className="seg" role="group" aria-label="Affichage">
            <button className={mode === 'stints' ? 'on' : ''} onClick={() => pickMode('stints')} title="Une courbe par relais : la moyenne de ses tours choisis">Moyenne par relais</button>
            <button className={mode === 'average' ? 'on' : ''} onClick={() => pickMode('average')} title="Une seule courbe : la moyenne de tous les tours choisis">Moyenne</button>
            <button className={mode === 'compare' ? 'on' : ''} onClick={() => pickMode('compare')} title={`Une courbe par tour (${MAX_COMPARE} au plus)`}>Comparer les tours</button>
          </div>
        </div>
        <div className="card-body stack-sm">
          <div className="row" role="group" aria-label="Choix rapides">
            <button className="chip" onClick={() => setChosen(null)}>Tous les tours comptés</button>
            {best != null && <button className="chip" onClick={() => { setChosen([best]); }}>Meilleur tour (T{best})</button>}
            <button className="chip" onClick={() => setChosen([])}>Aucun</button>
          </div>
          {rows.map((r) => {
            const laps = r.laps;
            const countedHere = laps.filter((t) => usable(t.lap) && !excluded.has(t.lap)).map((t) => t.lap);
            if (laps.length === 0) return null;
            const allOn = countedHere.length > 0 && countedHere.every((l) => sel.has(l));
            return (
              <div key={r.key} className="debrief-stint">
                <button className={`chip debrief-stint-btn ${allOn ? 'on' : ''}`} disabled={countedHere.length === 0} onClick={() => toggle(countedHere)} aria-pressed={allOn}
                  title="Choisir ou retirer tous les tours comptés de ce relais">
                  {r.label} {r.gomme ? <TyreBadge compound={r.gomme} /> : null}
                </button>
                <div className="lap-picker">
                  {laps.map((t) => {
                    const on = sel.has(t.lap);
                    const why = !usable(t.lap) ? 'tracé non enregistré' : excluded.has(t.lap) ? 'exclu des moyennes' : t.pit ? 'stand' : !t.valid ? 'invalide' : '';
                    return (
                      <button key={t.lap} className={`chip lap-chip ${on ? 'on' : ''} ${why ? 'dim' : ''}`} disabled={!usable(t.lap)} onClick={() => toggle([t.lap])} aria-pressed={on}
                        title={`Tour ${t.lap}${why ? ` (${why})` : ''}`}>
                        {mode === 'compare' && groups.findIndex((g) => g.id === String(t.lap)) >= 0 && <span className="legend-line" style={{ borderColor: COLORS[groups.findIndex((g) => g.id === String(t.lap)) % COLORS.length] }} />}
                        <b>T{t.lap}</b><span className="tnum">{lapTime(t.time)}</span>{t.lap === best && <span className="best-overall" aria-label="meilleur tour">★</span>}
                        {why && <span className="muted small">{why === 'exclu des moyennes' ? 'exclu' : why === 'tracé non enregistré' ? '—' : why}</span>}
                      </button>
                    );
                  })}
                </div>
              </div>
            );
          })}
          {tooMany && <div className="muted small">Comparaison : les {MAX_COMPARE} premiers tours choisis sont tracés. Pour en voir plus, passez en « Moyenne » ou « Moyenne par relais ».</div>}
          {groups.length > 0 && (
            <div className="table-scroll">
              <table className="mini-table">
                <thead>
                  <tr>
                    <th>Courbe</th><th className="r">Tours</th><th className="r">Temps moyen</th><th className="r">Écart</th><th className="r">Vmax</th>
                    <th className="r">Essence</th><th className="r hide-sm">Énergie</th>
                    {WHEELS.map((w) => <th key={w} className="r hide-sm" title="Usure moyenne par tour (points de %)">{WHEEL_SHORT[w]}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {groups.map((g, i) => {
                    const info = lapInfo(g.laps);
                    const ref = lapInfo(groups[0].laps).time;
                    const vmax = Math.max(...g.trace.speed.filter((v): v is number => v != null));
                    return (
                      <tr key={g.id}>
                        <td className="nowrap"><span className="legend-line" style={{ borderColor: COLORS[i % COLORS.length] }} /> <b>{g.label}</b></td>
                        <td className="r tnum">{g.laps.length}</td>
                        <td className="r tnum">{lapTime(info.time)}</td>
                        <td className="r tnum">{i > 0 && info.time != null && ref != null ? `${delta(info.time - ref, 3)} s` : '—'}</td>
                        <td className="r tnum">{isFinite(vmax) ? kph(vmax) : '—'}</td>
                        <td className="r tnum">{info.fuel != null ? `${info.fuel.toFixed(2)} L` : '—'}</td>
                        <td className="r tnum hide-sm">{info.energy != null ? `${info.energy.toFixed(2)} %` : '—'}</td>
                        {info.wear.map((w, k) => <td key={k} className="r tnum hide-sm">{w != null ? w.toFixed(2) : '—'}</td>)}
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
          <span className="muted hide-xs">glisser : agrandir une zone · Ctrl/⌘ + molette ou pincement : zoom · barre sous le graphique : défiler · double-clic : tout</span>
        </div>
        <div className="card-body stack-sm">
          {groups.length === 0 ? (
            <div className="muted">{missing.length ? 'Chargement des tracés…' : 'Choisissez au moins un tour ou un relais.'}</div>
          ) : (
            <>
              <div className="chart-title">Vitesse</div>
              <LineChart series={charts.speed} height={260} {...shared} means yFormat={(y) => `${Math.round(y)}`} valueFormat={(y) => kph(y)} ariaLabel="Vitesse le long du tour" />
              <div className="chart-title">Accélérateur <span className="pedal-throttle">(vert)</span> et frein <span className="pedal-brake">(rouge)</span>{groups.length > 1 && <span className="muted small"> · un trait par courbe : {groups[0].label} plein, puis tirets, points…</span>}</div>
              <LineChart series={charts.pedals} height={260} {...shared} means legend={groups.length > 1} dashedLabel={null} yDomain={[0, 100]} yFormat={(y) => `${Math.round(y)}`} valueFormat={(y) => `${Math.round(y)} %`} ariaLabel="Accélérateur et frein le long du tour" />
              {charts.gaps.some((s) => s.points.some((p) => p.y != null)) && (
                <>
                  <div className="chart-title">Écart cumulé par rapport à {groups[0].label} <span className="muted small">(au-dessus de 0 : plus lent à cet endroit)</span></div>
                  <LineChart series={charts.gaps} height={180} {...shared} legend={false} refLines={[{ y: 0 }]} yFormat={(y) => y.toFixed(1)} valueFormat={(y) => `${delta(y, 3)} s`} ariaLabel="Écart de temps cumulé le long du tour" />
                </>
              )}
              {mode === 'compare' && charts.gear.some((s) => s.points.some((p) => p.y != null)) && (
                <>
                  <div className="chart-title">Rapport engagé</div>
                  <LineChart series={charts.gear} height={Math.max(150, 26 * maxGear)} {...shared} legend={false} yTicks={Array.from({ length: Math.round(maxGear) }, (_, i) => i + 1)} yFormat={(y) => String(y)} valueFormat={(y) => `${y}e`} ariaLabel="Rapport engagé le long du tour" />
                </>
              )}
              {charts.rpm.some((s) => s.points.some((p) => p.y != null)) && (
                <>
                  <div className="chart-title">Régime moteur</div>
                  <LineChart series={charts.rpm} height={170} {...shared} legend={false} means yFormat={(y) => `${Math.round(y / 100) / 10}k`} valueFormat={(y) => `${Math.round(y)} tr/min`} ariaLabel="Régime moteur le long du tour" />
                </>
              )}
            </>
          )}
        </div>
      </section>
    </>
  );
}
