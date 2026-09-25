import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AxiosError } from 'axios';
import { login } from './authApi';
import { useAuthStore } from './authStore';

export default function LoginPage() {
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.setAuth);

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const result = await login(email, password);
      setAuth({
        user: result.user,
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
      });
      navigate('/app');
    } catch (err) {
      // Distinguish "wrong credentials" from "network / server error".
      if (err instanceof AxiosError && err.response?.status === 401) {
        setError('Invalid email or password.');
      } else if (err instanceof AxiosError && err.response) {
        setError(`Sign-in failed (${err.response.status}). Please try again.`);
      } else {
        setError('Could not reach the server. Is the API running?');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans flex flex-col">
      <header className="border-b border-slate-800">
        <div className="max-w-7xl mx-auto px-6 py-5 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-emerald-500 flex items-center justify-center font-bold text-slate-950">
              EN
            </div>
            <div>
              <div className="text-lg font-semibold tracking-tight">EcoNexus AI</div>
              <div className="text-xs text-slate-400">Smart Waste & Recycling Network</div>
            </div>
          </Link>
          <Link to="/" className="text-sm text-slate-300 hover:text-white transition">
            ← Back
          </Link>
        </div>
      </header>

      <main className="flex-1 flex items-center justify-center px-6 py-16">
        <div className="w-full max-w-md">
          <div className="rounded-2xl border border-slate-800 bg-slate-900/50 p-8">
            <h1 className="text-2xl font-bold tracking-tight">Sign in</h1>
            <p className="text-sm text-slate-400 mt-1">
              Access the operations dashboard.
            </p>

            {error && (
              <div className="mt-6 rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit} className="mt-6 space-y-5">
              <div>
                <label
                  htmlFor="email"
                  className="block text-sm font-medium text-slate-300 mb-2"
                >
                  Email
                </label>
                <input
                  id="email"
                  type="email"
                  required
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={submitting}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 disabled:opacity-50 transition"
                  placeholder="you@example.com"
                />
              </div>

              <div>
                <label
                  htmlFor="password"
                  className="block text-sm font-medium text-slate-300 mb-2"
                >
                  Password
                </label>
                <input
                  id="password"
                  type="password"
                  required
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={submitting}
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-2.5 text-slate-100 placeholder-slate-600 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 focus:border-emerald-500/40 disabled:opacity-50 transition"
                  placeholder="••••••••"
                />
              </div>

              <button
                type="submit"
                disabled={submitting}
                className="w-full rounded-lg bg-emerald-500 px-4 py-2.5 font-medium text-slate-950 hover:bg-emerald-400 focus:outline-none focus:ring-2 focus:ring-emerald-500/40 disabled:opacity-50 disabled:cursor-not-allowed transition"
              >
                {submitting ? 'Signing in…' : 'Sign in'}
              </button>
            </form>

            <div className="mt-6 text-xs text-slate-500 text-center">
              Demo credentials: <span className="font-mono">demo@econexus.local</span> /{' '}
              <span className="font-mono">Demo!Pass123</span>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
