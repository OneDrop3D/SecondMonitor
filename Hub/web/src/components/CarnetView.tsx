import { useEffect, useMemo, useState } from 'react';
import { LineChart, type Series } from '../charts/LineChart';
import { lapTick, lapTime, WHEELS, WHEEL_SHORT } from '../format';
import { api, usePoll } from '../live';
import { readPref, writePref } from '../prefs';
import { TyreBadge } from './Common';
import './carnet.css';

// ───── Types du carnet (GET /api/carnets, /api/carnets/{id}) ─────
export interface CarnetSummary {
  id: string;
  game?: string | null;
  track?: string | null;
  sessionType?: string | null;
  car?: string | null;
  startedAt: string;
  endedAt?: string | null;
  radios: number;
  laps: number;
  result?: string | null;
  hasRecap: boolean;
  current: boolean;
}

interface CarnetRadio { at: string; lap?: number | null; from?: string | null; question?: string | null; answer: string; kind?: string | null; category?: string | null; categorySource?: string | null; proactive: boolean }
interface CarnetNote { id: number; at: string; lap?: number | null; source: string; topic?: string | null; text: string; symptom?: string | null; tyres?: string | null }
interface Decision { at: string; lap?: number | null; symptom?: string | null; key: string; kind?: string | null; text: string; decision: string }
interface RecapSection { title: string; text: string }
interface CarnetRecap { generatedAt: string; sections: RecapSection[]; ai?: string | null; model?: string | null; note?: string | null }
interface CarnetSession {
  id: string;
  game?: string | null;
  track?: string | null;
  sessionType?: string | null;
  car?: string | null;
  className?: string | null;
  startedAt: string;
  endedAt?: string | null;
  finished: boolean;
  radios: CarnetRadio[];
  notes: CarnetNote[];
  decisions: Decision[];
  laps: { lap: number; time?: number | null; pit: boolean; valid: boolean }[];
  recap?: CarnetRecap | null;
}

export const CATEGORIES = ['Stratégie', 'Réglages de course', 'Réglages de la voiture', 'Informations course'] as const;

const CAT_CLASS: Record<string, string> = {
  'Stratégie': 'cat-strategy',
  'Réglages de course': 'cat-race',
  'Réglages de la voiture': 'cat-setup',
  'Informations course': 'cat-info',
};

/** Petite étiquette de catégorie (aussi utilisée dans l'écran Ingénieur IA). */
export function CategoryPill({ category }: { category: string }) {
  return <span className={`pill small-pill cat-pill ${CAT_CLASS[category] ?? 'cat-info'}`}>{category}</span>;
}

const SESSION: Record<string, string> = { Race: 'Course', Qualification: 'Qualification', Practice: 'Essais', WarmUp: 'Warm-up' };
const sessionName = (t?: string | null) => (t && SESSION[t]) || 'Session';
const dateTime = (s: string) => new Date(s).toLocaleString('fr-FR', { weekday: 'short', day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
const time = (s: string) => new Date(s).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });


export function CarnetView() {
  const { data: list, reload: reloadList } = usePoll<CarnetSummary[]>('/api/carnets', 10000);
  const [selected, setSelected] = useState<string | null>(() => readPref('smhub-carnet', '') || null);
  const id = selected && list?.some((s) => s.id === selected) ? selected : list?.[0]?.id ?? null;
  useEffect(() => { if (id) writePref('smhub-carnet', id); }, [id]);
  const current = list?.find((s) => s.id === id)?.current ?? false;

  return (
    <div className="layout-carnet">
      <section className="card carnet-list">
        <div className="card-head"><span className="card-title">Carnet</span><span className="muted">sessions enregistrées</span></div>
        <div className="card-body stack-sm">
          {list == null && <div className="muted">Chargement…</div>}
          {list?.length === 0 && (
            <div className="muted small">
              Aucune session pour l'instant. Le carnet se remplit tout seul pendant que vous roulez : radios, retours du pilote,
              réglages proposés, tours. Il est enregistré dans le dossier de données de SecondMonitor (sous-dossier « carnets »).
            </div>
          )}
          {list?.map((s) => (
            <button key={s.id} className={`carnet-item ${s.id === id ? 'on' : ''}`} onClick={() => setSelected(s.id)}>
              <span className="carnet-item-head">
                <b>{sessionName(s.sessionType)}</b>
                {s.current && <span className="pill small-pill carnet-live">en cours</span>}
                <span className="spacer" />
                {s.result && <span className="tnum">{s.result}</span>}
              </span>
              <span className="secondary small">{s.track ?? 'circuit inconnu'} · {s.game}</span>
              <span className="muted small">{dateTime(s.startedAt)} · {s.laps} tours · {s.radios} radios{s.hasRecap ? ' · récapitulatif' : ''}</span>
            </button>
          ))}
        </div>
      </section>
      {id ? <CarnetDetail key={id} id={id} live={current} onChanged={reloadList} /> : <div />}
    </div>
  );
}

