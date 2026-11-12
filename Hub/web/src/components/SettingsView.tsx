import { useEffect, useState } from 'react';
import { api } from '../live';
import type { HubSettings, HubStatus, RaceSnapshot } from '../types';
import { ThermalProfiles } from './ThermalProfiles';
import { WheelBindingCard } from './EngineerView';
import { isRadioStation, setRadioStation } from '../radio';

export function SettingsView({ theme, setTheme, snapshot }: { theme: string; setTheme: (t: string) => void; snapshot: RaceSnapshot | null }) {
  const [settings, setSettings] = useState<HubSettings | null>(null);
  const [status, setStatus] = useState<HubStatus | null>(null);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    api<HubSettings>('/api/settings').then(setSettings);
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

  return (
    <div className="stack" style={{ maxWidth: 900 }}>
      {message && <div className="cx-advice">{message}</div>}
      <RadioSourcesCard />
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
          <div className="card-head"><span className="card-title">Quitter SecondMonitor</span></div>
          <div className="card-body row">
            <span className="secondary">Ferme SecondMonitor sur cet ordinateur (les autres écrans perdent la connexion).</span>
            <span className="spacer" />
            <button className="btn" onClick={() => { if (confirm('Quitter SecondMonitor ?')) fetch('/api/quit', { method: 'POST' }); }}>Quitter</button>
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

/** Où appuyer pour parler à l'ingénieur : volant de cet ordinateur, touche ou volant du PC de jeu, ClickDeck. */
function RadioSourcesCard() {
  const [station, setStation] = useState(isRadioStation());
  return (
    <div className="card">
      <div className="card-head"><span className="card-title">Bouton radio</span><span className="muted">un appui pour parler, un second pour envoyer</span></div>
      <div className="card-body stack-sm">
        <ul className="guide-list secondary" style={{ margin: 0 }}>
          <li><b>Volant ou manette branché sur cet ordinateur</b> : choisissez le bouton ci-dessous.</li>
          <li><b>Touche du clavier ou bouton du volant du PC de jeu</b> : dans la fenêtre du <b>collecteur</b>, Réglages › « Bouton radio » › Choisir, puis appuyez sur la touche. Marche même quand le jeu est au premier plan.</li>
          <li><b>ClickDeck</b> : touche « Radio » du profil « SecondMonitor — Course ».</li>
        </ul>
        <label className="switch-row">
          <input type="checkbox" checked={station} onChange={(e) => { setRadioStation(e.target.checked); setStation(e.target.checked); }} />
          <span>Cet écran est le <b>poste radio</b> : il écoute le micro quand on appuie sur un bouton du PC de jeu ou de ClickDeck. Un seul écran devrait l'être (par défaut, la fenêtre de SecondMonitor).</span>
        </label>
      </div>
    </div>
  );
}
