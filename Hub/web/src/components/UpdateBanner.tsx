import { useCallback, useEffect, useState } from 'react';
import './UpdateBanner.css';

/** État des mises à jour, tel que GET /api/update le renvoie (UpdateApi.cs). */
export interface UpdateStatus {
  current: string;
  platform: string;
  automatic: boolean;
  intervalHours: number;
  dismissed: string | null;
  lastCheck: string | null;
  lastCheckError: string | null;
  state: 'attente' | 'verification' | 'telechargement' | 'pret' | 'installation' | 'redemarrage' | 'manuel' | 'erreur';
  done: number;
  total: number;
  error: string | null;
  message: string | null;
  previousFailure: string | null;
  testMode: boolean;
  offer: null | {
    version: string;
    date: string | null;
    notes: string | null;
    page: string | null;
    file: null | { name: string; size: number; key: string };
  };
}

interface UpdateResponse { local: boolean; status: UpdateStatus }

const BUSY = ['telechargement', 'installation', 'redemarrage'];

async function call(path: string, init?: RequestInit): Promise<UpdateStatus | null> {
  const res = await fetch(path, { ...init, headers: { 'Content-Type': 'application/json' } });
  if (!res.ok) {
    const body = await res.json().catch(() => null) as { error?: string } | null;
    throw new Error(body?.error ?? `${res.status} ${res.statusText}`);
  }
  return res.headers.get('content-type')?.includes('json') ? (await res.json()) as UpdateStatus : null;
}

/** Interroge le hub : toutes les minutes au repos, chaque seconde pendant un téléchargement ou une installation. */
export function useUpdate() {
  const [data, setData] = useState<UpdateResponse | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const refresh = useCallback(async () => {
    try {
      const r = await fetch('/api/update');
      if (r.ok) setData((await r.json()) as UpdateResponse);
    } catch { /* hub en cours de redémarrage */ }
  }, []);
  const busy = !!data && BUSY.includes(data.status.state);
  useEffect(() => {
    refresh();
    const t = window.setInterval(refresh, busy ? 1000 : 60000);
    return () => window.clearInterval(t);
  }, [refresh, busy]);
  const act = useCallback(async (path: string, body?: unknown, method = 'POST') => {
    setProblem(null);
    try {
      const s = await call(path, { method, body: body === undefined ? undefined : JSON.stringify(body) });
      if (s) setData((d) => (d ? { ...d, status: s } : d));
      else refresh();
    } catch (e) {
      setProblem((e as Error).message);
    }
  }, [refresh]);
  return {
    local: data?.local ?? false,
    status: data?.status ?? null,
    problem,
    check: () => act('/api/update/check'),
    install: () => act('/api/update/install'),
    dismiss: (version: string) => act('/api/update/dismiss', { version }),
    acknowledge: () => act('/api/update/ack'),
    setAutomatic: (automatic: boolean, intervalHours: number) => act('/api/update/settings', { automatic, intervalHours }, 'PUT'),
  };
}

const mo = (n: number) => `${Math.max(1, Math.round(n / 1048576))} Mo`;
const pct = (s: UpdateStatus) => (s.total ? Math.min(100, Math.round((s.done * 100) / s.total)) : 0);

/** Ce que disent le bandeau et la carte, selon l'avancement. */
function describe(s: UpdateStatus) {
  const v = s.offer?.version ?? '';
  switch (s.state) {
    case 'telechargement': return { text: `Téléchargement de la version ${v}… ${pct(s)} %`, busy: true };
    case 'installation': return { text: 'Préparation de l\'installation…', busy: true };
    case 'redemarrage': return { text: 'SecondMonitor se ferme et se rouvre tout seul avec la nouvelle version.', busy: true };
    case 'manuel': return { text: s.message ?? 'Suivez les étapes du système pour terminer l\'installation.', busy: false };
    default: return { text: '', busy: false };
  }
}

/** Petit bandeau discret : « Mise à jour x.y.z disponible — Installer ». Seulement sur l'ordinateur de SecondMonitor. */
export function UpdateBanner() {
  const u = useUpdate();
  const s = u.status;
  if (!s || !u.local) return null;

  if (s.previousFailure) {
    return (
      <div className="upd-banner warn" role="status">
        <span className="upd-text">La dernière mise à jour n'a pas abouti : {s.previousFailure} Votre version actuelle fonctionne normalement.</span>
        <button className="upd-btn" onClick={u.acknowledge}>Compris</button>
      </div>
    );
  }
  const o = s.offer;
  const d = describe(s);
  if (d.text) {
    return (
      <div className={`upd-banner ${s.state === 'manuel' ? 'warn' : ''}`} role="status">
        <span className="upd-text">{d.text}</span>
        {d.busy && <span className="upd-bar"><i style={{ width: `${s.state === 'telechargement' ? pct(s) : 100}%` }} /></span>}
      </div>
    );
  }
  if (!o || !o.file || s.dismissed === o.version) return null;
  const failed = s.state === 'erreur' || (s.state === 'pret' && !!s.error);
  return (
    <div className={`upd-banner ${failed ? 'warn' : ''}`} role="status">
      <span className="upd-text">
        {failed ? `Mise à jour ${o.version} : ${s.error ?? 'échec'}` : <>Mise à jour <b>{o.version}</b> disponible</>}
      </span>
      <button className="upd-btn primary" onClick={u.install} title={`${mo(o.file.size)} · SecondMonitor et le collecteur, réglages conservés`}>
        {failed ? 'Réessayer' : 'Installer'}
      </button>
      <button className="upd-close" onClick={() => u.dismiss(o.version)} title="Plus tard (reste disponible dans Réglages › Mises à jour)" aria-label="Plus tard">×</button>
      {u.problem && <span className="upd-text">{u.problem}</span>}
    </div>
  );
}

