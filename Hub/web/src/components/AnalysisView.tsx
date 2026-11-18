import { useMemo } from 'react';
import { usePoll } from '../live';
import { delta, lapTick, lapTime, num, WHEELS, WHEEL_LABEL } from '../format';
import type { DriverLaps, DriverRow, PlayerLap, RaceSnapshot } from '../types';
import type { Selection } from '../selection';
import { LineChart, type Series } from '../charts/LineChart';
import { DriverPicker } from './DriverPicker';
import { TyreBadge } from './Common';
import { seriesPoints, useTelemetry } from '../telemetry';

const WHEEL_COLORS = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-4)'];

interface Props {
  snapshot: RaceSnapshot;
  selection: Selection;
  onSelect: (d: DriverRow) => void;
}

/** Temps au tour comparés, dégradation des pneus par tour et prévisions (rythme, usure, carburant, duels). */
export function AnalysisView({ snapshot, selection, onSelect }: Props) {
  const { data: laps } = usePoll<DriverLaps[]>('/api/analysis/laps', 5000);
  const { data: myLaps } = usePoll<PlayerLap[]>('/api/player/laps', 5000);
  const telemetry = useTelemetry();
  const byId = useMemo(() => new Map(snapshot.drivers.map((d) => [d.id, d])), [snapshot.drivers]);
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const p = snapshot.player;

  return (
    <div className="stack">
      <section className="card">
        <div className="card-head">
          <span className="card-title">Temps au tour</span>
          <span className="muted hide-xs">tours propres · pointillés = projection du relais en cours</span>
        </div>
        <div className="card-body stack-sm">
          <DriverPicker snapshot={snapshot} selection={selection} />
          <LapTimesChart laps={laps} selection={selection} byId={byId} />
        </div>
      </section>

      <div className="grid-2">
        <section className="card">
          <div className="card-head"><span className="card-title">Rythme et dégradation</span><span className="muted">relais en cours</span></div>
          <div className="card-body table-scroll">
            <PaceTable laps={laps} selection={selection} byId={byId} onSelect={onSelect} />
          </div>
        </section>
        <section className="card">
          <div className="card-head"><span className="card-title">Prévisions</span><span className="muted">au rythme actuel</span></div>
          <div className="card-body">
            <Forecasts snapshot={snapshot} />
          </div>
        </section>
      </div>

      {p && p.wheels.length === 4 && (
        <div className="grid-2">
          <section className="card">
            <div className="card-head"><span className="card-title">Usure de mes pneus</span><span className="muted">par tour, avec projection</span></div>
            <div className="card-body">
              <WearChart telemetry={telemetry} snapshot={snapshot} />
            </div>
          </section>
          <section className="card">
            <div className="card-head"><span className="card-title">Carburant</span><span className="muted">et projection jusqu'à l'arrêt / l'arrivée</span></div>
            <div className="card-body">
              <FuelChart telemetry={telemetry} snapshot={snapshot} />
            </div>
          </section>
        </div>
      )}

      {me && (
        <section className="card">
          <div className="card-head"><span className="card-title">Mes tours</span><span className="muted">temps, consommation, usure prise sur le tour</span></div>
          <div className="card-body table-scroll">
            <MyLapsTable laps={myLaps ?? []} best={me.bestLap} />
          </div>
        </section>
      )}
    </div>
  );
}

