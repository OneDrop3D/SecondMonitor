import { useMemo, useState, type CSSProperties } from 'react';
import { usePoll } from '../live';
import { compoundInfo, delta, lapTime } from '../format';
import type { DriverRow, MicroSectorLaps, MicroSectorLapRow, MicroSectorReport, RaceSnapshot } from '../types';
import { useWidth } from '../charts/useSize';
import { LineChart, type Series } from '../charts/LineChart';
import { readJsonList, useStoredState, writeJsonPref } from '../prefs';
import { ClassSwatch, TyreBadge } from './Common';

type Mode = 'current' | 'last' | 'best';
type Status = 'overall' | 'personal' | 'slower' | 'pit' | 'none' | 'live';
type Scope = 'me' | 'selected' | 'class' | 'all';

interface Props {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (d: DriverRow) => void;
}

/** Configuration de l'écran, gardée au changement d'onglet et au redémarrage ; peut être enregistrée sous un nom. */
interface MsConfig {
  view: 'plateau' | 'tours';
  /** Plateau : tour affiché. */
  mode: Mode;
  showTimes: boolean;
  /** Plateau : « mine » (ma catégorie), « all » ou l'id d'une catégorie. */
  classFilter: string;
  /** Tous les tours : quelles voitures. */
  scope: Scope;
  drivers: string[];
  /** Tous les tours (plateau entier) : « all » ou l'id d'une catégorie. */
  lapsClass: string;
  /** Gommes affichées (clés de compoundKey) ; vide = toutes. */
  compounds: string[];
  includePits: boolean;
  includeInvalid: boolean;
  sort: 'lap' | 'time';
  cell: 'colour' | 'time' | 'delta';
}

const DEFAULT_CONFIG: MsConfig = {
  view: 'plateau', mode: 'last', showTimes: false, classFilter: 'mine',
  scope: 'me', drivers: [], lapsClass: 'all', compounds: [], includePits: false, includeInvalid: false, sort: 'lap', cell: 'colour',
};
const CONFIG_KEY = 'smhub-microsecteurs';
const PRESETS_KEY = 'smhub-microsecteurs-configs';
const MAX_ROWS = 400;

const STATUS_LABEL: Record<Status, string> = {
  overall: 'Meilleur de la catégorie',
  personal: 'Meilleur personnel',
  slower: 'Plus lent que son meilleur',
  pit: 'Passage au stand',
  none: 'Pas de mesure',
  live: 'En cours',
};

/** Regroupement des gommes : les jeux les nomment différemment (« Soft », « S », « C5 »… ; « Wet », « Rain »…). */
function compoundKey(name: string | null | undefined): string {
  const c = compoundInfo(name);
  return c.kind === 'other' ? `autre:${(name ?? '').trim().toLowerCase()}` : c.kind;
}

const COMPOUND_LABEL: Record<string, string> = {
  soft: 'Tendres', medium: 'Mediums', hard: 'Durs', inter: 'Intermédiaires', wet: 'Pluie', dry: 'Slicks', unknown: 'Inconnue',
};
const COMPOUND_COLOR: Record<string, string> = {
  soft: 'var(--tyre-soft)', medium: 'var(--tyre-medium)', hard: 'var(--tyre-hard)', inter: 'var(--tyre-inter)', wet: 'var(--tyre-wet)',
};
const COMPOUND_ORDER = ['soft', 'medium', 'hard', 'dry', 'inter', 'wet'];

function compoundLabel(key: string, names: Iterable<string>) {
  if (COMPOUND_LABEL[key]) return COMPOUND_LABEL[key];
  return [...names][0] ?? key.replace(/^autre:/, '');
}

const sameConfig = (a: MsConfig, b: MsConfig) => JSON.stringify({ ...DEFAULT_CONFIG, ...a }) === JSON.stringify({ ...DEFAULT_CONFIG, ...b });

