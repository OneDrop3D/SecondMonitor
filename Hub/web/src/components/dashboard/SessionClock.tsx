import { clock, lapTime } from '../../format';
import type { RaceSnapshot } from '../../types';

/*
 * Bloc « fin de session » de la barre du haut : temps restant (course au temps) ou tours restants (course aux tours),
 * avec l'estimation des tours restants calculée par le hub (rythme réel + règle de fin) et une barre d'avancement.
 */

const laps1 = (v: number | null | undefined) => (v == null ? '—' : v >= 10 ? v.toFixed(0) : v.toFixed(1));

export function SessionClock({ snapshot }: { snapshot: RaceSnapshot }) {
  const r = snapshot.raceLength;
  if (!r) return null;
  const race = snapshot.sessionType === 'Race';
  const timed = r.mode === 'time' || r.mode === 'time+1';
  let label: string;
  let big: string;
  let sub: string | null = null;
  let lastLap = false;
  const tip: string[] = [];

  if (race && r.mode === 'laps') {
    label = 'Tours';
    const lap = r.playerLap ?? r.leaderLap;
    const total = r.playerTotalLaps ?? r.leaderTotalLaps;
    big = `${lap ?? '—'}/${total ?? '—'}`;
    sub = r.playerLapsRemaining != null ? `reste ${laps1(r.playerLapsRemaining)} tour${r.playerLapsRemaining >= 2 ? 's' : ''}` : null;
    if (r.playerTotalLaps != null && r.leaderTotalLaps != null && r.playerTotalLaps < r.leaderTotalLaps) sub = `${sub ?? ''} · leader ${r.leaderLap}/${r.leaderTotalLaps}`;
    lastLap = lap != null && total != null && lap >= total;
    tip.push('Course aux tours : nombre de tours donné par le jeu.');
    if (r.playerTotalLaps != null && r.leaderTotalLaps != null && r.playerTotalLaps < r.leaderTotalLaps) tip.push('Vous êtes doublé : vous recevrez le drapeau à damier un tour plus tôt que le leader.');
  } else if (race && timed) {
    label = r.leaderFinished ? 'Damier' : r.mode === 'time+1' ? 'Temps (+1 tour)' : 'Temps restant';
    big = clock(r.timeRemaining);
    if (r.playerTotalLaps != null) {
      sub = `T${r.playerLap}/≈${r.playerTotalLaps} · reste ≈ ${laps1(r.playerLapsRemaining)}`;
      if (r.leaderTotalLaps != null && r.leaderTotalLaps !== r.playerTotalLaps) sub += ` · leader ≈ ${r.leaderTotalLaps}`;
      lastLap = r.playerLap != null && r.playerLap >= r.playerTotalLaps;
    } else {
      sub = 'tours : estimation après un tour';
    }
    tip.push('Estimation des tours : quand le temps est écoulé, le leader termine son tour en cours, puis chacun reçoit le drapeau à son passage suivant.');
    if (r.mode === 'time+1') tip.push('Un tour supplémentaire est ajouté après le temps.');
  } else if (r.timeRemaining != null || timed) {
    label = 'Temps restant';
    big = clock(r.timeRemaining);
    sub = r.playerLapsPossible != null ? `≈ ${Math.floor(r.playerLapsPossible)} tour${r.playerLapsPossible >= 2 ? 's' : ''} possible${r.playerLapsPossible >= 2 ? 's' : ''} à votre rythme` : null;
  } else if (r.mode === 'laps' && snapshot.totalLaps > 0) {
    label = 'Tours';
    big = `${r.playerLap ?? r.leaderLap}/${snapshot.totalLaps}`;
  } else {
    return null;
  }

  if (r.leaderPace) tip.push(`Rythme du leader : ${lapTime(r.leaderPace)}.`);
  if (r.playerPace) tip.push(`Votre rythme : ${lapTime(r.playerPace)}.`);
  if (r.playerFinishIn != null && race) tip.push(`Drapeau à damier pour vous dans ≈ ${clock(r.playerFinishIn)}.`);
  const pct = Math.round(Math.max(0, Math.min(1, r.progress)) * 100);

  return (
    <div className={`tm-box tm-clock ${lastLap ? 'last' : ''}`} title={tip.join('\n')}>
      <div className="tm-clock-top">
        <span className="tm-clock-label">{label}</span>
        {lastLap && <span className="tm-clock-flag">Dernier tour</span>}
        <b className="tm-clock-big tnum">{big}</b>
      </div>
      <div className="tm-clock-bar" role="progressbar" aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} aria-label="Avancement de la session"><span style={{ width: `${pct}%` }} /></div>
      {sub && <div className="tm-clock-sub tnum">{sub}</div>}
    </div>
  );
}
