import { useEffect, useState } from 'react';
import { api } from '../live';
import type { HubSettings, HubStatus, RaceSnapshot } from '../types';
import { collectorAddress, gameHint, linkStage, type LinkStage } from '../status';

/*
 * Écran « Connexion » : dit d'un coup d'œil si la chaîne PC de jeu → collecteur → SecondMonitor fonctionne,
 * et sinon quoi faire, étape par étape. L'adresse du PC se saisit ici si la recherche automatique ne le trouve pas.
 */

const IPV4 = /^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})(:\d{1,5})?$/;

export function stageText(stage: LinkStage, status: HubStatus | null, snapshot: RaceSnapshot | null): { kind: 'good' | 'warn' | 'wait'; icon: string; title: string; text: string } {
  const pc = collectorAddress(status);
  const game = status?.source.receiver?.game ?? snapshot?.source ?? null;
  switch (stage) {
    case 'demo': return { kind: 'good', icon: '✓', title: 'Course de démonstration', text: 'Les données affichées sont fictives. Arrêtez la démo dans les réglages pour revenir au jeu.' };
    case 'replay': return { kind: 'good', icon: '▶', title: 'Relecture d\'un enregistrement', text: status?.source.replay ?? '' };
    case 'ok': {
      const cars = snapshot?.drivers.length ?? 0;
      const kb = status?.source.receiver?.kbPerSecond;
      return { kind: 'good', icon: '✓', title: 'Tout fonctionne', text: `${game ?? 'Le jeu'}${pc ? ` sur ${pc}` : ''} → SecondMonitor${cars ? ` · ${cars} voitures` : ''}${kb ? ` · ${kb.toLocaleString('fr-FR')} Ko/s` : ''}` };
    }
    case 'no-collector': return { kind: 'wait', icon: '1', title: 'Ouvrez le collecteur sur le PC de jeu', text: 'Lancez « SecondMonitor Collecteur » sur le PC de jeu : il est trouvé automatiquement sur le même réseau. Sinon, saisissez son adresse ci-dessous.' };
    case 'no-game': return { kind: 'wait', icon: '2', title: `Collecteur connecté${pc ? ` (${pc})` : ''} : lancez le jeu`, text: 'Démarrez une session dans le jeu. Elle apparaît automatiquement.' };
    case 'no-data': return { kind: 'warn', icon: '3', title: `${game} est ouvert, mais n'envoie pas encore de données`, text: gameHint(game) };
  }
}

export function ConnectionHero({ stage, status, snapshot, onOpen }: { stage: LinkStage; status: HubStatus | null; snapshot: RaceSnapshot | null; onOpen?: () => void }) {
  const t = stageText(stage, status, snapshot);
  return (
    <div className={`cx-hero ${t.kind}`}>
      <div className="cx-big" aria-hidden>{t.icon}</div>
      <div className="cx-hero-body">
        <div className="cx-hero-title">{t.title}</div>
        <div className="cx-hero-text">{t.text}</div>
      </div>
      {onOpen && <button className="btn" onClick={onOpen}>Connexion</button>}
    </div>
  );
}

function Step({ n, state, title, detail }: { n: number; state: 'ok' | 'todo' | 'idle'; title: string; detail: string }) {
  return (
    <div className={`cx-step ${state}`}>
      <div className="cx-step-head"><span className="cx-tick">{state === 'ok' ? '✓' : state === 'todo' ? '!' : n}</span>{title}</div>
      <div className="cx-step-detail">{detail}</div>
    </div>
  );
}

