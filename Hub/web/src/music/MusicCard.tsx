import { registerBlock } from '../components/dashboard/registry';
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { musicApi, type MusicItem, type MusicKind, type MusicSettings, type MusicStatus } from './music';
import './music.css';

const KINDS: { id: MusicKind; label: string }[] = [
  { id: 'playlist', label: 'Playlist' },
  { id: 'artist', label: 'Artiste' },
  { id: 'album', label: 'Album' },
  { id: 'track', label: 'Titre' },
];

const isLocal = () => ['localhost', '127.0.0.1'].includes(location.hostname);

/**
 * Carte « Musique » (Deezer). `compact` : lecteur seul (bloc de l'accueil) ; sinon lecteur + réglages + guide (Réglages).
 * La musique joue dans l'application Deezer de l'ordinateur où est branché le casque du pilote.
 */
export function MusicCard({ compact = false }: { compact?: boolean }) {
  const [status, setStatus] = useState<MusicStatus | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [showGuide, setShowGuide] = useState(false);
  const alive = useRef(true);

  const load = useCallback(() => {
    musicApi.status().then((s) => { if (alive.current) { setStatus(s); setError(null); } }).catch((e) => alive.current && setError((e as Error).message));
  }, []);

  useEffect(() => {
    alive.current = true;
    load();
    const t = window.setInterval(load, 3000);
    return () => { alive.current = false; window.clearInterval(t); };
  }, [load]);

  const run = async (p: Promise<{ message: string | null }>) => {
    setBusy(true);
    try {
      const r = await p;
      setMessage(r.message);
    } catch (e) {
      setMessage((e as Error).message);
    } finally {
      setBusy(false);
      window.setTimeout(load, 400);
    }
  };

  const save = async (patch: Partial<MusicSettings>) => {
    if (!status) return;
    try {
      const next = await musicApi.saveSettings({ ...status.settings, ...patch });
      setStatus({ ...status, settings: next, enabled: next.enabled });
      load();
    } catch (e) {
      setMessage((e as Error).message);
    }
  };

  if (!status) {
    return (
      <div className="card music-card">
        <div className="card-head"><span className="card-title">Musique</span><span className="muted">Deezer</span></div>
        <div className="card-body muted">{error ? `Musique indisponible : ${error}` : 'Chargement…'}</div>
      </div>
    );
  }

  const st = status.state;
  const chip = !status.enabled ? { cls: '', text: 'désactivée' }
    : status.ducked ? { cls: 'radio', text: 'la radio passe avant' }
    : !status.sessionAllowed ? { cls: 'off', text: 'coupée pour cette session' }
    : st?.playing ? { cls: 'on', text: 'en lecture' }
    : st?.playing === false ? { cls: '', text: 'en pause' }
    : { cls: '', text: status.appRunning ? 'Deezer ouvert' : 'Deezer fermé' };

  return (
    <div className="card music-card">
      <div className="card-head">
        <span className="card-title">Musique</span>
        <span className="muted">Deezer</span>
        <span className={`music-chip ${chip.cls}`}>{chip.text}</span>
        <span className="spacer" />
        {!compact && <button className="chip" onClick={() => setShowGuide((v) => !v)}>{showGuide ? 'Masquer le guide' : 'Guide de mise en route'}</button>}
      </div>
      <div className="card-body stack-sm">
        {!status.enabled ? (
          <div className="stack-sm">
            <p className="secondary music-p">
              Écoutez Deezer dans le casque pendant que vous roulez, et pilotez-le à la voix (« mets mon Flow », « chanson suivante », « pause musique »).
              La musique joue dans <b>l'application Deezer de cet ordinateur</b>, avec votre compte et sa qualité de son ; elle se met en pause quand l'ingénieur parle.
            </p>
            <div className="row">
              <button className="btn primary" onClick={() => save({ enabled: true })}>Activer Deezer</button>
              {compact && <span className="muted">Réglages et guide : écran Réglages, carte Musique.</span>}
            </div>
          </div>
        ) : (
          <>
            <NowPlaying status={status} />
            <div className="music-controls" role="group" aria-label="Commandes de la musique">
              <button className="music-btn" disabled={busy} onClick={() => run(musicApi.command('previous'))} title="Morceau précédent" aria-label="Morceau précédent"><Glyph name="prev" /></button>
              {st?.playing ? (
                <button className="music-btn main" disabled={busy} onClick={() => run(musicApi.command('pause'))} title="Pause" aria-label="Pause"><Glyph name="pause" /></button>
              ) : (
                <button className="music-btn main" disabled={busy || !status.sessionAllowed} onClick={() => run(musicApi.command('play'))} title="Lecture" aria-label="Lecture"><Glyph name="play" /></button>
              )}
              <button className="music-btn" disabled={busy} onClick={() => run(musicApi.command('next'))} title="Morceau suivant" aria-label="Morceau suivant"><Glyph name="next" /></button>
              {status.canSetVolume && (
                <>
                  <span className="music-sep" />
                  <button className="music-btn" disabled={busy} onClick={() => run(musicApi.command('volumeDown'))} title="Moins fort (Deezer seul)" aria-label="Moins fort"><Glyph name="vol-" /></button>
                  <span className="music-vol tnum">{st?.volume != null ? `${st.volume} %` : '—'}</span>
                  <button className="music-btn" disabled={busy} onClick={() => run(musicApi.command('volumeUp'))} title="Plus fort (Deezer seul)" aria-label="Plus fort"><Glyph name="vol+" /></button>
                </>
              )}
              <span className="spacer" />
              {status.settings.favorite && (
                <button className="btn" disabled={busy || !status.sessionAllowed} onClick={() => run(musicApi.command('favorite'))} title={`Lancer ${status.settings.favorite.title}`}>Ma playlist</button>
              )}
              <button className="btn" disabled={busy || !status.sessionAllowed} onClick={() => run(musicApi.command('flow'))}>Mon Flow</button>
            </div>
            {status.output === 'pc' && !status.pcConnected && (
              <div className="callout warn music-callout">
                <span>La musique est réglée sur le PC de jeu, mais aucun collecteur ne la propose : dans la fenêtre du collecteur, cochez « Musique Deezer sur ce PC ».</span>
              </div>
            )}
            {!compact && <FavoriteBox status={status} save={save} onChanged={load} />}
            <MusicSearch disabled={!status.sessionAllowed} favoriteId={status.settings.favorite?.id ?? null} onFavorite={load} onPlayed={(m) => { setMessage(m); window.setTimeout(load, 400); }} />
            {(message ?? status.lastMessage) && <div className="music-msg secondary">{message ?? status.lastMessage}</div>}
            {!status.sessionAllowed && status.sessionReason && <div className="music-msg secondary">{status.sessionReason}</div>}
          </>
        )}

        {!compact && status.enabled && <MusicSettingsPanel status={status} save={save} />}
        {!compact && (showGuide || !status.enabled) && <MusicGuide status={status} />}
      </div>
    </div>
  );
}

