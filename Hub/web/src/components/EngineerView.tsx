import { useCallback, useEffect, useRef, useState } from 'react';
import { api, usePoll } from '../live';
import { Recorder, audioPref, canChooseSpeaker, listAudioDevices, playBase64Wav, setAudioPref, testSpeaker } from '../audio';
import { useRadio, type RadioMessage } from '../radio';
import type { HubSettings, MemoryState, NodeStatus, OllamaModel } from '../types';
import { CategoryPill, type CarnetSummary } from './CarnetView';
import { useStations } from '../stations';

interface Diagnostic {
  step: string;
  ok: boolean;
  milliseconds: number;
  detail: string;
  model?: string | null;
  audioBase64?: string | null;
}

interface ModelsResponse { ok: boolean; error?: string | null; models: OllamaModel[]; nodes: NodeStatus[] }

// Questions rapides selon la session : la course se joue sur la stratégie, les essais sur la voiture.
const QUICK_RACE = [
  'Quand est-ce que je rentre au stand ?',
  'Undercut possible sur la voiture devant ?',
  'Si je rentre maintenant, je ressors où ?',
  'Quel rythme pour gagner ?',
  'Mon meilleur tour ?',
  'Écart avec la voiture derrière ?',
  "Combien de tours d'essence ?",
  'Des dégâts sur la voiture ?',
  'La météo va évoluer ?',
];
const QUICK_PRACTICE = [
  'Comment sont mes pneus ?',
  'Mes pressions sont bonnes ?',
  'Où je perds du temps ?',
  'Quel est mon meilleur tour théorique ?',
  'Température des freins ?',
  'Quelle est ma vitesse de pointe ?',
  'Je suis régulier ?',
  'Des dégâts sur la voiture ?',
  'La météo va évoluer ?',
];

/** Réglages partagés par les cartes de cet onglet (un seul enregistrement, pas d'écrasement entre cartes). */
function useHubSettings() {
  const [settings, setSettingsState] = useState<HubSettings | null>(null);
  const current = useRef<HubSettings | null>(null);
  const setSettings = (s: HubSettings) => { current.current = s; setSettingsState(s); };
  useEffect(() => { api<HubSettings>('/api/settings').then(setSettings).catch(() => {}); }, []);
  const save = useCallback((patch: (s: HubSettings) => HubSettings) => {
    if (!current.current) return;
    const next = patch(current.current);
    setSettings(next);
    api<HubSettings>('/api/settings', { method: 'PUT', body: JSON.stringify(next) }).then(setSettings).catch(() => {});
  }, []);
  return { settings, save };
}

