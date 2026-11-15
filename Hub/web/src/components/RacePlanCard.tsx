import type { RacePlan } from '../types';

/*
 * Plan de course jusqu'au drapeau (calculé par le hub, RacePlanner) : arrêts restants, fenêtre du prochain arrêt,
 * ce qu'on met à chaque arrêt, un tour plus tôt / plus tard, ratio carburant / énergie, un arrêt de moins
 * (et le lift and coast mesuré). Utilisé par l'écran Stratégies et le bloc « Plan de course » de l'accueil.
 */

const fr = (v: number, d = 0) => v.toLocaleString('fr-FR', { minimumFractionDigits: d, maximumFractionDigits: d });
const stopsText = (n: number) => (n === 0 ? 'aucun arrêt' : n === 1 ? '1 arrêt' : `${n} arrêts`);

function load(fuel: number | null, energy: number | null) {
  const parts: string[] = [];
  if (energy != null && energy > 0.5) parts.push(`${fr(Math.ceil(energy))} %`);
  if (fuel != null && fuel > 0.5) parts.push(`${fr(Math.ceil(fuel))} L`);
  return parts.length ? parts.join(' · ') : '—';
}

/** Une ligne : « 2 arrêts · prochain T21 (T17–21) · ratio 0,93 ». */
export function RacePlanSummary({ plan }: { plan: RacePlan | null | undefined }) {
  if (!plan || plan.stopsNeeded == null) return null;
  if (plan.stopsNeeded === 0) return <div className="rplan-summary"><b>Aucun arrêt</b> · de quoi finir{plan.spareLaps != null ? ` (+${fr(Math.max(0, plan.spareLaps), 1)} t)` : ''}</div>;
  return (
    <div className="rplan-summary">
      <b>{stopsText(plan.stopsNeeded)}</b>
      {plan.nextStopLap != null && <> · prochain <b className="tnum">T{plan.nextStopLap}</b>{plan.windowEarliest != null && plan.windowLatest != null && plan.windowEarliest < plan.windowLatest ? <span className="muted"> (T{plan.windowEarliest}–{plan.windowLatest})</span> : null}</>}
      {plan.idealRatio != null && <> · ratio <b className="tnum">{fr(plan.idealRatio, 2)}</b></>}
    </div>
  );
}

