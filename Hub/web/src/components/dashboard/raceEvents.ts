import { useEffect, useRef, useState } from 'react';
import { api } from '../../live';
import type { RaceEvent, RaceSnapshot } from '../../types';

/**
 * Événements de course tenus par le hub (drapeaux, arrêts des voisins, meilleurs tours, tours invalidés…).
 * On ne relit /api/race/events que lorsque l'instantané annonce un nouvel événement.
 * `onNew` reçoit seulement les événements arrivés après le premier chargement (pour les notifications).
 */
export function useRaceEvents(snapshot: RaceSnapshot | null, onNew?: (events: RaceEvent[]) => void): RaceEvent[] {
  const [events, setEvents] = useState<RaceEvent[]>([]);
  const state = useRef({ session: -1, last: 0, loading: false, loaded: false });
  const handler = useRef(onNew);
  handler.current = onNew;
  const lastId = snapshot?.lastEventId ?? 0;
  const session = snapshot?.eventSessionId ?? 0;
  const latest = useRef({ lastId, session });
  latest.current = { lastId, session };
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    const st = state.current;
    if (!snapshot?.hasData) return;
    const newSession = session !== st.session;
    if (!newSession && lastId <= st.last) return;
    if (st.loading) return;
    st.loading = true;
    const after = newSession ? 0 : st.last;
    api<{ session: number; events: RaceEvent[] }>(`/api/race/events?after=${after}`)
      .then((r) => {
        const fresh = r.session !== st.session;
        st.session = r.session;
        if (fresh) st.last = 0;
        const added = r.events;
        if (added.length > 0) st.last = added[added.length - 1].id;
        setEvents((list) => (fresh ? added : [...list, ...added]).slice(-300));
        if (st.loaded && !fresh && added.length > 0) handler.current?.(added);
        st.loaded = true;
      })
      .then(() => {
        st.loading = false;
        // D'autres événements sont arrivés pendant la lecture : on relit.
        if (latest.current.lastId > st.last || latest.current.session !== st.session) window.setTimeout(() => setRetry((x) => x + 1), 300);
      }, () => { st.loading = false; });
  }, [lastId, session, snapshot?.hasData, retry]); // eslint-disable-line react-hooks/exhaustive-deps

  return events;
}
