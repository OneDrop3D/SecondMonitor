import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import { ErrorBoundary, installGlobalErrorReporting } from './components/ErrorBoundary';
import './styles.css';
import './music/music'; // musique Deezer : priorité à la radio
import { announceWindow } from './windows';

installGlobalErrorReporting();
announceWindow(); // fenêtre ajoutée (« ?fenetre=2 ») : son numéro reste à elle tant qu'elle est ouverte

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary label="application">
      <App />
    </ErrorBoundary>
  </StrictMode>,
);

// Fenêtre de SecondMonitor : la page signale qu'elle tourne (sans nouvelles au bout de 6 s, l'interface s'ouvre
// dans le navigateur, voir AppWindow.cs). Dans un navigateur, window.external n'a pas sendMessage : rien à faire.
try {
  (window.external as unknown as { sendMessage?: (m: string) => void } | undefined)?.sendMessage?.('pret');
} catch {
  /* hors de la fenêtre */
}
