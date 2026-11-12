import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useLive } from './live';
import { classColorVar, clock, flagLabel, num, sessionLabel } from './format';
import type { DriverRow, RaceSnapshot } from './types';
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
import { BrandLogo } from './components/BrandLogo';

type Tab = 'race' | 'analysis' | 'speeds' | 'sectors' | 'car' | 'weather' | 'strategy' | 'ai' | 'settings';

const TABS: { id: Tab; label: string; icon: string }[] = [
  { id: 'race', label: 'Course', icon: '🏁' },
  { id: 'analysis', label: 'Tours & prévisions', icon: '📈' },
  { id: 'speeds', label: 'Vitesses & trajectoires', icon: '⚡' },
  { id: 'sectors', label: 'Micro-secteurs', icon: '▦' },
  { id: 'car', label: 'Voiture', icon: '🌡' },
  { id: 'weather', label: 'Météo', icon: '☁' },
  { id: 'strategy', label: 'Stratégies', icon: '⏱' },
  { id: 'ai', label: 'Ingénieur IA', icon: '🎙' },
  { id: 'settings', label: 'Réglages', icon: '⚙' },
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
    return TABS.some((t) => t.id === h) ? h : 'race';
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [theme, setThemeState] = useState(() => readPref('smhub-theme', 'auto'));
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
    <div className="app">
      <div className="app-header">
      <TopBar snapshot={snapshot} link={link} />
      <nav className="tabs" aria-label="Sections">
        <div className="tabs-inner">
          {TABS.map((t) => (
            <button key={t.id} className={`tab ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)} aria-current={tab === t.id ? 'page' : undefined}>
              <span className="tab-icon" aria-hidden>{t.icon}</span>
              <span className="tab-label">{t.label}</span>
            </button>
          ))}
        </div>
      </nav>
      </div>
      <main className="content">
        {tab === 'settings' ? (
          <SettingsView theme={theme} setTheme={setThemeState} snapshot={snapshot} />
        ) : tab === 'ai' ? (
          <EngineerView radio={radio} hasLiveData={!!snapshot?.hasData} />
        ) : needsData && !snapshot?.hasData ? (
          <Waiting link={link} onSettings={() => setTab('settings')} onAi={() => setTab('ai')} />
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
      <RadioDock onOpen={() => setTab('ai')} />
      {selected && <DriverDrawer driver={selected} onClose={() => setSelectedId(null)} />}
    </div>
  );
}

function TopBar({ snapshot, link }: { snapshot: RaceSnapshot | null; link: string }) {
  const s = snapshot?.hasData ? snapshot : null;
  const linkColor = link === 'open' ? 'var(--good)' : link === 'connecting' ? 'var(--warning)' : 'var(--critical)';
  const linkText = link === 'open' ? 'Hub connecté' : link === 'connecting' ? 'Connexion…' : 'Hub injoignable';
  const remaining = s?.lengthType === 'Laps' && s.totalLaps > 0 ? `Tour ${s.leaderLap}/${s.totalLaps}` : clock(s?.timeRemaining);
  const me = useMemo(() => s?.drivers.find((d) => d.isPlayer), [s]);
  return (
    <header className="topbar">
      <div className="brand"><span className="brand-mark" aria-hidden>SM</span><span className="brand-name">SecondMonitor <span className="muted">Hub</span></span></div>
      {s && (
        <div className="session">
          <div className="session-block">
            <span className="session-label">{sessionLabel(s.sessionType)} · {s.track.name ?? 'Circuit'}</span>
            <span className="session-clock tnum">{remaining}</span>
          </div>
          {me && (
            <div className="session-block">
              <span className="session-label">Vous{s.isMultiClass ? ` · ${me.className ?? 'catégorie'}` : ''}</span>
              <span className="session-value tnum row nowrap" style={{ gap: 6 }}>
                <BrandLogo car={me.car} team={me.team} size={15} />
                P{s.isMultiClass ? me.classPosition : me.position}
                {s.isMultiClass && <span className="muted hide-sm">· P{me.position} gén.</span>}
              </span>
            </div>
          )}
          {me && (
            <div className="session-block">
              <span className="session-label">Tours</span>
              <span className="session-value tnum">
                {me.laps} bouclés{s.player?.lapsRemaining != null && <span className="muted"> · ~{Math.ceil(s.player.lapsRemaining)} restants</span>}
              </span>
            </div>
          )}
          {s.weather.trackTemp != null && (
            <div className="session-block hide-sm">
              <span className="session-label">Piste · Air</span>
              <span className="session-value tnum">{num(s.weather.trackTemp, 0)}° · {num(s.weather.air, 0)}°</span>
            </div>
          )}
          {s.weather.rain > 0 && <span className="pill rain">🌧 Pluie {s.weather.rain} %</span>}
          {s.flags.map((f) => <span key={f} className="pill flag">⚑ {flagLabel(f)}</span>)}
          {s.safetyCar && s.safetyCar !== 'None' && <span className="pill flag">SC</span>}
        </div>
      )}
      <span className="spacer" />
      {s && <span className="pill hide-sm">{s.source}</span>}
      <span className="link-state" title={linkText}><span className="dot" style={{ background: linkColor }} /><span className="hide-xs">{linkText}</span></span>
    </header>
  );
}

function Waiting({ link, onSettings, onAi }: { link: string; onSettings: () => void; onAi: () => void }) {
  return (
    <div className="empty">
      <div className="empty-icon" aria-hidden>📡</div>
      <h2>{link === 'open' ? 'En attente des données du jeu' : 'Connexion au hub…'}</h2>
      <p>Le hub tourne. Il attend que le PC de jeu diffuse ses données.</p>
      <ol className="steps">
        <li>Sur le PC de jeu, lancez <b>SecondMonitor-Collecteur</b> (ou SecondMonitor avec le plugin « Data Broadcast Server »).</li>
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
