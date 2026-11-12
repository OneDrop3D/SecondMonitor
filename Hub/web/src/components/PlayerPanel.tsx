import { lapTime, num } from '../format';
import type { PlayerStrategy, WheelStatus } from '../types';
import { StatusTag, TyreBadge } from './Common';

export function PlayerPanel({ player, compact = false }: { player: PlayerStrategy | null; compact?: boolean }) {
  if (!player) {
    return (
      <div className="card">
        <div className="card-head"><span className="card-title">Ma voiture</span></div>
        <div className="card-body muted">Aucune voiture joueur détectée (mode spectateur ?).</div>
      </div>
    );
  }

  const { fuel, energy } = player;
  return (
    <div className="card">
      <div className="card-head">
        <span className="card-title">Ma stratégie</span>
        <span className="spacer" />
        {player.lapsRemaining != null && <span className="pill tnum">≈ {num(player.lapsRemaining, 1)} tours restants</span>}
      </div>
      <div className="card-body stack">
        {player.recommendedStopLap != null && (
          <div className="callout">
            <span aria-hidden>🛞</span>
            <div>
              <div>Arrêt conseillé <strong className="tnum">tour {player.recommendedStopLap}</strong></div>
              <div className="secondary" style={{ fontSize: 12 }}>Limite : {player.recommendedStopReason}</div>
            </div>
          </div>
        )}

        <div className="kpis">
          {fuel && (
            <div className="kpi">
              <div className="label">Carburant</div>
              <div className="value tnum">{num(fuel.remaining, 1)} L</div>
              <div className="sub tnum">{num(fuel.perLap, 2)} L/tour · {num(fuel.lapsLeft, 1)} tours</div>
              <div className="meter" aria-hidden><span style={{ width: `${(fuel.remaining / Math.max(1, fuel.capacity)) * 100}%` }} /></div>
              <div style={{ marginTop: 6 }}>
                <StatusTag
                  status={fuel.status}
                  text={fuel.status === 'ok' ? `Assez pour finir (+${num(-(fuel.toAdd ?? 0), 1)} L)` : fuel.status === 'learning' ? 'Apprentissage…' : `${num(fuel.nextStopAdd, 0)} L au prochain arrêt${(fuel.stopsNeeded ?? 0) > 1 ? ` · ${fuel.stopsNeeded} arrêts` : ''}`}
                />
              </div>
            </div>
          )}
          {energy && (
            <div className="kpi">
              <div className="label">Énergie virtuelle</div>
              <div className="value tnum">{num(energy.remainingPct, 1)} %</div>
              <div className="sub tnum">{num(energy.perLapPct, 2)} %/tour · {num(energy.lapsLeft, 1)} tours</div>
              <div className="meter" aria-hidden><span style={{ width: `${energy.remainingPct}%` }} /></div>
              <div style={{ marginTop: 6 }}>
                <StatusTag
                  status={energy.status}
                  text={energy.status === 'ok' ? 'Assez pour finir' : energy.status === 'learning' ? 'Apprentissage…' : `${num(energy.nextStopAddPct, 0)} % au prochain arrêt`}
                />
              </div>
            </div>
          )}
          <div className="kpi">
            <div className="label">Rythme moyen</div>
            <div className="value tnum">{lapTime(player.averageLap)}</div>
            <div className="sub tnum">
              Dégradation {player.degradation == null ? '—' : `${player.degradation >= 0 ? '+' : '−'}${num(Math.abs(player.degradation), 2)} s/tour`}
            </div>
          </div>
          <div className="kpi">
            <div className="label">Relais en cours</div>
            <div className="value tnum">{player.stintLaps} tours</div>
            {player.hybrid && <div className="sub tnum">Batterie {num(player.hybrid.chargePct, 0)} %</div>}
          </div>
        </div>

        {player.wheels.length === 4 && (
          <div className="tyre-grid">
            {player.wheels.map((w) => <Wheel key={w.position} w={w} compact={compact} />)}
          </div>
        )}
      </div>
    </div>
  );
}

const WHEEL_NAMES: Record<string, string> = { FL: 'Avant gauche', FR: 'Avant droit', RL: 'Arrière gauche', RR: 'Arrière droit' };

function tempClass(t: number | null, ideal: number | null) {
  if (t == null || ideal == null) return '';
  if (t > ideal + 10) return 'hot';
  if (t < ideal - 15) return 'cold';
  return '';
}

function Wheel({ w, compact }: { w: WheelStatus; compact: boolean }) {
  const remaining = Math.max(0, 100 - w.wearPct);
  return (
    <div className="wheel" title={WHEEL_NAMES[w.position]}>
      <div className="wheel-head">
        <span className="row" style={{ gap: 6 }}>
          <span className="wheel-pos">{w.position}</span>
          <TyreBadge compound={w.compound} />
        </span>
        <span className="wheel-wear tnum" title="Gomme restante">{remaining.toFixed(0)}%</span>
      </div>
      <div className="meter" aria-hidden>
        <span style={{ width: `${remaining}%`, background: w.wearPct >= w.heavyWearLimitPct ? 'var(--critical)' : undefined }} />
      </div>
      {!compact && (
        <div className="temps tnum" aria-label="Températures intérieur / milieu / extérieur">
          <span className={tempClass(w.tempInner, w.idealTemp)}>{num(w.tempInner, 0)}°</span>
          <span className={tempClass(w.tempMiddle, w.idealTemp)}>{num(w.tempMiddle, 0)}°</span>
          <span className={tempClass(w.tempOuter, w.idealTemp)}>{num(w.tempOuter, 0)}°</span>
        </div>
      )}
      <div className="wheel-foot tnum">
        <span>{w.pressure != null ? `${(w.pressure / 100).toFixed(2)} bar` : '—'}</span>
        <span title="Tours avant usure forte">{w.lapsToHeavyWear != null ? `${w.lapsToHeavyWear.toFixed(0)} t.` : '—'}</span>
      </div>
    </div>
  );
}
