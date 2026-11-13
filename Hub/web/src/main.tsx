import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import { ErrorBoundary, installGlobalErrorReporting } from './components/ErrorBoundary';
import './styles.css';
import './music/music'; // musique Deezer : priorité à la radio

installGlobalErrorReporting();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary label="application">
      <App />
    </ErrorBoundary>
  </StrictMode>,
);