function LapTimesChart({ laps, selection, byId }: { laps: DriverLaps[] | null; selection: Selection; byId: Map<string, DriverRow> }) {
  const { series, domain } = useMemo(() => {
    const out: Series[] = [];
    const all: number[] = [];
    for (const id of selection.ids) {
      const d = byId.get(id);
      const l = laps?.find((x) => x.id === id);
      if (!d || !l) continue;
      const color = selection.colorOf(id);
      const pts = l.laps.map(([lap, time, flags]) => ({ x: lap, y: time != null && (flags & 3) === 0 ? time : null }));
      pts.forEach((p) => p.y != null && all.push(p.y));
      const label = d.isPlayer ? 'Vous' : `#${d.number ?? '–'} ${d.shortName}`;
      out.push({ id, label, color, points: pts });
      if (l.fit && d.laps >= l.fit.fromLap) {
        const end = Math.max(d.laps + 3, d.nextStop?.lap ?? d.laps + 8);
        const proj = [];
        for (let lap = d.laps; lap <= end; lap++) proj.push({ x: lap, y: l.fit.slope * lap + l.fit.intercept });
        out.push({ id: `${id}-proj`, label: `${label} (projection)`, color, points: proj, dashed: true, companion: true });
      }
      const pitLaps = l.laps.filter(([, , f]) => (f & 1) !== 0).map(([lap]) => lap);
      if (pitLaps.length) out[out.length - (l.fit && d.laps >= l.fit.fromLap ? 2 : 1)].markers = pitLaps.map((lap) => ({ x: lap, y: Number.NaN, title: `${label} : arrêt au tour ${lap}` }));
    }
    if (all.length < 2) return { series: out, domain: undefined };
    const sorted = [...all].sort((a, b) => a - b);
    const lo = sorted[0];
    const med = sorted[Math.floor(sorted.length / 2)];
    const hi = Math.min(sorted[sorted.length - 1], med * 1.05);
    const domain: [number, number] = [Math.floor((lo - 0.4) * 2) / 2, Math.ceil((hi + 0.4) * 2) / 2];
    for (const s of out) if (s.markers) s.markers = s.markers.map((m) => ({ ...m, y: domain[1] }));
    return { series: out, domain };
  }, [laps, selection, byId]);

  return (
    <LineChart
      series={series}
      height={300}
      yDomain={domain}
      xFormat={lapTick}
      xTitle={(x) => `Tour ${Math.round(x)}`}
      yFormat={(y) => lapTime(y).replace(/\.\d+$/, (m) => m.slice(0, 2))}
      valueFormat={lapTime}
      ariaLabel="Temps au tour des pilotes comparés"
      emptyText="Les temps apparaissent après deux tours chronométrés."
      footer={<div className="chart-note muted">Les points en haut du graphique marquent les arrêts au stand. Tours au stand, de sortie et invalides exclus.</div>}
    />
  );
}