export function EngineerView({ radio, hasLiveData, sessionType }: { radio: RadioMessage[]; hasLiveData: boolean; sessionType?: string | null }) {
  const QUICK = sessionType === 'Race' || !sessionType ? QUICK_RACE : QUICK_PRACTICE;
  const r = useRadio();
  const [text, setText] = useState('');
  const { settings, save } = useHubSettings();
  const [models, setModels] = useState<ModelsResponse | null>(null);
  const loadModels = useCallback(() => {
    api<ModelsResponse>('/api/ai/models').then(setModels).catch((e) => setModels({ ok: false, error: (e as Error).message, models: [], nodes: [] }));
  }, []);
  useEffect(loadModels, [loadModels]);

  // Barre d'espace : un appui = on écoute, un second appui = on envoie (hors champ de saisie).
  useEffect(() => {
    const isField = (t: EventTarget | null) => t instanceof HTMLInputElement || t instanceof HTMLTextAreaElement || t instanceof HTMLSelectElement;
    const down = (e: KeyboardEvent) => { if (e.code === 'Space' && !e.repeat && !isField(e.target)) { e.preventDefault(); r.toggleTalk(); } };
    window.addEventListener('keydown', down);
    return () => window.removeEventListener('keydown', down);
  }, [r]);

  const [sessionId, setSessionId] = useState<string>('current');
  const past = usePoll<{ radios: { at: string; from?: string | null; question?: string | null; answer: string; kind?: string | null; category?: string | null; proactive: boolean }[] }>(
    sessionId === 'current' ? null : `/api/carnets/${encodeURIComponent(sessionId)}`, 60000);
  const pastMessages: RadioMessage[] = (past.data?.radios ?? []).map((x) => ({
    at: x.at, from: x.from ?? '', question: x.question, answer: x.answer, kind: x.kind, category: x.category, proactive: x.proactive, milliseconds: 0,
  }));
  const messages = r.testMode ? r.testReplies : sessionId === 'current' ? radio : pastMessages;
  const lastProactive = [...radio].reverse().find((m) => m.awaitingAnswer);

  return (
    <div className="layout-ai">
      <div className="stack">
        <section className={`card ${r.testMode ? 'card-test' : ''}`}>
          <div className="card-head">
            <span className="card-title">Radio</span>
            {r.testMode ? <span className="pill test-pill">MODE TEST</span> : <span className="muted hide-xs">visible et audible sur tous les écrans</span>}
            <span className="spacer" />
            <label className="row secondary" style={{ gap: 6 }}>
              <input type="checkbox" checked={r.speakHere} onChange={(e) => r.setSpeakHere(e.target.checked)} />
              Écouter ici
            </label>
          </div>
          <div className="card-body stack">
            {!r.testMode && <SessionBar value={sessionId} onChange={setSessionId} />}
            {r.testMode && (
              <div className="callout test">
                <span aria-hidden>🧪</span>
                <div>
                  <b>Mode test</b> : les réponses restent sur cet écran (pas de diffusion radio, pas de mémoire).
                  {hasLiveData ? " L'IA répond avec les données de la course en cours." : " Aucune course en cours : l'IA répond sur une course fictive de 25 minutes."}
                </div>
              </div>
            )}
            {!r.testMode && lastProactive && Date.now() - new Date(lastProactive.at).getTime() < 120000 && (
              <div className="callout">
                <span aria-hidden>❓</span>
                <div><b>L'ingénieur attend votre réponse</b> : « {lastProactive.answer} »</div>
              </div>
            )}
            <div className="row">
              <span className="secondary">Je suis :</span>
              <div className="seg">
                {[['ingénieur', 'Ingénieur'], ['pilote', 'Pilote']].map(([k, l]) => (
                  <button key={k} className={r.role === k ? 'on' : ''} onClick={() => r.setRole(k)}>{l}</button>
                ))}
              </div>
            </div>

            <div className="talk-row">
              <button
                className={`talk-btn ${r.phase}`}
                disabled={!r.micAvailable || r.phase === 'thinking'}
                onClick={r.toggleTalk}
                title="Un appui pour parler, un second appui pour envoyer (aussi : barre d'espace ou bouton du volant)"
              >
                {r.phase === 'recording' ? '■ Envoyer' : r.phase === 'thinking' ? 'Réflexion…' : '🎙 Appuyer pour parler'}
              </button>
              <form className="row talk-form" onSubmit={(e) => { e.preventDefault(); r.ask(text).then(() => setText('')); }}>
                <input className="input" value={text} onChange={(e) => setText(e.target.value)} placeholder="Ou écrivez une question, une consigne, une remarque…" disabled={r.phase === 'thinking'} />
                <button className="btn" disabled={r.phase === 'thinking' || !text.trim()}>Envoyer</button>
              </form>
            </div>
            {r.phase === 'recording' && <div className="status short"><span className="ico">●</span>Je vous écoute… appuyez de nouveau pour envoyer.</div>}
            <PcStationLine />

            {!r.micAvailable && (
              <div className="secondary small">
                Micro indisponible sur cette adresse : le navigateur l'exige en HTTPS ou sur <span className="code">http://localhost</span>. Utilisez l'adresse <b>https://…:8687</b> indiquée dans Réglages.
              </div>
            )}
            <div className="row">
              {QUICK.map((q) => <button key={q} className="chip" onClick={() => r.ask(q)} disabled={r.phase === 'thinking'}>{q}</button>)}
            </div>
            <div className="muted small">
              Pour couper l'ingénieur : appuyez sur le bouton radio pendant qu'il parle ; si vous ne dites rien, la radio se referme toute seule.
              À la voix aussi : « on passe à mistral » (réflexion), « passe sur omni pour la voix », « utilise qwen sur le raspberry ».
              Vos retours (« j'ai du sous-virage en sortie », « je bloque l'avant au virage 1 ») reçoivent tout de suite un conseil de réglage ;
              répondez « oui », « non » ou « autre chose ? ». Tout est gardé dans le Carnet de la session.
            </div>
            {r.error && <div className="status critical"><span className="ico">!</span>{r.error}</div>}

            <div className="stack-sm">
              {sessionId !== 'current' && <div className="callout"><span aria-hidden>🗂</span><div>Session passée : échanges enregistrés dans le Carnet (lecture seule).</div></div>}
              {messages.length === 0 && <div className="muted">{r.testMode ? 'Posez une question pour tester.' : 'Aucun échange pour le moment.'}</div>}
              {[...messages].reverse().map((m, i) => <Message key={`${m.at}-${i}`} m={m} />)}
            </div>
          </div>
        </section>
        <MemoryCard />
      </div>

      <div className="stack">
        <TestCard />
        <BehaviourCard settings={settings} save={save} />
        <AudioDevicesCard />
        <WheelBindingCard />
        <ModelsCard settings={settings} save={save} models={models} reload={loadModels} />
        <SpecialistsCard settings={settings} save={save} models={models} />
        <MachinesCard settings={settings} save={save} models={models} reload={loadModels} />
      </div>
    </div>
  );
}

const KIND_LABEL: Record<string, string> = {
  calculée: 'réponse calculée',
  modèle: 'formulée par l\'IA',
  commande: 'commande',
  spontanée: 'l\'IA prend la parole',
  conseil: 'conseil de réglage',
  suite: 'suite de la conversation',
  retour: 'retour du pilote',
  accusé: 'réponse à venir',
  alerte: 'alerte de pilotage',
};

const SESSION_NAME: Record<string, string> = { Race: 'Course', Qualification: 'Qualification', Practice: 'Essais', WarmUp: 'Warm-up' };