function NowPlaying({ status }: { status: MusicStatus }) {
  const st = status.state;
  if (st?.title) {
    return (
      <div className="music-now">
        <div className="music-title" title={st.title}>{st.title}</div>
        <div className="secondary music-sub">{[st.artist, st.album].filter(Boolean).join(' · ') || st.source}</div>
      </div>
    );
  }
  return (
    <div className="music-now">
      <div className="music-title muted">{status.lastOpened ? `Dernier lancement : ${status.lastOpened}` : status.appRunning || status.settings.player === 'web' ? 'Titre en cours inconnu' : 'Deezer est fermé'}</div>
      <div className="muted music-sub">
        {status.platform === 'mac' && status.appRunning ? 'macOS ne donne pas toujours le titre aux autres applications.' : !status.appInstalled && status.settings.player === 'app' ? "L'application Deezer n'est pas installée : les liens s'ouvrent dans le navigateur." : 'Lecture, Flow ou recherche ci-dessous.'}
      </div>
    </div>
  );
}

/** Playlist préférée : lancée par « Lecture » quand rien n'est en cours, à la voix (« mets ma playlist ») et, si coché, au début de chaque session. */
function FavoriteBox({ status, save, onChanged }: { status: MusicStatus; save: (p: Partial<MusicSettings>) => void; onChanged: () => void }) {
  const fav = status.settings.favorite;
  if (!fav) {
    return <div className="muted small">Astuce : l'étoile ☆ d'un résultat de recherche en fait votre playlist préférée, lancée toute seule.</div>;
  }
  return (
    <div className="music-fav stack-sm">
      <div className="row">
        {fav.picture ? <img src={fav.picture} alt="" className="music-fav-img" /> : <span className="music-noimg" />}
        <span className="music-result-text">
          <span className="music-result-title">★ {fav.title}</span>
          <span className="muted">Préférée : « Lecture » la lance quand rien ne joue ; à la voix, « mets ma playlist ».</span>
        </span>
        <span className="spacer" />
        <button className="close" aria-label="Retirer la préférée" title="Retirer la préférée" onClick={() => musicApi.clearFavorite().then(onChanged).catch(() => {})}>✕</button>
      </div>
      <label className="switch">
        <input type="checkbox" checked={!!status.settings.favoriteAutoPlay} onChange={(e) => save({ favoriteAutoPlay: e.target.checked })} />
        <span>La lancer toute seule au début de chaque session (si la musique y est permise et que rien ne joue)</span>
      </label>
    </div>
  );
}

