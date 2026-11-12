import { useEffect, useState } from 'react';
import { api } from '../live';
import type { HubSettings, HubStatus, RaceSnapshot } from '../types';
import { ThermalProfiles } from './ThermalProfiles';
import { WheelBindingCard } from './EngineerView';

export function SettingsView({ theme, setTheme, snapshot }: { theme: string; setTheme: (t: string) => void; snapshot: RaceSnapshot | null }) {
  const [settings, setSettings] = useState<HubSettings | null>(null);
  const [status, setStatus] = useState<HubStatus | null>(null);
  const [hostsText, setHostsText] = useState('');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api<HubSettings>('/api/settings').then((s) => { setSettings(s); setHostsText(s.gameHosts.join('\n')); });
    const load = () => api<HubStatus>('/api/status').then(setStatus).catch(() => {});
    load();
    const t = window.setInterval(load, 2000);
    return () => window.clearInterval(t);
  }, []);

  const save = async (patch: Partial<HubSettings>) => {
    if (!settings) return;
    setSaving(true);
    try {
      const next = await api<HubSettings>('/api/settings', { method: 'PUT', body: JSON.stringify({ ...settings, ...patch }) });
      setSettings(next);
      setMessage('Réglages enregistrés');
      window.setTimeout(() => setMessage(null), 2500);
    } catch (e) {
      setMessage(`Erreur : ${(e as Error).message}`);
    } finally {
      setSaving(false);
    }
  };

  if (!settings) return <div className="empty">Chargement…</div>;
  const rx = status?.source.receiver;
  const macAddresses = rx?.localAddresses ?? [];

  return (
    <div className="stack" style={{ maxWidth: 900 }}>
      <div className="card">
        <div className="card-head"><span className="card-title">Connexion au PC de jeu</span></div>
        <div className="card-body">
          <p className="row" style={{ gap: 8 }}>
            État :
            {settings.demoMode ? (
              <span className="pill">Mode démo actif</span>
            ) : rx?.isConnected ? (
              <span className="status ok"><span className="ico">✓</span>Connecté à {rx.peers.join(', ')}</span>
            ) : (
              <span className="status short"><span className="ico">…</span>Recherche du PC de jeu…</span>
            )}
          </p>

          <ol className="secondary" style={{ lineHeight: 1.7, paddingLeft: 18 }}>
            <li>Sur le PC de jeu : copiez le dossier <span className="code">Collector/dist/win-x64</span> et lancez <b>SecondMonitor-Collecteur.exe</b> (rien à installer, très léger). Linux / SteamOS : voir LISEZ-MOI du collecteur.</li>
            <li>Si Windows le demande, autorisez l'accès au <b>réseau privé</b>. Pour qu'il démarre tout seul : <span className="code">SecondMonitor-Collecteur.exe --autostart on</span>.</li>
            <li>Le Mac le trouve tout seul sur le même réseau. Sinon (Wi-Fi ≠ Ethernet, autre sous-réseau, Tailscale), indiquez ci-dessous l'adresse IP du PC.</li>
            <li>Autre possibilité : le SecondMonitor complet avec le plugin <b>« Data Broadcast Server »</b> (port <span className="code">{settings.gamePort}</span>).</li>
          </ol>

          <div className="field">
            <label htmlFor="hosts">Adresse(s) du PC de jeu (une par ligne, optionnel)</label>
            <textarea id="hosts" className="input" rows={3} value={hostsText} onChange={(e) => setHostsText(e.target.value)} placeholder="192.168.1.20" />
          </div>
          <div className="row">
            <button className="btn primary" disabled={saving} onClick={() => save({ gameHosts: hostsText.split(/\s+/).filter(Boolean) })}>Enregistrer</button>
            {message && <span className="secondary">{message}</span>}
          </div>

        </div>
      </div>

      <div className="card">
        <div className="card-head"><span className="card-title">Ouvrir sur un autre appareil</span><span className="muted">PC de jeu, iPad, téléphone</span></div>
        <div className="card-body stack-sm">
          <div className="addr-grid">
            <span className="secondary">Affichage seul</span>
            <span>{macAddresses.length ? macAddresses.map((a) => <span key={a} className="code addr">http://{a}:{settings.webPort}</span>) : '—'}</span>
            <span className="secondary">Avec micro et volant</span>
            <span>
              {status?.httpsPort
                ? macAddresses.map((a) => <a key={a} className="code addr" href={`https://${a}:${status.httpsPort}`} target="_blank" rel="noreferrer">https://{a}:{status.httpsPort}</a>)
                : <span className="muted">HTTPS désactivé</span>}
            </span>
          </div>
          <p className="secondary small" style={{ margin: 0 }}>
            Le navigateur n'autorise le micro et le volant qu'en HTTPS (ou sur ce Mac via localhost). La première fois, il affiche un avertissement
            car le certificat est créé par le hub : cliquez sur « Paramètres avancés » puis « Continuer vers le site ». Rien ne sort de votre réseau.
          </p>
        </div>
      </div>

      <WheelBindingCard />

      <ThermalProfiles snapshot={snapshot} />

      <div className="card">
        <div className="card-head"><span className="card-title">Course de démonstration</span></div>
        <div className="card-body">
          <p className="secondary">Une course fictive (16 voitures, 2 catégories, arrêts, usure, énergie virtuelle) pour découvrir l'interface sans jeu.</p>
          <div className="row">
            <button className={`btn ${settings.demoMode ? '' : 'primary'}`} disabled={saving} onClick={() => save({ demoMode: !settings.demoMode })}>
              {settings.demoMode ? 'Arrêter la démo' : 'Lancer la démo'}
            </button>
            <label className="row secondary" style={{ gap: 6 }}>
              Vitesse
              <select className="select" value={settings.demoSpeed} onChange={(e) => save({ demoSpeed: Number(e.target.value) })}>
                {[1, 2, 5, 10].map((v) => <option key={v} value={v}>×{v}</option>)}
              </select>
            </label>
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-head"><span className="card-title">Affichage</span></div>
        <div className="card-body row">
          <span className="secondary">Thème</span>
          <div className="seg">
            {[['auto', 'Auto'], ['dark', 'Sombre'], ['light', 'Clair']].map(([k, l]) => (
              <button key={k} className={theme === k ? 'on' : ''} onClick={() => setTheme(k)}>{l}</button>
            ))}
          </div>
        </div>
      </div>

      {['localhost', '127.0.0.1'].includes(location.hostname) && (
        <div className="card">
          <div className="card-head"><span className="card-title">Arrêter le hub</span></div>
          <div className="card-body row">
            <span className="secondary">Ferme le serveur sur ce Mac (les autres écrans perdent la connexion).</span>
            <span className="spacer" />
            <button className="btn" onClick={() => { if (confirm('Arrêter SecondMonitor Hub ?')) fetch('/api/quit', { method: 'POST' }); }}>Quitter le hub</button>
          </div>
        </div>
      )}

      {status && (
        <div className="card">
          <div className="card-head"><span className="card-title">Journal</span><span className="muted">{status.clients} écran(s) connecté(s) · données dans {status.dataDir}</span></div>
          <div className="card-body"><div className="log">{status.source.log.join('\n') || '—'}</div></div>
        </div>
      )}
    </div>
  );
}
