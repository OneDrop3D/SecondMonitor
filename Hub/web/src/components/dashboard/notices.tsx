import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';

/*
 * Notifications de la barre du haut : chacune se ferme d'une croix, disparaît seule quand c'est pertinent,
 * ne revient pas en boucle pour le même événement (clé + délai de silence), et reste consultable ensuite
 * dans le bloc « Événements de course » de l'accueil.
 */

export type NoticeLevel = 'info' | 'good' | 'warn' | 'bad';

export interface Notice {
  id: number;
  /** Même clé = même événement : pas de doublon, pas de retour en boucle après fermeture. */
  key: string;
  title?: string;
  text: string;
  level: NoticeLevel;
  /** Origine affichée dans le fil d'événements (« Connexion », « Course »…). */
  source: string;
  at: number;
  /** Durée d'affichage (ms) ; null = jusqu'à fermeture. */
  ttl: number | null;
  closedAt?: number;
  closedBy?: 'user' | 'auto';
  /** Notification issue d'un événement de course déjà présent dans le fil (pas de doublon dans l'historique). */
  fromEvent?: boolean;
}

export interface NoticeInput {
  key: string;
  text: string;
  title?: string;
  level?: NoticeLevel;
  source?: string;
  ttl?: number | null;
  fromEvent?: boolean;
  /** Silence après fermeture par l'utilisateur (ms). 10 min par défaut. */
  quietAfterClose?: number;
}

interface NoticeApi {
  active: Notice[];
  history: Notice[];
  notify: (n: NoticeInput) => void;
  dismiss: (id: number) => void;
  dismissAll: () => void;
  /** Ferme la notification de cette clé si elle est affichée (événement terminé). */
  resolve: (key: string) => void;
}

const NoticeContext = createContext<NoticeApi | null>(null);

export function useNotices(): NoticeApi {
  const ctx = useContext(NoticeContext);
  if (!ctx) throw new Error('NoticeProvider manquant');
  return ctx;
}

const DEFAULT_QUIET = 10 * 60_000;
const AUTO_QUIET = 30_000;
const MAX_ACTIVE = 4;
const MAX_HISTORY = 100;

export function NoticeProvider({ children }: { children: ReactNode }) {
  const [active, setActive] = useState<Notice[]>([]);
  const [history, setHistory] = useState<Notice[]>([]);
  const nextId = useRef(1);
  const quiet = useRef(new Map<string, number>()); // clé → silence jusqu'à (ms)
  const timers = useRef(new Map<number, number>());

  const close = useCallback((id: number, by: 'user' | 'auto') => {
    window.clearTimeout(timers.current.get(id));
    timers.current.delete(id);
    setActive((list) => {
      const n = list.find((x) => x.id === id);
      if (!n) return list;
      const closed: Notice = { ...n, closedAt: Date.now(), closedBy: by };
      quiet.current.set(n.key, Date.now() + (by === 'user' ? (n as Notice & { quiet?: number }).quiet ?? DEFAULT_QUIET : AUTO_QUIET));
      setHistory((h) => [...h.filter((x) => x.id !== id), closed].slice(-MAX_HISTORY));
      return list.filter((x) => x.id !== id);
    });
  }, []);

  const arm = useCallback((n: Notice) => {
    window.clearTimeout(timers.current.get(n.id));
    if (n.ttl != null) timers.current.set(n.id, window.setTimeout(() => close(n.id, 'auto'), n.ttl));
  }, [close]);

  const notify = useCallback((input: NoticeInput) => {
    const until = quiet.current.get(input.key);
    if (until != null && until > Date.now()) return;
    setActive((list) => {
      const existing = list.find((x) => x.key === input.key);
      if (existing) {
        // Même événement : on met le texte à jour et on relance la minuterie, sans doublon.
        const updated: Notice = { ...existing, text: input.text, title: input.title ?? existing.title, level: input.level ?? existing.level, ttl: input.ttl === undefined ? existing.ttl : input.ttl };
        arm(updated);
        return list.map((x) => (x.id === existing.id ? updated : x));
      }
      const n = {
        id: nextId.current++,
        key: input.key,
        title: input.title,
        text: input.text,
        level: input.level ?? 'info',
        source: input.source ?? 'Notification',
        at: Date.now(),
        ttl: input.ttl === undefined ? 8000 : input.ttl,
        fromEvent: input.fromEvent,
        quiet: input.quietAfterClose,
      } as Notice;
      arm(n);
      const next = [...list, n];
      // Trop de notifications à la fois : la plus ancienne part dans l'historique.
      if (next.length > MAX_ACTIVE) {
        const oldest = next[0];
        window.setTimeout(() => close(oldest.id, 'auto'), 0);
      }
      return next;
    });
  }, [arm, close]);

  const resolve = useCallback((key: string) => {
    setActive((list) => {
      const n = list.find((x) => x.key === key);
      if (n) window.setTimeout(() => close(n.id, 'auto'), 0);
      return list;
    });
  }, [close]);

  useEffect(() => () => { for (const t of timers.current.values()) window.clearTimeout(t); }, []);

  // Autres modules (mise à jour, musique…) : window.dispatchEvent(new CustomEvent('sm-notify', { detail: { key, text, level } })).
  useEffect(() => {
    const on = (e: Event) => { const d = (e as CustomEvent<NoticeInput>).detail; if (d?.key && d.text) notify(d); };
    window.addEventListener('sm-notify', on);
    return () => window.removeEventListener('sm-notify', on);
  }, [notify]);

  const api = useMemo<NoticeApi>(() => ({
    active,
    history,
    notify,
    resolve,
    dismiss: (id) => close(id, 'user'),
    dismissAll: () => { for (const n of active) close(n.id, 'user'); },
  }), [active, history, notify, resolve, close]);

  return <NoticeContext.Provider value={api}>{children}</NoticeContext.Provider>;
}

