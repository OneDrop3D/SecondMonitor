import { num } from '../format';
import type { DriverRow, RaceSnapshot } from '../types';
import { TyreBadge } from './Common';
import { BrandLogo } from './BrandLogo';

/** Duels avec la voiture devant et derrière (catégorie) : écart, rythme comparé et tours avant le contact. */
export function BattlePanel({ snapshot, onSelect }: { snapshot: RaceSnapshot; onSelect: (d: DriverRow) => void }) {
  const battles = snapshot.player?.battles ?? [];
  if (snapshot.sessionType !== 'Race' || battles.length === 0) return null;
  const byId = new Map(snapshot.drivers.map((d) => [d.id, d]));
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Duels</span><span className="muted">dans votre catégorie</span></div>
      <div className="battles">
        {(['ahead', 'behind'] as const).map((side) => {
          const b = battles.find((x) => x.side === side);
          if (!b) return <div key={side} className="battle empty-battle muted">{side === 'ahead' ? 'Personne devant : vous menez' : 'Personne derrière'}</div>;
          const d = byId.get(b.id);
          const gaining = b.paceDelta != null && b.paceDelta > 0.03;
          const losing = b.paceDelta != null && b.paceDelta < -0.03;
          const threat = side === 'behind' ? losing : gaining;
          return (
            <button key={side} className={`battle ${threat ? (side === 'behind' ? 'threat' : 'chance') : ''}`} onClick={() => d && onSelect(d)}>
              <div className="battle-side">{side === 'ahead' ? '▲ Devant' : '▼ Derrière'}</div>
              <div className="battle-gap tnum">{b.gap != null ? `${num(b.gap, 1)} s` : '—'}</div>
              <div className="battle-name"><BrandLogo car={d?.car} team={d?.team} size={15} />#{b.number ?? '–'} {b.name}</div>
              <div className="battle-meta">
                <TyreBadge compound={b.compound} laps={b.tyreLaps} />
                <span className="tnum">
                  {b.paceDelta == null ? 'rythme …' : Math.abs(b.paceDelta) < 0.03 ? 'même rythme' : b.paceDelta > 0 ? `vous +${num(b.paceDelta, 2)} s/t` : `vous −${num(-b.paceDelta, 2)} s/t`}
                </span>
              </div>
              {b.lapsToContact != null && (
                <div className="battle-contact tnum">{b.lapsToContact < 1 ? 'Contact ce tour' : `Contact dans ~${Math.ceil(b.lapsToContact)} tours`}</div>
              )}
              {b.nextStopLap != null && <div className="muted tnum" style={{ fontSize: 12 }}>Arrêt estimé T{b.nextStopLap}</div>}
            </button>
          );
        })}
      </div>
    </section>
  );
}
