import { Component, type ReactNode } from 'react';

/**
 * Filet de sécurité : si un écran plante (donnée inattendue d'un jeu), on affiche l'erreur et un bouton pour
 * continuer, au lieu d'un écran noir. L'erreur est envoyée au journal de SecondMonitor (journal.txt).
 */
export class ErrorBoundary extends Component<{ children: ReactNode; label: string; resetKey?: unknown }, { error: Error | null }> {
  state = { error: null as Error | null };

  static getDerivedStateFromError(error: Error) {
    return { error };
  }

  componentDidCatch(error: Error, info: { componentStack?: string | null }) {
    reportError(`Écran « ${this.props.label} » : ${error.message}`, `${error.stack ?? ''}\n${info.componentStack ?? ''}`.slice(0, 4000));
  }

  componentDidUpdate(prev: { resetKey?: unknown }) {
    if (prev.resetKey !== this.props.resetKey && this.state.error) this.setState({ error: null });
  }

  render() {
    if (!this.state.error) return this.props.children;
    return (
      <div className="empty">
        <div className="empty-icon" aria-hidden>⚠</div>
        <h2>Cet écran a rencontré un problème</h2>
        <p className="secondary">{this.state.error.message}</p>
        <p className="secondary small">Le détail est enregistré dans le journal de SecondMonitor. Les autres écrans continuent de fonctionner.</p>
        <button className="btn primary" onClick={() => this.setState({ error: null })}>Réessayer</button>
      </div>
    );
  }
}

export function reportError(message: string, detail?: string) {
  try {
    fetch('/api/client-log', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ message, detail }) }).catch(() => undefined);
  } catch {
    /* hors ligne */
  }
}

/** Erreurs JavaScript hors des écrans (événements, promesses) : envoyées au journal aussi. */
export function installGlobalErrorReporting() {
  window.addEventListener('error', (e) => reportError(`Erreur : ${e.message}`, `${e.filename}:${e.lineno}`));
  window.addEventListener('unhandledrejection', (e) => reportError(`Promesse rejetée : ${String((e.reason as Error)?.message ?? e.reason)}`));
}
