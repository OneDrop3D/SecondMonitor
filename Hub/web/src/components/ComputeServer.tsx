import { useEffect, useState } from 'react';
import { usePoll } from '../live';
import { readPref } from '../prefs';
import { UpdateBanner, UpdateCard } from './UpdateBanner';

/*
 * Utilisation à trois ordinateurs : PC de jeu (collecteur) → ordinateur 2 (SecondMonitor qui affiche la course)
 * → ordinateur 3 (« serveur de calcul » : SecondMonitor qui prête l'Ollama de sa grosse carte graphique).
 *   ComputeServerView    écran de l'ordinateur 3 (code d'appairage, SecondMonitor reliés, modèles)
 *   ModeChoice           question du premier lancement : afficher la course, ou serveur de calcul
 *   ComputeModeCard      même choix dans Réglages
 *   ComputeLinkPanel     ordinateur 2 : serveurs trouvés sur le réseau, « Relier » avec le code (carte « Machines IA »)
 */

interface ComputeState {
  mode: string;
  id: string;
  nom: string;
  nomChoisi: string;
  port: number;
  portAnnonces: number;
  code: string | null;
  adresses: string[];
  ollama: { url: string; ok: boolean; error: string | null; verifie: boolean; modeles: { nom: string; taille: number }[] };
  annonce: { ilYA: number | null; destinations: number };
  clients: { id: string; nom: string; adresse: string | null; relieLe: string; enLigne: boolean; vuIlYA: number | null; requetes: number; derniereRequete: string | null }[];
}

interface Discovered {
  port: number;
  erreur: string | null;
  serveurs: { id: string; nom: string; adresse: string; port: number; modeles: number; ollama: boolean; vuIlYA: number; machine: string | null }[];
  liens: Record<string, { ok: boolean; refuse: boolean; error: string | null; ilYA: number }>;
}

/** Requête JSON dont l'erreur (« { error } ») est rendue lisible. */
async function send<T>(path: string, method: string, body?: unknown): Promise<T> {
  const res = await fetch(path, { method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body) });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error((data as { error?: string } | null)?.error ?? `${res.status} ${res.statusText}`);
  return data as T;
}

/** Change l'usage de cet ordinateur, puis recharge l'interface (écran du serveur de calcul, ou application complète). */
async function setMode(mode: 'affichage' | 'calcul') {
  await send('/api/mode', 'POST', { mode });
  window.location.reload();
}

const go = (bytes: number) => `${(bytes / 1e9).toFixed(1)} Go`;

function ago(seconds: number | null | undefined): string {
  if (seconds == null) return 'jamais vu';
  if (seconds < 5) return 'à l\'instant';
  if (seconds < 90) return `il y a ${Math.round(seconds)} s`;
  if (seconds < 5400) return `il y a ${Math.round(seconds / 60)} min`;
  return `il y a ${Math.round(seconds / 3600)} h`;
}

const isLocalScreen = () => ['localhost', '127.0.0.1'].includes(location.hostname);