/** Micro-secteurs : le tour est découpé en tronçons égaux, chronométrés par le hub pour tous les pilotes. */
export function SectorsView({ snapshot, classColor, onSelect }: Props) {
  const [config, setConfig] = useStoredState<MsConfig>(CONFIG_KEY, DEFAULT_CONFIG);
  const update = (patch: Partial<MsConfig>) => setConfig(patch);

  return (
    <div className="stack">
      <div className="row view-tabs-row">
        <div className="seg view-tabs" role="tablist" aria-label="Affichage">
          <button role="tab" aria-selected={config.view === 'plateau'} className={config.view === 'plateau' ? 'on' : ''} onClick={() => update({ view: 'plateau' })}>Plateau</button>
          <button role="tab" aria-selected={config.view === 'tours'} className={config.view === 'tours' ? 'on' : ''} onClick={() => update({ view: 'tours' })}>Tous les tours</button>
        </div>
        <span className="spacer" />
        <ConfigPresets config={config} apply={(c) => setConfig(() => ({ ...DEFAULT_CONFIG, ...c }))} />
      </div>
      {config.view === 'plateau'
        ? <FieldView snapshot={snapshot} classColor={classColor} onSelect={onSelect} config={config} update={update} />
        : <AllLapsView snapshot={snapshot} classColor={classColor} config={config} update={update} />}
    </div>
  );
}

/** Configurations nommées : enregistrer la configuration actuelle, la rappeler, la supprimer. */
function ConfigPresets({ config, apply }: { config: MsConfig; apply: (c: MsConfig) => void }) {
  const [presets, setPresets] = useState(() => readJsonList<{ name: string; config: MsConfig }>(PRESETS_KEY));
  const [naming, setNaming] = useState<string | null>(null);
  const current = presets.find((p) => sameConfig(p.config, config));
  const save = (list: { name: string; config: MsConfig }[]) => { setPresets(list); writeJsonPref(PRESETS_KEY, list); };
  const commit = () => {
    const name = (naming ?? '').trim();
    if (!name) return;
    save([...presets.filter((p) => p.name !== name), { name, config }].sort((a, b) => a.name.localeCompare(b.name, 'fr')));
    setNaming(null);
  };
  return (
    <div className="row config-presets" role="group" aria-label="Configurations enregistrées">
      {naming == null ? (
        <>
          <select className="select compact" value={current?.name ?? ''} aria-label="Rappeler une configuration"
            onChange={(e) => { const p = presets.find((x) => x.name === e.target.value); if (p) apply(p.config); }}>
            <option value="">{presets.length ? (current ? current.name : 'Configuration…') : 'Aucune configuration enregistrée'}</option>
            {presets.map((p) => <option key={p.name} value={p.name}>{p.name}</option>)}
          </select>
          <button className="chip" onClick={() => setNaming(current?.name ?? '')} title="Garder la configuration actuelle (affichage, filtres) sous un nom">Enregistrer…</button>
          {current && <button className="chip" onClick={() => save(presets.filter((p) => p.name !== current.name))} title={`Supprimer « ${current.name} »`}>Supprimer</button>}
        </>
      ) : (
        <form className="row" onSubmit={(e) => { e.preventDefault(); commit(); }}>
          <input className="input compact-input wide" autoFocus value={naming} placeholder="Nom (ex. Pluie toutes voitures)" maxLength={40}
            onChange={(e) => setNaming(e.target.value)} onKeyDown={(e) => { if (e.key === 'Escape') setNaming(null); }} aria-label="Nom de la configuration" />
          <button className="chip on" type="submit" disabled={!naming.trim()}>OK</button>
          <button className="chip" type="button" onClick={() => setNaming(null)}>Annuler</button>
        </form>
      )}
    </div>
  );
}

