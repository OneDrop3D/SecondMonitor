import { useEffect, useMemo, useState } from 'react';
import { LineChart, type Series } from '../charts/LineChart';
import { api } from '../live';
import { gap, lapTime } from '../format';
import type { DriverRow, LapEntry } from '../types';
import { TyreBadge } from './Common';
import { BrandLogo } from './BrandLogo';

/** Fiche détaillée d'un pilote : temps au tour (graphique), relais, arrêts. */
export function DriverDrawer({ driver, onClose }: { driver: DriverRow; onClose: () => void }) {
  const [laps, setLaps] = useState<LapEntry[]>([]);
  useEffect(() => {
    let alive = true;
    const load = () => api<LapEntry[]>(`/api/drivers/${encodeURIComponent(driver.id)}/laps`).then((l) => alive && setLaps(l)).catch(() => {});
    load();
    const t = window.setInterval(load, 5000);
    return () => { alive = false; window.clearInterval(t); };
  }, [driver.id]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  const { series, domain } = useMemo(() => {
    const timed = laps.filter((l) => l.time != null && l.time > 0);
    const clean = timed.filter((l) => !l.pit && !l.outLap).map((l) => l.time!);
    if (clean.length < 2) return { series: [] as Series[], domain: undefined };
    const sorted = [...clean].sort((x, y) => x - y);
    const hi = Math.min(sorted[sorted.length - 1], sorted[Math.floor(sorted.length / 2)] * 1.05);
    const dom: [number, number] = [Math.floor((sorted[0] - 0.4) * 2) / 2, Math.ceil((hi + 0.4) * 2) / 2];
    const main: Series = {
      id: 'laps',
      label: 'Temps au tour',
      color: 'var(--series-1)',
      points: timed.map((l) => ({ x: l.lap, y: l.pit || l.outLap ? null : l.time })),
      markers: timed.filter((l) => l.pit).map((l) => ({ x: l.lap, y: dom[1], title: `Arrêt au tour ${l.lap}` })),
    };
    return { series: [main], domain: dom };
  }, [laps]);

  return (
    <>
      <div className="drawer-backdrop" onClick={onClose} />
      <aside className="drawer" role="dialog" aria-label={`Détails de ${driver.name}`}>
        <div className="card-head">
          <BrandLogo car={driver.car} team={driver.team} size={30} />
          <span className="carnum">{driver.number ?? '–'}</span>
          <div>
            <div className="card-title">{driver.name}</div>
            <div className="muted" style={{ fontSize: 12 }}>{driver.className} · {driver.car}{driver.team ? ` · ${driver.team}` : ''}</div>
          </div>
          <span className="spacer" />
          <button className="close" onClick={onClose} aria-label="Fermer">✕</button>
        </div>
        <div className="card-body stack">
          <div className="kpis">
            <div className="kpi"><div className="label">Position</div><div className="value tnum">P{driver.position}</div><div className="sub">P{driver.classPosition} en catégorie</div></div>
            <div className="kpi"><div className="label">Écart au leader</div><div className="value tnum">{gap(driver.gapToLeader, driver.gapToLeaderLaps)}</div><div className="sub tnum">Interv. {gap(driver.interval, driver.intervalLaps)}</div></div>
            <div className="kpi"><div className="label">Meilleur tour</div><div className="value tnum">{lapTime(driver.bestLap)}</div><div className="sub tnum">Dernier {lapTime(driver.lastLap)}</div></div>
            <div className="kpi"><div className="label">Pneus</div><div className="value"><TyreBadge compound={driver.tyres.compound} laps={driver.tyres.stintLaps} /></div><div className="sub">{driver.pitStops} arrêt(s)</div></div>
          </div>

          <div>
            <div className="card-title" style={{ marginBottom: 6 }}>Temps au tour</div>
            <LineChart
              series={series}
              height={220}
              yDomain={domain}
              legend={false}
              xFormat={(x) => `T${Math.round(x)}`}
              xTitle={(x) => `Tour ${Math.round(x)}`}
              yFormat={(y) => lapTime(y).replace(/\.\d+$/, (m) => m.slice(0, 2))}
              valueFormat={lapTime}
              ariaLabel="Évolution des temps au tour"
              emptyText="Pas encore assez de tours chronométrés."
              footer={<div className="chart-note muted">Points en haut : arrêts au stand. Tours au stand et de sortie exclus.</div>}
            />
          </div>

          <div>
            <div className="card-title" style={{ marginBottom: 6 }}>Relais</div>
            <table className="mini-table">
              <thead><tr><th>#</th><th>Pneus</th><th>Tours</th><th>De → à</th><th>Moyenne</th><th>Meilleur</th></tr></thead>
              <tbody>
                {driver.stints.map((s) => (
                  <tr key={s.index}>
                    <td>{s.index}</td>
                    <td><TyreBadge compound={s.compound} /></td>
                    <td className="tnum">{s.laps}</td>
                    <td className="tnum">T{s.startLap} → {s.endLap != null ? `T${s.endLap}` : 'en cours'}</td>
                    <td className="tnum">{lapTime(s.averageLap)}</td>
                    <td className="tnum">{lapTime(s.bestLap)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {driver.nextStop && (
              <p className="secondary" style={{ fontSize: 13 }}>
                Prochain arrêt estimé : <b className="tnum">tour {driver.nextStop.lap}</b> (d'après {driver.nextStop.reason}).
              </p>
            )}
          </div>

          <div>
            <div className="card-title" style={{ marginBottom: 6 }}>Tous les tours</div>
            <table className="mini-table">
              <thead><tr><th>Tour</th><th>Temps</th><th>Pneus</th><th /></tr></thead>
              <tbody>
                {[...laps].reverse().map((l) => (
                  <tr key={l.lap}>
                    <td className="tnum">{l.lap}</td>
                    <td className="tnum">{lapTime(l.time)}</td>
                    <td><TyreBadge compound={l.compound} /></td>
                    <td className="muted">{l.pit ? 'Stand' : l.outLap ? 'Sortie' : !l.valid ? 'Invalide' : ''}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </aside>
    </>
  );
}