export function ConnectionView({ status, snapshot }: { status: HubStatus | null; snapshot: RaceSnapshot | null }) {
  const [settings, setSettings] = useState<HubSettings | null>(null);
  const [address, setAddress] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState<string | null>(null);

  useEffect(() => { api<HubSettings>('/api/settings').then(setSettings).catch(() => undefined); }, []);

  const stage = linkStage(status, snapshot);
  const rx = status?.source.receiver;
  const pc = collectorAddress(status);
  const game = rx?.game ?? null;
  const connected = !!rx?.isConnected;
  const dataOk = stage === 'ok';
  const hosts = settings?.gameHosts ?? [];
  const peers = rx?.peers ?? [];

  const saveHosts = async (next: string[]) => {
    if (!settings) return;
    try {
      const s = await api<HubSettings>('/api/settings', { method: 'PUT', body: JSON.stringify({ ...settings, gameHosts: next }) });
      setSettings(s);
      setSaved('Enregistré. Connexion en cours…');
      window.setTimeout(() => setSaved(null), 4000);
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const add = () => {
    const a = address.trim();
    const m = IPV4.exec(a);
    if (!m || [m[1], m[2], m[3], m[4]].some((x) => Number(x) > 255)) {
      setError('Adresse non valide. Exemple : 192.168.1.20 (elle est affichée dans la fenêtre du collecteur).');
      return;
    }
    setError(null);
    const full = m[5] ? a : `${a}:${settings?.gamePort ?? 52642}`;
    if (!hosts.includes(full)) saveHosts([...hosts, full]);
    setAddress('');
  };

  const hostState = (h: string) => peers.includes(h) ? 'connecté' : connected ? 'non utilisé' : 'recherche…';

  return (
    <div className="stack" style={{ maxWidth: 980 }}>
      <ConnectionHero stage={stage} status={status} snapshot={snapshot} />

      <div className="cx-steps">
        <Step n={1} state={connected || dataOk ? 'ok' : 'todo'} title="Collecteur (PC de jeu)" detail={connected ? `Connecté : ${pc}` : 'Pas encore trouvé'} />
        <Step n={2} state={game || dataOk ? 'ok' : connected ? 'todo' : 'idle'} title="Jeu" detail={game ?? snapshot?.source ?? (connected ? 'Aucun jeu lancé' : '—')} />
        <Step
          n={3}
          state={dataOk ? 'ok' : game ? 'todo' : 'idle'}
          title="Données de course"
          detail={dataOk ? `${snapshot?.drivers.length ?? 0} voitures${rx?.kbPerSecond ? ` · ${rx.kbPerSecond.toLocaleString('fr-FR')} Ko/s` : ''}` : game ? 'En attente : prenez la piste' : '—'}
        />
      </div>

      {stage === 'no-data' && (
        <div className="cx-advice"><b>Que faire ?</b> {gameHint(game)}</div>
      )}

      <div className="card">
        <div className="card-head"><span className="card-title">Adresse du PC de jeu</span><span className="muted">facultatif</span></div>
        <div className="card-body stack-sm">
          <p className="secondary" style={{ margin: 0 }}>
            Sur le même réseau, le PC est trouvé tout seul. Sinon (Wi-Fi et câble sur deux réseaux différents, VPN), tapez l'adresse affichée
            dans la fenêtre du collecteur, rubrique « Adresse de ce PC ».
          </p>
          <div className="cx-add">
            <input
              className="input cx-ip"
              inputMode="decimal"
              placeholder="192.168.1.20"
              value={address}
              onChange={(e) => { setAddress(e.target.value); setError(null); }}
              onKeyDown={(e) => { if (e.key === 'Enter') add(); }}
              aria-label="Adresse IP du PC de jeu"
            />
            <button className="btn primary" onClick={add}>Ajouter</button>
          </div>
          {error && <div className="cx-error">{error}</div>}
          {saved && <div className="secondary">{saved}</div>}
          {hosts.length > 0 && (
            <div className="cx-hosts">
              {hosts.map((h) => (
                <span key={h} className={`cx-host ${peers.includes(h) ? 'on' : ''}`}>
                  <span className="cx-dot" />{h.replace(/:52642$/, '')}<span className="muted"> · {hostState(h)}</span>
                  <button className="cx-remove" title="Retirer" onClick={() => saveHosts(hosts.filter((x) => x !== h))}>×</button>
                </span>
              ))}
            </div>
          )}
          {!connected && (
            <details className="cx-help">
              <summary>Le PC n'est pas trouvé ?</summary>
              <ul>
                <li>Le collecteur est-il ouvert sur le PC de jeu ? Sa fenêtre affiche « En attente » et l'adresse du PC.</li>
                <li>Windows a-t-il autorisé le collecteur sur le <b>réseau privé</b> ? Sinon : Paramètres Windows › Pare-feu › Autoriser une application › « SecondMonitor-Collecteur ».</li>
                <li>Les deux ordinateurs sont-ils sur le même réseau ? Sinon, ajoutez l'adresse du PC ci-dessus.</li>
                <li>Le SecondMonitor d'origine est-il ouvert sur le PC ? Fermez-le : il utilise le même port que le collecteur.</li>
              </ul>
            </details>
          )}
        </div>
      </div>

      <div className="card">
        <div className="card-head"><span className="card-title">Cet ordinateur</span><span className="muted">pour ouvrir SecondMonitor sur un iPad, un téléphone ou un autre PC</span></div>
        <div className="card-body cx-hosts">
          {(rx?.localAddresses ?? []).map((a) => <span key={a} className="cx-host"><span className="code">http://{a}:{settings?.webPort ?? 8686}</span></span>)}
          {status?.httpsPort != null && (rx?.localAddresses ?? []).slice(0, 1).map((a) => (
            <span key={`s${a}`} className="cx-host"><span className="muted">micro et volant :</span> <span className="code">https://{a}:{status.httpsPort}</span></span>
          ))}
        </div>
      </div>

      {status && (
        <details className="card cx-log">
          <summary className="card-head"><span className="card-title">Journal de réception</span><span className="muted">enregistrements et journal dans {status.dataDir}</span></summary>
          <div className="card-body"><div className="log">{status.source.log.join('\n') || '—'}</div></div>
        </details>
      )}
    </div>
  );
}
