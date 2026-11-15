import { useMemo, useState } from 'react';
import { usePoll } from '../../live';
import { clock, compass, delta, lapTime, num } from '../../format';
import { playBase64Wav } from '../../audio';
import { readPref, writePref } from '../../prefs';
import type { MicroSectorReport, RaceEvent, WeatherReport } from '../../types';
import type { RadioMessage } from '../../radio';
import { TrackMap } from '../TrackMap';
import { BattlePanel } from '../BattlePanel';
import { DamageList } from '../RaceExtras';
import { RacePlanCard, RacePlanSummary } from '../RacePlanCard';
import { AlertList, CarStatus, DeltaBar, Icon, MyCarPanel, NextStop, TimingGrid, classBests } from '../Timing';
import type { Notice } from './notices';
import { registerBlock, type BlockProps } from './registry';

/*
 * Blocs intégrés de l'accueil. Chaque bloc s'enregistre dans le registre (registry.ts) ;
 * la taille par défaut est en colonnes (sur 12, ou 6 sur tablette) et en rangées de 36 px.
 */


// ───────────────────────────── Classement ─────────────────────────────

function Standings({ ctx }: BlockProps) {
  const { snapshot, classColor, onSelect, classView, setClassView } = ctx;
  const [mode, setModeState] = useState(() => readPref('smhub-accueil-secteurs', 'mini'));
  const setMode = (m: string) => { setModeState(m); writePref('smhub-accueil-secteurs', m); };
  const { data: micro } = usePoll<MicroSectorReport>(mode === 'mini' ? '/api/analysis/microsectors' : null, 2000);
  const me = snapshot.drivers.find((d) => d.isPlayer);
  const classes = useMemo(() => {
    const seen = new Map<string, string>();
    for (const d of snapshot.drivers) if (!seen.has(d.classId)) seen.set(d.classId, d.className ?? d.classId);
    return [...seen.entries()];
  }, [snapshot.drivers]);
  return (
    <div className="db-standings">
      <div className="db-toolbar-in">
        {classes.length > 1 && (
          <div className="db-chips" role="group" aria-label="Catégories affichées">
            {me && <button className={`chip ${classView === 'mine' ? 'on' : ''}`} onClick={() => setClassView('mine')} title="Votre catégorie, plus les autres voitures à moins de 5 s de vous sur la piste">Ma catégorie</button>}
            <button className={`chip ${classView === 'all' ? 'on' : ''}`} onClick={() => setClassView('all')}>Toutes</button>
            {classes.filter(([id]) => id !== me?.classId).map(([id, name]) => (
              <button key={id} className={`chip ${classView === id ? 'on' : ''}`} onClick={() => setClassView(classView === id ? 'mine' : id)}>
                <span className="swatch" style={{ background: classColor(id) }} />{name}
              </button>
            ))}
          </div>
        )}
        <span className="spacer" />
        <div className="seg" role="group" aria-label="Colonnes des secteurs">
          <button className={mode === 'mini' ? 'on' : ''} onClick={() => setMode('mini')}>Mini-secteurs</button>
          <button className={mode !== 'mini' ? 'on' : ''} onClick={() => setMode('temps')}>S1 S2 S3</button>
        </div>
      </div>
      <TimingGrid snapshot={snapshot} classColor={classColor} onSelect={onSelect} classView={classView} miniSectors={mode === 'mini' ? micro : null} />
    </div>
  );
}

// ───────────────────────────── Radio ─────────────────────────────

const timeOf = (iso: string) => {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '' : d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
};
const roleLabel = (from: string | null | undefined) => (from?.startsWith('pilote') ? 'Pilote' : from?.startsWith('ingénieur IA') ? 'Ingénieur IA' : 'Ingénieur');

function speakLocally(text: string) {
  try {
    const u = new SpeechSynthesisUtterance(text);
    u.lang = 'fr-FR';
    window.speechSynthesis.cancel();
    window.speechSynthesis.speak(u);
  } catch { /* synthèse vocale indisponible */ }
}