/** Choix de la session à revoir (en cours ou passée, depuis le Carnet), et boutons « Terminer » / « Nouvelle session ». */
export function SessionBar({ value, onChange }: { value: string; onChange: (id: string) => void }) {
  const { data, reload } = usePoll<CarnetSummary[]>('/api/carnets', 15000);
  const [busy, setBusy] = useState(false);
  const act = async (path: string, confirmText: string) => {
    if (!window.confirm(confirmText)) return;
    setBusy(true);
    try {
      await fetch(path, { method: 'POST' });
      onChange('current');
      reload();
    } finally {
      setBusy(false);
    }
  };
  const sessions = (data ?? []).filter((c) => !c.current);
  const label = (c: CarnetSummary) => {
    const d = new Date(c.startedAt).toLocaleString('fr-FR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
    return `${d} · ${SESSION_NAME[c.sessionType ?? ''] ?? 'Session'}${c.track ? ` · ${c.track}` : ''}${c.laps ? ` · ${c.laps} tours` : ''}`;
  };
  return (
    <div className="row session-bar">
      <span className="secondary">Session :</span>
      <select className="select compact" value={value} onChange={(e) => onChange(e.target.value)} aria-label="Session à afficher">
        <option value="current">En cours</option>
        {sessions.map((c) => <option key={c.id} value={c.id}>{label(c)}</option>)}
      </select>
      <span className="spacer" />
      <button className="chip" disabled={busy} onClick={() => act('/api/session/end', 'Terminer la session ? Le carnet est fermé et récapitulé ; rien n\'y sera plus ajouté jusqu\'à la prochaine session.')}>Terminer la session</button>
      <button className="chip" disabled={busy} onClick={() => act('/api/session/new', 'Nouvelle session ? La mémoire de course et les calculs repartent de zéro (le carnet actuel est gardé).')}>Nouvelle session</button>
    </div>
  );
}

function Message({ m }: { m: RadioMessage }) {
  const [showFacts, setShowFacts] = useState(false);
  return (
    <div className={`msg ${m.test ? 'msg-test' : ''} ${m.proactive ? 'msg-proactive' : ''}`}>
      {(m.transcript || m.question) && (
        <div className="msg-q"><b>{m.from?.startsWith('pilote') ? 'Pilote' : 'Ingénieur'}</b> : « {m.transcript ?? m.question} »</div>
      )}
      {m.followUp && m.inReplyTo && <div className="msg-q muted">↩ réponse à « {m.inReplyTo} »</div>}
      <div className="row" style={{ alignItems: 'flex-start', flexWrap: 'nowrap' }}>
        <div className="msg-a">{m.proactive ? (m.awaitingAnswer ? '❓ ' : '📣 ') : '📻 '}{m.answer}</div>
        {m.audioBase64 && <button className="close" onClick={() => playBase64Wav(m.audioBase64!)} aria-label="Réécouter">▶</button>}
      </div>
      <div className="msg-meta">
        {new Date(m.at).toLocaleTimeString('fr-FR')}
        {m.listenMilliseconds ? ` · écoute ${(m.listenMilliseconds / 1000).toFixed(1)} s` : ''}
        {m.thinkMilliseconds ? ` · réflexion ${(m.thinkMilliseconds / 1000).toFixed(1)} s` : ''}
        {m.milliseconds ? ` · total ${(m.milliseconds / 1000).toFixed(1)} s` : ''}
        {m.kind && KIND_LABEL[m.kind] && <span className="pill small-pill">{KIND_LABEL[m.kind]}</span>}
        {m.category && <CategoryPill category={m.category} />}
        {m.specialist && m.kind === 'modèle' && <span className="pill small-pill">spécialiste {m.specialist}</span>}
        {m.awaitingAnswer && <span className="pill small-pill">attend votre réponse</span>}
        {m.scenario && <span className="pill small-pill">{m.scenario}</span>}
        {m.facts && <button className="chip tiny" onClick={() => setShowFacts(!showFacts)}>{showFacts ? 'masquer les faits' : 'faits utilisés'}</button>}
      </div>
      {showFacts && m.facts && <div className="log" style={{ marginTop: 6 }}>{m.facts}</div>}
    </div>
  );
}

function MemoryCard() {
  const { data, reload } = usePoll<MemoryState>('/api/ai/memory', 3000);
  const [note, setNote] = useState('');
  const add = async () => {
    if (!note.trim()) return;
    await api('/api/ai/memory', { method: 'POST', body: JSON.stringify({ text: note, source: 'ingénieur' }) });
    setNote('');
    reload();
  };
  const remove = async (id: number) => { await fetch(`/api/ai/memory/${id}`, { method: 'DELETE' }); reload(); };
  const clearPlan = async () => { await fetch('/api/ai/memory/plan', { method: 'DELETE' }); reload(); };
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Mémoire de la course</span><span className="muted hide-xs">retours du pilote et décisions, utilisés par l'IA</span></div>
      <div className="card-body stack-sm">
        {(data?.plannedStopLap != null || data?.noStopPlanned) && (
          <div className="kv">
            <span>Décision en cours</span>
            <span className="row nowrap"><b>{data.noStopPlanned ? 'aller au bout sans s\'arrêter' : `arrêt au tour ${data.plannedStopLap}`}</b><button className="chip tiny" onClick={clearPlan}>annuler</button></span>
          </div>
        )}
        {data?.pending && <div className="secondary small">Question en attente : « {data.pending} »</div>}
        {(data?.entries.length ?? 0) === 0 ? (
          <div className="muted small">Rien pour l'instant. Les remarques du pilote (« l'avant gauche bloque au virage 1 ») et ses réponses aux questions de l'IA s'ajoutent ici. Remis à zéro à chaque nouvelle session.</div>
        ) : (
          <ul className="memory">
            {[...data!.entries].reverse().map((e) => (
              <li key={e.id}>
                <span className="muted tnum">{e.lap != null ? `T${e.lap}` : new Date(e.at).toLocaleTimeString('fr-FR')}</span>
                <span className="memory-src">{e.source}</span>
                <span className="memory-text">{e.text}</span>
                <button className="close" onClick={() => remove(e.id)} aria-label="Effacer">✕</button>
              </li>
            ))}
          </ul>
        )}
        <form className="row" onSubmit={(e) => { e.preventDefault(); add(); }}>
          <input className="input" style={{ flex: 1 }} value={note} onChange={(e) => setNote(e.target.value)} placeholder="Ajouter une note (ex. « éviter le vibreur du virage 5 »)" />
          <button className="btn" disabled={!note.trim()}>Ajouter</button>
        </form>
      </div>
    </section>
  );
}