/** Écran de l'ordinateur 3 en mode serveur de calcul. */
export function ComputeServerView() {
  const { data: s, error, reload } = usePoll<ComputeState>('/api/calcul/etat', 2000);
  // Même thème que l'application (choisi dans ses Réglages) : ici, pas de barre d'application pour l'appliquer.
  useEffect(() => {
    const theme = readPref('smhub-theme', 'dark');
    if (theme === 'auto') document.documentElement.removeAttribute('data-theme');
    else document.documentElement.setAttribute('data-theme', theme);
  }, []);
  const [name, setName] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const act = async (fn: () => Promise<unknown>, done?: string) => {
    try {
      await fn();
      setMessage(done ?? null);
      reload();
    } catch (e) {
      setMessage((e as Error).message);
    }
  };

  if (!s) {
    return <div className="empty"><div className="empty-icon" aria-hidden>…</div><h2>{error ? `Serveur de calcul injoignable (${error})` : 'Serveur de calcul…'}</h2></div>;
  }

  const online = s.clients.filter((c) => c.enLigne);
  return (
    <div className="calcul-page">
      <header className="calcul-head">
        <div>
          <div className="calcul-kicker">SecondMonitor · serveur de calcul</div>
          <h1>{s.nom}</h1>
          <div className="secondary">
            Cet ordinateur prête les modèles de son Ollama au SecondMonitor d'un autre ordinateur, qui lui envoie ses questions
            et reçoit les réponses. Seuls les SecondMonitor reliés avec le code peuvent s'en servir.
          </div>
        </div>
        <div className={`status ${online.length > 0 ? 'ok' : 'short'}`}>
          <span className="ico">{online.length > 0 ? '✓' : '…'}</span>
          {online.length > 0 ? `Relié à ${online.map((c) => c.nom).join(', ')}` : 'En attente d\'un SecondMonitor'}
        </div>
      </header>
      {message && <div className="cx-advice">{message}</div>}

      <div className="calcul-grid">
        <section className="card">
          <div className="card-head"><span className="card-title">Code d'appairage</span><span className="muted">à saisir sur l'ordinateur qui affiche la course</span></div>
          <div className="card-body stack-sm">
            <div className="calcul-code" aria-label="Code d'appairage">{s.code ?? '— — —'}</div>
            <ol className="guide-list secondary small">
              <li>Sur l'ordinateur qui affiche la course : <b>Ingénieur IA</b> › carte <b>Machines IA</b>.</li>
              <li>À côté de « {s.nom} », cliquez <b>Relier</b> et saisissez ce code.</li>
              <li>Ses modèles apparaissent alors avec « · {s.nom} » dans les listes de modèles et de spécialistes.</li>
            </ol>
            <div className="row">
              <button className="chip" onClick={() => act(() => send('/api/calcul/code', 'POST'), 'Nouveau code : l\'ancien ne marche plus.')}>Nouveau code</button>
              <span className="muted small">Le code change après chaque appairage, et après 5 erreurs.</span>
            </div>
          </div>
        </section>

        <section className="card">
          <div className="card-head"><span className="card-title">SecondMonitor reliés</span><span className="muted">{s.clients.length || 'aucun'}</span></div>
          <div className="card-body stack-sm">
            {s.clients.length === 0 && <div className="muted">Aucun pour l'instant : reliez l'ordinateur qui affiche la course avec le code ci-contre.</div>}
            {s.clients.map((c) => (
              <div key={c.id} className="node-row">
                <span className="dot" style={{ background: c.enLigne ? 'var(--good)' : 'var(--surface-3)' }} title={c.enLigne ? 'en ligne' : 'hors ligne'} />
                <b>{c.nom}</b>
                <span className="muted small">{c.adresse ?? ''}</span>
                <span className="spacer" />
                <span className="muted small">
                  {c.enLigne ? 'en ligne' : ago(c.vuIlYA)}
                  {c.requetes > 0 ? ` · ${c.requetes} demande(s)` : ''}
                  {c.derniereRequete ? ` · ${c.derniereRequete}` : ''}
                </span>
                <button className="close" aria-label={`Retirer ${c.nom}`} title="Retirer : son jeton est refusé dès maintenant"
                  onClick={() => { if (confirm(`Retirer « ${c.nom} » ? Il faudra le relier de nouveau avec un code.`)) act(() => send(`/api/calcul/clients/${encodeURIComponent(c.id)}`, 'DELETE'), `« ${c.nom} » retiré.`); }}>✕</button>
              </div>
            ))}
          </div>
        </section>

        <section className="card">
          <div className="card-head">
            <span className="card-title">Ollama de cet ordinateur</span>
            <span className="muted">{s.ollama.url}</span>
            <span className="spacer" />
            <button className="chip" onClick={() => act(() => send('/api/calcul/actualiser', 'POST'))}>Actualiser</button>
          </div>
          <div className="card-body stack-sm">
            {!s.ollama.verifie ? (
              <div className="muted">Vérification…</div>
            ) : s.ollama.ok ? (
              <>
                <div className="status ok"><span className="ico">✓</span>Ollama répond : {s.ollama.modeles.length} modèle(s)</div>
                {s.ollama.modeles.length === 0 && <div className="secondary small">Aucun modèle installé : par exemple <span className="code">ollama pull qwen3:32b</span>.</div>}
                {s.ollama.modeles.map((m) => <div key={m.nom} className="kv"><span>{m.nom}</span><span>{go(m.taille)}</span></div>)}
              </>
            ) : (
              <>
                <div className="status critical"><span className="ico">!</span>Ollama ne répond pas ({s.ollama.error})</div>
                <div className="secondary small">
                  Installez Ollama depuis <b>ollama.com</b> puis lancez-le sur cet ordinateur. Inutile de l'ouvrir au réseau :
                  SecondMonitor le relaie lui-même, et seulement pour les SecondMonitor reliés.
                </div>
              </>
            )}
          </div>
        </section>

        <section className="card">
          <div className="card-head"><span className="card-title">Sur le réseau</span></div>
          <div className="card-body stack-sm">
            <div className="secondary small">
              Annoncé sous le nom « {s.nom} » (port UDP {s.portAnnonces}) {s.annonce.ilYA != null ? `· dernière annonce ${ago(s.annonce.ilYA)}` : ''}.
              Si l'autre ordinateur ne le trouve pas tout seul, il peut le relier par son adresse :
            </div>
            <div className="row">
              {s.adresses.length === 0 && <span className="muted small">aucune adresse réseau</span>}
              {s.adresses.map((a) => <span key={a} className="code">{a}:{s.port}</span>)}
            </div>
            <form className="row" onSubmit={(e) => { e.preventDefault(); if (name !== null) act(() => send('/api/calcul/nom', 'PUT', { nom: name }), 'Nom enregistré.').then(() => setName(null)); }}>
              <input className="input" style={{ flex: '1 1 180px' }} value={name ?? s.nomChoisi} placeholder={s.nom} onChange={(e) => setName(e.target.value)} aria-label="Nom de ce serveur" />
              <button className="btn" disabled={name === null}>Renommer</button>
            </form>
            <div className="muted small">
              Windows : si le pare-feu le demande, autorisez SecondMonitor sur les <b>réseaux privés</b>. Les échanges restent sur le réseau local, en clair :
              n'utilisez pas le serveur de calcul sur un réseau public.
            </div>
          </div>
        </section>

        <section className="card">
          <div className="card-head"><span className="card-title">Usage de cet ordinateur</span></div>
          <div className="card-body row">
            <span className="secondary">Pour afficher la course sur cet ordinateur au lieu de prêter ses modèles :</span>
            <span className="spacer" />
            <button className="btn" onClick={() => { if (confirm('Cet ordinateur n\'est plus serveur de calcul : les SecondMonitor reliés perdent ses modèles. Continuer ?')) act(() => setMode('affichage')); }}>Afficher la course ici</button>
            {isLocalScreen() && <button className="btn" onClick={() => { if (confirm('Quitter SecondMonitor ? Les SecondMonitor reliés perdent ses modèles.')) fetch('/api/quit', { method: 'POST' }); }}>Quitter</button>}
          </div>
        </section>

        <UpdateCard />
      </div>
      <UpdateBanner />
    </div>
  );
}

