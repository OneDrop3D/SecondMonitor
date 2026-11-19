import { useEffect, useMemo, useState } from 'react';
import { api, usePoll } from '../live';
import { lapTime } from '../format';
import { useStoredState } from '../prefs';
import { DEFAULT_MARGIN, MAX_REALISTIC_SAVING, prepare, type PrepInput, type PrepPlan } from '../prep';
import type { CarnetSummary } from './CarnetView';

/** Saisie gardée telle que tapée (durée « 2:00 », temps « 3:31.5 ») : rien n'est perdu en changeant d'écran. */
interface Form {
  track: string;
  car: string;
  duration: string;
  laps: string;
  lapTime: string;
  fuel: string;
  energy: string;
  tank: string;
  pitLoss: string;
  tyreLife: string;
  margin: string;
}

const EMPTY: Form = { track: '', car: '', duration: '', laps: '', lapTime: '', fuel: '', energy: '', tank: '', pitLoss: '', tyreLife: '', margin: String(DEFAULT_MARGIN) };

interface Saved { track?: string | null; car?: string | null; savedAt: string; stops: { lap: number; fuel?: number | null; energyPct?: number | null; tyres: boolean }[] }
interface Moyenne { temps?: number | null; essence?: number | null; energie?: number | null; usure?: number[] | null }
interface Bilan { course?: Moyenne | null }

const num = (s: string) => { const v = parseFloat(s.replace(',', '.')); return isFinite(v) && v > 0 ? v : null; };
/** « 3:31.5 », « 211.5 » → secondes ; « 2:00 », « 2h », « 120 » (minutes) → minutes. */
const clock = (s: string) => { const p = s.replace(',', '.').split(/[:h]/).filter((x) => x.trim() !== ''); if (p.length === 0) return null; const v = p.reduce((a, x) => a * 60 + parseFloat(x), 0) * (p.length === 1 && /h/.test(s) ? 60 : 1); return isFinite(v) && v > 0 ? v : null; };
const fr = (v: number, d: number) => v.toLocaleString('fr-FR', { minimumFractionDigits: d, maximumFractionDigits: d });
/** Usure forte à 70 % (valeur par défaut du jeu dans le hub). */
const HEAVY_WEAR = 70;

type Variant = 'base' | 'oneLess' | 'oneMore';

