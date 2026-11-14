import { useCallback, useEffect, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react';

/** Vue zoomée : coordonnées à l'écran = coordonnées du dessin × k + (x, y). */
export interface ZoomView { k: number; x: number; y: number }

export const FULL_VIEW: ZoomView = { k: 1, x: 0, y: 0 };

interface Options {
  width: number;
  height: number;
  minK?: number;
  maxK?: number;
}

/** Événement de geste de Safari / WebKit (pincement du trackpad), absent des types du DOM. */
interface GestureLike extends UIEvent { scale: number; clientX: number; clientY: number }

/** Cran de molette de souris dans Safari : multiples de 4,000244140625 (le trackpad envoie des valeurs quelconques). */
const SAFARI_NOTCH = 4.000244140625;

/**
 * Zoom et déplacement d'un dessin (carte, trajectoires) :
 * - molette de la souris, ou pincement du trackpad (Chrome et Firefox : molette + Ctrl ; Safari et la fenêtre Mac : gestes WebKit) ;
 * - glisser pour se déplacer, deux doigts sur écran tactile, double-clic pour revenir à la vue entière ;
 * - la page ne défile pas pendant le zoom.
 * Le zoom se fait autour du pointeur ; boutons et « zoom sur une zone » sont animés.
 */
export function useZoomPan<T extends Element>({ width, height, minK = 1, maxK = 40 }: Options) {
  const [el, setEl] = useState<T | null>(null);
  const [view, setView] = useState<ZoomView>(FULL_VIEW);
  const [dragging, setDragging] = useState(false);
  const viewRef = useRef(view);
  const limits = useRef({ width, height, minK, maxK });
  limits.current = { width, height, minK, maxK };
  const anim = useRef<number | null>(null);
  const gesture = useRef<{ k0: number; v0: ZoomView; cx: number; cy: number } | null>(null);
  const pointers = useRef(new Map<number, { x: number; y: number }>());
  const drag = useRef<{ id: number; x0: number; y0: number; v0: ZoomView; moved: boolean } | null>(null);
  const pinch = useRef<{ d0: number; v0: ZoomView; cx: number; cy: number } | null>(null);
  const lastDragEnd = useRef(0);

  const clamp = useCallback((v: ZoomView): ZoomView => {
    const { width: w, height: h, minK: lo, maxK: hi } = limits.current;
    const k = Math.min(hi, Math.max(lo, v.k));
    // Une bonne partie du dessin reste toujours à l'écran.
    const mx = w * 0.4, my = h * 0.4;
    return {
      k,
      x: Math.min(mx, Math.max(w - w * k - mx, v.x)),
      y: Math.min(my, Math.max(h - h * k - my, v.y)),
    };
  }, []);

  const apply = useCallback((v: ZoomView) => {
    const c = clamp(v);
    viewRef.current = c;
    setView(c);
  }, [clamp]);

  const stopAnimation = () => {
    if (anim.current != null) cancelAnimationFrame(anim.current);
    anim.current = null;
  };

  /** Zoom d'un facteur autour d'un point de l'écran (px dans le dessin). */
  const zoomAt = useCallback((factor: number, cx: number, cy: number) => {
    stopAnimation();
    const v = viewRef.current;
    const k = clamp({ ...v, k: v.k * factor }).k;
    const f = k / v.k;
    apply({ k, x: cx - (cx - v.x) * f, y: cy - (cy - v.y) * f });
  }, [apply, clamp]);

  /** Va vers une vue en douceur (point central et zoom interpolés), sans animation si l'utilisateur préfère moins de mouvement. */
  const animateTo = useCallback((target: ZoomView) => {
    stopAnimation();
    const to = clamp(target);
    const from = viewRef.current;
    const reduce = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduce) { apply(to); return; }
    const { width: w, height: h } = limits.current;
    const c0 = { x: (w / 2 - from.x) / from.k, y: (h / 2 - from.y) / from.k };
    const c1 = { x: (w / 2 - to.x) / to.k, y: (h / 2 - to.y) / to.k };
    const t0 = performance.now();
    const duration = 320;
    const step = (now: number) => {
      const t = Math.min(1, (now - t0) / duration);
      const e = 1 - (1 - t) ** 3;
      const k = from.k * (to.k / from.k) ** e;
      const cx = c0.x + (c1.x - c0.x) * e, cy = c0.y + (c1.y - c0.y) * e;
      const v = { k, x: w / 2 - cx * k, y: h / 2 - cy * k };
      viewRef.current = v;
      setView(v);
      anim.current = t < 1 ? requestAnimationFrame(step) : null;
    };
    anim.current = requestAnimationFrame(step);
  }, [apply, clamp]);

  /** Cadre une zone du dessin (px du dessin à la vue entière), avec une marge. */
  const fitBox = useCallback((x0: number, y0: number, x1: number, y1: number, pad = 48) => {
    const { width: w, height: h, maxK: hi } = limits.current;
    const bw = Math.max(1, x1 - x0), bh = Math.max(1, y1 - y0);
    const k = Math.min(hi, Math.max(1, Math.min((w - pad * 2) / bw, (h - pad * 2) / bh)));
    animateTo({ k, x: w / 2 - ((x0 + x1) / 2) * k, y: h / 2 - ((y0 + y1) / 2) * k });
  }, [animateTo]);

  const reset = useCallback(() => animateTo(FULL_VIEW), [animateTo]);
  const zoomBy = useCallback((factor: number) => {
    const v = viewRef.current;
    const { width: w, height: h } = limits.current;
    const k = clamp({ ...v, k: v.k * factor }).k;
    const f = k / v.k;
    animateTo({ k, x: w / 2 - (w / 2 - v.x) * f, y: h / 2 - (h / 2 - v.y) * f });
  }, [animateTo, clamp]);

  // Écouteurs natifs : la molette doit pouvoir empêcher le défilement de la page (React les rend passifs).
  useEffect(() => {
    if (!el) return;
    const local = (e: { clientX: number; clientY: number }) => {
      const r = el.getBoundingClientRect();
      return { x: e.clientX - r.left, y: e.clientY - r.top };
    };
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      if (gesture.current) return; // Safari envoie le pincement en gestes : pas deux fois
      const p = local(e);
      if (e.ctrlKey) {
        // Pincement du trackpad (ou Ctrl + molette) : petits pas, proportionnels.
        zoomAt(Math.min(1.4, Math.max(0.7, Math.exp(-e.deltaY * 0.01))), p.x, p.y);
        return;
      }
      if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) {
        // Glissement horizontal à deux doigts : déplacement.
        stopAnimation();
        const v = viewRef.current;
        apply({ ...v, x: v.x - e.deltaX });
        return;
      }
      let dy = e.deltaY * (e.deltaMode === 1 ? 33 : e.deltaMode === 2 ? 400 : 1);
      const notches = Math.abs(dy) / SAFARI_NOTCH;
      if (dy !== 0 && Math.abs(notches - Math.round(notches)) < 1e-6) dy = Math.sign(dy) * Math.round(notches) * 100;
      zoomAt(Math.min(1.6, Math.max(0.6, Math.exp(-dy * 0.002))), p.x, p.y);
    };
    const onGestureStart = (ev: Event) => {
      const e = ev as GestureLike;
      e.preventDefault();
      stopAnimation();
      const p = local(e);
      gesture.current = { k0: viewRef.current.k, v0: viewRef.current, cx: p.x, cy: p.y };
    };
    const onGestureChange = (ev: Event) => {
      const e = ev as GestureLike;
      e.preventDefault();
      const g = gesture.current;
      if (!g) return;
      const k = clamp({ ...g.v0, k: g.k0 * e.scale }).k;
      const f = k / g.v0.k;
      apply({ k, x: g.cx - (g.cx - g.v0.x) * f, y: g.cy - (g.cy - g.v0.y) * f });
    };
    const onGestureEnd = (ev: Event) => { ev.preventDefault(); gesture.current = null; };
    el.addEventListener('wheel', onWheel as EventListener, { passive: false });
    el.addEventListener('gesturestart', onGestureStart, { passive: false } as AddEventListenerOptions);
    el.addEventListener('gesturechange', onGestureChange, { passive: false } as AddEventListenerOptions);
    el.addEventListener('gestureend', onGestureEnd, { passive: false } as AddEventListenerOptions);
    return () => {
      el.removeEventListener('wheel', onWheel as EventListener);
      el.removeEventListener('gesturestart', onGestureStart);
      el.removeEventListener('gesturechange', onGestureChange);
      el.removeEventListener('gestureend', onGestureEnd);
    };
  }, [el, apply, clamp, zoomAt]);

  useEffect(() => () => stopAnimation(), []);

  const local = (e: ReactPointerEvent) => {
    const r = e.currentTarget.getBoundingClientRect();
    return { x: e.clientX - r.left, y: e.clientY - r.top };
  };

  const bind = {
    onPointerDown: (e: ReactPointerEvent) => {
      if (e.pointerType === 'mouse' && e.button !== 0) return;
      const p = local(e);
      pointers.current.set(e.pointerId, p);
      stopAnimation();
      if (pointers.current.size === 1) {
        drag.current = { id: e.pointerId, x0: p.x, y0: p.y, v0: viewRef.current, moved: false };
      } else if (pointers.current.size === 2) {
        const [a, b] = [...pointers.current.values()];
        pinch.current = { d0: Math.hypot(a.x - b.x, a.y - b.y) || 1, v0: viewRef.current, cx: (a.x + b.x) / 2, cy: (a.y + b.y) / 2 };
        drag.current = null;
        try { e.currentTarget.setPointerCapture(e.pointerId); } catch { /* pointeur déjà relâché */ }
      }
    },
    onPointerMove: (e: ReactPointerEvent) => {
      if (!pointers.current.has(e.pointerId)) return;
      const p = local(e);
      pointers.current.set(e.pointerId, p);
      const pz = pinch.current;
      if (pz && pointers.current.size >= 2) {
        const [a, b] = [...pointers.current.values()];
        const k = clamp({ ...pz.v0, k: pz.v0.k * (Math.hypot(a.x - b.x, a.y - b.y) / pz.d0) }).k;
        const f = k / pz.v0.k;
        const cx = (a.x + b.x) / 2, cy = (a.y + b.y) / 2;
        apply({ k, x: cx - (pz.cx - pz.v0.x) * f, y: cy - (pz.cy - pz.v0.y) * f });
        return;
      }
      const d = drag.current;
      if (!d || d.id !== e.pointerId) return;
      const dx = p.x - d.x0, dy = p.y - d.y0;
      if (!d.moved) {
        if (Math.hypot(dx, dy) < 4) return; // simple clic : on laisse passer (étiquettes cliquables)
        d.moved = true;
        setDragging(true);
        try { e.currentTarget.setPointerCapture(e.pointerId); } catch { /* pointeur déjà relâché */ }
      }
      apply({ k: d.v0.k, x: d.v0.x + dx, y: d.v0.y + dy });
    },
    onPointerUp: (e: ReactPointerEvent) => end(e.pointerId),
    onPointerCancel: (e: ReactPointerEvent) => end(e.pointerId),
    onDoubleClick: () => reset(),
  };

  function end(id: number) {
    pointers.current.delete(id);
    if (drag.current?.moved) lastDragEnd.current = performance.now();
    if (pointers.current.size < 2) pinch.current = null;
    if (pointers.current.size === 0) drag.current = null;
    setDragging(false);
  }

  /** Vrai juste après un glisser : le clic qui suit ne doit pas compter (étiquette relâchée sous le pointeur). */
  const justDragged = () => performance.now() - lastDragEnd.current < 250;

  return { ref: setEl, view, dragging, zoomBy, zoomAt, fitBox, reset, bind, justDragged, isZoomed: view.k > 1.001 };
}

/** Distance « ronde » pour l'échelle (10, 20, 50, 100, 200, 500 m, 1 km…) qui fait environ `targetPx` pixels. */
export function niceScale(metersPerPx: number, targetPx = 90): number {
  const raw = metersPerPx * targetPx;
  const mag = 10 ** Math.floor(Math.log10(Math.max(raw, 1e-6)));
  return [1, 2, 5, 10].map((m) => m * mag).reduce((best, v) => (Math.abs(v - raw) < Math.abs(best - raw) ? v : best), mag);
}