function BehaviourCard({ settings, save }: { settings: HubSettings | null; save: (p: (s: HubSettings) => HubSettings) => void }) {
  if (!settings) return null;
  const ai = settings.ai;
  const set = (patch: Partial<HubSettings['ai']>) => save((s) => ({ ...s, ai: { ...s.ai, ...patch } }));
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Comportement de l'ingénieur</span></div>
      <div className="card-body stack-sm">
        <label className="switch">
          <input type="checkbox" checked={ai.proactiveRadio} onChange={(e) => set({ proactiveRadio: e.target.checked })} />
          <span>L'IA prend la parole d'elle-même</span>
        </label>
        <div className="secondary small">
          Pluie qui arrive (« comment est la piste ? »), piste assez mouillée pour les pneus pluie, relais qu'on peut allonger
          (seulement si ça fait gagner un arrêt), box dans deux tours, voiture qui revient derrière, surchauffe moteur ou freins.
        </div>
        <div className="row">
          <span className="secondary">Au plus une intervention toutes les</span>
          <select className="select compact" value={ai.proactiveMinIntervalSec} onChange={(e) => set({ proactiveMinIntervalSec: Number(e.target.value) })} disabled={!ai.proactiveRadio}>
            {[60, 120, 180, 300, 600].map((v) => <option key={v} value={v}>{v / 60} min</option>)}
          </select>
          <span className="muted small">(box et pluie passent en priorité)</span>
        </div>
        <div className="row">
          <span className="secondary">Pneus pluie conseillés au-delà de</span>
          <input className="input compact-input num" type="number" min={5} max={80} value={ai.wetThresholdPct} onChange={(e) => set({ wetThresholdPct: Math.max(5, Math.min(80, Number(e.target.value) || 25)) })} />
          <span className="secondary">% d'humidité de la piste</span>
        </div>
        <label className="switch">
          <input type="checkbox" checked={ai.drivingAlerts ?? true} onChange={(e) => set({ drivingAlerts: e.target.checked })} />
          <span>Alertes de pilotage immédiates</span>
        </label>
        <div className="secondary small">
          Limites de piste (« Track limits virage 5 : trop large. 2 sur 3 »), temps annulé, pénalité, moteur coupé au départ,
          et usure brutale d'un pneu analysée (où, dans quelle phase du virage, quel réglage essayer).
        </div>
        <label className="switch">
          <input type="checkbox" checked={ai.trackLimitAlerts ?? true} onChange={(e) => set({ trackLimitAlerts: e.target.checked })} disabled={!(ai.drivingAlerts ?? true)} />
          <span>Annoncer les limites de piste et les temps annulés</span>
        </label>
        <div className="secondary small">À la radio aussi : « coupe les limites de piste », « remets les limites de piste ».</div>
        <label className="switch">
          <input type="checkbox" checked={ai.lmuPitMenu ?? false} onChange={(e) => set({ lmuPitMenu: e.target.checked })} />
          <span>Régler le menu des stands de Le Mans Ultimate</span>
        </label>
        <div className="secondary small">
          Quand vous demandez « softs neufs », « la réparation » ou « le plein », l'ingénieur règle le menu des stands du jeu par le collecteur du PC de jeu
          (API locale de LMU, non officielle : vérifiez le menu avant d'entrer). L'ingénieur le propose à la première demande ; à la radio : « active le menu des stands », « essaie l'API ».
        </div>
        <PitMenuTest />
        <label className="switch">
          <input type="checkbox" checked={ai.englishCalls ?? true} onChange={(e) => set({ englishCalls: e.target.checked })} />
          <span>Accusés de réception en anglais (« Copy »)</span>
        </label>
        <div className="row">
          <span className="secondary">« Copy, je reviens vers vous » après</span>
          <select className="select compact" value={ai.ackAfterSeconds ?? 2.5} onChange={(e) => set({ ackAfterSeconds: Number(e.target.value) })}>
            {[1.5, 2.5, 4, 6, 10].map((v) => <option key={v} value={v}>{String(v).replace('.', ',')} s</option>)}
          </select>
          <span className="secondary">de réflexion</span>
        </div>
        <div className="secondary small">
          Pendant que l'IA réfléchit, vous pouvez poser d'autres questions : les réponses calculées (pneus, essence, TC…) arrivent tout de suite,
          les autres attendent leur tour, puis l'ingénieur revient (« Pour la stratégie : … »).
        </div>
        <label className="switch">
          <input type="checkbox" checked={ai.strictAnswers} onChange={(e) => set({ strictAnswers: e.target.checked })} />
          <span>Réponses calculées dites telles quelles</span>
        </label>
        <div className="secondary small">
          Recommandé : pour les questions reconnues (stand, essence, écarts, pneus, vitesses, pluie…), le hub répond directement avec ses calculs,
          plus vite et sans erreur. L'IA ne formule que les questions libres.
        </div>
      </div>
    </section>
  );
}

/** Voix et micro sur le PC de jeu (collecteur) : affiché dans la carte Radio. */
function PcStationLine() {
  const { stations, voiceOutput } = useStations();
  const voice = stations.find((s) => s.voice);
  const mic = stations.find((s) => s.mic);
  if (!voice && !mic) return null;
  const where = voiceOutput === 'ecrans' ? 'sur les écrans (réglage « Voix de l\'ingénieur sur »)' : voiceOutput === 'deux' ? 'sur le PC de jeu et ici' : 'sur le PC de jeu seulement';
  return (
    <div className="callout">
      <span aria-hidden>🎧</span>
      <div>
        <b>{(voice ?? mic)!.name}</b> :{voice ? ` voix de l'ingénieur ${where}` : ''}{voice && mic ? ' ;' : ''}
        {mic ? " le bouton radio du PC de jeu (et ClickDeck) enregistre le micro du PC de jeu" : ''}.
      </div>
    </div>
  );
}

