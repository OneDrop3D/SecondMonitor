import { useCallback, useEffect, useRef, useState } from 'react';
import { useLive } from './live';
import { classColorVar } from './format';
import type { DriverRow } from './types';
import { Leaderboard } from './components/Leaderboard';
import { TrackMap } from './components/TrackMap';
import { PlayerPanel } from './components/PlayerPanel';
import { StrategyView } from './components/StrategyView';
import { DriverDrawer } from './components/DriverDrawer';
import { SettingsView } from './components/SettingsView';
import { EngineerView } from './components/EngineerView';
import { AnalysisView } from './components/AnalysisView';
import { SpeedView } from './components/SpeedView';
import { SectorsView } from './components/SectorsView';
import { CarView } from './components/CarView';
import { WeatherView } from './components/WeatherView';
import { BattlePanel } from './components/BattlePanel';
import { RadioDock } from './components/RadioDock';
import { RadioProvider, type RadioMessage } from './radio';
import { playBase64Wav, stopPlayback } from './audio';
import { useSelection } from './selection';
import type { ClassView } from './classView';
import { Icon, StatusBar, TimingHeader, TimingScreen } from './components/Timing';

type Tab = 'timing' | 'race' | 'analysis' | 'speeds' | 'sectors' | 'car' | 'weather' | 'strategy' | 'ai' | 'settings';

// Même organisation que SecondMonitor d'origine : le chronométrage d'abord, les autres écrans dans la barre de droite.
const TABS: { id: Tab; label: string; icon: string }[] = [
  { id: 'timing', label: 'Chronométrage', icon: 'timing' },
  { id: 'race', label: 'Classement & duels', icon: 'race' },
  { id: 'analysis', label: 'Tours & prévisions', icon: 'analysis' },
  { id: 'speeds', label: 'Vitesses & trajectoires', icon: 'speeds' },
  { id: 'sectors', label: 'Micro-secteurs', icon: 'sectors' },
  { id: 'car', label: 'Voiture', icon: 'car' },
  { id: 'weather', label: 'Météo', icon: 'weather' },
  { id: 'strategy', label: 'Stratégies', icon: 'strategy' },
  { id: 'ai', label: 'Ingénieur IA', icon: 'ai' },
  { id: 'settings', label: 'Réglages', icon: 'settings' },
];

function readPref(key: string, fallback: string) {
  try { return localStorage.getItem(key) ?? fallback; } catch { return fallback; }
}
function writePref(key: string, value: string) {
  try { localStorage.setItem(key, value); } catch { /* stockage indisponible */ }
}

export default function App() {
  return (
    <RadioProvider>
      <Shell />
    </RadioProvider>
  );
}