/** Premier lancement : comment utiliser cet ordinateur ? */
export function ModeChoice() {
  const [error, setError] = useState<string | null>(null);
  const choose = (mode: 'affichage' | 'calcul') => setMode(mode).catch((e) => setError((e as Error).message));
  return (
    <div className="guide-backdrop" style={{ zIndex: 95 }} role="dialog" aria-modal="true" aria-label="Usage de cet ordinateur">
      <div className="guide">
        <div className="guide-head"><b>Bienvenue dans SecondMonitor</b></div>
        <div className="guide-body">
          <h2>Comment utiliser cet ordinateur ?</h2>
          <div className="guide-two">
            <button className="guide-card calcul-choice" onClick={() => choose('affichage')}>
              <b>Afficher la course</b>
              <span>Chronométrage, stratégie et ingénieur IA, avec les données du PC de jeu. C'est le choix habituel.</span>
            </button>
            <button className="guide-card calcul-choice" onClick={() => choose('calcul')}>
              <b>Serveur de calcul</b>
              <span>
                Cet ordinateur prête les modèles de son Ollama (grosse carte graphique) au SecondMonitor d'un autre ordinateur,
                qui lui envoie ses questions. Il n'affiche pas la course.
              </span>
            </button>
          </div>
          <p className="secondary small">Vous pourrez changer d'avis dans Réglages › « Usage de cet ordinateur ».</p>
          {error && <div className="status critical"><span className="ico">!</span>{error}</div>}
        </div>
      </div>
    </div>
  );
}

/** Réglages : afficher la course, ou devenir serveur de calcul. */
export function ComputeModeCard() {
  const [error, setError] = useState<string | null>(null);
  const toCompute = () => {
    if (!confirm('Cet ordinateur devient serveur de calcul : il n\'affiche plus la course et ne reçoit plus les données du PC de jeu. Ses modèles servent au SecondMonitor d\'un autre ordinateur. Continuer ?')) return;
    setMode('calcul').catch((e) => setError((e as Error).message));
  };
  return (
    <div className="card">
      <div className="card-head"><span className="card-title">Usage de cet ordinateur</span></div>
      <div className="card-body stack-sm">
        <div className="row">
          <div className="seg">
            <button className="on">Afficher la course</button>
            <button onClick={toCompute}>Serveur de calcul</button>
          </div>
        </div>
        <p className="secondary small" style={{ margin: 0 }}>
          <b>Serveur de calcul</b> : pour un troisième ordinateur (PC avec une grosse carte graphique, autre Mac) qui fait tourner Ollama.
          Il prête ses modèles au SecondMonitor qui affiche la course : reliez-le ensuite dans Ingénieur IA › Machines IA, avec le code affiché sur son écran.
          Aussi au démarrage : <span className="code">--serveur-calcul</span>.
        </p>
        {error && <div className="status critical"><span className="ico">!</span>{error}</div>}
      </div>
    </div>
  );
}