function FieldView({ snapshot, classColor, onSelect, config, update }: Props & { config: MsConfig; update: (p: Partial<MsConfig>) => void }) {
  const { data } = usePoll<MicroSectorReport>('/api/analysis/microsectors', 1000);
  const { mode, showTimes } = config;
  const classes = [...new Map(snapshot.drivers.map((d) => [d.classId, d.className ?? d.classId])).entries()];
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const classFilter = config.classFilter === 'all' ? null
    : config.classFilter !== 'mine' && classes.some(([id]) => id === config.classFilter) ? config.classFilter
      : me?.classId ?? null;
  const rows = snapshot.drivers.filter((d) => !classFilter || d.classId === classFilter);
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
    <>
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
            <button className={mode === 'current' ? 'on' : ''} onClick={() => update({ mode: 'current' })}>En cours</button>
            <button className={mode === 'last' ? 'on' : ''} onClick={() => update({ mode: 'last' })}>Dernier tour</button>
            <button className={mode === 'best' ? 'on' : ''} onClick={() => update({ mode: 'best' })}>Meilleurs</button>
          </div>
          <label className="row secondary hide-xs" style={{ gap: 6 }}>
            <input type="checkbox" checked={showTimes} onChange={(e) => update({ showTimes: e.target.checked })} /> Temps
          </label>
        </div>
        {classes.length > 1 && (
          <div className="row card-toolbar" role="group" aria-label="Catégorie">
            <button className={`chip ${classFilter === null ? 'on' : ''}`} onClick={() => update({ classFilter: 'all' })}>Toutes</button>
            {classes.map(([id, name]) => (
              <button key={id} className={`chip ${classFilter === id ? 'on' : ''}`} onClick={() => update({ classFilter: id === me?.classId ? 'mine' : id })}><ClassSwatch color={classColor(id)} />{name}</button>
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
    </>
  );
}

interface LapLine {
  driverId: string;
  driverLabel: string;
  classId: string;
  isPlayer: boolean;
  row: MicroSectorLapRow;
  key: string;
}

const isClean = (r: MicroSectorLapRow) => r.flags === 0 && r.time != null && r.time > 0;

/** Tous les tours faits (vous, des pilotes choisis, votre catégorie ou tout le plateau), avec leurs micro-secteurs et les gommes. */
function AllLapsView({ snapshot, classColor, config, update }: { snapshot: RaceSnapshot; classColor: (c: string) => string; config: MsConfig; update: (p: Partial<MsConfig>) => void }) {
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const ids = config.scope === 'me' ? me?.id ?? '' : config.scope === 'selected' ? config.drivers.join(',') : '';
  const waiting = config.scope === 'selected' && config.drivers.length === 0;
  const { data } = usePoll<MicroSectorLaps>(waiting ? null : `/api/analysis/microsectors/laps${ids ? `?ids=${encodeURIComponent(ids)}` : ''}`, 5000);
  const classes = [...new Map(snapshot.drivers.map((d) => [d.classId, d.className ?? d.classId])).entries()];
  const count = data?.count ?? 24;

  // Voitures retenues (avant le filtre des gommes : la comparaison des gommes en a besoin).
  const scoped = useMemo(() => (data?.drivers ?? []).filter((d) => {
    switch (config.scope) {
      case 'me': return d.isPlayer;
      case 'selected': return config.drivers.includes(d.id);
      case 'class': return d.classId === (me?.classId ?? d.classId);
      default: return config.lapsClass === 'all' || d.classId === config.lapsClass;
    }
  }), [data, config.scope, config.drivers, config.lapsClass, me?.classId]);

  const lines: LapLine[] = useMemo(() => scoped.flatMap((d) => d.laps.map((row) => ({
    driverId: d.id,
    driverLabel: d.isPlayer ? 'Vous' : `#${d.number ?? '–'} ${d.name ?? d.id}`,
    classId: d.classId,
    isPlayer: d.isPlayer,
    row,
    key: `${d.id}#${row.lap}`,
  }))), [scoped]);

  // Gommes présentes (avec les noms donnés par le jeu).
  const compounds = useMemo(() => {
    const m = new Map<string, Set<string>>();
    for (const l of lines) {
      const k = compoundKey(l.row.compound);
      if (!m.has(k)) m.set(k, new Set());
      if (l.row.compound) m.get(k)!.add(l.row.compound);
    }
    const rank = (k: string) => (COMPOUND_ORDER.includes(k) ? COMPOUND_ORDER.indexOf(k) : 99);
    return [...m.entries()].sort((a, b) => rank(a[0]) - rank(b[0]));
  }, [lines]);
  const activeCompounds = config.compounds.filter((c) => compounds.some(([k]) => k === c));
  const toggleCompound = (k: string) => update({ compounds: activeCompounds.includes(k) ? activeCompounds.filter((c) => c !== k) : [...activeCompounds, k] });

  const shown = useMemo(() => {
    const list = lines.filter((l) => {
      const r = l.row;
      if (!r.ms || !r.ms.some((v) => v != null)) return false;
      if (!config.includePits && (r.flags & (1 | 2 | 8))) return false;
      if (!config.includeInvalid && (r.flags & 4)) return false;
      return activeCompounds.length === 0 || activeCompounds.includes(compoundKey(r.compound));
    });
    if (config.sort === 'time') list.sort((a, b) => (a.row.time ?? Infinity) - (b.row.time ?? Infinity));
    else list.sort((a, b) => (a.driverId === b.driverId ? a.row.lap - b.row.lap : Number(b.isPlayer) - Number(a.isPlayer) || a.driverLabel.localeCompare(b.driverLabel, 'fr')));
    return list;
  }, [lines, config.includePits, config.includeInvalid, config.sort, activeCompounds.join(',')]); // eslint-disable-line react-hooks/exhaustive-deps

  // Références des couleurs : meilleur de la catégorie (parmi les tours affichés) et meilleur de chaque pilote, micro-secteur par micro-secteur.
  // Par catégorie : une GT3 n'est pas comparée à une hypercar.
  const best = useMemo(() => {
    const byClass = new Map<string, { ms: (number | null)[]; lap: number | null }>();
    const perDriver = new Map<string, (number | null)[]>();
    for (const l of shown) {
      const cls = byClass.get(l.classId) ?? { ms: Array(count).fill(null), lap: null };
      byClass.set(l.classId, cls);
      const mine = perDriver.get(l.driverId) ?? Array(count).fill(null);
      perDriver.set(l.driverId, mine);
      l.row.ms?.forEach((v, i) => {
        if (v == null) return;
        if (cls.ms[i] == null || v < cls.ms[i]!) cls.ms[i] = v;
        if (mine[i] == null || v < mine[i]!) mine[i] = v;
      });
      if (isClean(l.row) && (cls.lap == null || l.row.time! < cls.lap)) cls.lap = l.row.time!;
    }
    const theoretical = [...byClass.entries()]
      .map(([classId, b]) => ({ classId, value: b.ms.every((v) => v != null) ? b.ms.reduce((a, v) => a! + v!, 0) : null }))
      .filter((t): t is { classId: string; value: number } => t.value != null);
    return { byClass, perDriver, theoretical };
  }, [shown, count]);
  const severalClasses = best.byClass.size > 1;
  const classesKey = classes.map((c) => c.join(':')).join(',');
  const classNameOf = useMemo(() => {
    const names = new Map(classes);
    return (id: string) => names.get(id) ?? id;
  }, [classesKey]); // eslint-disable-line react-hooks/exhaustive-deps

  const multipleDrivers = new Set(shown.map((l) => l.driverId)).size > 1 || config.scope !== 'me';

  const cell = (l: LapLine, v: number | null, i: number) => {
    if (v == null) return <span className="ms none" title={`Micro-secteur ${i + 1} : pas de mesure`} />;
    const b = best.byClass.get(l.classId)!.ms[i]!;
    const pb = best.perDriver.get(l.driverId)?.[i];
    const d = v - b;
    let cls: Status = 'slower';
    let style: CSSProperties | undefined;
    if (d <= 0.0005) cls = 'overall';
    else if (pb != null && v <= pb + 0.0005 && multipleDrivers) cls = 'personal';
    else {
      // Plus lent : du jaune au rouge selon l'écart (3 % du meilleur = rouge franc).
      const f = Math.round(Math.min(1, d / Math.max(0.01, b * 0.03)) * 100);
      style = { background: `color-mix(in oklab, var(--critical) ${f}%, var(--ms-slower))` };
    }
    const text = config.cell === 'time' ? v.toFixed(1) : config.cell === 'delta' ? (d <= 0.0005 ? '' : `+${d.toFixed(2)}`) : '';
    return (
      <span className={`ms ${cls}`} style={style} title={`Micro-secteur ${i + 1} · ${v.toFixed(3)} s${d > 0.0005 ? ` (${delta(d, 3)} s vs meilleur ${severalClasses ? 'de la catégorie' : 'affiché'})` : ` · meilleur ${severalClasses ? 'de la catégorie' : 'affiché'}`}`}>
        {text}
      </span>
    );
  };

  const available = snapshot.drivers.filter((d) => !config.drivers.includes(d.id));
  const shownRows = shown.slice(0, MAX_ROWS);
  return (
    <>
      <section className="card">
        <div className="card-head">
          <span className="card-title">Tous les tours</span>
          <span className="muted hide-xs">{shown.length} tour{shown.length > 1 ? 's' : ''} · micro-secteurs de {Math.round((data?.lapLength ?? 0) / count)} m</span>
          <span className="spacer" />
          <div className="seg" role="group" aria-label="Contenu des cases">
            <button className={config.cell === 'colour' ? 'on' : ''} onClick={() => update({ cell: 'colour' })}>Couleurs</button>
            <button className={config.cell === 'time' ? 'on' : ''} onClick={() => update({ cell: 'time' })}>Temps</button>
            <button className={config.cell === 'delta' ? 'on' : ''} onClick={() => update({ cell: 'delta' })}>Écarts</button>
          </div>
          <div className="seg" role="group" aria-label="Ordre">
            <button className={config.sort === 'lap' ? 'on' : ''} onClick={() => update({ sort: 'lap' })}>Ordre des tours</button>
            <button className={config.sort === 'time' ? 'on' : ''} onClick={() => update({ sort: 'time' })}>Plus rapides</button>
          </div>
        </div>
        <div className="card-toolbar stack-sm ms-filters">
          <div className="row" role="group" aria-label="Voitures">
            <span className="filter-label">Voitures</span>
            <button className={`chip ${config.scope === 'me' ? 'on' : ''}`} onClick={() => update({ scope: 'me' })}>Moi</button>
            <button className={`chip ${config.scope === 'selected' ? 'on' : ''}`} onClick={() => update({ scope: 'selected' })}>Pilotes choisis</button>
            <button className={`chip ${config.scope === 'class' ? 'on' : ''}`} onClick={() => update({ scope: 'class' })}>Ma catégorie</button>
            <button className={`chip ${config.scope === 'all' ? 'on' : ''}`} onClick={() => update({ scope: 'all' })}>Tout le plateau</button>
          </div>
          {config.scope === 'selected' && (
            <div className="picker" role="group" aria-label="Pilotes choisis">
              {config.drivers.map((id) => {
                const d = snapshot.drivers.find((x) => x.id === id);
                const name = d ? (d.isPlayer ? 'Vous' : `#${d.number ?? '–'} ${d.shortName}`) : data?.drivers.find((x) => x.id === id)?.name ?? id;
                return <button key={id} className="chip on picker-chip" onClick={() => update({ drivers: config.drivers.filter((x) => x !== id) })} title="Retirer">{name} <span className="muted" aria-hidden>×</span></button>;
              })}
              {available.length > 0 && (
                <select className="select compact" value="" onChange={(e) => e.target.value && update({ drivers: [...config.drivers, e.target.value] })} aria-label="Ajouter un pilote">
                  <option value="">+ Ajouter un pilote…</option>
                  {available.map((d) => <option key={d.id} value={d.id}>P{d.position} · #{d.number ?? '–'} {d.name}{d.className ? ` (${d.className})` : ''}</option>)}
                </select>
              )}
            </div>
          )}
          {config.scope === 'all' && classes.length > 1 && (
            <div className="row" role="group" aria-label="Catégorie">
              <span className="filter-label">Catégorie</span>
              <button className={`chip ${config.lapsClass === 'all' ? 'on' : ''}`} onClick={() => update({ lapsClass: 'all' })}>Toutes</button>
              {classes.map(([id, name]) => (
                <button key={id} className={`chip ${config.lapsClass === id ? 'on' : ''}`} onClick={() => update({ lapsClass: id })}><ClassSwatch color={classColor(id)} />{name}</button>
              ))}
            </div>
          )}
          <div className="row" role="group" aria-label="Gommes">
            <span className="filter-label">Pneus</span>
            <button className={`chip ${activeCompounds.length === 0 ? 'on' : ''}`} onClick={() => update({ compounds: [] })}>Tous</button>
            {compounds.map(([k, names]) => (
              <button key={k} className={`chip ${activeCompounds.includes(k) ? 'on' : ''}`} onClick={() => toggleCompound(k)} title={[...names].join(', ')}>
                <TyreBadge compound={[...names][0] ?? null} /> {compoundLabel(k, names)}
              </button>
            ))}
            {compounds.length === 0 && <span className="muted small">aucune gomme connue pour l'instant</span>}
            <span className="spacer" />
            <label className="row secondary small" style={{ gap: 6 }}><input type="checkbox" checked={config.includePits} onChange={(e) => update({ includePits: e.target.checked })} /> Tours de stand et de sortie</label>
            <label className="row secondary small" style={{ gap: 6 }}><input type="checkbox" checked={config.includeInvalid} onChange={(e) => update({ includeInvalid: e.target.checked })} /> Tours invalides</label>
          </div>
        </div>
        {waiting ? (
          <div className="card-body muted">Ajoutez un ou plusieurs pilotes pour voir leurs tours.</div>
        ) : shown.length === 0 ? (
          <div className="card-body muted">{data ? 'Aucun tour pour ces filtres (les tours apparaissent une fois bouclés).' : 'Chargement…'}</div>
        ) : (
          <div className="table-scroll ms-laps-scroll">
            <table className={`lb ms-table ms-laps ${config.cell !== 'colour' ? 'with-times' : ''}`}>
              <thead>
                <tr>
                  {multipleDrivers && <th className="sticky-col">Pilote</th>}
                  <th className={multipleDrivers ? '' : 'sticky-col'}>Tour</th>
                  <th>Pneus</th>
                  {Array.from({ length: count }, (_, i) => <th key={i} className="c ms-head">{i + 1}</th>)}
                  <th className="r">Temps</th>
                  <th className="r" title="Écart au meilleur tour affiché (de la même catégorie)">Écart</th>
                </tr>
              </thead>
              <tbody>
                {shownRows.map((l) => {
                  const r = l.row;
                  const flags = [r.flags & 1 && 'stand', r.flags & 2 && 'sortie', r.flags & 4 && 'invalide'].filter(Boolean).join(', ');
                  const bestLap = best.byClass.get(l.classId)?.lap ?? null;
                  return (
                    <tr key={l.key} className={`${l.isPlayer && multipleDrivers ? 'player' : ''} ${r.flags ? 'dim-row' : ''}`}>
                      {multipleDrivers && <td className="sticky-col nowrap"><ClassSwatch color={classColor(l.classId)} /> {l.isPlayer ? <b>Vous</b> : l.driverLabel}</td>}
                      <td className={`nowrap tnum ${multipleDrivers ? '' : 'sticky-col'}`}>T{r.lap}{flags && <span className="muted small"> {flags}</span>}</td>
                      <td className="nowrap">{r.compound ? <TyreBadge compound={r.compound} laps={r.tyreLaps ?? undefined} /> : <span className="muted">—</span>}</td>
                      {Array.from({ length: count }, (_, i) => <td key={i} className="c ms-td">{cell(l, r.ms?.[i] ?? null, i)}</td>)}
                      <td className={`r tnum ${isClean(r) && r.time === bestLap ? 'best-overall' : ''}`}>{lapTime(r.time)}</td>
                      <td className="r tnum">{isClean(r) && bestLap != null && r.time !== bestLap ? delta(r.time! - bestLap, 3) : ''}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
        <div className="legend card-foot">
          <span className="legend-item"><span className="ms overall" /> {severalClasses ? 'Meilleur de la catégorie (tours affichés)' : 'Meilleur des tours affichés'}</span>
          {multipleDrivers && <span className="legend-item"><span className="ms personal" /> Meilleur du pilote</span>}
          <span className="legend-item"><span className="ms slower" /> Plus lent</span>
          <span className="legend-item"><span className="ms slower" style={{ background: 'var(--critical)' }} /> 3 % ou plus</span>
          {best.theoretical.length > 0 && (
            <span className="legend-item">Théorique (meilleurs micro-secteurs affichés) :&nbsp;
              {best.theoretical.map((t, n) => <span key={t.classId}>{n > 0 && ' · '}{severalClasses && `${classNameOf(t.classId)} `}<b className="tnum">{lapTime(t.value)}</b></span>)}
            </span>
          )}
          {shown.length > MAX_ROWS && <span className="legend-item muted">{MAX_ROWS} premiers tours sur {shown.length}.</span>}
        </div>
      </section>
      <TyreComparison lines={lines} compounds={compounds} count={count} className={classNameOf} />
    </>
  );
}

interface CompoundStats {
  key: string;
  compound: string;
  label: string;
  names: string[];
  laps: number;
  cars: number;
  best: number | null;
  mean: number | null;
  /** Écart moyen au meilleur tour de chaque pilote (toutes gommes) : compare les gommes entre voitures de rythmes différents. */
  gapToOwnBest: number | null;
  /** Perte par tour d'âge des pneus (s/tour), pentes des relais pondérées par leur nombre de tours. */
  degradation: number | null;
  theoretical: number | null;
  /** Écart moyen au meilleur personnel selon l'âge des pneus. */
  byAge: { age: number; gap: number; n: number }[];
}

function linearSlope(points: [number, number][]): number | null {
  if (points.length < 3) return null;
  const n = points.length;
  const mx = points.reduce((a, p) => a + p[0], 0) / n;
  const my = points.reduce((a, p) => a + p[1], 0) / n;
  let num = 0, den = 0;
  for (const [x, y] of points) { num += (x - mx) * (y - my); den += (x - mx) ** 2; }
  return den > 0 ? num / den : null;
}

/** Comportement des gommes : rythme, dégradation au fil du relais et meilleurs micro-secteurs, par gomme, sur les voitures retenues. */
function TyreComparison({ lines, compounds, count, className }: { lines: LapLine[]; compounds: [string, Set<string>][]; count: number; className: (id: string) => string }) {
  const stats = useMemo(() => {
    const clean = lines.filter((l) => isClean(l.row));
    const ownBest = new Map<string, number>();
    for (const l of clean) ownBest.set(l.driverId, Math.min(ownBest.get(l.driverId) ?? Infinity, l.row.time!));
    // Une ligne par gomme, et par catégorie s'il y en a plusieurs (meilleur tour et moyenne ne se comparent pas entre catégories).
    const classIds = [...new Set(clean.map((l) => l.classId))];
    const groups = classIds.flatMap((classId) => compounds.map(([key, names]) => ({ classId, key, names })));
    return groups.map(({ classId, key, names }): CompoundStats => {
      const mine = clean.filter((l) => l.classId === classId && compoundKey(l.row.compound) === key);
      const times = mine.map((l) => l.row.time!);
      const gaps = mine.map((l) => l.row.time! - ownBest.get(l.driverId)!);
      // Dégradation : pente temps / âge des pneus dans chaque relais (tours à plus de 107 % de la médiane du relais écartés).
      const stints = new Map<string, LapLine[]>();
      for (const l of mine) {
        if (l.row.tyreLaps == null) continue;
        const k = `${l.driverId}#${l.row.stint ?? 0}`;
        stints.set(k, [...(stints.get(k) ?? []), l]);
      }
      let wSum = 0, sSum = 0;
      for (const list of stints.values()) {
        const sorted = list.map((l) => l.row.time!).sort((a, b) => a - b);
        const median = sorted[Math.floor(sorted.length / 2)];
        const pts = list.filter((l) => l.row.time! <= median * 1.07).map((l) => [l.row.tyreLaps!, l.row.time!] as [number, number]);
        const slope = linearSlope(pts);
        if (slope != null) { wSum += pts.length; sSum += slope * pts.length; }
      }
      const byAgeMap = new Map<number, number[]>();
      mine.forEach((l, i) => { if (l.row.tyreLaps != null) byAgeMap.set(l.row.tyreLaps, [...(byAgeMap.get(l.row.tyreLaps) ?? []), gaps[i]]); });
      const msBest: (number | null)[] = Array(count).fill(null);
      for (const l of mine) l.row.ms?.forEach((v, i) => { if (v != null && (msBest[i] == null || v < msBest[i]!)) msBest[i] = v; });
      return {
        key: `${classId}|${key}`,
        compound: key,
        label: classIds.length > 1 ? `${compoundLabel(key, names)} · ${className(classId)}` : compoundLabel(key, names),
        names: [...names],
        laps: mine.length,
        cars: new Set(mine.map((l) => l.driverId)).size,
        best: times.length ? Math.min(...times) : null,
        mean: times.length ? times.reduce((a, v) => a + v, 0) / times.length : null,
        gapToOwnBest: gaps.length ? gaps.reduce((a, v) => a + v, 0) / gaps.length : null,
        degradation: wSum > 0 ? sSum / wSum : null,
        theoretical: msBest.every((v) => v != null) ? msBest.reduce((a, v) => a! + v!, 0) : null,
        byAge: [...byAgeMap.entries()].sort((a, b) => a[0] - b[0]).map(([age, g]) => ({ age, gap: g.reduce((a, v) => a + v, 0) / g.length, n: g.length })),
      };
    }).filter((s) => s.laps > 0);
  }, [lines, compounds, count, className]);

  // Couleur de la gomme ; pointillés pour la deuxième catégorie et les suivantes.
  const classOrder = [...new Set(stats.map((s) => s.key.split('|')[0]))];
  const series: Series[] = stats.map((s) => ({
    id: s.key,
    label: `${s.label} (${s.cars} voiture${s.cars > 1 ? 's' : ''})`,
    color: COMPOUND_COLOR[s.compound] ?? 'var(--tyre-other)',
    dashed: classOrder.indexOf(s.key.split('|')[0]) > 0,
    width: 2,
    points: s.byAge.map((a) => ({ x: a.age, y: a.gap })),
    markers: s.byAge.map((a) => ({ x: a.age, y: a.gap, title: `${s.label}, pneus de ${a.age} tour${a.age > 1 ? 's' : ''} : ${delta(a.gap, 3)} s en moyenne (${a.n} tour${a.n > 1 ? 's' : ''})` })),
  }));

  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Comportement des pneus</span>
        <span className="muted hide-xs">tours propres des voitures retenues, toutes gommes (le filtre des pneus ne s'applique pas ici)</span>
      </div>
      {stats.length === 0 ? (
        <div className="card-body muted">Pas encore de tour propre avec une gomme connue pour ces voitures.</div>
      ) : (
        <div className="card-body stack-sm">
          <div className="table-scroll">
            <table className="mini-table">
              <thead>
                <tr>
                  <th>Pneus</th>
                  <th className="r">Tours</th>
                  <th className="r">Voitures</th>
                  <th className="r">Meilleur</th>
                  <th className="r">Moyenne</th>
                  <th className="r" title="Écart moyen au meilleur tour de chaque pilote (toutes gommes) : compare des voitures de rythmes différents">Écart au meilleur perso</th>
                  <th className="r" title="Temps perdu par tour d'âge des pneus, dans chaque relais">Dégradation</th>
                  <th className="r hide-sm" title="Somme des meilleurs micro-secteurs faits avec cette gomme">Théorique</th>
                </tr>
              </thead>
              <tbody>
                {stats.map((s) => (
                  <tr key={s.key}>
                    <td className="nowrap" title={s.names.join(', ')}><TyreBadge compound={s.names[0] ?? null} /> {s.label}</td>
                    <td className="r tnum">{s.laps}</td>
                    <td className="r tnum">{s.cars}</td>
                    <td className="r tnum">{lapTime(s.best)}</td>
                    <td className="r tnum">{lapTime(s.mean)}</td>
                    <td className="r tnum">{s.gapToOwnBest != null ? `${delta(s.gapToOwnBest, 3)} s` : '—'}</td>
                    <td className="r tnum">{s.degradation != null ? `${delta(s.degradation, 3)} s/tour` : <span className="muted" title="Il faut au moins 3 tours propres dans un relais">—</span>}</td>
                    <td className="r tnum hide-sm">{lapTime(s.theoretical)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {series.some((s) => s.points.length > 1) && (
            <>
              <div className="chart-title">Écart au meilleur tour personnel selon l'âge des pneus</div>
              <LineChart series={series} height={220} legend dashedLabel={null} xFormat={(x) => `${Math.round(x)}`} xTitle={(x) => `Pneus de ${Math.round(x)} tour${x > 1 ? 's' : ''}`}
                yFormat={(y) => y.toFixed(1)} valueFormat={(y) => `${delta(y, 3)} s`} ariaLabel="Écart au meilleur tour selon l'âge des pneus, par gomme" />
              <div className="chart-note muted">Moyenne de toutes les voitures retenues pour chaque âge de pneus (1 = tour de sortie des stands). Plus la courbe monte, plus la gomme se dégrade.</div>
            </>
          )}
        </div>
      )}
    </section>
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
