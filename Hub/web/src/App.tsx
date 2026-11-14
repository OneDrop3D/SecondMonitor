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
import { RadioProvider, isRadioStation, type RadioMessage } from './radio';
import { playBase64Wav, stopPlayback } from './audio';
import { screensPlayVoice, setStations, type StationsState } from './stations';
import { useSelection } from './selection';
import type { ClassView } from './classView';
import { Icon, StatusBar, TimingHeader } from './components/Timing';
import { Dashboard } from './components/dashboard/Dashboard';
import { NoticeProvider, NoticeStack, useNotices } from './components/dashboard/notices';
import { useRaceEvents } from './components/dashboard/raceEvents';
import { ConnectionView, ConnectionHero } from './components/ConnectionView';
import { GuideView } from './components/GuideView';
import { CarnetView } from './components/CarnetView';
import { ErrorBoundary } from './components/ErrorBoundary';
import { collectorAddress, linkStage, useHubStatus } from './status';
import { UpdateBanner } from './components/UpdateBanner';
import { readPref, writePref } from './prefs';

type Tab = 'timing' | 'race' | 'analysis' | 'speeds' | 'sectors' | 'car' | 'weather' | 'strategy' | 'ai' | 'carnet' | 'connection' | 'settings';

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
  { id: 'carnet', label: 'Carnet', icon: 'carnet' },
  { id: 'connection', label: 'Connexion au PC de jeu', icon: 'link' },
  { id: 'settings', label: 'Réglages', icon: 'settings' },
];