/**
 * Ordinateur 2 : serveurs de calcul entendus sur le réseau, et liaison avec le code affiché sur leur écran.
 * <paramref name="onLinked"/> : relire les réglages (nouvelle machine) puis les modèles.
 */
export function ComputeLinkPanel({ onLinked }: { onLinked: () => void }) {
  const { data, reload } = usePoll<Discovered>('/api/calcul/decouverts', 3000);
  const [open, setOpen] = useState<string | null>(null);
  const [code, setCode] = useState('');
  const [address, setAddress] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);

  const link = async (adresse: string, port: number | undefined, id: string) => {
    setBusy(true);
    setMessage(null);
    try {
      const r = await send<{ machine: string }>('/api/calcul/relier', 'POST', { adresse, port, code });
      setMessage({ ok: true, text: `Relié : ses modèles apparaissent avec « · ${r.machine} » dans Modèles et Spécialistes.` });
      setOpen(null);
      setCode('');
      setAddress('');
      onLinked();
      reload();
    } catch (e) {
      setMessage({ ok: false, text: (e as Error).message });
      setOpen(id);
    } finally {
      setBusy(false);
    }
  };

  const codeInput = (autoFocus: boolean) => (
    <input className="input calcul-code-input" inputMode="numeric" autoComplete="off" value={code} onChange={(e) => setCode(e.target.value)}
      placeholder="code à 6 chiffres" aria-label="Code d'appairage" autoFocus={autoFocus} />
  );
  const servers = data?.serveurs ?? [];

  return (
    <div className="stack-sm calcul-link">
      <div className="row">
        <b className="small">Serveurs de calcul sur le réseau</b>
        <span className="muted small">un autre ordinateur avec SecondMonitor en mode « serveur de calcul »</span>
      </div>
      {data?.erreur && <div className="status short small"><span className="ico">!</span>{data.erreur}</div>}
      {servers.length === 0 && !data?.erreur && <div className="muted small">Aucun entendu pour l'instant (annonces sur le port UDP {data?.port ?? 8689}).</div>}
      {servers.map((s) => (
        <div key={s.id} className="stack-sm">
          <div className="node-row">
            <span className="dot" style={{ background: s.ollama ? 'var(--good)' : 'var(--warning)' }} title={s.ollama ? 'Ollama prêt' : 'Ollama ne répond pas sur ce serveur'} />
            <b>{s.nom}</b>
            <span className="muted small">{s.adresse}:{s.port} · {s.modeles} modèle(s){s.ollama ? '' : ' · Ollama arrêté'}</span>
            <span className="spacer" />
            {s.machine && !data?.liens[s.machine]?.refuse
              ? <span className="pill">relié · {s.machine}</span>
              : <button className="btn small-btn" onClick={() => { setOpen(open === s.id ? null : s.id); setCode(''); setMessage(null); }}>{s.machine ? 'Relier de nouveau' : 'Relier'}</button>}
          </div>
          {open === s.id && (
            <form className="row" onSubmit={(e) => { e.preventDefault(); link(s.adresse, s.port, s.id); }}>
              <span className="secondary small">Code affiché sur « {s.nom} » :</span>
              {codeInput(true)}
              <button className="btn primary" disabled={busy || code.replace(/\D/g, '').length !== 6}>{busy ? 'Liaison…' : 'Relier'}</button>
            </form>
          )}
        </div>
      ))}
      <details className="small secondary">
        <summary>Relier par son adresse (si le serveur n'apparaît pas)</summary>
        <form className="row" style={{ marginTop: 8 }} onSubmit={(e) => { e.preventDefault(); link(address, undefined, 'adresse'); }}>
          <input className="input" style={{ flex: '2 1 180px' }} value={address} onChange={(e) => setAddress(e.target.value)} placeholder="192.168.1.40 ou pc-calcul.local:8686" aria-label="Adresse du serveur de calcul" />
          {codeInput(false)}
          <button className="btn" disabled={busy || !address.trim() || code.replace(/\D/g, '').length !== 6}>Relier</button>
        </form>
        <div className="muted" style={{ marginTop: 6 }}>L'adresse est affichée sur l'écran du serveur de calcul, carte « Sur le réseau ».</div>
      </details>
      {message && <div className={`status ${message.ok ? 'ok' : 'critical'}`}><span className="ico">{message.ok ? '✓' : '!'}</span>{message.text}</div>}
    </div>
  );
}