function AudioDevicesCard() {
  const [devices, setDevices] = useState<{ inputs: MediaDeviceInfo[]; outputs: MediaDeviceInfo[]; named: boolean } | null>(null);
  const [mic, setMic] = useState(() => audioPref('mic'));
  const [speaker, setSpeaker] = useState(() => audioPref('speaker'));
  const load = useCallback(async (ask = false) => setDevices(await listAudioDevices(ask)), []);
  useEffect(() => {
    load();
    navigator.mediaDevices?.addEventListener?.('devicechange', () => load());
  }, [load]);
  const speakerOk = canChooseSpeaker();
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Micro et haut-parleur de cet écran</span></div>
      <div className="card-body stack-sm">
        <p className="secondary small" style={{ margin: 0 }}>
          Chaque écran utilise ses propres appareils : sur le Mac, le micro du Mac (ou un micro branché dessus) ;
          pour le micro et le casque du PC de jeu, choisissez-les dans la fenêtre du collecteur (« Radio de l'ingénieur »).
        </p>
        <div className="field">
          <label>Micro</label>
          <select className="select" value={mic} onChange={(e) => { setMic(e.target.value); setAudioPref('mic', e.target.value); }}>
            <option value="">Micro par défaut du système</option>
            {devices?.inputs.filter((d) => d.deviceId && d.deviceId !== 'default').map((d, i) => <option key={d.deviceId} value={d.deviceId}>{d.label || `Micro ${i + 1}`}</option>)}
          </select>
        </div>
        <div className="field">
          <label>Haut-parleur / casque (voix de l'ingénieur)</label>
          <select className="select" value={speaker} disabled={!speakerOk} onChange={(e) => { setSpeaker(e.target.value); setAudioPref('speaker', e.target.value); }}>
            <option value="">Sortie par défaut du système</option>
            {devices?.outputs.filter((d) => d.deviceId && d.deviceId !== 'default').map((d, i) => <option key={d.deviceId} value={d.deviceId}>{d.label || `Sortie ${i + 1}`}</option>)}
          </select>
          {!speakerOk && <span className="muted small">Ce navigateur ne permet pas de choisir la sortie (Chrome ou Edge le permettent) : réglez-la dans le système.</span>}
        </div>
        <div className="row">
          {!devices?.named && <button className="btn" onClick={() => load(true)}>Afficher les noms des appareils</button>}
          <button className="btn" onClick={() => testSpeaker()}>Tester le son</button>
        </div>
      </div>
    </section>
  );
}

const STEPS: { id: string; label: string; hint: string }[] = [
  { id: 'ollama', label: 'Ollama', hint: 'Les machines IA répondent' },
  { id: 'think', label: 'Réflexion', hint: 'Le modèle répond à une question' },
  { id: 'speak', label: 'Voix', hint: 'Synthèse vocale (écoutez le résultat)' },
  { id: 'listen', label: 'Écoute', hint: 'Parlez 4 s : transcription de votre voix' },
];

export function TestCard() {
  const r = useRadio();
  const [results, setResults] = useState<Record<string, Diagnostic | 'running'>>({});
  const [recording, setRecording] = useState(false);

  const run = async (step: string) => {
    setResults((x) => ({ ...x, [step]: 'running' }));
    try {
      let res: Diagnostic;
      if (step === 'listen') {
        const rec = new Recorder();
        await rec.start();
        setRecording(true);
        await new Promise((ok) => window.setTimeout(ok, 4000));
        const wav = await rec.stop();
        setRecording(false);
        const form = new FormData();
        form.append('audio', wav, 'test.wav');
        form.append('transcribeOnly', 'true');
        const resp = await fetch('/api/ai/listen', { method: 'POST', body: form });
        res = await resp.json();
      } else {
        res = await api<Diagnostic>('/api/ai/test', { method: 'POST', body: JSON.stringify({ step }) });
      }
      setResults((x) => ({ ...x, [step]: res }));
      if (res.audioBase64) playBase64Wav(res.audioBase64);
    } catch (e) {
      setRecording(false);
      setResults((x) => ({ ...x, [step]: { step, ok: false, milliseconds: 0, detail: (e as Error).message } }));
    }
  };

  const runAll = async () => {
    for (const s of STEPS.filter((s) => s.id !== 'listen')) await run(s.id);
  };

  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Tester l'ingénieur IA</span>
        <span className="spacer" />
        <label className="switch">
          <input type="checkbox" checked={r.testMode} onChange={(e) => r.setTestMode(e.target.checked)} />
          <span>Mode test</span>
        </label>
      </div>
      <div className="card-body stack-sm">
        <p className="secondary small" style={{ margin: 0 }}>
          En mode test, vos questions (voix, texte, bouton du volant) ne passent pas à la radio des autres écrans. Sans jeu lancé, l'IA répond sur une course fictive.
        </p>
        <div className="diag">
          {STEPS.map((s) => {
            const res = results[s.id];
            return (
              <div key={s.id} className="diag-row">
                <span className={`diag-ico ${res === 'running' ? 'run' : res ? (res.ok ? 'ok' : 'ko') : ''}`} aria-hidden>{res === 'running' ? '…' : res ? (res.ok ? '✓' : '!') : '○'}</span>
                <div className="diag-text">
                  <div><b>{s.label}</b> <span className="muted">— {s.id === 'listen' && recording ? 'parlez maintenant…' : s.hint}</span></div>
                  {res && res !== 'running' && (
                    <div className={`small ${res.ok ? 'secondary' : 'critical-text'}`}>
                      {res.detail}{res.milliseconds ? ` · ${(res.milliseconds / 1000).toFixed(1)} s` : ''}{res.model ? ` · ${res.model}` : ''}
                    </div>
                  )}
                </div>
                {res && res !== 'running' && res.audioBase64 && <button className="close" onClick={() => playBase64Wav(res.audioBase64!)} aria-label="Réécouter">▶</button>}
                <button className="btn small-btn" disabled={res === 'running' || (s.id === 'listen' && !r.micAvailable)} onClick={() => run(s.id)}>Tester</button>
              </div>
            );
          })}
          <div className="diag-row">
            <span className={`diag-ico ${r.micAvailable ? 'ok' : 'ko'}`} aria-hidden>{r.micAvailable ? '✓' : '!'}</span>
            <div className="diag-text">
              <div><b>Micro</b> <span className="muted">— sur cet écran</span></div>
              <div className="small secondary">{r.micAvailable ? 'Autorisé par le navigateur.' : 'Indisponible : ouvrez l\'adresse HTTPS (Réglages) ou http://localhost.'}</div>
            </div>
          </div>
          <div className="diag-row">
            <span className={`diag-ico ${r.binding ? (r.boundPressed ? 'run' : 'ok') : ''}`} aria-hidden>{r.binding ? '◎' : '○'}</span>
            <div className="diag-text">
              <div><b>Bouton du volant</b> <span className="muted">— appuyez dessus pour vérifier</span></div>
              <div className="small secondary">{r.binding ? (r.boundPressed ? 'Bouton enfoncé ✓' : `Bouton ${r.binding.button + 1} associé (${r.binding.mode === 'hold' ? 'maintenir' : 'appui pour parler, appui pour envoyer'})`) : 'Aucun bouton associé.'}</div>
            </div>
          </div>
        </div>
        <div className="row">
          <button className="btn primary" onClick={runAll}>Tout tester</button>
          <button className="btn" onClick={() => { r.setTestMode(true); r.ask('Quand est-ce que je rentre au stand ?', { test: true }); }}>Question d'essai</button>
        </div>
      </div>
    </section>
  );
}

