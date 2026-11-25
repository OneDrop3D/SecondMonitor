import { useEffect, useMemo, useState } from 'react';
import { api, usePoll } from '../live';
import { lapTime } from '../format';
import { useStoredState } from '../prefs';
import { DEFAULT_MARGIN, MAX_REALISTIC_SAVING, prepare, type PrepInput, type PrepPlan } from '../prep';
import type { CarnetSummary } from './CarnetView';
import { Icon } from './Timing';

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
const DURATIONS = ['1:00', '2:00', '3:00', '4:00', '6:00', '12:00', '24:00'];

type Variant = 'base' | 'oneLess' | 'oneMore';

/** État d'un plan, toujours en couleur + mot : rien à économiser, à économiser, pas jouable. */
const verdict = (p: PrepPlan) =>
  p.saving <= 0.001 ? { cls: 'ok', text: 'rien à économiser' }
  : p.saving <= MAX_REALISTIC_SAVING ? { cls: 'warn', text: `économiser ${fr(p.saving * 100, 0)} %` }
  : { cls: 'bad', text: `pas jouable (−${fr(p.saving * 100, 0)} %)` };

export function PrepView() {
  const [f, setF] = useStoredState<Form>('smhub-preparation', EMPTY);
  const [variant, setVariant] = useState<Variant>('base');
  const { data: carnets } = usePoll<CarnetSummary[]>('/api/carnets', 30000);
  const sessions = useMemo(() => (carnets ?? []).filter((c) => c.laps > 0), [carnets]);
  const [taken, setTaken] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [saved, setSaved] = useState<Saved | null>(null);

  const loadSaved = async () => {
    const r = await fetch('/api/preparation');
    setSaved(r.status === 200 ? ((await r.json()) as Saved) : null);
  };
  useEffect(() => { void loadSaved().catch(() => undefined); }, []);

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

  const takeAverages = async (s: CarnetSummary) => {
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
      setTaken(s.id);
      setMessage(m ? 'Moyennes reprises dans les cases en bleu : vérifiez-les.' : 'Cette session n\'a pas encore de moyennes.');
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
      setMessage('Plan gardé : en course, l\'ingénieur part de ces arrêts tant que vous ne décidez rien d\'autre à la radio.');
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
  // Les cases sans lesquelles aucun plan ne sort : signalées tant qu'elles sont vides.
  const missing = (k: keyof Form) => !prep && (k === 'duration' ? !input.minutes && !input.laps : k === 'lapTime' ? !input.lapTime : k === 'fuel' ? !input.fuelPerLap : k === 'tank' ? !input.tank : false);
  const fromSession = (k: keyof Form) => !!taken && !!f[k] && ['lapTime', 'fuel', 'energy', 'tyreLife'].includes(k);
  const tile = (k: keyof Form, icon: string, label: string, unit: string, hint: string, mode: 'decimal' | 'text' = 'decimal') => (
    <label className={`prep-tile ${missing(k) ? 'need' : ''} ${fromSession(k) ? 'taken' : ''}`}>
      <span className="prep-tile-head"><Icon name={icon} size={14} />{label}{missing(k) && <span className="prep-need">à remplir</span>}</span>
      <span className="prep-tile-value">
        <input className="prep-input tnum" inputMode={mode} value={f[k]} onChange={(e) => setF({ [k]: e.target.value })} placeholder={hint} />
        {unit && <span className="prep-unit">{unit}</span>}
      </span>
    </label>
  );

  const options = prep ? ([['oneLess', prep.oneLess], ['base', prep.base], ['oneMore', prep.oneMore]] as [Variant, PrepPlan | null][]).filter((o): o is [Variant, PrepPlan] => o[1] != null) : [];
  const step = (n: number) => (sessions.length > 0 ? n : n - 1);

  return (
    <div className="stack prep">
      {sessions.length > 0 && (
        <section className="card">
          <div className="card-head">
            <span className="prep-step">1</span>
            <span className="card-title">Partir d'une session roulée</span>
            <span className="muted hide-xs">un clic reprend le temps au tour, l'essence, l'énergie et l'usure</span>
          </div>
          <div className="card-body prep-sessions">
            {sessions.slice(0, 8).map((s) => (
              <button key={s.id} className={`prep-session ${taken === s.id ? 'on' : ''}`} disabled={busy} onClick={() => void takeAverages(s)}>
                <span className="prep-session-track"><Icon name="track" size={14} />{s.track ?? 'Circuit inconnu'}</span>
                <span className="muted small">{s.car ?? 'voiture ?'}</span>
                <span className="small"><b className="tnum">{s.laps}</b> tours · {new Date(s.startedAt).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' })}</span>
                {taken === s.id && <span className="prep-session-check">✓ reprise</span>}
              </button>
            ))}
          </div>
        </section>
      )}

      <section className="card">
        <div className="card-head">
          <span className="prep-step">{step(2)}</span>
          <span className="card-title">La course</span>
        </div>
        <div className="card-body stack-sm">
          <div className="prep-group">
            <div className="prep-group-title"><Icon name="flag" size={14} />Course</div>
            <div className={`prep-durations ${missing('duration') ? 'need' : ''}`}>
              <span className="prep-tile-head"><Icon name="stop" size={14} />Durée{missing('duration') && <span className="prep-need">à choisir</span>}</span>
              <div className="prep-chips">
                {DURATIONS.map((d) => (
                  <button key={d} className={`chip ${f.duration === d && !f.laps ? 'on' : ''}`} onClick={() => setF({ duration: d, laps: '' })}>{parseInt(d, 10)} h</button>
                ))}
                <input className="prep-input prep-duration tnum" value={f.duration} onChange={(e) => setF({ duration: e.target.value })} placeholder="autre (h:mm)" aria-label="Autre durée (h:mm)" />
              </div>
            </div>
            <div className="prep-tiles">
              <label className="prep-tile">
                <span className="prep-tile-head"><Icon name="track" size={14} />Circuit</span>
                <span className="prep-tile-value">
                  <input className="prep-input" list="prep-tracks" value={f.track} onChange={(e) => setF({ track: e.target.value })} placeholder="n'importe lequel" />
                </span>
                <datalist id="prep-tracks">{tracks.map((t) => <option key={t} value={t} />)}</datalist>
              </label>
              {tile('car', 'car', 'Voiture', '', 'facultatif', 'text')}
              {tile('laps', 'race', 'ou en tours', 'tours', 'prime sur la durée')}
            </div>
          </div>

          <div className="prep-group">
            <div className="prep-group-title"><Icon name="gauge" size={14} />Voiture</div>
            <div className="prep-tiles">
              {tile('lapTime', 'stop', 'Temps au tour', '', '3:31.5', 'text')}
              {tile('fuel', 'fuel', 'Essence par tour', 'L', '7,3')}
              {tile('tank', 'fuel', 'Réservoir', 'L', '100')}
              {tile('energy', 'energy', 'Énergie par tour', '%', 'LMU, facultatif')}
            </div>
          </div>

          <div className="prep-group">
            <div className="prep-group-title"><Icon name="wrench" size={14} />Stand</div>
            <div className="prep-tiles">
              {tile('pitLoss', 'race', 'Passage au stand', 's', '60')}
              {tile('tyreLife', 'car', 'Pneus tiennent', 'tours', 'vide : à chaque arrêt')}
              {tile('margin', 'flag', 'Marge à l\'arrivée', 'tours', String(DEFAULT_MARGIN))}
            </div>
          </div>

          {message && <div className="callout"><span aria-hidden>ℹ︎</span><div>{message}</div></div>}
        </div>
      </section>

      <section className="card">
        <div className="card-head">
          <span className="prep-step">{step(3)}</span>
          <span className="card-title">Le plan</span>
          <span className="spacer" />
          {saved && (
            <span className="prep-saved">
              ✓ Gardé pour la course{saved.track ? ` · ${saved.track}` : ''}{saved.stops.length ? ` · T${saved.stops.map((s) => s.lap).join(', T')}` : ' · sans arrêt'}
            </span>
          )}
        </div>
        {!prep || !plan ? (
          <div className="card-body prep-empty">
            <Icon name="prep" size={28} />
            <div>Remplissez les cases marquées <span className="prep-need">à remplir</span> : le plan se dessine ici.</div>
          </div>
        ) : (
          <div className="card-body stack-sm">
            <div className="prep-variants" role="group" aria-label="Nombre d'arrêts">
              {options.map(([key, p]) => {
                const v = verdict(p);
                const delta = Math.round(p.pitTime - prep.base.pitTime);
                return (
                  <button key={key} className={`prep-variant ${v.cls} ${variant === key ? 'on' : ''}`} onClick={() => setVariant(key)} aria-pressed={variant === key}>
                    {key === 'base' && <span className="prep-variant-tag">conseillé</span>}
                    <b className="prep-variant-n tnum">{p.stops.length}</b>
                    <span>{p.stops.length > 1 ? 'arrêts' : 'arrêt'}</span>
                    <span className="small tnum">{Math.round(p.pitTime)} s au stand{key !== 'base' && delta !== 0 ? ` (${delta > 0 ? '+' : '−'}${Math.abs(delta)} s)` : ''}</span>
                    <span className={`prep-verdict ${v.cls}`}>{v.text}</span>
                  </button>
                );
              })}
            </div>

            <PrepTimeline plan={plan} input={input} />

            {plan.saving > 0.001 && (
              <div className={`callout ${plan.saving > MAX_REALISTIC_SAVING ? 'warn' : ''}`}>
                <span aria-hidden>{plan.saving > MAX_REALISTIC_SAVING ? '⚠︎' : '↓'}</span>
                <div>
                  Viser <b>{fr(plan.fuelTarget, 2)} L</b>{plan.energyTarget != null ? <> et <b>{fr(plan.energyTarget, 1)} %</b> d'énergie</> : null} par tour
                  ({fr(plan.saving * 100, 0)} % de moins).{plan.saving > MAX_REALISTIC_SAVING && ` Au-delà de ${fr(MAX_REALISTIC_SAVING * 100, 0)} %, ce n'est pas réaliste.`}
                </div>
              </div>
            )}

            <div className="row">
              <button className="btn primary prep-keep" disabled={busy} onClick={() => void keep()}>✓ Garder ce plan pour la course</button>
              {saved && <button className="btn" disabled={busy} onClick={() => void forget()}>Retirer le plan gardé</button>}
            </div>
          </div>
        )}
      </section>
    </div>
  );
}

/** La course d'un coup d'œil : niveau d'essence (et d'énergie) tour par tour, relais, et ce qu'on fait à chaque arrêt. */
function PrepTimeline({ plan, input }: { plan: PrepPlan; input: PrepInput }) {
  const L = plan.laps;
  const H = 100;
  // Niveaux en dents de scie : chaque relais descend, chaque arrêt remonte.
  const levels = (perLap: number, full: number, add: (k: number) => number) => {
    let lvl = full, from = 0;
    const pts: [number, number][] = [[0, lvl]];
    plan.stops.forEach((s, k) => { lvl -= (s.lap - from) * perLap; pts.push([s.lap, lvl]); lvl += add(k); pts.push([s.lap, lvl]); from = s.lap; });
    lvl -= (L - from) * perLap;
    pts.push([L, lvl]);
    return pts;
  };
  const toPath = (pts: [number, number][], full: number) => pts.map(([x, y], k) => `${k ? 'L' : 'M'}${x} ${H - (Math.max(0, y) / full) * (H - 6)}`).join(' ');
  const fuel = levels(plan.fuelTarget, input.tank, (k) => plan.stops[k].fuel);
  const energy = plan.energyTarget != null ? levels(plan.energyTarget, 100, (k) => plan.stops[k].energy ?? 0) : null;
  const fuelPath = toPath(fuel, input.tank);
  const left = Math.max(0, fuel[fuel.length - 1][1]);
  const stints = [...plan.stops.map((s) => s.lap), L].map((end, k, a) => end - (k ? a[k - 1] : 0));
  const pct = (lap: number) => `${(lap / L) * 100}%`;

  return (
    <div className="prep-timeline">
      <div className="prep-legend small">
        <span><i className="prep-key fuel" />essence</span>
        {energy && <span><i className="prep-key energy" />énergie</span>}
        <span className="muted">départ plein · arrivée avec {fr(left, 1)} L ({fr(left / plan.fuelTarget, 1)} tour)</span>
      </div>
      <div className="prep-chart">
        <svg viewBox={`0 0 ${L} ${H}`} preserveAspectRatio="none" aria-hidden>
          <path d={`${fuelPath} L${L} ${H} L0 ${H} Z`} className="prep-area" />
          <path d={fuelPath} className="prep-line fuel" vectorEffect="non-scaling-stroke" />
          {energy && <path d={toPath(energy, 100)} className="prep-line energy" vectorEffect="non-scaling-stroke" />}
        </svg>
        {plan.stops.map((s) => <span key={s.lap} className="prep-pit" style={{ left: pct(s.lap) }}><span>T{s.lap}</span></span>)}
      </div>
      <div className="prep-stints">
        {stints.map((n, k) => (
          <div key={k} className={`prep-stint s${k % 2}`} style={{ flexGrow: n }} title={`Relais ${k + 1} : ${n} tours`}>
            <b className="tnum">{n}</b><span className="hide-xs"> tours</span>
          </div>
        ))}
      </div>
      <div className="prep-axis small muted"><span>départ</span><span className="tnum">arrivée · {L} tours</span></div>

      {plan.stops.length === 0 ? (
        <div className="prep-nostop"><Icon name="flag" size={16} />D'une traite, marge comprise.</div>
      ) : (
        <div className="prep-stops">
          {plan.stops.map((s, k) => (
            <div key={s.lap} className="prep-stop">
              <div className="prep-stop-head"><span className="prep-step sm">{k + 1}</span>fin du <b className="tnum">T{s.lap}</b></div>
              <div className="prep-stop-row"><Icon name="fuel" size={14} /><b className="tnum">+{fr(s.fuel, 1)} L</b></div>
              {s.energy != null && <div className="prep-stop-row"><Icon name="energy" size={14} /><b className="tnum">{fr(s.energy, 0)} %</b>{s.ratio != null && <span className="muted small" title="Ratio essence / énergie du menu des stands">ratio {fr(s.ratio, 2)}</span>}</div>}
              <div className={`prep-stop-row ${s.tyres ? 'tyres' : 'muted'}`}><Icon name="car" size={14} />{s.tyres ? '4 pneus neufs' : 'pneus gardés'}</div>
              {s.stationary != null && <div className="prep-stop-row muted" title="Temps immobile LMU : 2 s + 0,40 s par % d'énergie + 12 s pour les pneus"><Icon name="stop" size={14} />{Math.round(s.stationary)} s immobile</div>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
