// web/src/components/layout/Footer.tsx
// EcoNexus AI — site footer.
// Tailwind-only. No external UI kit. Dark-mode aware.

import { Link } from 'react-router-dom';

const PLATFORM_LINKS = [
  { label: 'Stations', path: '/stations' },
  { label: 'Vehicles', path: '/vehicles' },
  { label: 'Jobs', path: '/jobs' },
  { label: 'Recycling', path: '/recycling' },
  { label: 'Citizen', path: '/citizen' },
];

const OPERATIONS_LINKS = [
  { label: 'Assistant', path: '/assistant' },
  { label: 'Forecast', path: '/forecast' },
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Sign in', path: '/login' },
];

const RESOURCE_LINKS = [
  { label: 'Documentation', path: '/docs' },
  { label: 'Architecture', path: '/architecture' },
  { label: 'Privacy', path: '/legal' },
  { label: 'Terms', path: '/legal' },
];

const GITHUB_URL = 'https://github.com/yogiinarendranath-cell/EcoNexusAI';
const LINKEDIN_URL = 'https://www.linkedin.com';

export default function Footer() {
  const year = new Date().getFullYear();

  return (
    <footer className="mt-16 border-t border-emerald-500/30 bg-neutral-50 dark:bg-neutral-900">
      <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
        {/* ── Four columns ─────────────────────────────────────── */}
        <div className="grid grid-cols-1 gap-10 sm:grid-cols-2 lg:grid-cols-4">

          {/* Brand */}
          <div>
            <h3 className="flex items-center gap-2 text-lg font-bold text-emerald-700 dark:text-emerald-400">
              <span aria-hidden>♻️</span>
              EcoNexus AI
            </h3>
            <p className="mt-3 text-sm leading-relaxed text-neutral-600 dark:text-neutral-400">
              AI-powered smart waste &amp; recycling network. IoT stations,
              predictive analytics, optimised collection routing, and citizen
              engagement — through one event-driven platform.
            </p>

            <div className="mt-4 flex items-center gap-2">
              {/* GitHub */}
              <a
                href={GITHUB_URL}
                target="_blank"
                rel="noreferrer noopener"
                aria-label="GitHub"
                className="rounded-md border border-neutral-300 p-2 text-neutral-700 transition hover:-translate-y-0.5 hover:border-emerald-500 hover:text-emerald-700 dark:border-neutral-700 dark:text-neutral-300 dark:hover:border-emerald-400 dark:hover:text-emerald-400"
              >
                <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden>
                  <path d="M8 0C3.58 0 0 3.58 0 8a8 8 0 0 0 5.47 7.59c.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82a7.4 7.4 0 0 1 2-.27c.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z" />
                </svg>
              </a>

              {/* LinkedIn */}
              <a
                href={LINKEDIN_URL}
                target="_blank"
                rel="noreferrer noopener"
                aria-label="LinkedIn"
                className="rounded-md border border-neutral-300 p-2 text-neutral-700 transition hover:-translate-y-0.5 hover:border-emerald-500 hover:text-emerald-700 dark:border-neutral-700 dark:text-neutral-300 dark:hover:border-emerald-400 dark:hover:text-emerald-400"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
                  <path d="M4.98 3.5A2.5 2.5 0 1 1 0 3.5a2.5 2.5 0 0 1 4.98 0ZM.5 8h4.9v15H.5V8Zm7.5 0h4.7v2.05h.07c.65-1.2 2.24-2.46 4.6-2.46 4.92 0 5.83 3.16 5.83 7.27V23h-4.9v-7.2c0-1.72-.03-3.93-2.4-3.93-2.4 0-2.77 1.87-2.77 3.8V23H8V8Z" />
                </svg>
              </a>

              <a
                href="/api/v1/ping"
                className="rounded-md border border-neutral-300 px-3 py-2 text-xs font-semibold text-neutral-700 transition hover:-translate-y-0.5 hover:border-emerald-500 hover:text-emerald-700 dark:border-neutral-700 dark:text-neutral-300 dark:hover:border-emerald-400 dark:hover:text-emerald-400"
              >
                API status
              </a>
            </div>
          </div>

          {/* Platform */}
          <nav aria-label="Platform">
            <h4 className="text-sm font-bold uppercase tracking-wide text-neutral-800 dark:text-neutral-200">
              Platform
            </h4>
            <ul className="mt-3 space-y-2">
              {PLATFORM_LINKS.map((l) => (
                <li key={l.path + l.label}>
                  <Link
                    to={l.path}
                    className="text-sm text-neutral-600 transition hover:text-emerald-700 dark:text-neutral-400 dark:hover:text-emerald-400"
                  >
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>

          {/* Operations */}
          <nav aria-label="Operations">
            <h4 className="text-sm font-bold uppercase tracking-wide text-neutral-800 dark:text-neutral-200">
              Operations
            </h4>
            <ul className="mt-3 space-y-2">
              {OPERATIONS_LINKS.map((l) => (
                <li key={l.path + l.label}>
                  <Link
                    to={l.path}
                    className="text-sm text-neutral-600 transition hover:text-emerald-700 dark:text-neutral-400 dark:hover:text-emerald-400"
                  >
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>

          {/* Resources */}
          <nav aria-label="Resources">
            <h4 className="text-sm font-bold uppercase tracking-wide text-neutral-800 dark:text-neutral-200">
              Resources
            </h4>
            <ul className="mt-3 space-y-2">
              {RESOURCE_LINKS.map((l) => (
                <li key={l.path + l.label}>
                  <Link
                    to={l.path}
                    className="text-sm text-neutral-600 transition hover:text-emerald-700 dark:text-neutral-400 dark:hover:text-emerald-400"
                  >
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        </div>

        {/* ── Bottom bar ───────────────────────────────────────── */}
        <div className="mt-10 flex flex-col items-center justify-between gap-4 border-t border-neutral-200 pt-6 sm:flex-row dark:border-neutral-800">
          <p className="text-xs text-neutral-500 dark:text-neutral-400">
            © {year} EcoNexus AI. All rights reserved.
          </p>

          <p className="text-xs text-neutral-500 dark:text-neutral-400">
            Built with{' '}
            <span className="text-emerald-600 dark:text-emerald-400">.NET 10</span> ·{' '}
            <span className="text-emerald-600 dark:text-emerald-400">React 19</span> ·{' '}
            <span className="text-emerald-600 dark:text-emerald-400">Tailwind 4</span>
          </p>

          <p className="text-xs text-neutral-500 dark:text-neutral-400">
            Made with <span aria-hidden>💚</span> by{' '}
            <span className="font-semibold text-emerald-700 dark:text-emerald-400">
              Narendra Nath
            </span>
          </p>
        </div>
      </div>
    </footer>
  );
}