/** Carte « Mises à jour » des Réglages. */
export function UpdateCard() {
  const u = useUpdate();
  const s = u.status;
  const [checking, setChecking] = useState(false);
  if (!s) return null;
  const o = s.offer;
  const d = describe(s);
  const check = async () => { setChecking(true); await u.check(); setChecking(false); };
  const when = s.lastCheck ? new Date(s.lastCheck).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' }) : null;

  return (
    <div className="card">
      <div className="card-head">
        <span className="card-title">Mises à jour</span>
        <span className="muted">version {s.current}{s.testMode ? ' · MODE TEST (serveur local)' : ''}</span>
      </div>
      <div className="card-body stack-sm">
        {s.previousFailure && (
          <div className="cx-advice">
            La dernière mise à jour automatique n'a pas abouti : {s.previousFailure} Votre version actuelle a été conservée.
            {' '}<button className="btn" onClick={u.acknowledge}>Compris</button>
          </div>
        )}
        {o ? (
          <div className="upd-offer">
            <div><b>Version {o.version} disponible</b>{o.date ? <span className="muted"> · publiée le {o.date}</span> : null}</div>
            {o.notes && <pre className="upd-notes">{o.notes.replace(/^#+\s*/gm, '').replace(/\*\*/g, '').trim()}</pre>}
            {d.text ? (
              <div className="upd-progress">
                <span className="secondary">{d.text}</span>
                {d.busy && <span className="upd-bar"><i style={{ width: `${s.state === 'telechargement' ? pct(s) : 100}%` }} /></span>}
              </div>
            ) : o.file ? (
              u.local ? (
                <div className="row">
                  <button className="btn primary" onClick={u.install}>{s.state === 'erreur' || s.error ? 'Réessayer' : 'Installer'}</button>
                  <span className="secondary">
                    {mo(o.file.size)}, vérifié (SHA-256), puis SecondMonitor et le collecteur sont remplacés et rouverts tout seuls.
                    Vos réglages sont conservés. Aucune autorisation à donner dans les Réglages du système.
                  </span>
                </div>
              ) : (
                <span className="secondary">À installer depuis l'ordinateur où SecondMonitor est ouvert (fenêtre de l'application).</span>
              )
            ) : (
              <span className="secondary">
                Pas de fichier pour ce système dans cette version.{' '}
                {o.page && <a href={o.page} target="_blank" rel="noreferrer">Page de téléchargement</a>}
              </span>
            )}
            {(s.error || u.problem) && !d.text && <div className="upd-error">{s.error ?? u.problem} Votre version actuelle n'a pas été modifiée.</div>}
          </div>
        ) : (
          <div className="secondary">
            {s.state === 'verification' || checking ? 'Vérification…' : s.lastCheckError ? `Vérification impossible : ${s.lastCheckError}.` : when ? `SecondMonitor est à jour (vérifié le ${when}).` : 'Pas encore vérifié.'}
          </div>
        )}
        <div className="row">
          <label className="switch-row">
            <input type="checkbox" checked={s.automatic} onChange={(e) => u.setAutomatic(e.target.checked, s.intervalHours)} />
            <span>Rechercher les mises à jour au démarrage puis toutes les</span>
          </label>
          <select className="select" value={s.intervalHours} disabled={!s.automatic} onChange={(e) => u.setAutomatic(s.automatic, Number(e.target.value))} aria-label="Intervalle en heures">
            {[1, 3, 6, 12, 24].map((h) => <option key={h} value={h}>{h} h</option>)}
          </select>
          <span className="spacer" />
          <button className="btn" disabled={checking || BUSY.includes(s.state)} onClick={check}>Vérifier maintenant</button>
        </div>
        <div className="muted" style={{ fontSize: 12 }}>
          Source : publications OneDrop3D/SecondMonitor sur GitHub, fichier {s.platform || '—'}. Le collecteur du PC de jeu se met à jour depuis sa propre fenêtre.
        </div>
      </div>
    </div>
  );
}