function MusicSearch({ disabled, onPlayed, favoriteId, onFavorite }: { disabled: boolean; onPlayed: (message: string) => void; favoriteId: number | null; onFavorite: () => void }) {
  const [q, setQ] = useState('');
  const [kind, setKind] = useState<MusicKind>('playlist');
  const [items, setItems] = useState<MusicItem[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = async (e?: FormEvent, k = kind) => {
    e?.preventDefault();
    if (!q.trim()) return;
    setLoading(true);
    setError(null);
    try {
      setItems(await musicApi.search(q.trim(), k));
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="stack-sm">
      <form className="music-search" onSubmit={search}>
        <input className="input" value={q} onChange={(e) => setQ(e.target.value)} placeholder="Rechercher sur Deezer…" aria-label="Rechercher sur Deezer" />
        <div className="seg">
          {KINDS.map((k) => <button type="button" key={k.id} className={kind === k.id ? 'on' : ''} onClick={() => { setKind(k.id); if (q.trim()) search(undefined, k.id); }}>{k.label}</button>)}
        </div>
        <button className="btn" type="submit" disabled={loading || !q.trim()}>{loading ? '…' : 'Chercher'}</button>
      </form>
      {error && <div className="music-msg secondary">{error}</div>}
      {items && (
        items.length === 0 ? <div className="muted">Aucun résultat.</div> : (
          <ul className="music-results">
            {items.slice(0, 8).map((it) => (
              <li key={`${it.kind}-${it.id}`} className="music-result-row">
                <button className="music-result" disabled={disabled} onClick={async () => {
                  try { onPlayed((await musicApi.open(it, kind)).message); } catch (err) { onPlayed((err as Error).message); }
                }}>
                  {it.picture ? <img src={it.picture} alt="" loading="lazy" /> : <span className="music-noimg" />}
                  <span className="music-result-text">
                    <span className="music-result-title">{it.title}</span>
                    <span className="muted">{[it.subtitle, kind === 'artist' ? fans(it.count) : kind === 'track' ? null : it.count ? `${it.count} titres` : null].filter(Boolean).join(' · ')}</span>
                  </span>
                  <Glyph name="play" />
                </button>
                <button className={`music-star ${favoriteId === it.id ? 'on' : ''}`} title={favoriteId === it.id ? 'Votre préférée' : 'En faire ma playlist préférée'} aria-label={favoriteId === it.id ? 'Préférée' : 'Choisir comme préférée'}
                  onClick={() => musicApi.setFavorite(it, kind).then(onFavorite).catch((err) => onPlayed((err as Error).message))}>
                  {favoriteId === it.id ? '★' : '☆'}
                </button>
              </li>
            ))}
          </ul>
        )
      )}
    </div>
  );
}

function fans(n: number) {
  if (!n) return null;
  return n >= 1_000_000 ? `${(n / 1_000_000).toFixed(1).replace('.', ',')} M de fans` : n >= 1000 ? `${Math.round(n / 1000)} k fans` : `${n} fans`;
}

function MusicSettingsPanel({ status, save }: { status: MusicStatus; save: (p: Partial<MusicSettings>) => void }) {
  const s = status.settings;
  const mac = status.platform === 'mac';
  const [permMsg, setPermMsg] = useState<string | null>(null);

  const toggleKeys = async (on: boolean) => {
    save({ mediaKeys: on });
    // L'autorisation n'est demandée qu'une seule fois (ensuite : bouton vers les réglages du Mac).
    if (on && mac && !status.permission.granted && !s.accessibilityAsked && isLocal()) {
      try {
        const r = await musicApi.askPermission();
        setPermMsg(r.granted ? 'Autorisation accordée.' : 'Cochez « SecondMonitor » dans la fenêtre Accessibilité qui vient de s’ouvrir, puis revenez ici.');
      } catch (e) {
        setPermMsg((e as Error).message);
      }
    }
  };

  return (
    <div className="music-settings stack-sm">
      <div className="music-set-row">
        <span className="secondary">Musique sur</span>
        <div className="seg">
          <button className={(s.output ?? 'ordinateur') === 'ordinateur' ? 'on' : ''} onClick={() => save({ output: 'ordinateur' })}>Cet ordinateur</button>
          <button className={s.output === 'pc' ? 'on' : ''} onClick={() => save({ output: 'pc' })}>Le PC de jeu (casque)</button>
        </div>
      </div>
      {s.output === 'pc' && (
        <div className="secondary small">
          Recherche, commandes et voix restent ici ; la musique joue dans l'application Deezer du PC de jeu, donc dans le casque.
          Dans la fenêtre du collecteur, cochez « Musique Deezer sur ce PC ». {status.pcConnected ? 'Collecteur relié.' : 'Aucun collecteur ne la propose pour l\'instant.'}
        </div>
      )}
      <div className="music-set-row">
        <span className="secondary">Lecteur</span>
        <div className="seg">
          <button className={s.player === 'app' ? 'on' : ''} onClick={() => save({ player: 'app' })}>Application Deezer</button>
          <button className={s.player === 'web' ? 'on' : ''} onClick={() => save({ player: 'web' })}>Lecteur web</button>
        </div>
      </div>
      <div className="music-set-row">
        <span className="secondary">Musique permise</span>
        <div className="seg">
          <button className={s.sessionPolicy === 'toujours' ? 'on' : ''} onClick={() => save({ sessionPolicy: 'toujours' })}>Toujours</button>
          <button className={s.sessionPolicy === 'pasEnCourse' ? 'on' : ''} onClick={() => save({ sessionPolicy: 'pasEnCourse' })}>Pas en course</button>
          <button className={s.sessionPolicy === 'essaisSeulement' ? 'on' : ''} onClick={() => save({ sessionPolicy: 'essaisSeulement' })}>Seulement en essais</button>
        </div>
      </div>
      <div className="music-set-row">
        <span className="secondary">Quand la radio parle</span>
        <div className="seg">
          <button className={s.duckMode === 'pause' ? 'on' : ''} onClick={() => save({ duckMode: 'pause' })}>Pause</button>
          {status.canSetVolume && <button className={s.duckMode === 'volume' ? 'on' : ''} onClick={() => save({ duckMode: 'volume' })}>Baisser Deezer</button>}
          <button className={s.duckMode === 'aucun' ? 'on' : ''} onClick={() => save({ duckMode: 'aucun' })}>Ne rien faire</button>
        </div>
      </div>
      {s.duckMode === 'volume' && status.canSetVolume && (
        <label className="music-set-row">
          <span className="secondary">Volume de Deezer pendant la radio</span>
          <select className="select compact" value={s.duckVolumePct} onChange={(e) => save({ duckVolumePct: Number(e.target.value) })}>
            {[0, 10, 20, 25, 35, 50].map((v) => <option key={v} value={v}>{v} %</option>)}
          </select>
        </label>
      )}
      {mac && (
        <div className="stack-sm">
          <label className="switch-row">
            <input type="checkbox" checked={s.mediaKeys} onChange={(e) => toggleKeys(e.target.checked)} />
            <span>
              <b>Touches multimédia</b> (lecture, suivant, précédent) — à cocher seulement si les boutons ci-dessus ne commandent pas Deezer.
              macOS demande alors l'autorisation <b>Accessibilité</b> : c'est elle qui permet à une application de « taper » des touches.
              SecondMonitor ne s'en sert que pour ces trois touches.
            </span>
          </label>
          {s.mediaKeys && !status.permission.granted && (
            <div className="callout warn music-callout">
              <span>
                Autorisation Accessibilité non accordée. Réglages Système › Confidentialité et sécurité › Accessibilité : cochez « SecondMonitor ».
                {!isLocal() && ' (à faire sur l’ordinateur de SecondMonitor)'}
              </span>
              {isLocal() && <button className="btn" onClick={() => musicApi.openPermissionSettings()}>Ouvrir les réglages</button>}
            </div>
          )}
          {permMsg && <div className="music-msg secondary">{permMsg}</div>}
        </div>
      )}
      <div className="row">
        <span className="spacer" />
        <button className="btn" onClick={() => save({ enabled: false })}>Désactiver Deezer</button>
      </div>
    </div>
  );
}

function MusicGuide({ status }: { status: MusicStatus }) {
  const p = status.platform;
  return (
    <div className="music-guide stack-sm">
      <div className="card-title">Mise en route</div>
      <ol className="guide-list secondary">
        {p === 'linux' ? (
          <>
            <li>Deezer n'a pas d'application Linux officielle : ouvrez <b>deezer.com</b> dans Chrome ou Firefox et connectez-vous (ou installez l'application non officielle « deezer-linux »). Choisissez « Lecteur web » ci-dessus.</li>
            <li>Installez <b>playerctl</b> (<span className="code">sudo apt install playerctl</span>) : c'est lui qui commande le lecteur (lecture, suivant, volume, titre).</li>
          </>
        ) : (
          <>
            <li>Installez l'<b>application Deezer</b> sur cet ordinateur (deezer.com/download{p === 'mac' ? ', ou l’App Store' : ', ou le Microsoft Store'}) et connectez-vous à votre compte. {status.appInstalled ? <b>✓ installée</b> : <b>pas encore installée</b>}</li>
            <li>Réglez la <b>qualité</b> dans Deezer (Paramètres › Audio : HiFi / haute qualité selon votre abonnement) et la sortie sur le casque du pilote.</li>
          </>
        )}
        <li>Activez Deezer ici, puis essayez les boutons : lecture, pause, morceau suivant.</li>
        {p === 'mac' && (
          <li>
            Si les boutons ne font rien (certaines versions de macOS bloquent la commande directe), cochez <b>Touches multimédia</b> :
            macOS demande une fois l'autorisation <b>Accessibilité</b> pour SecondMonitor. Tant que l'application n'est pas signée par un
            développeur Apple, macOS peut la redemander après une mise à jour de SecondMonitor (décocher puis recocher SecondMonitor dans la liste).
          </li>
        )}
        <li>À la radio, sans passer par l'IA : « mets de la musique », « mets mon Flow », « mets la playlist rock », « mets l'album Discovery »,
          « mets de la musique de Queen », « pause musique », « chanson suivante », « remets la précédente »,
          {status.canSetVolume ? ' « monte / baisse la musique »,' : ''} « c'est quoi ce morceau ? ».</li>
        <li>La radio passe avant : quand vous appuyez sur la radio ou que l'ingénieur parle, Deezer se met en pause{status.canSetVolume ? ' (ou baisse)' : ''} puis reprend.</li>
      </ol>
      <div className="muted music-p">
        Limites : Deezer ne propose pas de commande officielle pour les applications tierces. SecondMonitor cherche dans le catalogue public de Deezer
        puis ouvre le résultat dans Deezer ; selon la version de Deezer, la lecture peut ne pas démarrer toute seule (appuyez alors sur lecture).
        {p === 'mac' ? ' Sur Mac, le volume de Deezer seul ne peut pas être réglé (le volume du Mac changerait aussi la voix de l’ingénieur), et le titre en cours n’est pas toujours lisible.' : ''}
        {' '}Seuls les mots recherchés sont envoyés à Deezer, jamais votre compte.
      </div>
    </div>
  );
}

function Glyph({ name }: { name: 'play' | 'pause' | 'next' | 'prev' | 'vol+' | 'vol-' }) {
  const paths: Record<string, string> = {
    play: 'M8 5.5v13l10-6.5z',
    pause: 'M7 5h3.5v14H7zM13.5 5H17v14h-3.5z',
    next: 'M6 5.5v13l8.5-6.5zM16 5h2.5v14H16z',
    prev: 'M18 5.5v13L9.5 12zM5.5 5H8v14H5.5z',
    'vol+': 'M4 9.5h3.5L12 6v12l-4.5-3.5H4zM16 12h5M18.5 9.5v5',
    'vol-': 'M4 9.5h3.5L12 6v12l-4.5-3.5H4zM16 12h5',
  };
  const filled = name === 'play' || name === 'pause' || name === 'next' || name === 'prev';
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" aria-hidden fill={filled ? 'currentColor' : 'none'} stroke={filled ? 'none' : 'currentColor'} strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round">
      <path d={paths[name]} />
    </svg>
  );
}

/** Bloc pour la grille de l'accueil (registre de blocs) : la carte en version compacte. */
export const musicBlock = {
  id: 'musique',
  title: 'Musique',
  Component: () => <MusicCard compact />,
};

// Bloc de l'accueil (Disposition › Ajouter un bloc).
registerBlock({
  type: 'musique',
  title: 'Musique',
  icon: 'music',
  description: 'Deezer : titre en cours, lecture, recherche',
  size: { w: 4, h: 5 },
  medium: { w: 3, h: 5 },
  render: () => <MusicCard compact />,
});

export default MusicCard;