export function RadioFeed({ messages, limit = 30 }: { messages: RadioMessage[]; limit?: number }) {
  const list = [...messages].reverse().slice(0, limit);
  if (list.length === 0) {
    return <div className="db-empty"><Icon name="radio" size={22} /><div>Pas encore d'échange radio.</div><div className="muted small">Les questions du pilote ou de l'ingénieur et les réponses de l'IA apparaîtront ici.</div></div>;
  }
  return (
    <ul className="rd-list">
      {list.map((m, i) => {
        const said = m.transcript ?? m.question;
        return (
          <li key={`${m.at}-${i}`} className={`rd-item ${m.proactive ? 'proactive' : ''}`}>
            <div className="rd-head">
              <span className={`rd-avatar ${said ? (m.from?.startsWith('pilote') ? 'pilot' : 'eng') : 'ai'}`} aria-hidden>{said ? (m.from?.startsWith('pilote') ? 'P' : 'I') : 'IA'}</span>
              <span className="rd-who">{said ? `${roleLabel(m.from)} → ingénieur IA` : m.proactive ? 'Ingénieur IA (spontané)' : roleLabel(m.from)}</span>
              <span className="rd-time tnum">{timeOf(m.at)}</span>
              {m.audioBase64
                ? <button className="rd-play" onClick={() => playBase64Wav(m.audioBase64!)} title="Réécouter la voix de l'ingénieur" aria-label="Réécouter"><Icon name="play" size={14} /></button>
                : <button className="rd-play off" onClick={() => speakLocally(m.answer)} title="Audio non conservé : lire avec la voix de cet appareil" aria-label="Lire avec la voix de l'appareil"><Icon name="play" size={14} /></button>}
            </div>
            {said && <div className="rd-q">« {said} »</div>}
            <div className="rd-a">{m.answer}</div>
          </li>
        );
      })}
    </ul>
  );
}

// ───────────────────────────── Événements de course ─────────────────────────────

const FILTERS: { id: string; label: string; kinds?: string[] }[] = [
  { id: 'all', label: 'Tout' },
  { id: 'flag', label: 'Drapeaux', kinds: ['flag', 'session'] },
  { id: 'pit', label: 'Stands', kinds: ['pit'] },
  { id: 'bestlap', label: 'Chronos', kinds: ['bestlap', 'position'] },
  { id: 'track', label: 'Limites & pénalités', kinds: ['track', 'penalty', 'retire'] },
  { id: 'car', label: 'Pneus & dégâts', kinds: ['tyres', 'damage'] },
  { id: 'notif', label: 'Notifications', kinds: ['notif'] },
];

const KIND_ICON: Record<string, string> = {
  flag: '⚑', session: '◷', pit: 'P', bestlap: '◆', track: '⚠', penalty: '⛔', retire: '✕', position: '⇅', tyres: '◎', damage: '✱', notif: '✉',
};
const KIND_LABEL: Record<string, string> = {
  flag: 'Drapeau', session: 'Session', pit: 'Stand', bestlap: 'Meilleur tour', track: 'Limites de piste', penalty: 'Pénalité', retire: 'Abandon', position: 'Position', tyres: 'Pneus', damage: 'Dégâts', notif: 'Notification',
};

interface FeedItem { key: string; kind: string; level: string; title: string; text: string | null; when: string; at: number; player: boolean }

