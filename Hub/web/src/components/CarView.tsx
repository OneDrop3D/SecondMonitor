import { useMemo, useState } from 'react';
import { num, thermal, thermalColor, WHEEL_LABEL } from '../format';
import type { EngineDto, RaceSnapshot, WheelStatus } from '../types';
import { LineChart, type Series } from '../charts/LineChart';
import { seriesPoints, useTelemetry, type TelemetryStore } from '../telemetry';
import { TyreBadge } from './Common';
import { Expandable } from '../charts/Expandable';
import { ThermalProfiles } from './ThermalProfiles';

const WHEEL_COLORS = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-4)'];
const WINDOWS: { label: string; sec: number | null }[] = [
  { label: '5 min', sec: 300 },
  { label: '15 min', sec: 900 },
  { label: '1 h', sec: 3600 },
  { label: 'Session', sec: null },
];

/** Télémétrie de ma voiture : pneus (surface, carcasse, pressions, usure), freins, moteur, dégâts, et leur évolution. */
export function CarView({ snapshot }: { snapshot: RaceSnapshot }) {
  const p = snapshot.player;
  const telemetry = useTelemetry();
  const [win, setWin] = useState<number | null>(900);
  const [editRanges, setEditRanges] = useState(false);
  if (!p) return <div className="empty"><h2>Aucune voiture joueur</h2><p>Le jeu ne signale pas de voiture pilotée (mode spectateur ?).</p></div>;
  const wheels = p.wheels;

  return (
    <div className="stack">
      <div className="grid-car">
        <section className="card">
          <div className="card-body">
            <Expandable
              title="Pneus et freins"
              subtitle="surface (côtés et milieu) · carcasse · frein · pression"
              height={0}
              actions={<button className="chip" onClick={() => setEditRanges(!editRanges)}>{editRanges ? 'Fermer les plages' : '🎨 Plages de couleurs'}</button>}
              render={(_, expanded) => (
                <div className={expanded ? 'tyres-expanded' : undefined}>
                  {wheels.length === 4 ? (
                    <div className="car-diagram">
                      {wheels.map((w) => <WheelCard key={w.position} w={w} />)}
                      <div className="car-body" aria-hidden><span>AV</span><span>AR</span></div>
                    </div>
                  ) : <div className="muted">Le jeu ne transmet pas les données des pneus.</div>}
                  <ThermalLegend profile={snapshot.thermalProfile} />
                </div>
              )}
            />
          </div>
        </section>
        <div className="stack">
          <EngineCard engine={p.engine} />
          <section className="card">
            <div className="card-head"><span className="card-title">Piste</span></div>
            <div className="card-body tiles">
              <div className="tile"><div className="tile-label">Piste</div><div className="tile-value tnum">{num(snapshot.weather.trackTemp, 1)} °C</div></div>
              <div className="tile"><div className="tile-label">Air</div><div className="tile-value tnum">{num(snapshot.weather.air, 1)} °C</div></div>
              <div className="tile"><div className="tile-label">Pluie · humidité</div><div className="tile-value tnum">{snapshot.weather.rain} % · {snapshot.weather.wetness} %</div></div>
            </div>
          </section>
        </div>
      </div>

      {editRanges && <ThermalProfiles snapshot={snapshot} />}

      <section className="card">
        <div className="card-head">
          <span className="card-title">Évolution</span>
          <span className="muted hide-xs">une mesure par seconde</span>
          <span className="spacer" />
          <div className="seg" role="group" aria-label="Durée affichée">
            {WINDOWS.map((w) => <button key={w.label} className={win === w.sec ? 'on' : ''} onClick={() => setWin(w.sec)}>{w.label}</button>)}
          </div>
        </div>
        <div className="card-body grid-charts">
          <WheelChart title="Surface des pneus" prefix="tyre" store={telemetry} win={win} unit="°C" band={idealBand(wheels[0]?.idealTemp, wheels[0]?.tempWindow, 'plage idéale')} />
          <WheelChart title="Carcasse des pneus" prefix="core" store={telemetry} win={win} unit="°C" band={idealBand(wheels[0]?.coreIdeal, wheels[0]?.coreWindow, 'plage idéale')} />
          <WheelChart title="Freins" prefix="brake" store={telemetry} win={win} unit="°C" band={idealBand(wheels[0]?.brakeIdeal, wheels[0]?.brakeWindow, 'plage idéale')} />
          <WheelChart title="Pressions" prefix="press" store={telemetry} win={win} unit="bar" scale={0.01} digits={2} refY={wheels[0]?.idealPressure != null ? wheels[0].idealPressure / 100 : undefined} />
          <SimpleChart title="Moteur" store={telemetry} win={win} unit="°C" items={[{ key: 'water', label: 'Eau', color: 'var(--series-1)' }, { key: 'oil', label: 'Huile', color: 'var(--series-2)' }]} />
          <SimpleChart title="Piste et air" store={telemetry} win={win} unit="°C" items={[{ key: 'track', label: 'Piste', color: 'var(--series-4)' }, { key: 'air', label: 'Air', color: 'var(--series-3)' }]} />
        </div>
      </section>
    </div>
  );
}