/** Association d'un bouton du volant (ou de la manette) à la radio. Réglage propre à chaque appareil. */
export function WheelBindingCard() {
  const r = useRadio();
  const wheels = r.pads;
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Bouton du volant</span><span className="muted">radio</span></div>
      <div className="card-body stack-sm">
        {!r.gamepadAvailable ? (
          <div className="callout warn small">
            <span aria-hidden>🔒</span>
            <div>Le navigateur ne donne accès au volant qu'en HTTPS ou sur localhost. Sur le PC de jeu, ouvrez l'adresse <b>https://…:8687</b> indiquée dans Réglages.</div>
          </div>
        ) : (
          <>
            <p className="secondary small" style={{ margin: 0 }}>
              À faire <b>sur l'ordinateur où le volant est branché</b> (le PC de jeu) : ouvrez le hub dans Chrome ou Edge, gardez la fenêtre visible (second écran),
              puis associez un bouton.
            </p>
            <div className="kv">
              <span>Bouton associé</span>
              <b>{r.binding ? `Bouton ${r.binding.button + 1}` : 'aucun'}</b>
            </div>
            {r.binding && <div className="muted small">{r.binding.gamepadId}</div>}
            <div className="row">
              {r.learning ? (
                <>
                  <span className="status short"><span className="ico">…</span>Appuyez sur le bouton choisi du volant</span>
                  <button className="btn" onClick={r.cancelLearn}>Annuler</button>
                </>
              ) : (
                <button className="btn primary" onClick={r.learn}>{r.binding ? 'Changer de bouton' : 'Associer un bouton'}</button>
              )}
              {r.binding && <button className="btn" onClick={() => r.setBinding(null)}>Retirer</button>}
            </div>
            {r.binding && (
              <div className="row">
                <span className="secondary">Fonctionnement :</span>
                <div className="seg">
                  <button className={r.binding.mode === 'toggle' ? 'on' : ''} onClick={() => r.setBinding({ ...r.binding!, mode: 'toggle' })}>Appui : parler · appui : envoyer</button>
                  <button className={r.binding.mode === 'hold' ? 'on' : ''} onClick={() => r.setBinding({ ...r.binding!, mode: 'hold' })}>Maintenir</button>
                </div>
              </div>
            )}
            <div className="small muted">
              {wheels.length === 0
                ? 'Aucun volant détecté : appuyez sur un bouton du volant pour que le navigateur le signale.'
                : wheels.map((w) => `${w.id.slice(0, 48)} (${w.buttons} boutons${w.pressed.length ? ` · enfoncé : ${w.pressed.map((b) => b + 1).join(', ')}` : ''})`).join(' · ')}
            </div>
          </>
        )}
      </div>
    </section>
  );
}