export function EventFeed({ events, notices, sessionTime }: { events: RaceEvent[]; notices: Notice[]; sessionTime: number }) {
  const [filter, setFilterState] = useState(() => readPref('smhub-accueil-filtre', 'all'));
  const setFilter = (f: string) => { setFilterState(f); writePref('smhub-accueil-filtre', f); };
  const now = Date.now();
  const items: FeedItem[] = [
    ...events.map((e) => ({
      key: `e${e.id}`, kind: e.kind, level: e.level, title: e.title, text: e.text, player: e.isPlayer,
      when: `${e.lap ? `T${e.lap} · ` : ''}${clock(e.time)}`,
      at: now - Math.max(0, sessionTime - e.time) * 1000,
    })),
    ...notices.filter((n) => !n.fromEvent).map((n) => ({
      key: `n${n.id}`, kind: 'notif', level: n.level, title: n.title ?? n.source, text: n.text, player: false,
      when: new Date(n.at).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }), at: n.at,
    })),
  ].sort((a, b) => b.at - a.at);
  const f = FILTERS.find((x) => x.id === filter) ?? FILTERS[0];
  const shown = f.kinds ? items.filter((i) => f.kinds!.includes(i.kind)) : items;
  const count = (k?: string[]) => (k ? items.filter((i) => k.includes(i.kind)).length : items.length);
  return (
    <div className="ev-wrap">
      <div className="db-chips ev-filters" role="group" aria-label="Filtrer les événements">
        {FILTERS.map((x) => {
          const n = count(x.kinds);
          return (
            <button key={x.id} className={`chip ${filter === x.id ? 'on' : ''}`} onClick={() => setFilter(x.id)} disabled={n === 0 && x.id !== 'all' && filter !== x.id}>
              {x.label}{n > 0 && <span className="ev-count tnum">{n}</span>}
            </button>
          );
        })}
      </div>
      {shown.length === 0 ? (
        <div className="db-empty"><div className="muted">Rien pour l'instant.</div></div>
      ) : (
        <ul className="ev-list">
          {shown.slice(0, 120).map((i) => (
            <li key={i.key} className={`ev-item ${i.level} k-${i.kind} ${i.player ? 'me' : ''}`}>
              <span className="ev-ico" title={KIND_LABEL[i.kind] ?? i.kind} aria-hidden>{KIND_ICON[i.kind] ?? '•'}</span>
              <div className="ev-body">
                <div className="ev-top"><b className="ev-title">{i.title}</b><span className="ev-when tnum">{i.when}</span></div>
                {i.text && <div className="ev-text">{i.text}</div>}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

// ───────────────────────────── Meilleurs tours ─────────────────────────────

function BestLaps({ ctx }: BlockProps) {
  const s = ctx.snapshot;
  const me = s.drivers.find((d) => d.isPlayer);
  const { laps } = useMemo(() => classBests(s.drivers), [s.drivers]);
  const classId = me?.classId ?? s.drivers[0]?.classId;
  const classBest = classId != null ? laps.get(classId) : null;
  const overall = s.drivers.filter((d) => d.bestLap).sort((a, b) => a.bestLap! - b.bestLap!)[0];
  const sectors = me?.bestSectors ?? [];
  const theoretical = sectors.length === 3 && sectors.every((v) => v != null && v > 0) ? sectors.reduce((a, b) => a! + b!, 0)! : null;
  const gapTo = (v: number | null | undefined, ref: number | null | undefined) => (v != null && ref != null && v > 0 && ref > 0 ? v - ref : null);
  const myGap = gapTo(me?.bestLap, classBest?.time);
  return (
    <div className="bl-grid">
      <div className="bl-card session">
        <div className="tm-label">Meilleur tour {s.isMultiClass && me?.className ? me.className : 'de la session'}</div>
        <div className="bl-row">
          <b className="bl-time purple tnum">{lapTime(classBest?.time)}</b>
          <span className="bl-who">{classBest ? `${classBest.driver.number != null ? `#${classBest.driver.number} ` : ''}${classBest.driver.name}` : '—'}</span>
        </div>
      </div>
      <div className="bl-card">
        <div className="tm-label">Mon meilleur tour</div>
        <div className="bl-row">
          <b className="bl-time tnum">{lapTime(me?.bestLap)}</b>
          {myGap != null && <span className={`bl-gap tnum ${myGap < 0.0005 ? 'best' : ''}`}>{myGap < 0.0005 ? 'référence' : `+${myGap.toFixed(3)}`}</span>}
        </div>
        {me?.bestLapNumber ? <div className="muted small">au tour {me.bestLapNumber}</div> : null}
      </div>
      <div className="bl-card">
        <div className="tm-label" title="Somme de vos meilleurs secteurs">Meilleur théorique</div>
        <div className="bl-row">
          <b className="bl-time tnum">{lapTime(theoretical)}</b>
          {theoretical != null && me?.bestLap ? <span className="bl-gap tnum">{delta(theoretical - me.bestLap, 3)}</span> : null}
        </div>
      </div>
      <div className="bl-card">
        <div className="tm-label">Dernier tour</div>
        <div className="bl-row">
          <b className="bl-time tnum">{lapTime(me?.lastLap)}</b>
          {me?.lastLap && me.bestLap ? <span className={`bl-gap tnum ${me.lastLapIsPersonalBest ? 'best' : ''}`}>{me.lastLapIsPersonalBest ? 'record' : delta(me.lastLap - me.bestLap, 3)}</span> : null}
        </div>
      </div>
      {s.isMultiClass && overall && overall.classId !== classId && (
        <div className="bl-card">
          <div className="tm-label">Meilleur tour toutes catégories</div>
          <div className="bl-row"><b className="bl-time tnum">{lapTime(overall.bestLap)}</b><span className="bl-who">{overall.name} · {overall.className}</span></div>
        </div>
      )}
    </div>
  );
}

// ───────────────────────────── Météo ─────────────────────────────

function Weather({ ctx }: BlockProps) {
  const w = ctx.snapshot.weather;
  const { data } = usePoll<WeatherReport>('/api/weather', 15000);
  const trend = (v: number | null | undefined, unit: string) => (v == null ? null : `${v >= 0 ? '+' : '−'}${Math.abs(v).toFixed(1)} ${unit}/h`);
  return (
    <div className="wx-wrap">
      <div className="wx-grid">
        <div className="wx-cell"><span className="tm-label">Air</span><b className="tnum">{num(w.air, 1)} °C</b><span className="muted small tnum">{trend(data?.airPerHour, '°C')}</span></div>
        <div className="wx-cell"><span className="tm-label">Piste</span><b className="tnum">{num(w.trackTemp, 1)} °C</b><span className="muted small tnum">{trend(data?.trackPerHour, '°C')}</span></div>
        <div className={`wx-cell ${w.rain > 0 ? 'warn' : ''}`}><span className="tm-label">Pluie</span><b className="tnum">{w.rain} %</b><span className="muted small">{w.rain === 0 ? 'sec' : w.rain < 30 ? 'faible' : w.rain < 60 ? 'modérée' : 'forte'}</span></div>
        <div className={`wx-cell ${w.wetness > 10 ? 'warn' : ''}`}><span className="tm-label">Piste mouillée</span><b className="tnum">{w.wetness} %</b><span className="muted small">{w.wetness === 0 ? 'sèche' : w.wetness < 30 ? 'humide' : 'mouillée'}</span></div>
        <div className="wx-cell">
          <span className="tm-label">Vent</span>
          <b className="tnum">{w.wind != null ? <><span className="tm-wind-arrow" style={{ transform: `rotate(${(w.windFrom ?? 0) + 180}deg)` }} aria-hidden>↑</span> {num(w.wind, 0)} km/h</> : '—'}</b>
          <span className="muted small">{w.wind != null ? `du ${compass(w.windFrom)}` : 'non transmis'}</span>
        </div>
      </div>
      {(data?.summary ?? w.trend) && <div className="wx-summary secondary">{data?.summary ?? w.trend}</div>}
    </div>
  );
}

// ───────────────────────────── Voiture, stratégie, dégâts, delta, duels ─────────────────────────────

function MyCar({ ctx }: BlockProps) {
  const p = ctx.snapshot.player;
  return (
    <div className="db-car">
      {p && <AlertList alerts={p.alerts ?? []} />}
      <CarStatus snapshot={ctx.snapshot} />
    </div>
  );
}

function Strategy({ ctx }: BlockProps) {
  const s = ctx.snapshot;
  if (!s.player) return <div className="db-empty muted">Pas de voiture joueur (mode spectateur).</div>;
  return s.sessionType === 'Race'
    ? <><RacePlanSummary plan={s.player.plan} /><NextStop snapshot={s} /></>
    : <MyCarPanel snapshot={s} />;
}

function Plan({ ctx, size }: BlockProps) {
  const s = ctx.snapshot;
  if (s.sessionType !== 'Race' || !s.player) return <div className="db-empty muted">Le plan de course (arrêts restants, fenêtre, ±1 tour, ratio, lift and coast) s'affiche en course.</div>;
  return <div className="db-pad"><RacePlanCard plan={s.player.plan} compact={size.bodyHeight < 260} /></div>;
}

function Damage({ ctx }: BlockProps) {
  const p = ctx.snapshot.player;
  if (!p) return <div className="db-empty muted">Pas de voiture joueur.</div>;
  const any = (p.damage ?? []).some((d) => d.percent >= 1);
  if (!any) return <div className="db-empty"><span className="db-ok" aria-hidden>✓</span><div>Aucun dégât.</div><div className="muted small">Pièce par pièce quand le jeu le fournit (F1, ACC, LMU).</div></div>;
  return <div className="db-pad"><DamageList player={p} /></div>;
}

function Delta({ ctx }: BlockProps) {
  const s = ctx.snapshot;
  const me = s.drivers.find((d) => d.isPlayer);
  return (
    <div className="db-delta">
      <DeltaBar label="Δ meilleur tour perso" value={s.player?.deltaBest ?? null} />
      <DeltaBar label="Δ tour précédent" value={s.player?.deltaLast ?? null} />
      <div className="db-delta-now">
        <span className="tm-label">Tour en cours</span>
        <b className={`tnum ${me && !me.currentLapValid ? 'invalid' : ''}`} title={me && !me.currentLapValid ? 'Tour invalidé' : undefined}>{lapTime(me?.currentLap)}</b>
      </div>
    </div>
  );
}

function Duels({ ctx }: BlockProps) {
  const s = ctx.snapshot;
  if (s.sessionType !== 'Race' || !(s.player?.battles?.length)) return <div className="db-empty muted">Les duels (devant / derrière, rythme comparé) s'affichent en course.</div>;
  return <div className="db-duels"><BattlePanel snapshot={s} onSelect={ctx.onSelect} /></div>;
}

// ───────────────────────────── Enregistrement ─────────────────────────────

registerBlock({
  type: 'classement', title: 'Classement', icon: 'timing', order: 1, addByDefault: true,
  description: 'Le tableau de chronométrage : écarts, intervalles, mini-secteurs, places gagnées, arrêts, pneus.',
  size: { w: 8, h: 14 }, medium: { w: 6, h: 12 }, minW: 4, minH: 5,
  render: (p) => <Standings {...p} />,
});
registerBlock({
  type: 'carte', title: 'Carte du circuit', icon: 'map', order: 2, addByDefault: true,
  description: 'Position des voitures sur le tracé, couleurs par catégorie.',
  size: { w: 4, h: 8 }, medium: { w: 3, h: 8 }, minW: 2, minH: 4,
  render: ({ ctx, size }) => (
    <div className="db-map">
      <TrackMap snapshot={ctx.snapshot} classColor={ctx.classColor} onSelect={ctx.onSelect} classView={ctx.classView} maxHeight={size.bodyHeight - (ctx.snapshot.isMultiClass ? 78 : 50)} />
    </div>
  ),
});
registerBlock({
  type: 'radio', title: 'Radio', icon: 'radio', order: 3, addByDefault: true,
  description: 'Derniers échanges pilote ↔ ingénieur, avec réécoute de la voix.',
  size: { w: 4, h: 6 }, medium: { w: 3, h: 8 }, minW: 2, minH: 3,
  badge: ({ ctx }) => (ctx.radio.length > 0 ? <span className="db-badge tnum">{ctx.radio.length}</span> : null),
  render: ({ ctx }) => <RadioFeed messages={ctx.radio} />,
});
registerBlock({
  type: 'voiture', title: 'Ma voiture', icon: 'car', order: 4, addByDefault: true,
  description: 'Pneus (températures, usure, pressions, freins), carburant, moteur, alertes.',
  size: { w: 5, h: 9 }, medium: { w: 6, h: 8 }, minW: 3, minH: 4,
  render: (p) => <MyCar {...p} />,
});
registerBlock({
  type: 'strategie', title: 'Stratégie', icon: 'strategy', order: 5, addByDefault: true,
  description: 'Course : tours restants, arrêt conseillé, undercut / overcut. Essais : meilleur tour et pneus sur le tour.',
  size: { w: 3, h: 9 }, medium: { w: 3, h: 8 }, minW: 2, minH: 3,
  badge: ({ ctx }) => <span className="db-sub">{ctx.snapshot.sessionType === 'Race' ? 'prochain arrêt' : 'ma voiture en essais'}</span>,
  render: (p) => <Strategy {...p} />,
});
registerBlock({
  type: 'plan', title: 'Plan de course', icon: 'strategy', order: 5.5, addByDefault: true,
  description: 'Jusqu’au drapeau : arrêts restants, fenêtre du prochain arrêt, un tour plus tôt ou plus tard, ratio carburant / énergie, un arrêt de moins en lift and coast.',
  size: { w: 4, h: 9 }, medium: { w: 3, h: 9 }, minW: 2, minH: 4,
  badge: ({ ctx }) => (ctx.snapshot.player?.plan?.stopsNeeded != null ? <span className="db-badge tnum">{ctx.snapshot.player.plan.stopsNeeded}</span> : null),
  render: (p) => <Plan {...p} />,
});
registerBlock({
  type: 'evenements', title: 'Événements de course', icon: 'flag', order: 6, addByDefault: true,
  description: 'Drapeaux, arrêts des voisins, meilleurs tours, tours invalidés, alertes et notifications passées.',
  size: { w: 4, h: 9 }, medium: { w: 3, h: 8 }, minW: 2, minH: 3,
  badge: ({ ctx }) => <span className="db-badge tnum">{ctx.events.length + ctx.noticeHistory.filter((n) => !n.fromEvent).length}</span>,
  render: ({ ctx }) => <EventFeed events={ctx.events} notices={ctx.noticeHistory} sessionTime={ctx.snapshot.sessionTime} />,
});
registerBlock({
  type: 'meilleurs', title: 'Meilleurs tours', icon: 'trophy', order: 7, addByDefault: true,
  description: 'Meilleur tour de la session, le vôtre et l\'écart, meilleur théorique, dernier tour.',
  size: { w: 4, h: 5 }, medium: { w: 3, h: 5 }, minW: 2, minH: 3,
  render: (p) => <BestLaps {...p} />,
});
registerBlock({
  type: 'duels', title: 'Duels', icon: 'race', order: 8, addByDefault: true,
  description: 'Voiture devant et derrière : écart, rythme comparé, tours avant le contact.',
  size: { w: 4, h: 5 }, medium: { w: 3, h: 5 }, minW: 2, minH: 3,
  render: (p) => <Duels {...p} />,
});
registerBlock({
  type: 'degats', title: 'Dégâts', icon: 'wrench', order: 9, addByDefault: true,
  description: 'Dégâts pièce par pièce et leur coût mesuré au tour.',
  size: { w: 4, h: 5 }, medium: { w: 3, h: 5 }, minW: 2, minH: 3,
  render: (p) => <Damage {...p} />,
});
registerBlock({
  type: 'meteo', title: 'Météo', icon: 'weather', order: 10,
  description: 'Air, piste, pluie, piste mouillée, vent et tendance.',
  size: { w: 4, h: 4 }, medium: { w: 3, h: 4 }, minW: 2, minH: 3,
  render: (p) => <Weather {...p} />,
});
registerBlock({
  type: 'delta', title: 'Delta', icon: 'delta', order: 11,
  description: 'Écart en direct avec votre meilleur tour et votre tour précédent.',
  size: { w: 4, h: 4 }, medium: { w: 3, h: 4 }, minW: 2, minH: 3,
  render: (p) => <Delta {...p} />,
});