export function RacePlanCard({ plan, compact = false }: { plan: RacePlan | null | undefined; compact?: boolean }) {
  if (!plan) return <div className="muted">Plan de course disponible en course, après un tour de consommation mesuré.</div>;
  const o = plan.oneLess;
  const lc = plan.liftCoast;
  return (
    <div className={`rplan ${compact ? 'compact' : ''}`}>
      <div className="rplan-head">
        <div className="rplan-big">
          <span className="tm-label">Arrêts restants</span>
          <b className="tnum">{plan.stopsNeeded ?? '—'}</b>
        </div>
        <div className="rplan-big">
          <span className="tm-label">Tours à faire</span>
          <b className="tnum">{fr(plan.lapsRemaining, 1)}</b>
        </div>
        <div className="rplan-big">
          <span className="tm-label">Autonomie</span>
          <b className="tnum">{fr(plan.rangeNow, 1)} t</b>
          <span className="muted small">{plan.limiter}</span>
        </div>
        {plan.idealRatio != null && (
          <div className="rplan-big" title="Litres d'essence par % d'énergie virtuelle (menu des stands LMU) qui équilibrent les deux">
            <span className="tm-label">Ratio idéal</span>
            <b className="tnum">{fr(plan.idealRatio, 2)}</b>
          </div>
        )}
      </div>

      {plan.canFinish && <div className="rplan-line ok">De quoi finir sans s'arrêter{plan.spareLaps != null ? `, ${fr(Math.max(0, plan.spareLaps), 1)} tour(s) de marge` : ''}.</div>}

      {plan.nextStopLap != null && (
        <div className="rplan-line">
          Prochain arrêt <b className="tnum">tour {plan.nextStopLap}</b>{plan.plannedByDriver ? ' (décidé à la radio)' : ''}
          {plan.windowEarliest != null && plan.windowLatest != null && <span className="muted"> · fenêtre tours {plan.windowEarliest} à {plan.windowLatest}</span>}
        </div>
      )}

      {!compact && plan.stops.length > 0 && (
        <table className="rplan-table">
          <thead><tr><th>Arrêt</th><th>Tour</th><th>À mettre</th><th>Immobile</th></tr></thead>
          <tbody>
            {plan.stops.map((s, i) => (
              <tr key={i}>
                <td>{i + 1}{s.last ? <span className="muted"> · dernier</span> : null}</td>
                <td className="tnum">{s.lap}</td>
                <td className="tnum">{load(s.fuelLitres, s.energyPct)}</td>
                <td className="tnum">{s.stopSeconds != null ? `${fr(s.stopSeconds)} s` : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {plan.shifts.length > 0 && (
        <div className="rplan-shifts">
          {plan.shifts.map((s) => (
            <div key={s.delta} className={`rplan-shift ${s.feasible ? (s.extraStop ? 'warn' : 'ok') : 'bad'} ${s.delta === 0 ? 'base' : ''}`}>
              <span className="tm-label">{s.delta < 0 ? 'Un tour plus tôt' : s.delta > 0 ? 'Un tour plus tard' : 'Prévu'}</span>
              <b className="tnum">T{s.lap}</b>
              <span className="small">
                {!s.feasible
                  ? s.savingNeededPerLap != null ? `${s.reason} · −${fr(s.savingNeededPerLap, s.savingUnit === 'L' ? 2 : 1)} ${s.savingUnit}/t` : s.reason
                  : s.extraStop ? `+1 arrêt (${s.stops})` : load(s.fuelLitres, s.energyPct)}
              </span>
            </div>
          ))}
        </div>
      )}

      {o && (
        <div className={`rplan-line ${o.netGain != null ? (o.netGain > 5 ? 'ok' : o.netGain < -5 ? 'bad' : '') : o.realistic ? '' : 'bad'}`}>
          <b>{stopsText(o.stops)} au lieu de {stopsText(o.stops + 1)}</b> : {o.limiter === 'énergie virtuelle' && o.energySavingPerLap != null
            ? `−${fr(o.energySavingPerLap, 1)} % d'énergie par tour (cible ${fr(o.energyPerLapTarget ?? 0, 1)} %)`
            : o.fuelSavingPerLap != null ? `−${fr(o.fuelSavingPerLap, 2)} L par tour (cible ${fr(o.fuelPerLapTarget ?? 0, 2)} L)` : `−${fr(o.savingPct, 0)} %`}
          {!o.realistic && ' · pas jouable'}
          {o.netGain != null && o.liftCoastCost != null && <> · lift and coast {fr(o.liftCoastCost)} s contre {fr(o.pitTimeSaved ?? 0)} s d'arrêt : <b>{o.netGain >= 0 ? `+${fr(o.netGain)}` : fr(o.netGain)} s</b></>}
          {o.realistic && o.netGain == null && !lc && <span className="muted"> · à mesurer : dites « je fais un tour en lift and coast »</span>}
        </div>
      )}

      {lc && (
        <div className="rplan-line muted small">
          Lift and coast mesuré (tour {lc.testLap} contre {lc.referenceLap}) : {lc.timeLoss != null ? `${fr(lc.timeLoss, 2)} s/tour` : '—'}
          {lc.energySaved != null ? ` · −${fr(lc.energySaved, 1)} % d'énergie` : ''}{lc.fuelSaved != null ? ` · −${fr(lc.fuelSaved, 2)} L` : ''}
        </div>
      )}

      {!compact && (plan.tyreLife != null || plan.pitLoss != null) && (
        <div className="muted small">
          {plan.tyreLife != null && <>Pneus : {fr(plan.tyreLife)} tours par train{plan.tyreLapsLeft != null ? `, encore ${fr(plan.tyreLapsLeft)} sur celui-ci` : ''}. </>}
          {plan.pitLoss != null && <>Un arrêt coûte {fr(plan.pitLoss)} s. </>}
          Relais plein : {fr(plan.fullStint, 1)} tours.
        </div>
      )}
    </div>
  );
}
