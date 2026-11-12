import type { DamagePart, PlayerStrategy } from '../types';

/*
 * Dégâts pièce par pièce et simulation d'arrêt (undercut, overcut, position à la sortie) :
 * utilisés par le Chronométrage, l'écran Voiture et l'écran Stratégies.
 */

const GROUPS: Record<DamagePart['group'], string> = {
  aero: 'Aéro', engine: 'Moteur', gearbox: 'Boîte', suspension: 'Suspension', brakes: 'Freins', tyres: 'Pneus', electronics: 'Électronique',
};

export function DamageList({ player, compact = false }: { player: PlayerStrategy; compact?: boolean }) {
  const parts = (player.damage ?? []).filter((d) => d.percent >= 1).sort((a, b) => b.percent - a.percent);
  const ev = player.damageEvent;
  if (parts.length === 0) return compact ? null : <div className="muted">Aucun dégât.</div>;
  const shown = compact ? parts.slice(0, 4) : parts;
  return (
    <div className={`dmg ${compact ? 'compact' : ''}`}>
      {shown.map((d) => (
        <div key={d.key} className={`dmg-row ${d.percent >= 50 ? 'bad' : d.percent >= 15 ? 'warn' : ''}`} title={`${GROUPS[d.group] ?? d.group} · ${d.label}`}>
          <span className="dmg-name">{d.label}</span>
          <span className="dmg-bar"><span style={{ width: `${Math.min(100, d.percent)}%` }} /></span>
          <span className="tnum dmg-pct">{d.percent.toFixed(0)} %</span>
        </div>
      ))}
      {compact && parts.length > shown.length && <div className="muted small">+ {parts.length - shown.length} autre(s)</div>}
      {ev?.lossPerLap != null && <div className="dmg-loss">Coût mesuré : <b className="tnum">{ev.lossPerLap > 0 ? '+' : ''}{ev.lossPerLap.toFixed(2)} s/tour</b> ({ev.label})</div>}
    </div>
  );
}

const CUT_TITLE: Record<string, string> = { undercut: 'Undercut', overcut: 'Overcut', 'menace-undercut': 'Menace d\'undercut' };

export function PitPlanCard({ player, compact = false }: { player: PlayerStrategy; compact?: boolean }) {
  const plan = player.pitPlan;
  if (!plan) return compact ? null : <div className="muted">Simulation d'arrêt disponible en course.</div>;
  const cuts = compact ? plan.cuts.filter((c) => c.feasible) : plan.cuts;
  return (
    <div className="pitplan">
      {plan.rejoinNow && <div className="pitplan-rejoin">{plan.rejoinNow.text}</div>}
      {cuts.map((c) => (
        <div key={`${c.kind}-${c.rivalId}`} className={`pitplan-cut ${c.feasible ? (c.kind === 'menace-undercut' ? 'threat' : 'ok') : ''}`}>
          <b>{CUT_TITLE[c.kind] ?? c.kind}{c.number != null ? ` · n° ${c.number}` : ''}</b>
          <span className={`pill small-pill ${c.feasible ? 'on' : ''}`}>{c.kind === 'menace-undercut' ? (c.feasible ? 'réelle' : 'faible') : c.feasible ? 'jouable' : 'non'}</span>
          {!compact && <div className="secondary">{c.text}</div>}
        </div>
      ))}
      {!compact && (
        <div className="muted small">
          Perte au stand {plan.pitLoss.toFixed(0)} s ({plan.pitLossSource}) · pneus neufs {plan.freshTyreGain != null ? `${plan.freshTyreGain.toFixed(1)} s/tour` : 'inconnus'} ({plan.freshTyreGainSource}) · tour de sortie sur pneus froids {plan.warmupPenalty.toFixed(1)} s
        </div>
      )}
    </div>
  );
}