function idealBand(ideal: number | null | undefined, window: number | null | undefined, label: string) {
  if (ideal == null) return undefined;
  const w = window ?? 10;
  return { y0: ideal - w, y1: ideal + w, label };
}

function Temp({ value, ideal, window, label }: { value: number | null; ideal: number | null; window: number | null; label: string }) {
  const t = thermal(value, ideal, window);
  return (
    <span className="temp-cell tnum" style={{ background: thermalColor(t) }} title={`${label} : ${value != null ? `${value.toFixed(1)} °C` : '—'}${ideal != null ? ` (idéal ${ideal.toFixed(0)} °C)` : ''}`}>
      {value != null ? Math.round(value) : '—'}
    </span>
  );
}

function WheelCard({ w }: { w: WheelStatus }) {
  const remaining = Math.max(0, 100 - w.wearPct);
  const left = w.position.endsWith('L');
  // Le jeu donne les côtés gauche / milieu / droite du pneu (tempInner = gauche, tempOuter = droite dans le DTO) :
  // on les affiche à leur place réelle. Pour une roue gauche, l'intérieur (côté voiture) est à droite.
  const temps = [
    { v: w.tempInner, l: left ? 'Extérieur' : 'Intérieur' },
    { v: w.tempMiddle, l: 'Milieu' },
    { v: w.tempOuter, l: left ? 'Intérieur' : 'Extérieur' },
  ];
  const pressureDelta = w.pressure != null && w.idealPressure != null ? w.pressure - w.idealPressure : null;
  return (
    <div className={`wheel-card pos-${w.position}`} aria-label={WHEEL_LABEL[w.position]}>
      <div className="wheel-card-head">
        <b>{w.position}</b>
        <TyreBadge compound={w.compound} />
        <span className="spacer" />
        <span className="wheel-wear tnum" title="Gomme restante">{remaining.toFixed(0)} %</span>
      </div>
      <div className="meter" aria-hidden><span style={{ width: `${remaining}%`, background: w.wearPct >= w.heavyWearLimitPct ? 'var(--critical)' : undefined }} /></div>
      <div className="temp-row">{temps.map((t) => <Temp key={t.l} value={t.v} ideal={w.idealTemp} window={w.tempWindow} label={t.l} />)}</div>
      <div className="wheel-lines">
        <span>Carcasse</span><Temp value={w.core} ideal={w.coreIdeal ?? w.idealTemp} window={w.coreWindow ?? w.tempWindow} label="Carcasse" />
        <span>Frein</span><Temp value={w.brake} ideal={w.brakeIdeal} window={w.brakeWindow} label="Frein" />
        <span>Pression</span>
        <span className="tnum">
          {w.pressure != null ? `${(w.pressure / 100).toFixed(2)} bar` : '—'}
          {pressureDelta != null && Math.abs(pressureDelta) > 2 && <span className="muted"> ({pressureDelta > 0 ? '+' : '−'}{Math.abs(pressureDelta / 100).toFixed(2)})</span>}
        </span>
        <span>Usure/tour</span><span className="tnum">{w.wearPerLapPct != null ? `${w.wearPerLapPct.toFixed(2)} %` : '…'}</span>
      </div>
    </div>
  );
}

function ThermalLegend({ profile }: { profile: string | null }) {
  return (
    <div className="legend" style={{ marginTop: 10 }}>
      <span className="legend-item"><span className="temp-swatch" style={{ background: thermalColor(-1) }} />Froid</span>
      <span className="legend-item"><span className="temp-swatch" style={{ background: thermalColor(0) }} />Bon</span>
      <span className="legend-item"><span className="temp-swatch" style={{ background: thermalColor(1) }} />Trop chaud</span>
      <span className="legend-item muted">Plages : {profile ? `profil « ${profile} »` : 'données du jeu'}</span>
    </div>
  );
}

function Gauge({ label, value, ideal, window, unit, min, max }: { label: string; value: number | null; ideal: number | null; window: number | null; unit: string; min: number; max: number }) {
  const t = thermal(value, ideal, window);
  const pos = (v: number) => `${Math.max(0, Math.min(100, ((v - min) / (max - min)) * 100))}%`;
  const status = t == null ? '' : t === 0 ? 'Normal' : t > 0 ? 'Chaud' : 'Froid';
  return (
    <div className="gauge">
      <div className="gauge-head"><span>{label}</span><b className="tnum">{value != null ? `${value.toFixed(0)} ${unit}` : '—'}</b></div>
      <div className="gauge-track">
        {ideal != null && <span className="gauge-band" style={{ left: pos(ideal - (window ?? 10)), right: `calc(100% - ${pos(ideal + (window ?? 10))})` }} />}
        {value != null && <span className="gauge-needle" style={{ left: pos(value), background: thermalColor(t === 0 ? null : t) }} />}
      </div>
      <div className="gauge-foot muted">{ideal != null ? `idéal ${ideal.toFixed(0)} ± ${(window ?? 10).toFixed(0)} ${unit}` : ''}{status ? ` · ${status}` : ''}</div>
    </div>
  );
}