function Shell() {
  const [radio, setRadio] = useState<RadioMessage[]>([]);
  const speakRef = useRef(readPref('smhub-speak', 'true') === 'true');
  const { snapshot, link } = useLive((m) => {
    if (m.type === 'radioHistory') {
      setRadio(m.data as RadioMessage[]);
      return;
    }
    if (m.type === 'radioStop') { stopPlayback(); return; }
    if (m.type !== 'radio') return;
    const msg = m.data as RadioMessage;
    setRadio((r) => [...r.slice(-49), msg]);
    speakRef.current = readPref('smhub-speak', 'true') === 'true';
    if (speakRef.current && msg.audioBase64) playBase64Wav(msg.audioBase64);
  });
  const [tab, setTabState] = useState<Tab>(() => {
    const h = location.hash.slice(1) as Tab;
    return TABS.some((t) => t.id === h) ? h : 'timing';
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [theme, setThemeState] = useState(() => readPref('smhub-theme', 'dark'));
  const [version, setVersion] = useState<string | null>(null);
  useEffect(() => {
    fetch('/api/status').then((r) => r.json()).then((st) => setVersion(st.version ? String(st.version).split('.').slice(0, 3).join('.') : null)).catch(() => undefined);
  }, []);
  const [classView, setClassViewState] = useState<ClassView>(() => readPref('smhub-classview', 'mine'));
  const setClassView = (v: ClassView) => { setClassViewState(v); writePref('smhub-classview', v === 'mine' || v === 'all' ? v : 'mine'); };
  const classOrder = useRef(new Map<string, number>());

  const setTab = (t: Tab) => { setTabState(t); window.scrollTo({ top: 0 }); };

  useEffect(() => {
    if (theme === 'auto') document.documentElement.removeAttribute('data-theme');
    else document.documentElement.setAttribute('data-theme', theme);
    writePref('smhub-theme', theme);
  }, [theme]);

  useEffect(() => { history.replaceState(null, '', `#${tab}`); }, [tab]);

  // Couleur de catégorie stable pendant toute la session (ordre d'apparition, jamais selon le classement).
  const classColor = useCallback((classId: string) => {
    const map = classOrder.current;
    if (!map.has(classId)) map.set(classId, map.size);
    return classColorVar(map.get(classId)!);
  }, []);
  if (snapshot) for (const d of snapshot.drivers) classColor(d.classId);

  const selection = useSelection(snapshot);
  const selected = snapshot?.drivers.find((d) => d.id === selectedId) ?? null;
  const onSelect = (d: DriverRow) => setSelectedId(d.id);
  const needsData = !['ai', 'settings'].includes(tab);

  return (
    <div className="app sm-shell">
      {snapshot?.hasData && <TimingHeader snapshot={snapshot} />}
      <div className="sm-body">
        <main className={`content ${tab === 'timing' && snapshot?.hasData ? 'content-full' : ''}`}>
          {tab === 'settings' ? (
            <SettingsView theme={theme} setTheme={setThemeState} snapshot={snapshot} />
          ) : tab === 'ai' ? (
            <EngineerView radio={radio} hasLiveData={!!snapshot?.hasData} />
          ) : needsData && !snapshot?.hasData ? (
            <Waiting link={link} onSettings={() => setTab('settings')} onAi={() => setTab('ai')} />
          ) : snapshot && tab === 'timing' ? (
            <TimingScreen snapshot={snapshot} classColor={classColor} onSelect={onSelect} classView={classView} setClassView={setClassView} />
          ) : snapshot && tab === 'race' ? (
            <div className="layout-race">
              <div className="area-board"><Leaderboard snapshot={snapshot} classColor={classColor} onSelect={onSelect} classView={classView} setClassView={setClassView} /></div>
              <div className="area-side">
                <TrackMap snapshot={snapshot} classColor={classColor} onSelect={onSelect} classView={classView} />
                <BattlePanel snapshot={snapshot} onSelect={onSelect} />
                <PlayerPanel player={snapshot.player} compact />
              </div>
            </div>
          ) : snapshot && tab === 'analysis' ? (
            <AnalysisView snapshot={snapshot} selection={selection} onSelect={onSelect} />
          ) : snapshot && tab === 'speeds' ? (
            <SpeedView snapshot={snapshot} selection={selection} classColor={classColor} />
          ) : snapshot && tab === 'sectors' ? (
            <SectorsView snapshot={snapshot} classColor={classColor} onSelect={onSelect} />
          ) : snapshot && tab === 'car' ? (
            <CarView snapshot={snapshot} />
          ) : snapshot && tab === 'weather' ? (
            <WeatherView snapshot={snapshot} />
          ) : snapshot && tab === 'strategy' ? (
            <StrategyView snapshot={snapshot} classColor={classColor} onSelect={onSelect} />
          ) : null}
        </main>
        <nav className="sm-rail" aria-label="Écrans">
          {TABS.map((t) => (
            <button key={t.id} className={`sm-rail-btn ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)} aria-current={tab === t.id ? 'page' : undefined} title={t.label}>
              <Icon name={t.icon} size={22} />
              <span className="sm-rail-label">{t.label}</span>
            </button>
          ))}
        </nav>
      </div>
      <StatusBar snapshot={snapshot} link={link} version={version} />
      <RadioDock onOpen={() => setTab('ai')} />
      {selected && <DriverDrawer driver={selected} onClose={() => setSelectedId(null)} />}
    </div>
  );
}

function Waiting({ link, onSettings, onAi }: { link: string; onSettings: () => void; onAi: () => void }) {
  return (
    <div className="empty">
      <div className="empty-icon" aria-hidden>📡</div>
      <h2>{link === 'open' ? 'En attente des données du jeu' : 'Connexion…'}</h2>
      <p>SecondMonitor est prêt. Il attend les données du PC de jeu.</p>
      <ol className="steps">
        <li>Sur le PC de jeu, ouvrez <b>SecondMonitor Collecteur</b> (installé avec SecondMonitor). Rien d'autre à installer.</li>
        <li>Lancez une session dans le jeu (LMU, ACC, rFactor 2, F1, AMS2, R3E…).</li>
        <li>Si rien n'apparaît, indiquez l'adresse IP du PC dans les réglages.</li>
      </ol>
      <div className="row" style={{ justifyContent: 'center' }}>
        <button className="btn primary" onClick={onSettings}>Ouvrir les réglages (ou lancer la démo)</button>
        <button className="btn" onClick={onAi}>Tester l'ingénieur IA</button>
      </div>
    </div>
  );
}