export function PrepView() {
  const [f, setF] = useStoredState<Form>('smhub-preparation', EMPTY);
  const [variant, setVariant] = useState<Variant>('base');
  const { data: carnets } = usePoll<CarnetSummary[]>('/api/carnets', 30000);
  const sessions = useMemo(() => (carnets ?? []).filter((c) => c.laps > 0), [carnets]);
  const [source, setSource] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [saved, setSaved] = useState<Saved | null>(null);

  const loadSaved = async () => {
    const r = await fetch('/api/preparation');
    setSaved(r.status === 200 ? ((await r.json()) as Saved) : null);
  };
  useEffect(() => { void loadSaved().catch(() => undefined); }, []);

  // Session proposée : la plus récente sur le circuit saisi, sinon la plus récente.
  const sourceId = source || sessions.find((s) => f.track && s.track === f.track)?.id || sessions[0]?.id || '';
  const set = (k: keyof Form) => (e: { target: { value: string } }) => setF({ [k]: e.target.value });

  const input: PrepInput = {
    minutes: clock(f.duration),
    laps: num(f.laps),
    lapTime: clock(f.lapTime) ?? 0,
    fuelPerLap: num(f.fuel) ?? 0,
    energyPerLap: num(f.energy),
    tank: num(f.tank) ?? 0,
    pitLoss: num(f.pitLoss) ?? 0,
    tyreLife: num(f.tyreLife),
    margin: f.margin.trim() === '' ? DEFAULT_MARGIN : Math.max(0, parseFloat(f.margin.replace(',', '.')) || 0),
  };
  const prep = useMemo(() => prepare(input), [JSON.stringify(input)]); // eslint-disable-line react-hooks/exhaustive-deps
  const plan: PrepPlan | null = prep ? (variant === 'oneLess' ? prep.oneLess : variant === 'oneMore' ? prep.oneMore : prep.base) ?? prep.base : null;

  const takeAverages = async () => {
    const s = sessions.find((x) => x.id === sourceId);
    if (!s) return;
    setBusy(true);
    setMessage(null);
    try {
      const b = await api<Bilan>(`/api/carnets/${encodeURIComponent(s.id)}/bilan`);
      const m = b.course;
      const wear = m?.usure?.filter((v) => v > 0.01) ?? [];
      setF({
        track: s.track ?? f.track,
        car: s.car ?? f.car,
        lapTime: m?.temps ? lapTime(m.temps) : f.lapTime,
        fuel: m?.essence ? fr(m.essence, 2) : f.fuel,
        energy: m?.energie ? fr(m.energie, 2) : f.energy,
        tyreLife: wear.length ? String(Math.floor(HEAVY_WEAR / Math.max(...wear))) : f.tyreLife,
      });
      setMessage(m ? 'Moyennes de la course reprises : vérifiez-les avant de garder le plan.' : 'Cette session n\'a pas encore de moyennes.');
    } catch (e) {
      setMessage(`Moyennes indisponibles (${(e as Error).message}).`);
    } finally {
      setBusy(false);
    }
  };

  const keep = async () => {
    if (!plan) return;
    setBusy(true);
    try {
      await api('/api/preparation', {
        method: 'PUT',
        body: JSON.stringify({
          track: f.track, car: f.car, laps: plan.laps, lapTime: input.lapTime, fuelPerLap: plan.fuelTarget, energyPerLap: plan.energyTarget,
          stops: plan.stops.map((s) => ({ lap: s.lap, fuel: Math.round(s.fuel * 10) / 10, energyPct: s.energy != null ? Math.round(s.energy) : null, tyres: s.tyres })),
        }),
      });
      await loadSaved();
      setMessage('Plan gardé : pendant la course, l\'ingénieur part de ces arrêts tant que vous ne décidez rien d\'autre à la radio.');
    } catch (e) {
      setMessage(`Plan non enregistré (${(e as Error).message}).`);
    } finally {
      setBusy(false);
    }
  };
  const forget = async () => {
    setBusy(true);
    try { const r = await fetch('/api/preparation', { method: 'DELETE' }); if (!r.ok) throw new Error(`${r.status}`); await loadSaved(); setMessage('Plan retiré.'); } catch (e) { setMessage(`Plan non retiré (${(e as Error).message}).`); } finally { setBusy(false); }
  };

  const tracks = [...new Set(sessions.map((s) => s.track).filter((t): t is string => !!t))];
  const field = (k: keyof Form, label: string, hint: string, mode: 'decimal' | 'text' = 'decimal') => (
    <div className="field prep-field">
      <label htmlFor={`prep-${k}`}>{label}</label>
      <input id={`prep-${k}`} className="input" inputMode={mode} value={f[k]} onChange={set(k)} placeholder={hint} />
    </div>
  );

  return (
    <div className="stack prep">
      <section className="card">
        <div className="card-head">
          <span className="card-title">Préparer la course</span>
          <span className="muted hide-xs">les arrêts décidés à l'avance, pour moins de questions en course</span>
        </div>
        <div className="card-body stack-sm">
          {sessions.length > 0 && (
            <div className="row prep-source">
              <select className="select" value={sourceId} onChange={(e) => setSource(e.target.value)} aria-label="Session dont reprendre les moyennes">
                {sessions.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.track ?? 'Circuit inconnu'} · {s.car ?? 'voiture ?'} · {new Date(s.startedAt).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' })} · {s.laps} tours
                  </option>
                ))}
              </select>
              <button className="btn" disabled={busy || !sourceId} onClick={() => void takeAverages()}>Reprendre les moyennes de cette session</button>
            </div>
          )}
          <div className="prep-grid">
            <div className="field prep-field">
              <label htmlFor="prep-track">Circuit</label>
              <input id="prep-track" className="input" list="prep-tracks" value={f.track} onChange={set('track')} placeholder="vide : n'importe quel circuit" />
              <datalist id="prep-tracks">{tracks.map((t) => <option key={t} value={t} />)}</datalist>
            </div>
            {field('car', 'Voiture', 'facultatif', 'text')}
            {field('duration', 'Durée de la course (h:mm)', 'ex. 2:00', 'text')}
            {field('laps', 'ou nombre de tours', 'l\'emporte sur la durée')}
            {field('lapTime', 'Temps au tour moyen', 'ex. 3:31.5', 'text')}
            {field('fuel', 'Essence (L par tour)', 'ex. 7,3')}
            {field('energy', 'Énergie virtuelle (% par tour)', 'LMU, facultatif')}
            {field('tank', 'Réservoir (L)', 'ex. 100')}
            {field('pitLoss', 'Passage au stand (s perdues)', 'sans l\'arrêt, ex. 60')}
            {field('tyreLife', 'Vie d\'un train de pneus (tours)', 'vide : à chaque arrêt')}
            {field('margin', 'Marge à l\'arrivée (tours)', String(DEFAULT_MARGIN))}
          </div>
          {message && <div className="callout"><span aria-hidden>ℹ︎</span><div>{message}</div></div>}
        </div>
      </section>

      <section className="card">
        <div className="card-head">
          <span className="card-title">Plan d'arrêts</span>
          <span className="spacer" />
          {prep && (
            <div className="seg" role="group" aria-label="Variante affichée">
              <button className={variant === 'base' ? 'on' : ''} onClick={() => setVariant('base')}>{prep.base.stops.length} arrêt{prep.base.stops.length > 1 ? 's' : ''}</button>
              {prep.oneLess && <button className={variant === 'oneLess' ? 'on' : ''} onClick={() => setVariant('oneLess')}>Un arrêt de moins</button>}
              <button className={variant === 'oneMore' ? 'on' : ''} onClick={() => setVariant('oneMore')}>Un arrêt de plus</button>
            </div>
          )}
        </div>
        {!prep || !plan ? (
          <div className="card-body muted">Indiquez la durée (ou le nombre de tours), le temps au tour, l'essence par tour et le réservoir : le plan s'affiche ici.</div>
        ) : (
          <div className="card-body stack-sm">
            <div className="prep-summary">
              <span><b className="tnum">{plan.laps}</b> tours</span>
              <span><b className="tnum">{plan.stops.length}</b> arrêt{plan.stops.length > 1 ? 's' : ''}</span>
              <span><b className="tnum">{Math.round(plan.pitTime)} s</b> au stand en tout{input.energyPerLap ? '' : ' (passages seulement)'}</span>
              {variant !== 'base' && <span className="muted">{plan.pitTime > prep.base.pitTime ? '+' : '−'}{Math.round(Math.abs(plan.pitTime - prep.base.pitTime))} s par rapport à {prep.base.stops.length} arrêt{prep.base.stops.length > 1 ? 's' : ''}</span>}
            </div>
            {plan.saving > 0.001 && (
              <div className={`callout ${plan.saving > MAX_REALISTIC_SAVING ? 'warn' : ''}`}>
                <span aria-hidden>{plan.saving > MAX_REALISTIC_SAVING ? '⚠︎' : '↓'}</span>
                <div>
                  Il faut économiser <b>{fr(plan.saving * 100, 0)} %</b> par tour : viser {fr(plan.fuelTarget, 2)} L{plan.energyTarget != null ? ` et ${fr(plan.energyTarget, 1)} % d'énergie` : ''} par tour.
                  {plan.saving > MAX_REALISTIC_SAVING && ` Au-delà de ${fr(MAX_REALISTIC_SAVING * 100, 0)} %, ce n'est pas réaliste.`}
                </div>
              </div>
            )}
            {plan.stops.length === 0 ? (
              <div className="muted">Aucun arrêt : la course se fait d'une traite (marge comprise).</div>
            ) : (
              <div className="table-scroll">
                <table className="mini-table prep-table">
                  <thead>
                    <tr>
                      <th>Arrêt</th>
                      <th className="r">Fin du tour</th>
                      <th className="r">Relais suivant</th>
                      <th className="r">Essence</th>
                      {input.energyPerLap ? <th className="r">Énergie</th> : null}
                      {input.energyPerLap ? <th className="r" title="Ratio essence / énergie à régler dans le menu des stands">Ratio</th> : null}
                      <th>Pneus</th>
                      {input.energyPerLap ? <th className="r" title="Temps immobile LMU : 2 s + 0,40 s par % d'énergie + 12 s pour les pneus">Immobile</th> : null}
                    </tr>
                  </thead>
                  <tbody>
                    {plan.stops.map((s, k) => (
                      <tr key={s.lap}>
                        <td>{k + 1}</td>
                        <td className="r tnum"><b>T{s.lap}</b></td>
                        <td className="r tnum">{s.stint} tours</td>
                        <td className="r tnum">{fr(s.fuel, 1)} L</td>
                        {input.energyPerLap ? <td className="r tnum">{s.energy != null ? `${fr(s.energy, 0)} %` : '—'}</td> : null}
                        {input.energyPerLap ? <td className="r tnum">{s.ratio != null ? fr(s.ratio, 2) : '—'}</td> : null}
                        <td>{s.tyres ? 'Quatre neufs' : 'On garde'}</td>
                        {input.energyPerLap ? <td className="r tnum">{s.stationary != null ? `${Math.round(s.stationary)} s` : '—'}</td> : null}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            <div className="muted small">Départ réservoir plein{input.energyPerLap ? ' et 100 % d\'énergie' : ''}, relais réguliers ; le dernier garde la marge.</div>
            <div className="row">
              <button className="btn primary" disabled={busy} onClick={() => void keep()}>Garder ce plan pour la course</button>
              {saved && <button className="btn" disabled={busy} onClick={() => void forget()}>Retirer le plan gardé</button>}
            </div>
          </div>
        )}
        {saved && (
          <div className="card-foot muted">
            Plan gardé le {new Date(saved.savedAt).toLocaleString('fr-FR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' })}
            {saved.track ? ` pour ${saved.track}` : ' (tout circuit)'} : {saved.stops.length ? `arrêts aux tours ${saved.stops.map((s) => s.lap).join(', ')}` : 'aucun arrêt'}.
            L'ingénieur s'en sert en course tant que vous ne décidez rien d'autre à la radio.
          </div>
        )}
      </section>
    </div>
  );
}