const LEVEL_ICON: Record<NoticeLevel, string> = { info: 'i', good: '✓', warn: '!', bad: '!' };

/** Pile de notifications en haut à droite, chacune avec sa croix. */
export function NoticeStack() {
  const { active, dismiss, dismissAll } = useNotices();
  if (active.length === 0) return null;
  return (
    <div className="nt-stack" role="region" aria-label="Notifications" aria-live="polite">
      {active.map((n) => (
        <div key={n.id} className={`nt-card ${n.level}`} role={n.level === 'bad' ? 'alert' : 'status'}>
          <span className="nt-ico" aria-hidden>{LEVEL_ICON[n.level]}</span>
          <div className="nt-body">
            {n.title && <div className="nt-title">{n.title}</div>}
            <div className="nt-text">{n.text}</div>
          </div>
          <button className="nt-close" onClick={() => dismiss(n.id)} aria-label="Fermer la notification" title="Fermer (reste visible dans Événements de course)">×</button>
          {n.ttl != null && <span className="nt-timer" style={{ animationDuration: `${n.ttl}ms` }} aria-hidden />}
        </div>
      ))}
      {active.length > 1 && <button className="nt-clear" onClick={dismissAll}>Tout fermer</button>}
    </div>
  );
}

const keyOf = (text: string) => text.replace(/[0-9]+([.,][0-9]+)?/g, '#');
// Partagé entre tous les blocs : masquer une alerte dans « Ma voiture » la masque aussi dans « Stratégie ».
const dismissedKeys = new Set<string>();
const seenAt = new Map<string, number>();
const listeners = new Set<() => void>();

/**
 * Alertes calculées à chaque instantané (pneus, dégâts, drapeaux) qu'on peut masquer d'une croix.
 * Une alerte masquée le reste tant qu'elle est présente (les chiffres qui bougent ne comptent pas) ;
 * elle ne revient qu'après avoir disparu pendant au moins `graceMs` (pas de retour en boucle).
 */
export function useDismissable(current: string[], scope = 'alerte', graceMs = 30_000) {
  const [, force] = useState(0);
  useEffect(() => {
    const l = () => force((x) => x + 1);
    listeners.add(l);
    return () => { listeners.delete(l); };
  }, []);
  const now = Date.now();
  const k = (t: string) => `${scope}:${keyOf(t)}`;
  for (const t of current) {
    const key = k(t);
    const prev = seenAt.get(key);
    if (prev != null && now - prev > graceMs) dismissedKeys.delete(key);
    seenAt.set(key, now);
  }
  return {
    visible: current.filter((t) => !dismissedKeys.has(k(t))),
    dismiss: (text: string) => { dismissedKeys.add(k(text)); listeners.forEach((l) => l()); },
  };
}