function ModelsCard({ settings, save, models, reload }: { settings: HubSettings | null; save: (p: (s: HubSettings) => HubSettings) => void; models: ModelsResponse | null; reload: () => void }) {
  const list = models?.models ?? [];
  const [aliasName, setAliasName] = useState('');
  const [aliasTarget, setAliasTarget] = useState('');

  // Préremplissage : si rien n'est choisi, on prend le premier modèle qui sait tout faire.
  useEffect(() => {
    if (!settings || list.length === 0 || settings.ai.thinkModel) return;
    const omni = list.find((m) => m.capabilities.includes('audio') && m.capabilities.includes('speech'));
    const any = omni ?? list.find((m) => m.capabilities.includes('completion'));
    if (any) {
      const frVoice = any.voices.find((v) => v === 'female-french') ?? any.voices.find((v) => v.includes('french')) ?? 'default';
      save((s) => ({ ...s, ai: { ...s.ai, thinkModel: any.id, listenModel: omni ? omni.id : '', speakModel: omni ? omni.id : '', voice: frVoice } }));
    }
  }, [settings, list, save]);

  if (!settings) return null;
  const ai = settings.ai;
  const set = (patch: Partial<HubSettings['ai']>) => save((s) => ({ ...s, ai: { ...s.ai, ...patch } }));
  const by = (cap: string) => list.filter((m) => m.capabilities.includes(cap));
  const speakModel = list.find((m) => m.id === (ai.speakModel || ai.thinkModel));
  const voices = [...(speakModel?.voices ?? [])].sort((a, b) => Number(b.includes('french')) - Number(a.includes('french')));

  return (
    <section className="card">
      <div className="card-head">
        <span className="card-title">Modèles</span>
        <span className="muted">un modèle par rôle, sur la machine de votre choix</span>
        <span className="spacer" />
        <button className="chip" onClick={reload}>Actualiser</button>
      </div>
      <div className="card-body">
        {models?.error && <div className="status critical" style={{ marginBottom: 10 }}><span className="ico">!</span>{models.error}</div>}
        <ModelSelect label="Réflexion — comprend la question et répond" value={ai.thinkModel} options={by('completion')} onChange={(v) => set({ thinkModel: v })} />
        <ModelSelect label="Écoute — comprend la voix" value={ai.listenModel} options={by('audio')} onChange={(v) => set({ listenModel: v })} allowSame />
        <ModelSelect label="Voix — parle" value={ai.speakModel} options={by('speech')} onChange={(v) => set({ speakModel: v })} allowSame />
        <div className="field">
          <label>Voix</label>
          <select className="select" value={ai.voice} onChange={(e) => set({ voice: e.target.value })}>
            {(voices.length ? voices : ['default']).map((v) => <option key={v} value={v}>{v}</option>)}
          </select>
        </div>

        <div className="field">
          <label>Surnoms pour changer de modèle à la voix (« on passe à … »)</label>
          {Object.entries(ai.aliases ?? {}).map(([k, v]) => (
            <div key={k} className="kv">
              <span>« {k} »</span>
              <span className="row nowrap">{v}<button className="close" onClick={() => { const next = { ...ai.aliases }; delete next[k]; set({ aliases: next }); }} aria-label="Retirer">✕</button></span>
            </div>
          ))}
          <form className="row" onSubmit={(e) => { e.preventDefault(); if (aliasName.trim() && aliasTarget) { set({ aliases: { ...(ai.aliases ?? {}), [aliasName.trim().toLowerCase()]: aliasTarget } }); setAliasName(''); } }}>
            <input className="input" style={{ flex: '1 1 120px' }} value={aliasName} onChange={(e) => setAliasName(e.target.value)} placeholder="surnom (ex. omniaudio)" />
            <select className="select" style={{ flex: '1 1 160px' }} value={aliasTarget} onChange={(e) => setAliasTarget(e.target.value)}>
              <option value="">— modèle —</option>
              {list.map((m) => <option key={m.id} value={m.id}>{m.name} · {m.node}</option>)}
            </select>
            <button className="btn" disabled={!aliasName.trim() || !aliasTarget}>Ajouter</button>
          </form>
        </div>
        <p className="secondary small">Les chiffres sont toujours calculés par le hub, jamais par l'IA : elle choisit et formule.</p>
      </div>
    </section>
  );
}

/** Lecture seule du menu des stands de LMU par le collecteur ; le résultat arrive à la radio. */
function PitMenuTest() {
  const [state, setState] = useState<string | null>(null);
  const test = async () => {
    setState('Demande envoyée au collecteur…');
    try {
      const r = await api<{ sent: boolean }>('/api/pitmenu/test', { method: 'POST' });
      setState(r.sent ? 'Demande envoyée : le résultat arrive à la radio.' : 'Aucun collecteur relié pour le menu des stands (collecteur ouvert sur le PC de jeu, case « menu des stands » cochée).');
    } catch (e) {
      setState((e as Error).message);
    }
  };
  return (
    <div className="row">
      <button className="chip" onClick={test}>Tester le menu des stands</button>
      {state && <span className="muted small">{state}</span>}
    </div>
  );
}

const SPECIALISTS: [string, string][] = [['pneus', 'Pneus'], ['stratégie', 'Stratégie'], ['voiture', 'Voiture et réglages'], ['course', 'Course']];

/** Un modèle par spécialiste (sinon le modèle « réflexion ») : chacun ne reçoit que les faits de son domaine. */
function SpecialistsCard({ settings, save, models }: { settings: HubSettings | null; save: (p: (s: HubSettings) => HubSettings) => void; models: ModelsResponse | null }) {
  if (!settings) return null;
  const chat = (models?.models ?? []).filter((m) => m.capabilities.includes('completion'));
  const set = (key: string, value: string) => save((s) => ({ ...s, ai: { ...s.ai, specialists: { ...(s.ai.specialists ?? {}), [key]: value } } }));
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Spécialistes</span><span className="muted hide-xs">un domaine par question</span></div>
      <div className="card-body stack-sm">
        <div className="secondary small">
          Chaque question libre va au spécialiste de son domaine, qui ne reçoit que ses faits : réponse plus rapide et plus juste,
          puis son contexte est vidé. Par défaut, chacun utilise le modèle « réflexion ».
        </div>
        {SPECIALISTS.map(([key, label]) => (
          <ModelSelect key={key} label={label} value={settings.ai.specialists?.[key] ?? ''} options={chat} onChange={(v) => set(key, v)} allowSame />
        ))}
      </div>
    </section>
  );
}

