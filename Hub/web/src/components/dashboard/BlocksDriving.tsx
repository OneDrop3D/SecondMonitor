import { useMemo } from 'react';
import { usePoll } from '../../live';
import { LineChart, type Series } from '../../charts/LineChart';
import type { CarControls, InputTrace } from '../../types';
import { registerBlock, type BlockProps } from './registry';

/*
 * Blocs du pilotage (accueil) :
 *  - « Réglages et limites » : répartition, migration, TC, ABS, cartographie, regen, énergie, limites de piste, pénalités ;
 *  - « Pédales et volant » : pédales en direct et traces du tour (dernier tour comparé au meilleur), par distance.
 */

const level = (v: number | null | undefined, max: number | null | undefined) => (v == null ? null : max != null ? `${v} / ${max}` : `${v}`);

function ControlsBlock({ ctx }: BlockProps) {
  const c: CarControls | null | undefined = ctx.snapshot.player?.controls;
  if (!c) return <div className="db-empty muted">Le jeu n'envoie pas les réglages de la voiture (Le Mans Ultimate, F1 24 et ACC les donnent).</div>;
  const rows: [string, string | null][] = [
    ['Répartition', c.frontBrakeBiasPct != null ? `${c.frontBrakeBiasPct.toFixed(1).replace('.', ',')} % avant` : null],
    ['Migration', level(c.migration, c.migrationMax)],
    ['TC', c.tc === 0 ? 'coupé' : level(c.tc, c.tcMax)],
    ['Coupure TC', level(c.tcCut, c.tcCutMax)],
    ['Glissement TC', level(c.tcSlip, c.tcSlipMax)],
    ['ABS', c.absMax ? (c.abs === 0 ? 'coupé' : level(c.abs, c.absMax)) : null],
    ['Cartographie', level(c.motorMap, c.motorMapMax)],
    ['Regen', c.regenKw ? `${Math.round(c.regenKw)} kW` : null],
    ['Moteur électrique', c.electricMotor],
    ['Batterie', c.batteryPct != null ? `${Math.round(c.batteryPct)} %` : null],
    ['Énergie virtuelle', c.virtualEnergyPct != null ? `${Math.round(c.virtualEnergyPct)} %` : null],
  ];
  const pts = c.trackLimitsPoints;
  const max = c.trackLimitsPointsPerPenalty;
  const danger = pts != null && max != null && max > 0 && max - pts <= 1;
  return (
    <div className="dr-controls">
      <div className="dr-kv">
        {rows.filter(([, v]) => v != null).map(([k, v]) => (
          <div key={k} className="kv"><span>{k}</span><b className="tnum">{v}</b></div>
        ))}
      </div>
      <div className={`dr-limits ${danger ? 'danger' : ''}`}>
        <div className="tm-label">Limites de piste</div>
        {pts != null ? (
          <>
            <div className="dr-dots" aria-label={`${pts} points sur ${max ?? '?'}`}>
              {Array.from({ length: Math.max(1, Math.round(max ?? 3)) }, (_, i) => <span key={i} className={i < Math.floor(pts) ? 'on' : ''} />)}
            </div>
            <div className="small">{`${Math.floor(pts)} point${pts >= 2 ? 's' : ''}${max ? ` sur ${max} avant pénalité` : ''}`}</div>
          </>
        ) : <div className="muted small">non transmises par le jeu</div>}
        {c.penalties ? <div className="pill flag">{c.penalties} pénalité{c.penalties > 1 ? 's' : ''} à purger</div> : null}
        {c.lapInvalidated && <div className="pill">tour annulé</div>}
        {!c.engineRunning && <div className="pill">moteur coupé</div>}
        {c.pitRequested && <div className="pill">arrêt demandé</div>}
      </div>
    </div>
  );
}

function Bar({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="dr-bar" title={`${label} ${Math.round(value)} %`}>
      <div className="dr-bar-fill" style={{ height: `${Math.max(0, Math.min(100, value))}%`, background: color }} />
      <span className="dr-bar-label">{label}</span>
    </div>
  );
}

function toSeries(t: InputTrace | null, suffix: string, dashed: boolean): Series[] {
  if (!t) return [];
  const pts = (arr: (number | null)[]) => arr.map((y, i) => ({ x: i * t.step, y }));
  return [
    { id: `thr${suffix}`, label: `Accélérateur${suffix}`, color: 'var(--good)', points: pts(t.throttle), dashed, width: dashed ? 1 : 1.6 },
    { id: `brk${suffix}`, label: `Frein${suffix}`, color: 'var(--critical)', points: pts(t.brake), dashed, width: dashed ? 1 : 1.6 },
    { id: `str${suffix}`, label: `Volant${suffix}`, color: 'var(--accent)', points: pts(t.steering.map((v) => (v == null ? null : 50 + v / 2))), dashed, width: 1 },
  ];
}

function PedalsBlock({ ctx, size }: BlockProps) {
  const inputs = ctx.snapshot.player?.inputs;
  const { data: last } = usePoll<InputTrace>('/api/analysis/inputs?lap=last', 5000);
  const { data: best } = usePoll<InputTrace>('/api/analysis/inputs?lap=best', 15000);
  const lastOk = last && last.throttle ? last : null;
  const bestOk = best && best.throttle && best.lap !== lastOk?.lap ? best : null;
  const series = useMemo(() => [...toSeries(lastOk, '', false), ...toSeries(bestOk, ' (meilleur)', true)], [lastOk, bestOk]);
  return (
    <div className="dr-pedals">
      <div className="dr-live">
        {inputs ? (
          <>
            <Bar label="Frein" value={inputs.brake} color="var(--critical)" />
            <Bar label="Gaz" value={inputs.throttle} color="var(--good)" />
            <div className="dr-wheel" title={`Volant ${inputs.steering}`}>
              <div className="dr-wheel-dot" style={{ left: `${50 + inputs.steering / 2}%` }} />
            </div>
            <div className="dr-gear tnum">{inputs.gear || '—'}</div>
          </>
        ) : <span className="muted small">pas de pédales</span>}
      </div>
      <div className="dr-chart">
        {lastOk ? (
          <LineChart
            series={series}
            height={Math.max(110, size.bodyHeight - 40)}
            yDomain={[0, 100]}
            xFormat={(x) => `${Math.round(x)} m`}
            yFormat={(y) => `${Math.round(y)}`}
            valueFormat={(y) => `${Math.round(y)} %`}
            ariaLabel="Pédales et volant sur le tour"
            legend={false}
            footer={<span className="muted small">Tour {lastOk.lap}{bestOk ? ` · pointillés : meilleur tour (tour ${bestOk.lap})` : ''} · volant : 50 = droit</span>}
          />
        ) : <div className="db-empty muted">Les traces apparaissent après un tour complet.</div>}
      </div>
    </div>
  );
}

registerBlock({
  type: 'reglages', title: 'Réglages et limites', icon: 'gauge', order: 12,
  description: 'Répartition, migration, TC, ABS, cartographie, regen, énergie, points de limites de piste et pénalités.',
  size: { w: 4, h: 6 }, medium: { w: 3, h: 6 }, minW: 2, minH: 4,
  render: (p) => <ControlsBlock {...p} />,
});
registerBlock({
  type: 'pedales', title: 'Pédales et volant', icon: 'analysis', order: 13,
  description: 'Accélérateur, frein et volant en direct, et sur tout le tour comparé à votre meilleur tour.',
  size: { w: 8, h: 6 }, medium: { w: 6, h: 6 }, minW: 3, minH: 4,
  render: (p) => <PedalsBlock {...p} />,
});
