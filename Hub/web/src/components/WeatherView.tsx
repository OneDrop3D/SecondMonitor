import { useMemo } from 'react';
import { usePoll } from '../live';
import { compass, num } from '../format';
import type { RaceSnapshot, WeatherReport, WeatherSample } from '../types';
import { LineChart, type Series } from '../charts/LineChart';

const minutes = (t: number) => `${Math.round(t / 60)} min`;

/** Météo en temps réel, historique de la session et tendance pour les 30 prochaines minutes. */
export function WeatherView({ snapshot }: { snapshot: RaceSnapshot }) {
  const { data } = usePoll<WeatherReport>('/api/weather', 10000);
  const w = snapshot.weather;
  const now = data?.history.at(-1);

  const temps = useMemo(() => build(data, [
    { key: 'track', label: 'Piste', color: 'var(--series-4)' },
    { key: 'air', label: 'Air', color: 'var(--series-3)' },
  ]), [data]);
  const wet = useMemo(() => build(data, [
    { key: 'rain', label: 'Pluie', color: 'var(--series-1)' },
    { key: 'wetness', label: 'Piste mouillée', color: 'var(--series-7)' },
  ]), [data]);

  return (
    <div className="stack">
      <section className="card">
        <div className="card-head"><span className="card-title">Maintenant</span><span className="muted">temps de session {minutes(snapshot.sessionTime)}</span></div>
        <div className="card-body tiles tiles-wide">
          <div className="tile"><div className="tile-label">Piste</div><div className="tile-value tnum">{num(w.trackTemp, 1)} °C</div><div className="tile-sub tnum">{data?.trackPerHour != null ? `${data.trackPerHour >= 0 ? '+' : '−'}${Math.abs(data.trackPerHour).toFixed(1)} °C/h` : 'tendance…'}</div></div>
          <div className="tile"><div className="tile-label">Air</div><div className="tile-value tnum">{num(w.air, 1)} °C</div><div className="tile-sub tnum">{data?.airPerHour != null ? `${data.airPerHour >= 0 ? '+' : '−'}${Math.abs(data.airPerHour).toFixed(1)} °C/h` : 'tendance…'}</div></div>
          <div className={`tile ${w.rain > 0 ? 'warn' : ''}`}><div className="tile-label">Pluie</div><div className="tile-value tnum">{w.rain} %</div><div className="tile-sub">{w.rain === 0 ? 'temps sec' : w.rain < 30 ? 'pluie faible' : w.rain < 60 ? 'pluie modérée' : 'forte pluie'}</div></div>
          <div className={`tile ${w.wetness > 10 ? 'warn' : ''}`}><div className="tile-label">Piste mouillée</div><div className="tile-value tnum">{w.wetness} %</div><div className="tile-sub">{w.wetness === 0 ? 'sèche' : w.wetness < 30 ? 'humide' : 'mouillée'}</div></div>
          <div className="tile">
            <div className="tile-label">Vent</div>
            <div className="tile-value tnum">
              {w.wind != null ? (
                <span className="row nowrap" style={{ gap: 8 }}>
                  <span className="wind-arrow" style={{ transform: `rotate(${(w.windFrom ?? 0) + 180}deg)` }} aria-hidden>↑</span>
                  {num(w.wind, 0)} km/h
                </span>
              ) : '—'}
            </div>
            <div className="tile-sub">{w.wind != null ? `vient du ${compass(w.windFrom)} (${num(w.windFrom, 0)}°)` : 'non transmis par ce jeu'}</div>
          </div>
        </div>
        {data?.summary && <div className="card-foot secondary">{data.summary}</div>}
      </section>

      <section className="card">
        <div className="card-head"><span className="card-title">Prévision (tendance)</span><span className="muted">prolongation des {num(data?.trendMinutes ?? 0, 0)} dernières minutes</span></div>
        <div className="card-body">
          {data && data.forecast.length > 0 ? (
            <div className="tiles tiles-wide">
              {data.forecast.map((f) => <ForecastTile key={f.t} f={f} now={now} />)}
            </div>
          ) : <div className="muted">{data?.summary ?? 'Chargement…'}</div>}
          <p className="muted chart-note">
            Le protocole SecondMonitor ne transmet pas les prévisions officielles du jeu : il s'agit d'une extrapolation prudente de l'évolution récente,
            plafonnée (± 8 °C, ± 25 points de pluie). À recouper avec la météo annoncée dans le jeu.
          </p>
        </div>
      </section>

      <div className="grid-2">
        <section className="card">
          <div className="card-head"><span className="card-title">Températures</span><span className="muted">historique · pointillés = tendance</span></div>
          <div className="card-body">
            <LineChart series={temps} height={240} xFormat={minutes} xTitle={(t) => `Session ${minutes(t)}`} yFormat={(y) => `${y.toFixed(0)}°`} valueFormat={(y) => `${y.toFixed(1)} °C`} refLines={now ? [{ x: now.t, label: 'maintenant' }] : undefined} ariaLabel="Températures piste et air" emptyText="L'historique se construit (une mesure toutes les 10 s)." />
          </div>
        </section>
        <section className="card">
          <div className="card-head"><span className="card-title">Pluie et piste</span><span className="muted">intensité et humidité (%)</span></div>
          <div className="card-body">
            <LineChart series={wet} height={240} yDomain={[0, 100]} xFormat={minutes} xTitle={(t) => `Session ${minutes(t)}`} yFormat={(y) => `${y.toFixed(0)} %`} refLines={now ? [{ x: now.t, label: 'maintenant' }] : undefined} ariaLabel="Pluie et humidité de la piste" emptyText="L'historique se construit." />
          </div>
        </section>
      </div>
    </div>
  );
}

function build(data: WeatherReport | null, items: { key: keyof WeatherSample; label: string; color: string }[]): Series[] {
  if (!data) return [];
  const out: Series[] = [];
  for (const it of items) {
    const pts = data.history.map((h) => ({ x: h.t, y: (h[it.key] as number | null) ?? null }));
    out.push({ id: String(it.key), label: it.label, color: it.color, points: pts });
    const last = data.history.at(-1);
    if (last && data.forecast.length) {
      out.push({ id: `${String(it.key)}-f`, label: '', color: it.color, dashed: true, companion: true, points: [{ x: last.t, y: (last[it.key] as number | null) ?? null }, ...data.forecast.map((f) => ({ x: f.t, y: (f[it.key] as number | null) ?? null }))] });
    }
  }
  return out;
}

function ForecastTile({ f, now }: { f: WeatherSample; now: WeatherSample | undefined }) {
  const inMin = now ? Math.round((f.t - now.t) / 60) : null;
  const dTrack = now?.track != null && f.track != null ? f.track - now.track : null;
  const rainy = f.rain > 0 || f.wetness > 10;
  return (
    <div className={`tile ${rainy ? 'warn' : ''}`}>
      <div className="tile-label">Dans {inMin ?? '?'} min</div>
      <div className="tile-value tnum">{rainy ? '🌧' : '☀'} {num(f.track, 0)} °C</div>
      <div className="tile-sub tnum">piste {dTrack != null ? `${dTrack >= 0 ? '+' : '−'}${Math.abs(dTrack).toFixed(1)} °C` : ''} · pluie {f.rain} % · humidité {f.wetness} %</div>
    </div>
  );
}
