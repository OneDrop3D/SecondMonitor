import { useMemo } from 'react';
import { clock, compoundInfo, flagLabel, gap, lapTime, num, sessionLabel, thermal, thermalColor } from '../format';
import type { DriverRow, HubStatus, RaceSnapshot, TyreThermal, WheelStatus } from '../types';
import { collectorAddress, linkStage } from '../status';
import { visibleDrivers, type ClassView } from '../classView';
import { TrackMap } from './TrackMap';
import { BattlePanel } from './BattlePanel';
import { BrandLogo } from './BrandLogo';
import { DamageList, PitPlanCard } from './RaceExtras';

/*
 * Écran « Chronométrage » : reprend la disposition de SecondMonitor d'origine
 * (bandeau de session en haut, grand tableau de chronométrage, état de la voiture et carte en bas),
 * avec les nouveautés du hub (catégories, pneus, arrêts estimés, duels).
 */

const EPS = 0.0005;
const same = (a: number | null | undefined, b: number | null | undefined) => a != null && b != null && Math.abs(a - b) < EPS;

interface SectorBest { time: number; driver: DriverRow; lap: number | null }

/** Meilleurs secteurs et meilleur tour, par catégorie. */
function classBests(drivers: DriverRow[]) {
  const sectors = new Map<string, (SectorBest | null)[]>();
  const laps = new Map<string, { time: number; driver: DriverRow } | null>();
  for (const d of drivers) {
    const s = sectors.get(d.classId) ?? [null, null, null];
    (d.bestSectors ?? []).forEach((v, i) => {
      if (v != null && v > 0 && (s[i] == null || v < s[i]!.time)) s[i] = { time: v, driver: d, lap: d.bestSectorLaps?.[i] ?? null };
    });
    sectors.set(d.classId, s);
    const best = laps.get(d.classId);
    if (d.bestLap != null && d.bestLap > 0 && (best == null || d.bestLap < best.time)) laps.set(d.classId, { time: d.bestLap, driver: d });
    else if (!laps.has(d.classId)) laps.set(d.classId, null);
  }
  return { sectors, laps };
}

function sectorTime(v: number | null | undefined) {
  if (v == null || v <= 0) return '—';
  return v >= 60 ? lapTime(v) : v.toFixed(3);
}

// ───────────────────────────── Bandeau du haut ─────────────────────────────

export function TimingHeader({ snapshot }: { snapshot: RaceSnapshot }) {
  const s = snapshot;
  const me = s.drivers.find((d) => d.isPlayer);
  const classId = me?.classId ?? s.drivers[0]?.classId;
  const { sectors, laps } = useMemo(() => classBests(s.drivers), [s.drivers]);
  const bestSectors = classId != null ? sectors.get(classId) ?? [null, null, null] : [null, null, null];
  const bestLap = classId != null ? laps.get(classId) : null;
  const remaining = s.lengthType === 'Laps' && s.totalLaps > 0 ? `Tour ${Math.min(s.leaderLap, s.totalLaps)}/${s.totalLaps}` : `Temps : ${clock(s.timeRemaining)}`;
  const km = s.track.length > 0 ? `${(s.track.length / 1000).toFixed(2)} km` : '';
  const className = s.isMultiClass && me?.className ? ` ${me.className}` : '';

  return (
    <header className="tm-head">
      <div className="tm-box tm-session">
        <div className="tm-session-top">
          <span className="tm-session-type">{sessionLabel(s.sessionType)}</span>
          <span className="tm-track" title={s.track.layout ?? undefined}>{s.track.name ?? 'Circuit'}</span>
        </div>
        <div className="tm-session-sub">{km}{s.source ? ` · ${s.source}` : ''}</div>
      </div>
      <div className="tm-box tm-weather">
        <span className="tm-wx" title="Température de l'air"><Icon name="air" /><b className="tnum">{num(s.weather.air, 1)} °C</b></span>
        <span className="tm-wx" title="Température de la piste"><Icon name="track" /><b className="tnum">{num(s.weather.trackTemp, 1)} °C</b></span>
        <span className="tm-wx" title="Pluie"><Icon name="rain" /><b className="tnum">{num(s.weather.rain, 0)} %</b></span>
      </div>
      <div className="tm-box tm-deltas">
        <DeltaBar label="Δ meilleur tour perso" value={s.player?.deltaBest ?? null} />
        <DeltaBar label="Δ tour précédent" value={s.player?.deltaLast ?? null} />
      </div>
      <div className="tm-box tm-best tm-sectors" title={`Meilleurs secteurs${className}`}>
        {bestSectors.map((b, i) => (
          <div key={i} className="tm-best-line">
            <span className="muted">S{i + 1} :</span>{' '}
            {b ? <><span className="tm-best-name">{b.driver.name}</span><span className="muted">{b.lap ? ` (T${b.lap})` : ''}</span> <b className="tnum">{sectorTime(b.time)}</b></> : <span className="muted">—</span>}
          </div>
        ))}
      </div>
      <div className="tm-box tm-best tm-bestlap">
        <div className="tm-best-line tm-bestlap-line">
          {bestLap ? <><span className="tm-best-name">{bestLap.driver.name}</span><span className="muted">{bestLap.driver.bestLapNumber ? ` (T${bestLap.driver.bestLapNumber})` : ''}</span> <b className="tnum">{lapTime(bestLap.time)}</b></> : <span className="muted">Pas encore de meilleur tour</span>}
        </div>
        <div className="tm-remaining tnum">{remaining}</div>
        {(s.flags.length > 0 || (s.safetyCar && s.safetyCar !== 'None')) && (
          <div className="tm-flags">
            {s.flags.map((f) => <span key={f} className="pill flag">⚑ {flagLabel(f)}</span>)}
            {s.safetyCar && s.safetyCar !== 'None' && <span className="pill flag">Safety car</span>}
          </div>
        )}
      </div>
    </header>
  );
}

