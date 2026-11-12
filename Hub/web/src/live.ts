import { useEffect, useRef, useState } from 'react';
import type { RaceSnapshot } from './types';

export type LinkState = 'connecting' | 'open' | 'closed';

export interface LiveMessage {
  type: string;
  data: unknown;
}

/** Connexion WebSocket au hub, avec reconnexion automatique. */
export function useLive(onMessage?: (m: LiveMessage) => void) {
  const [snapshot, setSnapshot] = useState<RaceSnapshot | null>(null);
  const [link, setLink] = useState<LinkState>('connecting');
  const handler = useRef(onMessage);
  handler.current = onMessage;

  useEffect(() => {
    let socket: WebSocket | null = null;
    let retry: number | undefined;
    let stopped = false;

    const connect = () => {
      setLink('connecting');
      const proto = location.protocol === 'https:' ? 'wss' : 'ws';
      socket = new WebSocket(`${proto}://${location.host}/ws`);
      socket.onopen = () => setLink('open');
      socket.onmessage = (ev) => {
        try {
          const msg = JSON.parse(ev.data) as LiveMessage;
          if (msg.type === 'snapshot') setSnapshot(msg.data as RaceSnapshot);
          handler.current?.(msg);
        } catch {
          /* message ignoré */
        }
      };
      socket.onclose = () => {
        setLink('closed');
        if (!stopped) retry = window.setTimeout(connect, 1500);
      };
      socket.onerror = () => socket?.close();
    };

    connect();
    return () => {
      stopped = true;
      window.clearTimeout(retry);
      socket?.close();
    };
  }, []);

  return { snapshot, link };
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
  });
  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  return (await res.json()) as T;
}

/**
 * Interroge périodiquement une route de l'API tant que la vue est affichée.
 * Garde la dernière réponse en cas d'erreur passagère ; s'arrête quand la page est masquée.
 */
export function usePoll<T>(path: string | null, intervalMs: number): { data: T | null; error: string | null; reload: () => void } {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);
  useEffect(() => {
    if (!path) return;
    let alive = true;
    let timer: number | undefined;
    let first = true;
    const load = async () => {
      if (first || document.visibilityState === 'visible') {
        first = false;
        try {
          const r = await api<T>(path);
          if (alive) { setData(r); setError(null); }
        } catch (e) {
          if (alive) setError((e as Error).message);
        }
      }
      if (alive) timer = window.setTimeout(load, intervalMs);
    };
    load();
    return () => { alive = false; window.clearTimeout(timer); };
  }, [path, intervalMs, tick]);
  return { data, error, reload: () => setTick((t) => t + 1) };
}