function PaceTable({ laps, selection, byId, onSelect }: { laps: DriverLaps[] | null; selection: Selection; byId: Map<string, DriverRow>; onSelect: (d: DriverRow) => void }) {
  const rows = selection.ids.map((id) => byId.get(id)).filter((d): d is DriverRow => !!d);
  const me = rows.find((d) => d.isPlayer);
  return (
    <table className="mini-table">
      <thead>
        <tr><th>Pilote</th><th>Pneus</th><th className="r">Rythme</th><th className="r" title="Perte de temps par tour sur le relais">Dégr./tour</th><th className="r" title="Temps prévu dans 5 tours">Dans 5 tours</th><th className="r hide-sm">vs vous</th></tr>
      </thead>
      <tbody>
        {rows.map((d) => {
          const fit = laps?.find((l) => l.id === d.id)?.fit;
          const in5 = fit ? fit.slope * (d.laps + 5) + fit.intercept : null;
          const vsMe = me && d !== me && d.paceLast3 != null && me.paceLast3 != null ? d.paceLast3 - me.paceLast3 : null;
          return (
            <tr key={d.id} onClick={() => onSelect(d)} className="clickable">
              <td><span className="legend-line" style={{ borderColor: selection.colorOf(d.id) }} /> {d.isPlayer ? <b>Vous</b> : `#${d.number ?? '–'} ${d.shortName}`}</td>
              <td><TyreBadge compound={d.tyres.compound} laps={d.tyres.stintLaps} /></td>
              <td className="r tnum">{lapTime(d.paceLast3)}</td>
              <td className="r tnum">{d.degradation != null ? `${delta(d.degradation, 2)} s` : '—'}</td>
              <td className="r tnum">{lapTime(in5)}</td>
              <td className="r tnum hide-sm">{vsMe != null ? <span title={vsMe > 0 ? 'Plus lent que vous' : 'Plus rapide que vous'}>{delta(vsMe, 2)} s</span> : '—'}</td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function Forecasts({ snapshot }: { snapshot: RaceSnapshot }) {
  const p = snapshot.player;
  const me = snapshot.drivers.find((d) => d.isPlayer);
  if (!p || !me) return <div className="muted">Aucune voiture joueur.</div>;
  const remaining = p.lapsRemaining;
  const worst = [...p.wheels].sort((a, b) => b.wearPct - a.wearPct)[0];
  const wearAtFinish = worst?.wearPerLapPct != null && remaining != null ? worst.wearPct + worst.wearPerLapPct * remaining : null;
  const fuelAtFinish = p.fuel?.perLap != null && remaining != null ? p.fuel.remaining - p.fuel.perLap * remaining : null;
  const items: { label: string; value: string; sub?: string; tone?: 'warn' | 'bad' }[] = [];
  if (remaining != null) items.push({ label: 'Tours restants', value: num(remaining, 1), sub: p.averageLap ? `rythme ${lapTime(p.averageLap)}` : undefined });
  if (p.recommendedStopLap != null) items.push({ label: 'Prochain arrêt', value: `T${p.recommendedStopLap}`, sub: `dans ${Math.max(0, p.recommendedStopLap - me.laps)} tours · ${p.recommendedStopReason}`, tone: p.recommendedStopLap - me.laps <= 1 ? 'warn' : undefined });
  if (fuelAtFinish != null) items.push({ label: "Carburant à l'arrivée", value: `${num(fuelAtFinish, 1)} L`, sub: fuelAtFinish < 0 ? 'sans ravitailler : panne' : 'sans ravitailler', tone: fuelAtFinish < 0 ? 'bad' : undefined });
  if (wearAtFinish != null && worst) items.push({ label: "Usure à l'arrivée", value: `${num(Math.min(100, wearAtFinish), 0)} %`, sub: `${WHEEL_LABEL[worst.position].toLowerCase()} · limite ${num(worst.heavyWearLimitPct, 0)} %`, tone: wearAtFinish > worst.heavyWearLimitPct ? 'warn' : undefined });
  if (p.degradation != null && p.averageLap != null) items.push({ label: 'Mon rythme dans 5 tours', value: lapTime(p.averageLap + p.degradation * 5), sub: `${delta(p.degradation, 2)} s/tour` });
  for (const b of p.battles) {
    const who = `#${b.number ?? '–'} ${b.name}`;
    items.push({
      label: b.side === 'ahead' ? 'Voiture devant' : 'Voiture derrière',
      value: b.lapsToContact != null ? (b.lapsToContact < 1 ? 'contact ce tour' : `contact ~${Math.ceil(b.lapsToContact)} t.`) : b.gap != null ? `${num(b.gap, 1)} s` : '—',
      sub: `${who} · ${b.paceDelta == null ? 'rythme inconnu' : Math.abs(b.paceDelta) < 0.03 ? 'même rythme' : b.paceDelta > 0 ? `vous gagnez ${num(b.paceDelta, 2)} s/t` : `vous perdez ${num(-b.paceDelta, 2)} s/t`}`,
      tone: b.side === 'behind' && b.lapsToContact != null ? 'warn' : undefined,
    });
  }
  return (
    <div className="tiles">
      {items.map((i) => (
        <div key={i.label} className={`tile ${i.tone ?? ''}`}>
          <div className="tile-label">{i.label}</div>
          <div className="tile-value tnum">{i.value}</div>
          {i.sub && <div className="tile-sub">{i.sub}</div>}
        </div>
      ))}
    </div>
  );
}

function WearChart({ telemetry, snapshot }: { telemetry: ReturnType<typeof useTelemetry>; snapshot: RaceSnapshot }) {
  const p = snapshot.player!;
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const series = useMemo(() => {
    const out: Series[] = [];
    p.wheels.forEach((w, i) => {
      const pts = seriesPoints(telemetry, `wear${w.position}`, null, 'lap');
      out.push({ id: w.position, label: WHEEL_LABEL[w.position], color: WHEEL_COLORS[i], points: pts });
      if (w.wearPerLapPct != null && me) {
        const now = me.laps + me.lapFraction;
        const end = now + Math.min(40, Math.max(3, p.lapsRemaining ?? 10));
        out.push({ id: `${w.position}-p`, label: '', color: WHEEL_COLORS[i], companion: true, dashed: true, points: [{ x: now, y: w.wearPct }, { x: end, y: Math.min(100, w.wearPct + w.wearPerLapPct * (end - now)) }] });
      }
    });
    return out;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [telemetry.version, p, me?.laps]);
  const limit = p.wheels[0]?.heavyWearLimitPct;
  return (
    <LineChart
      series={series}
      height={240}
      xFormat={lapTick}
      xTitle={(x) => `Tour ${x.toFixed(1)}`}
      yFormat={(y) => `${Math.round(y)} %`}
      valueFormat={(y) => `${y.toFixed(1)} %`}
      refLines={[...(limit ? [{ y: limit, label: 'usure forte' }] : []), ...(p.recommendedStopLap ? [{ x: p.recommendedStopLap, label: 'arrêt conseillé' }] : [])]}
      ariaLabel="Usure des pneus par tour"
      emptyText="L'usure apparaît dès que le jeu la transmet (voiture du joueur)."
      footer={
        <div className="wheel-rates">
          {p.wheels.map((w, i) => (
            <span key={w.position} className="tnum"><span className="legend-line" style={{ borderColor: WHEEL_COLORS[i] }} /> {w.position} {w.wearPerLapPct != null ? `${num(w.wearPerLapPct, 2)} %/tour` : '…'}</span>
          ))}
        </div>
      }
    />
  );
}

function FuelChart({ telemetry, snapshot }: { telemetry: ReturnType<typeof useTelemetry>; snapshot: RaceSnapshot }) {
  const p = snapshot.player!;
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const hasEnergy = p.energy != null;
  const series = useMemo(() => {
    const out: Series[] = [];
    const key = hasEnergy ? 'energy' : 'fuel';
    const now = me ? me.laps + me.lapFraction : 0;
    out.push({ id: key, label: hasEnergy ? 'Énergie virtuelle (%)' : 'Carburant (L)', color: 'var(--series-1)', points: seriesPoints(telemetry, key, null, 'lap') });
    const perLap = hasEnergy ? p.energy?.perLapPct : p.fuel?.perLap;
    const left = hasEnergy ? p.energy?.remainingPct : p.fuel?.remaining;
    if (perLap && left != null && me) {
      const end = now + left / perLap;
      out.push({ id: `${key}-p`, label: '', color: 'var(--series-1)', companion: true, dashed: true, points: [{ x: now, y: left }, { x: end, y: 0 }] });
    }
    return out;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [telemetry.version, p, me?.laps]);
  const finishLap = me && p.lapsRemaining != null ? me.laps + me.lapFraction + p.lapsRemaining : null;
  return (
    <LineChart
      series={series}
      height={240}
      legend={false}
      xFormat={lapTick}
      xTitle={(x) => `Tour ${x.toFixed(1)}`}
      yFormat={(y) => (hasEnergy ? `${Math.round(y)} %` : `${Math.round(y)} L`)}
      valueFormat={(y) => (hasEnergy ? `${y.toFixed(1)} %` : `${y.toFixed(1)} L`)}
      refLines={[...(p.recommendedStopLap ? [{ x: p.recommendedStopLap, label: 'arrêt' }] : []), ...(finishLap ? [{ x: finishLap, label: 'arrivée' }] : [])]}
      ariaLabel={hasEnergy ? 'Énergie virtuelle restante par tour' : 'Carburant restant par tour'}
      footer={<div className="chart-note muted">{hasEnergy ? 'Énergie virtuelle' : 'Carburant'} restant ; pointillés : à vide au rythme de consommation actuel.</div>}
    />
  );
}

function MyLapsTable({ laps, best }: { laps: PlayerLap[]; best: number | null }) {
  if (laps.length === 0) return <div className="muted">Pas encore de tour complet.</div>;
  return (
    <table className="mini-table">
      <thead>
        <tr>
          <th>Tour</th><th className="r">Temps</th><th className="r">Δ meilleur</th><th className="r">Carburant</th><th className="r hide-sm">Énergie</th>
          {WHEELS.map((w) => <th key={w} className="r hide-xs" title={`Usure prise sur le tour — ${WHEEL_LABEL[w]}`}>{w}</th>)}
          <th />
        </tr>
      </thead>
      <tbody>
        {[...laps].reverse().slice(0, 60).map((l) => (
          <tr key={l.lap}>
            <td className="tnum">{l.lap}</td>
            <td className={`r tnum ${best != null && l.time != null && Math.abs(l.time - best) < 0.0005 ? 'best-personal' : ''}`}>{lapTime(l.time)}</td>
            <td className="r tnum muted">{best != null && l.time != null ? delta(l.time - best, 3) : '—'}</td>
            <td className="r tnum">{l.fuel != null ? `${l.fuel.toFixed(2)} L` : '—'}</td>
            <td className="r tnum hide-sm">{l.energy != null ? `${l.energy.toFixed(1)} %` : '—'}</td>
            {WHEELS.map((w, i) => <td key={w} className="r tnum hide-xs">{l.wearDelta ? `${l.wearDelta[i].toFixed(2)}` : '—'}</td>)}
            <td className="muted">{l.pit ? 'Stand' : !l.valid ? 'Invalide' : ''}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