function DeltaBar({ label, value }: { label: string; value: number | null }) {
  const max = 2; // ± 2 s sur toute la largeur
  const pct = value == null ? 0 : Math.max(-1, Math.min(1, value / max)) * 50;
  const kind = value == null ? '' : value < 0 ? 'faster' : value > 0 ? 'slower' : '';
  return (
    <div className="tm-delta">
      <div className="tm-delta-label">{label}</div>
      <div className="tm-delta-track">
        <span className="tm-delta-mid" />
        {value != null && <span className={`tm-delta-fill ${kind}`} style={pct < 0 ? { right: '50%', width: `${-pct}%` } : { left: '50%', width: `${pct}%` }} />}
        <span className={`tm-delta-value tnum ${kind}`}>{value == null ? '—' : `${value > 0 ? '+' : value < 0 ? '−' : ''}${Math.abs(value).toFixed(2)}`}</span>
      </div>
    </div>
  );
}

// ───────────────────────────── Tableau de chronométrage ─────────────────────────────

interface GridProps {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (d: DriverRow) => void;
  classView: ClassView;
}

export function TimingGrid({ snapshot, classColor, onSelect, classView }: GridProps) {
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const { rows, traffic } = visibleDrivers(snapshot, classView);
  const { sectors, laps } = useMemo(() => classBests(snapshot.drivers), [snapshot.drivers]);
  const isRace = snapshot.sessionType === 'Race';
  const multiClass = new Set(snapshot.drivers.map((d) => d.classId)).size > 1;
  const classPositions = multiClass && classView !== 'all';
  const myDistance = me ? me.laps + me.lapFraction : null;

  return (
    <div className="tm-grid-wrap">
      <table className="tm-grid">
        <thead>
          <tr>
            <th className="c">#</th>
            <th className="tm-col-num c">N°</th>
            <th>Pilote</th>
            <th className="tm-col-car">Voiture</th>
            {multiClass && <th className="tm-col-class">Catégorie</th>}
            <th className="c">Tours</th>
            <th className="r">Dernier tour</th>
            <th className="r tm-col-sector">S1</th>
            <th className="r tm-col-sector">S2</th>
            <th className="r tm-col-sector">S3</th>
            <th className="r">Meilleur tour</th>
            <th className="r tm-col-current">Tour en cours</th>
            <th className="tm-col-pits">Stands</th>
            <th className="tm-col-tyre">Pneus</th>
            <th className="r">{isRace ? 'Écart' : 'Écart meilleur'}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((d) => {
            const cs = sectors.get(d.classId) ?? [null, null, null];
            const cl = laps.get(d.classId);
            const lastBestClass = same(d.lastLap, cl?.time);
            const bestClass = same(d.bestLap, cl?.time);
            let lapState = '';
            if (isRace && myDistance != null && !d.isPlayer) {
              const diff = d.laps + d.lapFraction - myDistance;
              if (diff <= -1) lapState = 'lapped';
              else if (diff >= 1) lapState = 'lapping';
            }
            const pb = !d.isPlayer && d.lastLapIsPersonalBest;
            const pos = classPositions ? d.classPosition : d.position;
            const tyre = compoundInfo(d.tyres.compound);
            const gapText = isRace
              ? (multiClass ? gap(d.gapToClassLeader, d.gapToClassLeaderLaps) : gap(d.gapToLeader, d.gapToLeaderLaps))
              : (bestClass ? '—' : d.bestLap != null && cl ? gap(d.bestLap - cl.time) : '—');
            return (
              <tr
                key={d.id}
                className={`${d.isPlayer ? 'me' : ''} ${pb ? 'pb' : ''} ${traffic.has(d.id) ? 'traffic' : ''} ${lapState}`}
                onClick={() => onSelect(d)}
              >
                <td className="c tnum tm-pos">{pos}</td>
                <td className="c tnum tm-col-num muted">{d.number ?? ''}</td>
                <td className="tm-name">
                  <span className="tm-name-in">{d.name}</span>
                  {d.positionsGained !== 0 && isRace && <span className={`tm-gain ${d.positionsGained > 0 ? 'up' : 'down'}`}>{d.positionsGained > 0 ? '▲' : '▼'}{Math.abs(d.positionsGained)}</span>}
                </td>
                <td className="tm-col-car"><span className="tm-car"><BrandLogo car={d.car} team={d.team} size={14} /><span>{d.car ?? '—'}</span></span></td>
                {multiClass && <td className="tm-col-class"><span className="tm-class" style={{ borderColor: classColor(d.classId) }}>{d.className ?? d.classId}</span></td>}
                <td className="c tnum">{d.laps}</td>
                <td className={`r tnum ${lastBestClass ? 'cell-purple' : d.lastLapIsPersonalBest ? 'cell-green' : ''}`}>{lapTime(d.lastLap)}</td>
                {[0, 1, 2].map((i) => {
                  const v = d.lastSectors?.[i];
                  const cls = same(v, cs[i]?.time) ? 'cell-purple' : same(v, d.bestSectors?.[i]) ? 'cell-green' : '';
                  return <td key={i} className={`r tnum tm-col-sector ${cls}`}>{sectorTime(v)}</td>;
                })}
                <td className={`r tnum ${bestClass ? 'cell-purple' : ''}`}>
                  {d.bestLap ? <>{d.bestLapNumber ? <span className="tm-lapno">T{d.bestLapNumber}/</span> : null}{lapTime(d.bestLap)}</> : '—'}
                </td>
                <td className={`r tnum tm-col-current ${d.currentLapValid ? '' : 'invalid'}`} title={d.currentLapValid ? undefined : 'Tour invalidé'}>{lapTime(d.currentLap)}</td>
                <td className="tm-col-pits">
                  {d.inPits ? <span className="tm-inpit">Au stand</span> : d.finishStatus === 'Finished' ? <span>Arrivé</span> : <span className="muted">En piste</span>}
                  {d.pitStops > 0 && <span className="muted"> · {d.pitStops}</span>}
                </td>
                <td className="tm-col-tyre">
                  <span className="tyre" title={`${tyre.label} — ${d.tyres.stintLaps} tour(s)`}>
                    <span className={`tyre-dot ${tyre.kind}`}>{tyre.letter}</span>
                    <span className="tnum secondary">{d.tyres.stintLaps}</span>
                  </span>
                  {d.nextStop && <span className="muted tnum tm-nextstop" title={d.nextStop.reason}> → T{d.nextStop.lap}</span>}
                </td>
                <td className="r tnum">{gapText}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// ───────────────────────────── État de la voiture (bas gauche) ─────────────────────────────

function WheelBox({ w, left, th }: { w: WheelStatus; left: boolean; th?: TyreThermal }) {
  // Surface : extérieur, milieu, intérieur vus de dessus (l'extérieur est du côté de la carrosserie).
  const temps = left ? [w.tempOuter, w.tempMiddle, w.tempInner] : [w.tempInner, w.tempMiddle, w.tempOuter];
  const c = compoundInfo(w.compound);
  const wear = w.wearPct ?? 0;
  const life = Math.max(0, 100 - wear);
  const tColor = (v: number | null) => thermalColor(thermal(v, w.idealTemp, w.tempWindow));
  const pressureOff = w.pressure != null && w.idealPressure != null && Math.abs(w.pressure - w.idealPressure) > 5;
  const alarm = th && (th.hotSeconds >= 30 || th.lastLapState === 'chaud');
  return (
    <div className={`tm-wheel ${alarm ? 'alarm' : ''}`} title={`${w.position} — ${c.label}${th?.lastLapAverage != null ? ` — moyenne du dernier tour ${th.lastLapAverage.toFixed(0)}°` : ''}`}>
      <div className="tm-wheel-temps">
        {temps.map((v, i) => <span key={i} className="tnum" style={{ background: tColor(v) }}>{v != null ? v.toFixed(0) : '—'}</span>)}
      </div>
      <div className="tm-wheel-mid">
        <div className="tm-wear" title={`Usure ${wear.toFixed(0)} %`}>
          <span className="tm-wear-fill" style={{ width: `${life}%` }} />
          <span className="tm-wear-text"><span className={`tyre-dot ${c.kind}`}>{c.letter}</span> {life.toFixed(0)} %</span>
        </div>
        <span className="tm-core tnum" style={{ background: thermalColor(thermal(w.core, w.coreIdeal, w.coreWindow)) }} title="Carcasse">{w.core != null ? w.core.toFixed(0) : '—'}</span>
      </div>
      <div className="tm-wheel-bottom">
        <span className="tnum" title="Freins" style={{ color: w.brake != null ? thermalColor(thermal(w.brake, w.brakeIdeal, w.brakeWindow)) : undefined }}>
          <Icon name="brake" /> {w.brake != null ? `${w.brake.toFixed(0)}°` : '—'}
        </span>
        <span className={`tnum ${pressureOff ? 'warn-text' : ''}`} title={`Pression${w.idealPressure != null ? ` (idéal ${w.idealPressure.toFixed(0)} kPa)` : ''}`}>
          {w.pressure != null ? `${w.pressure.toFixed(0)} kPa` : '—'}
        </span>
      </div>
      {th?.lastLapAverage != null && (
        <div className={`tm-wheel-avg ${th.lastLapState === 'chaud' ? 'hot' : th.lastLapState === 'froid' ? 'cold' : ''}`}>
          moy. tour {th.lastLapAverage.toFixed(0)}°{th.hotSeconds >= 30 ? ` · chaud depuis ${th.hotSeconds.toFixed(0)} s` : ''}
        </div>
      )}
    </div>
  );
}

export function CarStatus({ snapshot }: { snapshot: RaceSnapshot }) {
  const p = snapshot.player;
  if (!p) return <div className="tm-panel tm-car-status muted" style={{ padding: 12 }}>Pas de voiture joueur (mode spectateur).</div>;
  const byPos = new Map(p.wheels.map((w) => [w.position, w]));
  const fuel = p.fuel;
  const e = p.engine;
  const timeLeft = fuel?.lapsLeft != null && p.averageLap != null ? fuel.lapsLeft * p.averageLap : null;
  return (
    <div className="tm-panel tm-car-status">
      <div className="tm-wheels">
        {(['FL', 'FR', 'RL', 'RR'] as const).map((pos) => {
          const w = byPos.get(pos);
          const th = p.tyreThermal?.find((t) => t.position === pos);
          return w ? <WheelBox key={pos} w={w} left={pos === 'FL' || pos === 'RL'} th={th} /> : <div key={pos} className="tm-wheel muted">—</div>;
        })}
      </div>
      <div className="tm-fuel">
        <div className="tm-fuel-main">
          <Icon name="fuel" />
          <div>
            <div className="tm-label">Carburant</div>
            <div className="tm-big tnum">{fuel ? num(fuel.remaining, 1, ' L') : p.energy ? num(p.energy.remainingPct, 0, ' %') : '—'}</div>
          </div>
        </div>
        <div className="tm-kv"><span>Tours</span><b className="tnum">{num(fuel?.lapsLeft ?? p.energy?.lapsLeft, 1)}</b></div>
        <div className="tm-kv"><span>Temps</span><b className="tnum">{clock(timeLeft)}</b></div>
        <div className="tm-kv"><span>Moy./tour</span><b className="tnum">{fuel?.perLap != null ? `${fuel.perLap.toFixed(2)} L` : p.energy?.perLapPct != null ? `${p.energy.perLapPct.toFixed(1)} %` : '—'}</b></div>
        {fuel?.toAdd != null && fuel.toAdd > 0 && <div className="tm-kv"><span>À ajouter</span><b className="tnum">{fuel.toAdd.toFixed(1)} L</b></div>}
        {p.recommendedStopLap != null && <div className="tm-kv"><span>Arrêt conseillé</span><b className="tnum">T{p.recommendedStopLap}</b></div>}
      </div>
      <div className="tm-engine">
        <div className="tm-gear"><span className="tm-gear-val">{e?.gear ?? 'N'}</span><span className="tnum muted">{num(p.speed, 0, ' km/h')}</span></div>
        <div className="tm-kv"><span>Régime</span><b className="tnum">{num(e?.rpm, 0)}</b></div>
        <div className="tm-kv" title="Eau"><span><Icon name="water" /> Eau</span><b className="tnum" style={{ color: e?.waterTemp != null ? thermalColor(thermal(e.waterTemp, e.waterIdeal, e.waterWindow)) : undefined }}>{num(e?.waterTemp, 0, ' °C')}</b></div>
        <div className="tm-kv" title="Huile"><span><Icon name="oil" /> Huile</span><b className="tnum" style={{ color: e?.oilTemp != null ? thermalColor(thermal(e.oilTemp, e.oilIdeal, e.oilWindow)) : undefined }}>{num(e?.oilTemp, 0, ' °C')}</b></div>
        {e?.oilPressure != null && <div className="tm-kv"><span>Pression huile</span><b className="tnum">{e.oilPressure.toFixed(0)} kPa</b></div>}
      </div>
      {(p.damage ?? []).some((d) => d.percent >= 1) && <div className="tm-fuel tm-damage-box"><div className="tm-label">Dégâts</div><DamageList player={p} compact /></div>}
    </div>
  );
}

// ───────────────────────────── Écran complet ─────────────────────────────

export function TimingScreen(props: GridProps & { setClassView: (v: ClassView) => void }) {
  const { snapshot, classColor, onSelect, classView, setClassView } = props;
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const classes = useMemo(() => {
    const seen = new Map<string, string>();
    for (const d of snapshot.drivers) if (!seen.has(d.classId)) seen.set(d.classId, d.className ?? d.classId);
    return [...seen.entries()];
  }, [snapshot.drivers]);
  return (
    <div className="tm-screen">
      {classes.length > 1 && (
        <div className="tm-classbar" role="group" aria-label="Catégories affichées">
          {me && <button className={`chip ${classView === 'mine' ? 'on' : ''}`} onClick={() => setClassView('mine')} title="Votre catégorie, plus les autres voitures à moins de 5 s de vous sur la piste">Ma catégorie</button>}
          <button className={`chip ${classView === 'all' ? 'on' : ''}`} onClick={() => setClassView('all')}>Toutes</button>
          {classes.filter(([id]) => id !== me?.classId).map(([id, name]) => (
            <button key={id} className={`chip ${classView === id ? 'on' : ''}`} onClick={() => setClassView(classView === id ? 'mine' : id)}>
              <span className="swatch" style={{ background: classColor(id) }} />{name}
            </button>
          ))}
        </div>
      )}
      <TimingGrid snapshot={snapshot} classColor={classColor} onSelect={onSelect} classView={classView} />
      <div className="tm-bottom">
        <CarStatus snapshot={snapshot} />
        <div className="tm-panel tm-map"><TrackMap snapshot={snapshot} classColor={classColor} onSelect={onSelect} classView={classView} /></div>
        <div className="tm-side">
          {snapshot.sessionType === 'Race'
            ? <><BattlePanel snapshot={snapshot} onSelect={onSelect} /><NextStop snapshot={snapshot} /></>
            : <MyCarPanel snapshot={snapshot} />}
        </div>
      </div>
    </div>
  );
}

function NextStop({ snapshot }: { snapshot: RaceSnapshot }) {
  const p = snapshot.player;
  if (!p) return null;
  return (
    <div className="tm-panel tm-strategy">
      {(p.alerts ?? []).map((a) => <div key={a} className="tm-alert" role="alert">⚠ {a}</div>)}
      <div className="tm-panel-title">Stratégie</div>
      <div className="tm-kv"><span>Tours restants</span><b className="tnum">{p.lapsRemaining != null ? `≈ ${p.lapsRemaining.toFixed(1)}` : '—'}</b></div>
      <div className="tm-kv"><span>Relais</span><b className="tnum">{p.stintLaps} tours</b></div>
      <div className="tm-kv"><span>Rythme moyen</span><b className="tnum">{lapTime(p.averageLap)}</b></div>
      <div className="tm-kv"><span>Dégradation</span><b className="tnum">{p.degradation != null ? `${p.degradation > 0 ? '+' : ''}${p.degradation.toFixed(2)} s/tour` : '—'}</b></div>
      {p.recommendedStopLap != null && <div className="tm-callout">Arrêt conseillé <b>tour {p.recommendedStopLap}</b><div className="muted">{p.recommendedStopReason}</div></div>}
      <PitPlanCard player={p} compact />
    </div>
  );
}

/** Essais et qualification : la voiture plutôt que la course (meilleur tour, pneus sur le tour, pressions). */
function MyCarPanel({ snapshot }: { snapshot: RaceSnapshot }) {
  const p = snapshot.player;
  const me = snapshot.drivers.find((d) => d.isPlayer);
  if (!p || !me) return null;
  const classBest = Math.min(...snapshot.drivers.filter((d) => d.classId === me.classId && d.bestLap).map((d) => d.bestLap!));
  const label = (pos: string) => ({ FL: 'AvG', FR: 'AvD', RL: 'ArG', RR: 'ArD' } as Record<string, string>)[pos] ?? pos;
  return (
    <div className="tm-panel tm-strategy">
      {(p.alerts ?? []).map((a) => <div key={a} className="tm-alert" role="alert">⚠ {a}</div>)}
      <div className="tm-panel-title">{snapshot.sessionType === 'Qualification' ? 'Qualification' : 'Essais'} · ma voiture</div>
      <div className="tm-kv"><span>Meilleur tour</span><b className="tnum">{lapTime(me.bestLap)}{me.bestLapNumber ? <span className="muted"> (T{me.bestLapNumber})</span> : null}</b></div>
      <div className="tm-kv"><span>Écart au meilleur de la catégorie</span><b className="tnum">{me.bestLap && isFinite(classBest) ? (me.bestLap - classBest < 0.0005 ? '—' : `+${(me.bestLap - classBest).toFixed(3)}`) : '—'}</b></div>
      <div className="tm-kv"><span>Dernier tour</span><b className="tnum">{lapTime(me.lastLap)}</b></div>
      <div className="tm-panel-title" style={{ marginTop: 6 }}>Pneus sur le dernier tour</div>
      {(p.tyreThermal ?? []).map((t) => {
        const w = p.wheels.find((x) => x.position === t.position);
        const dp = w?.pressure != null && w.idealPressure != null ? w.pressure - w.idealPressure : null;
        return (
          <div key={t.position} className="tm-kv">
            <span>{label(t.position)}</span>
            <b className="tnum">
              <span style={{ color: t.lastLapState === 'chaud' ? 'var(--critical)' : t.lastLapState === 'froid' ? 'var(--temp-cold)' : undefined }}>{t.lastLapAverage != null ? `${t.lastLapAverage.toFixed(0)}°` : '—'}</span>
              {dp != null && <span className={Math.abs(dp) >= 5 ? 'warn-text' : 'muted'}> · {dp > 0 ? '+' : ''}{dp.toFixed(0)} kPa</span>}
            </b>
          </div>
        );
      })}
    </div>
  );
}

// ───────────────────────────── Barre d'état (bas) ─────────────────────────────

export function StatusBar({ snapshot, link, version, status, onConnection }: {
  snapshot: RaceSnapshot | null; link: string; version: string | null; status: HubStatus | null; onConnection: () => void;
}) {
  const s = snapshot?.hasData ? snapshot : null;
  const stage = linkStage(status, snapshot);
  const pc = collectorAddress(status);
  const game = status?.source.receiver?.game;
  const [linkColor, linkText] = link !== 'open'
    ? ['var(--critical)', 'SecondMonitor injoignable']
    : stage === 'ok' ? ['var(--good)', `PC de jeu ${pc ?? ''} · données reçues`]
      : stage === 'demo' ? ['var(--good)', 'Démonstration']
        : stage === 'replay' ? ['var(--good)', 'Relecture']
          : stage === 'no-data' ? ['var(--warning)', `${game ?? 'Jeu'} ouvert, en attente de données`]
            : stage === 'no-game' ? ['var(--warning)', `Collecteur connecté (${pc}) · lancez le jeu`]
              : ['var(--warning)', 'PC de jeu non connecté'];
  const progress = s && s.timeRemaining != null && s.sessionTime > 0 ? Math.min(1, s.sessionTime / (s.sessionTime + s.timeRemaining)) : s && s.totalLaps > 0 ? Math.min(1, s.leaderLap / s.totalLaps) : 0;
  return (
    <footer className="tm-status">
      <span className="tm-status-item"><b>SecondMonitor</b>{version ? <span className="muted"> {version}</span> : null}</span>
      {s && <span className="tm-status-item">{s.source ?? 'Jeu'}</span>}
      {s && <span className="tm-status-item tnum">{clock(s.sessionTime)}</span>}
      <span className="tm-progress" aria-hidden><span style={{ width: `${progress * 100}%` }} /></span>
      {s && s.dataRate.playerHz > 0 && <span className="tm-status-item muted tnum hide-sm" title="Fréquence des données reçues">vous {s.dataRate.playerHz} Hz · adversaires {s.dataRate.opponentsHz} Hz</span>}
      <button className="tm-status-item tm-status-link" onClick={onConnection} title="Connexion au PC de jeu">
        <span className="dot" style={{ background: linkColor }} /> {linkText}
        {status?.source.receiver?.kbPerSecond ? <span className="muted tnum"> · {status.source.receiver.kbPerSecond.toLocaleString('fr-FR')} Ko/s</span> : null}
      </button>
    </footer>
  );
}

// ───────────────────────────── Icônes (traits simples, comme l'original) ─────────────────────────────

const PATHS: Record<string, string> = {
  timing: 'M4 6h16M4 12h16M4 18h16M8 3v18',
  race: 'M5 21V4m0 0h12l-2 4 2 4H5',
  analysis: 'M3 20h18M5 16l4-5 4 3 6-8',
  speeds: 'M12 14l4-4M4 18a8 8 0 1 1 16 0',
  sectors: 'M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4zM13 13h7v7h-7z',
  car: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zm0 5a4 4 0 1 0 0 8 4 4 0 0 0 0-8z',
  weather: 'M7 18h10a4 4 0 0 0 0-8 6 6 0 0 0-11.5 1.5A3.5 3.5 0 0 0 7 18z',
  strategy: 'M12 7v5l3 2M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z',
  ai: 'M12 3a3 3 0 0 0-3 3v5a3 3 0 0 0 6 0V6a3 3 0 0 0-3-3zM5 11a7 7 0 0 0 14 0M12 18v3',
  link: 'M9 15l6-6M10 6l1.5-1.5a4.2 4.2 0 0 1 6 6L16 12M14 18l-1.5 1.5a4.2 4.2 0 0 1-6-6L8 12',
  settings: 'M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6zM12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1 7 17M17 7l2.1-2.1',
  air: 'M3 8h11a3 3 0 1 0-3-3M3 12h15a3 3 0 1 1-3 3M3 16h7',
  track: 'M4 20 10 4h4l6 16M12 7v2M12 12v2M12 17v2',
  rain: 'M7 14h10a4 4 0 0 0 0-8 6 6 0 0 0-11.5 1.5A3.5 3.5 0 0 0 7 14zM8 17l-1 3M12 17l-1 3M16 17l-1 3',
  brake: 'M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16zm0 5a3 3 0 1 0 0 6 3 3 0 0 0 0-6z',
  fuel: 'M5 21V5a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v16M3 21h14M7 8h6M15 10h2a2 2 0 0 1 2 2v5a1 1 0 0 0 2 0V9l-3-3',
  water: 'M12 3s6 7 6 11a6 6 0 0 1-12 0c0-4 6-11 6-11z',
  oil: 'M4 14h10l4-4h3M4 14v4h10l3-3M7 10V7h4v3',
};

export function Icon({ name, size = 16 }: { name: string; size?: number }) {
  return (
    <svg className="tm-icon" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      <path d={PATHS[name] ?? ''} />
    </svg>
  );
}

