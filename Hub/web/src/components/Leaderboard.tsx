import { useMemo, useState } from 'react';
import { gap, lapTime, num } from '../format';
import type { DriverRow, RaceSnapshot } from '../types';
import { ClassSwatch, Confidence, TyreBadge, WearBar } from './Common';
import { BrandLogo } from './BrandLogo';
import { classInterval, visibleDrivers, type ClassView } from '../classView';

type GapMode = 'leader' | 'class' | 'player';

interface Props {
  snapshot: RaceSnapshot;
  classColor: (classId: string) => string;
  onSelect: (driver: DriverRow) => void;
  classView: ClassView;
  setClassView: (v: ClassView) => void;
}

export function Leaderboard({ snapshot, classColor, onSelect, classView, setClassView }: Props) {
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const [gapMode, setGapMode] = useState<GapMode>(() => (snapshot.isMultiClass ? 'class' : 'leader'));
  const isRace = snapshot.sessionType === 'Race';

  const classes = useMemo(() => {
    const seen = new Map<string, string>();
    for (const d of snapshot.drivers) if (!seen.has(d.classId)) seen.set(d.classId, d.className ?? d.classId);
    return [...seen.entries()];
  }, [snapshot.drivers]);

  const multiClass = classes.length > 1;
  const { rows, traffic } = visibleDrivers(snapshot, classView);
  const classFilter = classView === 'all' ? null : classView === 'mine' ? me?.classId ?? null : classView;
  const showWear = snapshot.drivers.some((d) => d.tyres.wear);
  const classPositions = multiClass && classView !== 'all';

  const gapCell = (d: DriverRow) => {
    if (gapMode === 'player') return d.isPlayer ? '—' : gap(d.gapToPlayer);
    if (gapMode === 'class') return gap(d.gapToClassLeader, d.gapToClassLeaderLaps);
    return gap(d.gapToLeader, d.gapToLeaderLaps);
  };

  return (
    <div className="card">
      <div className="card-head">
        <span className="card-title">Classement</span>
        <span className="muted">{rows.length - traffic.size} voitures{traffic.size ? ` + ${traffic.size} en trafic` : ''}</span>
        <span className="spacer" />
        {multiClass && (
          <div className="row chips" role="group" aria-label="Catégories affichées">
            {me && (
              <button className={`chip ${classView === 'mine' ? 'on' : ''}`} onClick={() => setClassView('mine')} title="Votre catégorie, plus les autres voitures à moins de 5 s de vous sur la piste">
                <ClassSwatch color={classColor(me.classId)} />Ma catégorie
              </button>
            )}
            <button className={`chip ${classView === 'all' ? 'on' : ''}`} onClick={() => setClassView('all')}>Toutes</button>
            {classes.filter(([id]) => id !== me?.classId).map(([id, name]) => (
              <button key={id} className={`chip ${classView === id ? 'on' : ''}`} onClick={() => setClassView(classView === id ? 'mine' : id)}>
                <ClassSwatch color={classColor(id)} />
                {name}
              </button>
            ))}
          </div>
        )}
        <div className="seg" role="group" aria-label="Référence des écarts">
          <button className={gapMode === 'leader' ? 'on' : ''} onClick={() => setGapMode('leader')}>Leader</button>
          {multiClass && <button className={gapMode === 'class' ? 'on' : ''} onClick={() => setGapMode('class')}>Catégorie</button>}
          <button className={gapMode === 'player' ? 'on' : ''} onClick={() => setGapMode('player')}>Moi</button>
        </div>
      </div>
      <div className="lb-wrap table-scroll">
        <table className="lb">
          <thead>
            <tr>
              <th className="c">Pos</th>
              {multiClass && <th className="cq-sm">Cat.</th>}
              <th className="c">N°</th>
              <th>Pilote</th>
              <th className="r">{gapMode === 'player' ? 'Écart moi' : gapMode === 'class' ? 'Écart cat.' : 'Écart'}</th>
              <th className="r cq-xs">{isRace ? 'Interv.' : 'Δ devant'}</th>
              <th className="r">Dernier</th>
              <th className="r cq-sm">Meilleur</th>
              <th>Pneus</th>
              {showWear && <th className="cq-md">Usure</th>}
              {isRace && <th className="c cq-sm">Arrêts</th>}
              {isRace && <th className="cq-md">Prochain arrêt</th>}
              <th className="r cq-md" title="Moyenne des 3 derniers tours propres · dégradation (s/tour)">Rythme</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {rows.map((d) => (
              <tr
                key={d.id}
                className={`lb-row ${d.isPlayer ? 'player' : ''} ${d.inPits ? 'pitting' : ''} ${traffic.has(d.id) ? 'traffic' : ''}`}
                onClick={() => onSelect(d)}
                tabIndex={0}
                onKeyDown={(e) => e.key === 'Enter' && onSelect(d)}
              >
                <td className="c">
                  <div className="pos tnum" title={classPositions ? `P${d.position} au général` : undefined}>{classPositions && !traffic.has(d.id) ? d.classPosition : d.position}</div>
                  {classPositions && !traffic.has(d.id) && <div className="pos-overall tnum">P{d.position} gén.</div>}
                  {isRace && d.positionsGained !== 0 && (
                    <div className={`delta-pos ${d.positionsGained > 0 ? 'up' : 'down'}`}>
                      {d.positionsGained > 0 ? `▲${d.positionsGained}` : `▼${-d.positionsGained}`}
                    </div>
                  )}
                </td>
                {multiClass && (
                  <td className="cq-sm">
                    <span className="class-tag">
                      <span className="bar" style={{ background: classColor(d.classId) }} />
                      <span>
                        {d.className}
                        <span className="muted"> P{d.classPosition}</span>
                      </span>
                    </span>
                  </td>
                )}
                <td className="c"><span className="carnum">{d.number ?? '–'}</span></td>
                <td>
                  <div className="driver-name"><BrandLogo car={d.car} team={d.team} size={16} />{d.name}{d.isPlayer && <span className="muted"> (vous)</span>}{traffic.has(d.id) && <span className="traffic-tag">trafic {d.gapToPlayer != null ? gap(d.gapToPlayer) : ''}</span>}</div>
                  <div className="driver-car cq-sm">{d.car}</div>
                </td>
                <td className="r tnum">{gapCell(d)}</td>
                <td className="r tnum cq-xs">
                  {traffic.has(d.id) ? <span className="muted">—</span>
                    : classPositions && isRace ? (d.classPosition === 1 ? '—' : (() => { const ci = classInterval(d, snapshot.drivers); return gap(ci.seconds, ci.laps); })())
                    : d.position === 1 && !classFilter ? '—' : gap(d.interval, d.intervalLaps)}
                </td>
                <td className={`r tnum ${d.lastLapIsPersonalBest ? 'best-personal' : ''}`}>{lapTime(d.lastLap)}</td>
                <td className={`r tnum cq-sm ${d.hasOverallBest ? 'best-overall' : ''}`} title={d.hasOverallBest ? 'Meilleur tour absolu' : d.hasClassBest ? 'Meilleur tour de la catégorie' : undefined}>
                  {lapTime(d.bestLap)}{d.hasOverallBest ? ' ◆' : d.hasClassBest && multiClass ? ' ◇' : ''}
                </td>
                <td><TyreBadge compound={d.tyres.compound} laps={d.tyres.stintLaps} /></td>
                {showWear && (
                  <td className="cq-md">
                    {d.tyres.wear ? (
                      <span className="row" style={{ gap: 6 }}>
                        <WearBar pct={Math.max(...d.tyres.wear)} />
                        <span className="secondary tnum">{Math.max(...d.tyres.wear).toFixed(0)} %</span>
                      </span>
                    ) : <span className="muted" title="Le jeu ne transmet pas l'usure de cet adversaire">n/d</span>}
                  </td>
                )}
                {isRace && (
                  <td className="c tnum cq-sm">
                    {d.pitStops}
                    {d.lastPitLap != null && <span className="muted"> · T{d.lastPitLap}</span>}
                  </td>
                )}
                {isRace && (
                  <td className="cq-md">
                    {d.nextStop ? (
                      <span className="row" style={{ gap: 6 }} title={`Estimation d'après ${d.nextStop.reason}`}>
                        <span className="tnum">T{d.nextStop.lap}</span>
                        <span className="muted tnum">{d.nextStop.lapsUntil === 0 ? 'imminent' : `dans ${d.nextStop.lapsUntil}`}</span>
                        <Confidence level={d.nextStop.confidence} />
                      </span>
                    ) : <span className="muted">—</span>}
                  </td>
                )}
                <td className="r tnum cq-md">
                  {lapTime(d.paceLast3)}
                  {d.degradation != null && (
                    <span className="muted"> {d.degradation >= 0 ? '+' : '−'}{num(Math.abs(d.degradation), 2)}</span>
                  )}
                </td>
                <td>
                  {d.inPits && <span className="badge pit">STAND</span>}
                  {!d.inPits && d.pitRequested && <span className="badge box">BOX</span>}
                  {d.finishStatus === 'Finished' && <span className="badge fin">🏁</span>}
                  {(d.finishStatus === 'Dnf' || d.finishStatus === 'Dq') && <span className="badge fin">{d.finishStatus.toUpperCase()}</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