export default function App() {
  return (
    <RadioProvider>
      <NoticeProvider>
        <Shell />
      </NoticeProvider>
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
    if (m.type === 'stations') { setStations(m.data as StationsState); return; }
    // Appui venu d'ailleurs : pour le poste radio, sauf si le micro du PC de jeu s'en charge (« poste »).
    if (m.type === 'radioPress') { if (isRadioStation() && !(m.data as { poste?: string | null })?.poste) window.dispatchEvent(new Event('sm-radio-press')); return; }
    if (m.type !== 'radio') return;
    const msg = m.data as RadioMessage;
    setRadio((r) => [...r.slice(-49), msg]);
    speakRef.current = readPref('smhub-speak', 'true') === 'true';
    if (speakRef.current && msg.audioBase64 && screensPlayVoice()) playBase64Wav(msg.audioBase64);
  });
  const [tab, setTabState] = useState<Tab>(() => {
    const h = location.hash.slice(1) as Tab;
    return TABS.some((t) => t.id === h) ? h : 'timing';
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [theme, setThemeState] = useState(() => readPref('smhub-theme', 'dark'));
  const status = useHubStatus();
  const version = status?.version ? String(status.version).split('.').slice(0, 3).join('.') : null;
  const [guide, setGuide] = useState(() => readPref('smhub-guide-vu', '') === '');
  const closeGuide = () => { setGuide(false); writePref('smhub-guide-vu', '1'); };
  const notices = useNotices();
  const [layoutEdit, setLayoutEdit] = useState(false);
  // Événements de course du hub ; drapeaux, dégâts et pénalités s'affichent aussi en notification (croix, disparition seule).
  const raceEvents = useRaceEvents(snapshot, (added) => {
    for (const e of added) {
      if (e.kind === 'flag' && e.level !== 'good' || e.kind === 'damage' || e.kind === 'penalty') {
        notices.notify({ key: `course-${e.kind}-${e.title}`, title: e.title, text: e.text ?? '', level: e.level, source: 'Course', ttl: e.level === 'bad' ? 12000 : 8000, fromEvent: true, quietAfterClose: 60_000 });
      }
    }
  });
  const stage = linkStage(status, snapshot);
  const lastStage = useRef(stage);
  useEffect(() => {
    // Notification quand la chaîne se met à fonctionner (ou se coupe) : on voit tout de suite que ça marche.
    const before = lastStage.current;
    lastStage.current = stage;
    // (Notification avec croix, fermée toute seule ; avant, elle pouvait rester bloquée si l'état changeait deux fois de suite.)
    if (stage === 'ok' && before !== 'ok' && before !== 'demo') {
      const game = status?.source.receiver?.game ?? snapshot?.source;
      const pc = collectorAddress(status);
      notices.resolve('connexion-perdue');
      notices.notify({ key: 'connexion-ok', text: `Données reçues${game ? ` : ${game}` : ''}${pc ? ` (PC ${pc})` : ''}`, level: 'good', source: 'Connexion', ttl: 6000 });
    } else if (before === 'no-collector' && stage !== 'no-collector' && stage !== 'demo') {
      notices.resolve('connexion-perdue');
      notices.notify({ key: 'collecteur-ok', text: `Collecteur connecté${collectorAddress(status) ? ` : ${collectorAddress(status)}` : ''}`, level: 'good', source: 'Connexion', ttl: 6000 });
    } else if (before === 'ok' && stage === 'no-collector') {
      notices.notify({ key: 'connexion-perdue', text: 'Connexion au PC de jeu perdue : reconnexion automatique…', level: 'warn', source: 'Connexion', ttl: 20000 });
    }
  }, [stage]); // eslint-disable-line react-hooks/exhaustive-deps
  const [classView, setClassViewState] = useState<ClassView>(() => readPref('smhub-classview', 'mine'));
  const setClassView = (v: ClassView) => { setClassViewState(v); writePref('smhub-classview', v === 'mine' || v === 'all' ? v : 'mine'); };
  const classOrder = useRef(new Map<string, number>());

  const setTab = (t: Tab) => { setTabState(t); setLayoutEdit(false); window.scrollTo({ top: 0 }); };

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
  const needsData = !['ai', 'carnet', 'settings', 'connection'].includes(tab);

  return (
    <div className="app sm-shell">
      {snapshot?.hasData && (
        <TimingHeader
          snapshot={snapshot}
          tools={tab === 'timing' ? (
            <button className={`tm-tool ${layoutEdit ? 'on' : ''}`} onClick={() => setLayoutEdit((v) => !v)} aria-pressed={layoutEdit} title="Organiser les blocs de l'accueil (déplacer, taille, masquer, ajouter)">
              <Icon name="layout" size={18} /> {layoutEdit ? 'Terminé' : 'Disposition'}
            </button>
          ) : undefined}
        />
      )}
      <div className="sm-body">
        <main className={`content ${tab === 'timing' && snapshot?.hasData ? 'content-full' : ''}`}>
          <ErrorBoundary label={TABS.find((t) => t.id === tab)?.label ?? tab} resetKey={tab}>
          {tab === 'settings' ? (
            <SettingsView theme={theme} setTheme={setThemeState} snapshot={snapshot} />
          ) : tab === 'ai' ? (
            <EngineerView radio={radio} hasLiveData={!!snapshot?.hasData} sessionType={snapshot?.sessionType} />
          ) : tab === 'carnet' ? (
            <CarnetView />
          ) : tab === 'connection' ? (
            <ConnectionView status={status} snapshot={snapshot} />
          ) : needsData && !snapshot?.hasData ? (
            <Waiting link={link} status={status} snapshot={snapshot} onConnection={() => setTab('connection')} onSettings={() => setTab('settings')} onGuide={() => setGuide(true)} />
          ) : snapshot && tab === 'timing' ? (
            <Dashboard
              ctx={{ snapshot, classColor, onSelect, classView, setClassView, radio, events: raceEvents, noticeHistory: notices.history }}
              editing={layoutEdit}
              onDone={() => setLayoutEdit(false)}
            />
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
          </ErrorBoundary>
        </main>
        <nav className="sm-rail" aria-label="Écrans">
          {TABS.map((t) => (
            <button key={t.id} className={`sm-rail-btn ${tab === t.id ? 'active' : ''}`} onClick={() => setTab(t.id)} aria-current={tab === t.id ? 'page' : undefined} title={t.label}>
              <Icon name={t.icon} size={22} />
              <span className="sm-rail-label">{t.label}</span>
            </button>
          ))}
          <button className="sm-rail-btn sm-rail-guide" onClick={() => setGuide(true)} title="Guide de mise en route">
            <span className="sm-rail-q" aria-hidden>?</span>
            <span className="sm-rail-label">Guide de mise en route</span>
          </button>
        </nav>
      </div>
      <StatusBar snapshot={snapshot} link={link} version={version} status={status} onConnection={() => setTab('connection')} />
      <NoticeStack />
      {guide && <GuideView status={status} snapshot={snapshot} onClose={closeGuide} onNavigate={(t) => setTab(t)} />}
      <RadioDock onOpen={() => setTab('ai')} />
      <UpdateBanner />
      {selected && <DriverDrawer driver={selected} onClose={() => setSelectedId(null)} />}
    </div>
  );
}

function Waiting({ link, status, snapshot, onConnection, onSettings, onGuide }: {
  link: string;
  status: ReturnType<typeof useHubStatus>;
  snapshot: Parameters<typeof linkStage>[1];
  onConnection: () => void;
  onSettings: () => void;
  onGuide: () => void;
}) {
  if (link !== 'open') {
    return <div className="empty"><div className="empty-icon" aria-hidden>…</div><h2>Connexion à SecondMonitor…</h2></div>;
  }
  return (
    <div className="waiting">
      <ConnectionHero stage={linkStage(status, snapshot)} status={status} snapshot={snapshot} />
      <div className="row" style={{ justifyContent: 'center' }}>
        <button className="btn primary" onClick={onConnection}>Voir la connexion en détail</button>
        <button className="btn" onClick={onGuide}>Guide de mise en route</button>
        <button className="btn" onClick={onSettings}>Essayer avec la démo</button>
      </div>
    </div>
  );
}
