import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import { ErrorBoundary, installGlobalErrorReporting } from './components/ErrorBoundary';
import './styles.css';

installGlobalErrorReporting();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ErrorBoundary label="application">
      <App />
    </ErrorBoundary>
  </StrictMode>,
);