function ModelSelect({ label, value, options, onChange, allowSame }: { label: string; value: string; options: OllamaModel[]; onChange: (v: string) => void; allowSame?: boolean }) {
  const known = options.some((m) => m.id === value);
  return (
    <div className="field">
      <label>{label}</label>
      <select className="select" value={value} onChange={(e) => onChange(e.target.value)}>
        {allowSame ? <option value="">Même modèle que la réflexion</option> : <option value="">— choisir —</option>}
        {value && !known && <option value={value}>{value} (machine injoignable)</option>}
        {options.map((m) => <option key={m.id} value={m.id}>{m.name} · {m.node} ({(m.sizeBytes / 1e9).toFixed(1)} Go)</option>)}
      </select>
    </div>
  );
}

/** Machines IA du réseau : ce Mac, un Raspberry Pi, un autre PC… chacune fait tourner Ollama. */
function MachinesCard({ settings, save, models, reload }: { settings: HubSettings | null; save: (p: (s: HubSettings) => HubSettings) => void; models: ModelsResponse | null; reload: () => void }) {
  const [name, setName] = useState('');
  const [url, setUrl] = useState('');
  if (!settings) return null;
  const ai = settings.ai;
  const status = (n: string) => models?.nodes.find((x) => x.name.toLowerCase() === n.toLowerCase());
  const add = () => {
    if (!name.trim() || !url.trim()) return;
    save((s) => ({ ...s, ai: { ...s.ai, nodes: [...s.ai.nodes.filter((x) => x.name.toLowerCase() !== name.trim().toLowerCase()), { name: name.trim(), url: url.trim() }] } }));
    setName(''); setUrl('');
    window.setTimeout(reload, 400);
  };
  const local = status(ai.localNodeName);
  return (
    <section className="card">
      <div className="card-head"><span className="card-title">Machines IA</span><span className="muted">répartir les modèles sur le réseau</span></div>
      <div className="card-body stack-sm">
        <div className="node-row">
          <NodeDot s={local} />
          <b>{ai.localNodeName}</b>
          <span className="muted small">ce Mac · {ai.ollamaUrl}</span>
          <span className="spacer" />
          <span className="muted small">{local?.ok ? `${local.modelCount} modèle(s)` : local?.error ?? ''}</span>
        </div>
        {ai.nodes.map((n) => {
          const st = status(n.name);
          return (
            <div key={n.name} className="node-row">
              <NodeDot s={st} />
              <b>{n.name}</b>
              <span className="muted small">{n.url}</span>
              <span className="spacer" />
              <span className="muted small">{st?.ok ? `${st.modelCount} modèle(s) · ${st.milliseconds} ms` : st?.error ?? '…'}</span>
              <button className="close" onClick={() => { save((s) => ({ ...s, ai: { ...s.ai, nodes: s.ai.nodes.filter((x) => x.name !== n.name) } })); window.setTimeout(reload, 400); }} aria-label={`Retirer ${n.name}`}>✕</button>
            </div>
          );
        })}
        <form className="row" onSubmit={(e) => { e.preventDefault(); add(); }}>
          <input className="input" style={{ flex: '1 1 110px' }} value={name} onChange={(e) => setName(e.target.value)} placeholder="nom (ex. raspberry)" />
          <input className="input" style={{ flex: '2 1 180px' }} value={url} onChange={(e) => setUrl(e.target.value)} placeholder="raspberrypi.local ou 192.168.1.40" />
          <button className="btn" disabled={!name.trim() || !url.trim()}>Ajouter</button>
        </form>
        <details className="small secondary">
          <summary>Installer Ollama sur un Raspberry Pi (ou un autre PC)</summary>
          <ol style={{ paddingLeft: 18, lineHeight: 1.7 }}>
            <li>Sur le Raspberry Pi 5 (8 Go conseillés) : <span className="code">curl -fsSL https://ollama.com/install.sh | sh</span></li>
            <li>L'ouvrir au réseau : <span className="code">sudo systemctl edit ollama</span>, ajouter <span className="code">[Service]</span> puis <span className="code">Environment="OLLAMA_HOST=0.0.0.0"</span>, puis <span className="code">sudo systemctl restart ollama</span>.</li>
            <li>Télécharger un petit modèle : <span className="code">ollama pull qwen3:1.7b</span> (ou un modèle de voix).</li>
            <li>Ajouter la machine ici, puis choisir ses modèles dans « Modèles » (ils apparaissent avec « · raspberry »).</li>
          </ol>
          Un Raspberry Pi est bien plus lent que le Mac : donnez-lui plutôt un rôle léger (voix ou écoute), ou les réponses spontanées,
          et gardez la réflexion sur la machine la plus puissante.
        </details>
      </div>
    </section>
  );
}

function NodeDot({ s }: { s: NodeStatus | undefined }) {
  return <span className="dot" style={{ background: !s ? 'var(--surface-3)' : s.ok ? 'var(--good)' : 'var(--critical)' }} title={!s ? 'inconnu' : s.ok ? 'joignable' : 'injoignable'} />;
}
