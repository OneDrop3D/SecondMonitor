import { compoundInfo } from '../format';
import type { StrategyStatus } from '../types';

export function TyreBadge({ compound, laps }: { compound: string | null; laps?: number }) {
  const c = compoundInfo(compound);
  return (
    <span className="tyre" title={`${c.label}${laps != null ? ` — ${laps} tour(s) dans ce relais` : ''}`}>
      <span className={`tyre-dot ${c.kind}`}>{c.letter}</span>
      {laps != null && <span className="secondary tnum">{laps}</span>}
    </span>
  );
}

export function WearBar({ pct }: { pct: number }) {
  return (
    <span className="wearbar" title={`Usure ${pct.toFixed(0)} %`}>
      <span style={{ width: `${Math.min(100, Math.max(0, pct))}%` }} />
    </span>
  );
}

export function Confidence({ level }: { level: 'high' | 'medium' | 'low' }) {
  const n = level === 'high' ? 3 : level === 'medium' ? 2 : 1;
  const label = level === 'high' ? 'fiable' : level === 'medium' ? 'moyenne' : 'faible';
  return (
    <span className="conf" title={`Confiance ${label}`} aria-label={`Confiance ${label}`}>
      {[0, 1, 2].map((i) => <i key={i} className={i < n ? 'on' : ''} />)}
    </span>
  );
}

const STATUS_TEXT: Record<string, { ico: string; text: string }> = {
  ok: { ico: '✓', text: 'Suffisant' },
  short: { ico: '!', text: 'Arrêt nécessaire' },
  critical: { ico: '!', text: 'Critique' },
  learning: { ico: '…', text: 'Apprentissage' },
};

export function StatusTag({ status, text }: { status: StrategyStatus; text?: string }) {
  if (!status) return null;
  const s = STATUS_TEXT[status];
  return (
    <span className={`status ${status}`}>
      <span className="ico" aria-hidden>{s.ico}</span>
      {text ?? s.text}
    </span>
  );
}

export function ClassSwatch({ color }: { color: string }) {
  return <span className="swatch" style={{ background: color }} />;
}