function EngineCard({ engine }: { engine: EngineDto | null }) {
  if (!engine) {
    return (
      <section className="card">
        <div className="card-head"><span className="card-title">Moteur</span></div>
        <div className="card-body muted">Le jeu ne transmet pas les températures moteur.</div>
      </section>
    );
  }
  const damages = [
    ['Moteur', engine.damageEngine], ['Boîte', engine.damageTransmission], ['Suspensions', engine.damageSuspension], ['Carrosserie', engine.damageBodywork],
  ].filter(([, v]) => v != null) as [string, number][];
  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Moteur</span>
        <span className="spacer" />
        {engine.rpm > 0 && <span className="pill tnum">{engine.rpm} tr/min{engine.gear ? ` · rapport ${engine.gear}` : ''}</span>}
      </div>
      <div className="card-body stack-sm">
        <Gauge label="Eau" value={engine.waterTemp} ideal={engine.waterIdeal} window={engine.waterWindow} unit="°C" min={50} max={130} />
        <Gauge label="Huile" value={engine.oilTemp} ideal={engine.oilIdeal} window={engine.oilWindow} unit="°C" min={50} max={150} />
        {engine.oilPressure != null && <div className="kv"><span>Pression d'huile</span><b className="tnum">{(engine.oilPressure / 100).toFixed(2)} bar</b></div>}
        {damages.length > 0 && (
          <div className="damage">
            {damages.map(([label, v]) => (
              <div key={label} className="kv">
                <span>{label}</span>
                <span className="row nowrap" style={{ gap: 8 }}>
                  <span className="meter small" aria-hidden><span style={{ width: `${v}%`, background: v > 30 ? 'var(--critical)' : v > 5 ? 'var(--warning)' : 'var(--good)' }} /></span>
                  <b className="tnum">{v.toFixed(0)} %</b>
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

const clockFmt = (t: number) => {
  const m = Math.floor(t / 60);
  return m >= 60 ? `${Math.floor(m / 60)}h${String(m % 60).padStart(2, '0')}` : `${m} min`;
};

function WheelChart({ title, prefix, store, win, unit, band, scale = 1, digits = 0, refY }: { title: string; prefix: string; store: TelemetryStore; win: number | null; unit: string; band?: { y0: number; y1: number; label: string }; scale?: number; digits?: number; refY?: number }) {
  const series: Series[] = useMemo(() => ['FL', 'FR', 'RL', 'RR'].map((w, i) => ({
    id: w,
    label: w,
    color: WHEEL_COLORS[i],
    width: 1.5,
    points: seriesPoints(store, `${prefix}${w}`, win).map((p) => ({ x: p.x, y: p.y == null ? null : p.y * scale })),
    // eslint-disable-next-line react-hooks/exhaustive-deps
  })), [store.version, win, prefix, scale]);
  return (
    <Expandable title={title} height={260} render={(h) => (
      <LineChart
        series={series}
        height={h}
        band={band}
        refLines={refY != null ? [{ y: refY, label: 'idéal' }] : undefined}
        xFormat={clockFmt}
        xTitle={(t) => `Session ${clockFmt(t)} ${String(Math.floor(t % 60)).padStart(2, '0')} s`}
        yFormat={(y) => y.toFixed(digits)}
        valueFormat={(y) => `${y.toFixed(digits === 0 ? 1 : digits)} ${unit}`}
        ariaLabel={`${title} par roue`}
        emptyText="Le jeu ne transmet pas cette donnée."
      />
    )} />
  );
}

function SimpleChart({ title, store, win, unit, items }: { title: string; store: TelemetryStore; win: number | null; unit: string; items: { key: string; label: string; color: string }[] }) {
  const series: Series[] = useMemo(() => items.map((it) => ({ id: it.key, label: it.label, color: it.color, width: 1.75, points: seriesPoints(store, it.key, win) })),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [store.version, win]);
  return (
    <Expandable title={title} height={260} render={(h) => (
      <LineChart series={series} height={h} xFormat={clockFmt} yFormat={(y) => y.toFixed(0)} valueFormat={(y) => `${y.toFixed(1)} ${unit}`} ariaLabel={title} emptyText="Le jeu ne transmet pas cette donnée." />
    )} />
  );
}