function CarnetDetail({ id, live, onChanged }: { id: string; live: boolean; onChanged: () => void }) {
  const { data, reload } = usePoll<CarnetSession>(`/api/carnets/${encodeURIComponent(id)}`, live ? 5000 : 60000);
  const [filter, setFilter] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [note, setNote] = useState('');

  const counts = useMemo(() => {
    const c: Record<string, number> = {};
    for (const r of data?.radios ?? []) c[r.category ?? 'Informations course'] = (c[r.category ?? 'Informations course'] ?? 0) + 1;
    return c;
  }, [data]);

  if (!data) return <section className="card"><div className="card-body muted">Chargement…</div></section>;

  const regenerate = async () => {
    setBusy(true);
    setError(null);
    try {
      await api(`/api/carnets/${encodeURIComponent(id)}/recap`, { method: 'POST' });
      reload();
      onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  const addNote = async () => {
    if (!note.trim()) return;
    await api(`/api/carnets/${encodeURIComponent(id)}/notes`, { method: 'POST', body: JSON.stringify({ text: note, source: 'ingénieur' }) }).catch(() => undefined);
    setNote('');
    reload();
  };
  const remove = async () => {
    if (!window.confirm('Supprimer ce carnet ? Cette action est définitive.')) return;
    await fetch(`/api/carnets/${encodeURIComponent(id)}`, { method: 'DELETE' });
    onChanged();
  };

  const radios = data.radios.filter((r) => !filter || (r.category ?? 'Informations course') === filter);
  const pilotNotes = data.notes.filter((n) => n.topic !== 'réglage');
  const recap = data.recap;

  return (
    <div className="stack">
      <section className="card">
        <div className="card-head">
          <span className="card-title">{sessionName(data.sessionType)} · {data.track ?? 'circuit inconnu'}</span>
          <span className="muted">{data.game}{data.car ? ` · ${data.car}` : ''}{data.className ? ` (${data.className})` : ''}</span>
          <span className="spacer" />
          <span className="muted small">{dateTime(data.startedAt)}{data.endedAt ? ` → ${time(data.endedAt)}` : live ? ' · en cours' : ''}</span>
        </div>
        <div className="card-body stack-sm">
          <div className="row">
            <span className="card-title">Récapitulatif</span>
            {recap?.model && <span className="pill small-pill">rédigé par {recap.model.split('@')[0]}</span>}
            {recap && !recap.ai && recap.sections.length > 0 && <span className="pill small-pill">calculé sans IA</span>}
            <span className="spacer" />
            <button className="btn" onClick={regenerate} disabled={busy}>{busy ? 'Génération…' : recap?.sections.length ? 'Régénérer' : 'Générer maintenant'}</button>
            <button className="btn" onClick={remove} title="Supprimer ce carnet">Supprimer</button>
          </div>
          {error && <div className="status critical"><span className="ico">!</span>{error}</div>}
          {!recap?.sections.length ? (
            <div className="muted small">
              {recap?.note ?? (live
                ? 'Le récapitulatif se fait tout seul à la fin de la course ou au changement de session. Vous pouvez aussi le générer maintenant.'
                : 'Pas encore de récapitulatif pour cette session.')}
            </div>
          ) : (
            <>
              {recap.ai && <p className="carnet-ai">{recap.ai}</p>}
              <dl className="carnet-recap">
                {recap.sections.map((s) => (
                  <div key={s.title} className="carnet-recap-row">
                    <dt>{s.title}</dt>
                    <dd>{s.text}</dd>
                  </div>
                ))}
              </dl>
              <div className="muted small">Généré {dateTime(recap.generatedAt)}{recap.note ? ` · ${recap.note}` : ''}</div>
            </>
          )}
        </div>
      </section>

      <BilanCarnet id={id} live={live} />

      <div className="layout-carnet-detail">
        <section className="card">
          <div className="card-head">
            <span className="card-title">Radios</span>
            <span className="muted">{data.radios.length} échanges, dans l'ordre</span>
          </div>
          <div className="card-toolbar row">
            <button className={`chip ${filter == null ? 'on' : ''}`} onClick={() => setFilter(null)}>Toutes ({data.radios.length})</button>
            {CATEGORIES.map((c) => (
              <button key={c} className={`chip ${filter === c ? 'on' : ''}`} onClick={() => setFilter(filter === c ? null : c)} disabled={!counts[c]}>
                <span className={`cat-dot ${CAT_CLASS[c]}`} aria-hidden />{c} ({counts[c] ?? 0})
              </button>
            ))}
          </div>
          <div className="card-body stack-sm">
            {radios.length === 0 && <div className="muted small">Aucun échange{filter ? ' dans cette catégorie' : ''}.</div>}
            {radios.map((r, i) => (
              <div key={`${r.at}-${i}`} className={`msg carnet-radio ${r.proactive ? 'msg-proactive' : ''}`}>
                <div className="msg-meta carnet-radio-meta">
                  <span className="tnum">{r.lap != null ? `T${r.lap}` : ''}</span>
                  <span>{time(r.at)}</span>
                  {r.category && <CategoryPill category={r.category} />}
                  {r.categorySource === 'modèle' && <span className="muted">classé par l'IA</span>}
                </div>
                {r.question && <div className="msg-q"><b>{r.from?.startsWith('pilote') ? 'Pilote' : 'Ingénieur'}</b> : « {r.question} »</div>}
                <div className="msg-a">{r.answer}</div>
              </div>
            ))}
          </div>
        </section>

        <div className="stack">
          <section className="card">
            <div className="card-head"><span className="card-title">Notes et retours</span><span className="muted">pilote et ingénieur</span></div>
            <div className="card-body stack-sm">
              {pilotNotes.length === 0 && <div className="muted small">Aucune note. Les retours du pilote à la radio s'ajoutent ici automatiquement.</div>}
              <ul className="memory">
                {pilotNotes.map((n) => (
                  <li key={`${n.id}-${n.at}`}>
                    <span className="muted tnum">{n.lap != null ? `T${n.lap}` : time(n.at)}</span>
                    <span className="memory-src">{n.source}</span>
                    <span className="memory-text">{n.text}{n.tyres ? <span className="muted"> · pneus {n.tyres}</span> : null}</span>
                  </li>
                ))}
              </ul>
              <form className="row" onSubmit={(e) => { e.preventDefault(); addNote(); }}>
                <input className="input" style={{ flex: 1 }} value={note} onChange={(e) => setNote(e.target.value)} placeholder="Ajouter une note (ex. « essayer un cran d'aileron en moins »)" />
                <button className="btn" disabled={!note.trim()}>Ajouter</button>
              </form>
            </div>
          </section>

          <section className="card">
            <div className="card-head"><span className="card-title">Réglages proposés</span><span className="muted">et réponse du pilote</span></div>
            <div className="card-body stack-sm">
              {data.decisions.length === 0 ? (
                <div className="muted small">Aucun réglage proposé pendant cette session.</div>
              ) : (
                <ul className="memory">
                  {data.decisions.map((d, i) => (
                    <li key={`${d.at}-${i}`}>
                      <span className="muted tnum">{d.lap != null ? `T${d.lap}` : time(d.at)}</span>
                      <span className={`memory-src decision-${d.decision === 'refusé' ? 'no' : d.decision === 'sans effet' ? 'meh' : 'yes'}`}>{d.decision}</span>
                      <span className="memory-text">{d.text}{d.kind === 'setup' ? <span className="muted"> · au stand</span> : d.kind === 'course' ? <span className="muted"> · en roulant</span> : null}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}

// ───── Bilan par tour (GET /api/carnets/{id}/bilan) ─────
interface BilanTour { lap: number; time?: number | null; pit: boolean; valid: boolean; fuel?: number | null; energy?: number | null; wearDelta?: number[] | null }
interface Moyenne { relais: number; gomme?: string | null; premier: number; dernier: number; comptes: number; temps?: number | null; essence?: number | null; energie?: number | null; usure?: number[] | null }
interface Bilan { tours: BilanTour[]; exclus: number[]; relais: Moyenne[]; course?: Moyenne | null }

type Valeurs = (number | null | undefined)[];
interface Rubrique { id: string; label: string; cols: string[]; tour: (t: BilanTour) => Valeurs; moyenne: (m: Moyenne) => Valeurs; fmt: (v: number) => string; axe: (v: number) => string; note?: string }

const QUATRE: Valeurs = [null, null, null, null];
const RUBRIQUES: Rubrique[] = [
  { id: 'temps', label: 'Temps au tour', cols: ['Temps'], tour: (t) => [t.time], moyenne: (m) => [m.temps], fmt: lapTime, axe: (v) => lapTime(v).replace(/\.\d+$/, (x) => x.slice(0, 2)) },
  { id: 'essence', label: 'Essence', cols: ['Essence'], tour: (t) => [t.fuel], moyenne: (m) => [m.essence], fmt: (v) => `${v.toFixed(2)} L`, axe: (v) => v.toFixed(2) },
  { id: 'energie', label: 'Énergie virtuelle', cols: ['Énergie'], tour: (t) => [t.energy], moyenne: (m) => [m.energie], fmt: (v) => `${v.toFixed(2)} %`, axe: (v) => v.toFixed(2) },
  { id: 'pneus', label: 'Pneus (usure)', cols: WHEELS.map((w) => WHEEL_SHORT[w]), tour: (t) => t.wearDelta ?? QUATRE, moyenne: (m) => m.usure ?? QUATRE, fmt: (v) => v.toFixed(2), axe: (v) => v.toFixed(2),
    // Unité dite une fois : avec « % » dans chaque case, les quatre roues ne tiennent pas sur un téléphone.
    note: 'Usure prise à chaque tour, en points de pourcentage.' },
];
const COULEURS = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-4)'];
const valeur = (v: number | null | undefined, fmt: (v: number) => string) => (v == null || !isFinite(v) ? '—' : fmt(v));

/** Bilan d'une session par catégorie : chaque tour, moyenne par relais et de la course ; un tour décoché sort des moyennes. */
function BilanCarnet({ id, live }: { id: string; live: boolean }) {
  const url = `/api/carnets/${encodeURIComponent(id)}/bilan`;
  const { data, reload } = usePoll<Bilan>(url, live ? 5000 : 60000);
  const [choix, setChoix] = useState(() => readPref('smhub-carnet-bilan', 'temps'));
  const [envoi, setEnvoi] = useState<number | null>(null);
  const disponible = (x: Rubrique) => data?.tours.some((t) => x.tour(t).some((v) => v != null)) ?? false;
  const r = RUBRIQUES.find((x) => x.id === choix && disponible(x)) ?? RUBRIQUES[0];
  const exclus = useMemo(() => new Set(data?.exclus), [data]);
  const series = useMemo<Series[]>(() => r.cols.map((c, i) => ({
    id: c, label: c, color: COULEURS[i],
    points: (data?.tours ?? []).map((t) => ({ x: t.lap, y: exclus.has(t.lap) ? null : r.tour(t)[i] ?? null })),
  })), [data, r, exclus]);

  if (!data) return null;
  const choisir = (x: string) => { setChoix(x); writePref('smhub-carnet-bilan', x); };
  const compter = async (tour: number, compte: boolean) => {
    setEnvoi(tour);
    await api(`${url}/tours`, { method: 'POST', body: JSON.stringify({ tour, compte }) }).catch(() => undefined);
    setEnvoi(null);
    reload();
  };
  const relaisDe = (lap: number) => data.relais.find((m) => lap >= m.premier && lap <= m.dernier)?.relais;
  const tempsSeuls = data.tours.length > 0 && !data.tours.some((t) => t.fuel != null || t.energy != null || t.wearDelta != null);
  const moyenneCourse = data.course && r.cols.length === 1 ? r.moyenne(data.course)[0] : null;

  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Bilan par tour</span>
        <span className="muted">chaque tour, moyenne par relais et sur la course</span>
      </div>
      <div className="card-toolbar row">
        {RUBRIQUES.map((x) => (
          <button key={x.id} className={`chip ${x.id === r.id ? 'on' : ''}`} onClick={() => choisir(x.id)} disabled={!disponible(x)}>{x.label}</button>
        ))}
      </div>
      <div className="card-body stack-sm">
        {data.tours.length === 0 ? (
          <div className="muted small">Aucun tour enregistré dans ce carnet.</div>
        ) : (
          <>
            {tempsSeuls && <div className="muted small">Ce carnet ne contient que les temps au tour : l'essence, l'énergie et l'usure par tour sont enregistrées à partir de cette version.</div>}
            <LineChart
              series={series}
              height={200}
              xFormat={lapTick}
              xTitle={(x) => `Tour ${Math.round(x)}`}
              yFormat={r.axe}
              valueFormat={r.fmt}
              refLines={[
                ...data.relais.slice(1).map((m) => ({ x: m.premier - 0.5 })),
                ...(moyenneCourse != null ? [{ y: moyenneCourse, label: 'moyenne' }] : []),
              ]}
              ariaLabel={`${r.label} à chaque tour`}
              emptyText="Pas assez de tours comptés pour tracer la courbe."
              footer={<div className="chart-note muted">Traits verticaux : arrêts au stand. Les tours exclus n'apparaissent pas.{r.note ? ` ${r.note}` : ''}</div>}
            />
            <div className="bilan-scroll">
              <table className="mini-table">
                <thead>
                  <tr>
                    <th>Relais</th><th className="hide-xs">Tours</th><th className="r">Comptés</th>
                    {r.cols.map((c) => <th key={c} className="r">{r.cols.length > 1 ? c : 'Moyenne'}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {data.relais.map((m) => (
                    <tr key={m.relais}>
                      <td><span className="row nowrap">{m.relais}<TyreBadge compound={m.gomme ?? null} /></span></td>
                      <td className="tnum hide-xs">T{m.premier} → T{m.dernier}</td>
                      <td className="r tnum">{m.comptes}</td>
                      {r.moyenne(m).map((v, i) => <td key={i} className="r tnum">{valeur(v, r.fmt)}</td>)}
                    </tr>
                  ))}
                  {data.course && (
                    <tr className="bilan-course">
                      <td>Course</td>
                      <td className="tnum hide-xs">T{data.course.premier} → T{data.course.dernier}</td>
                      <td className="r tnum">{data.course.comptes}</td>
                      {r.moyenne(data.course).map((v, i) => <td key={i} className="r tnum">{valeur(v, r.fmt)}</td>)}
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
            <div className="muted small">
              Exclus d'office : les tours au stand (l'arrêt et le ravitaillement faussent temps et consommation) et les tours invalides
              à plus de 2 % du rythme médian. Décochez un tour pour le retirer des moyennes (accident, safety car…), cochez-le pour l'y remettre :
              le choix est enregistré dans le carnet.
            </div>
            <div className="bilan-scroll bilan-tours">
              <table className="mini-table">
                <thead>
                  <tr>
                    <th>Compté</th><th>Tour</th><th className="hide-xs">Relais</th>
                    {r.cols.map((c) => <th key={c} className="r">{c}</th>)}
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {data.tours.map((t) => (
                    <tr key={t.lap} className={exclus.has(t.lap) ? 'bilan-exclu' : ''}>
                      <td>
                        <input type="checkbox" className="bilan-case" checked={!exclus.has(t.lap)} disabled={envoi === t.lap}
                          onChange={(e) => compter(t.lap, e.target.checked)} aria-label={`Compter le tour ${t.lap} dans les moyennes`} />
                      </td>
                      <td className="tnum">{t.lap}</td>
                      <td className="tnum hide-xs">{relaisDe(t.lap)}</td>
                      {r.tour(t).map((v, i) => <td key={i} className="r tnum">{valeur(v, r.fmt)}</td>)}
                      <td className="muted">{[t.pit && 'stand', !t.valid && 'invalide'].filter(Boolean).join(' · ')}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </section>
  );
}
