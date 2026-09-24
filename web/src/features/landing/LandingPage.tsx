import { Link } from 'react-router-dom';
import UserMenu from '../auth/UserMenu';

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans">
      {/* Header */}
      <header className="border-b border-slate-800">
        <div className="max-w-7xl mx-auto px-6 py-5 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-emerald-500 flex items-center justify-center font-bold text-slate-950">
              EN
            </div>
            <div>
              <div className="text-lg font-semibold tracking-tight">EcoNexus AI</div>
              <div className="text-xs text-slate-400">Smart Waste & Recycling Network</div>
            </div>
          </div>
          <nav className="flex items-center gap-4">
            <Link
              to="/stations"
              className="px-4 py-2 rounded-lg bg-emerald-500 text-slate-950 font-medium hover:bg-emerald-400 transition text-sm"
            >
              View stations
            </Link>
            <UserMenu />
          </nav>
        </div>
      </header>

      {/* Hero */}
      <main className="max-w-7xl mx-auto px-6 py-16">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          {/* Left: copy */}
          <div>
            <div className="inline-block px-3 py-1 rounded-full bg-emerald-500/10 text-emerald-400 text-xs font-medium mb-6 border border-emerald-500/20">
              Cloud-native · AI-enabled · Event-driven
            </div>
            <h1 className="text-4xl md:text-5xl font-bold tracking-tight leading-tight">
              Intelligent waste collection,
              <br />
              recycling & resource recovery.
            </h1>
            <p className="mt-6 text-lg text-slate-400 leading-relaxed">
              EcoNexus AI connects IoT-enabled waste stations, predictive analytics,
              intelligent collection routing, citizen engagement, and recycling
              operations through a real-time event-driven platform.
            </p>
            <div className="mt-8 flex flex-wrap gap-4">
              <Link
                to="/stations"
                className="px-6 py-3 rounded-lg bg-emerald-500 text-slate-950 font-medium hover:bg-emerald-400 transition"
              >
                Open dashboard
              </Link>
              <a
                href="https://github.com/yogiinarendranath-cell/EcoNexusAI"
                target="_blank"
                rel="noreferrer"
                className="px-6 py-3 rounded-lg border border-slate-700 text-slate-200 hover:bg-slate-900 transition"
              >
                View on GitHub
              </a>
            </div>
          </div>

          {/* Right: hero image */}
          <div className="relative">
            <div className="absolute -inset-4 bg-emerald-500/10 blur-3xl rounded-full" />
            <div className="relative rounded-2xl overflow-hidden border border-slate-800 shadow-2xl shadow-emerald-500/10">
              <img
                src="/hero-city.png"
                alt="A futuristic smart city featuring AI-powered waste sorting, IoT-connected collection vehicles, and a live operations center."
                className="w-full h-auto block"
                loading="eager"
              />
            </div>
          </div>
        </div>

        {/* Status cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-24">
          <StatusCard label="Smart Stations" value="2,481" sublabel="2,392 online" tone="default" />
          <StatusCard label="Today's Waste" value="184.6 t" sublabel="71.4% recycled" tone="default" />
          <StatusCard label="Critical Alerts" value="31" sublabel="Action required" tone="critical" />
        </div>

        {/* Footer note */}
        <div className="mt-16 text-xs text-slate-500">
          Step 9.5c — Stations route wired. Live data fetch lives at /stations.
        </div>
      </main>
    </div>
  );
}

type StatusCardProps = {
  label: string;
  value: string;
  sublabel: string;
  tone?: 'default' | 'critical';
};

function StatusCard({ label, value, sublabel, tone = 'default' }: StatusCardProps) {
  const isCritical = tone === 'critical';
  return (
    <div
      className={
        'rounded-xl border p-6 bg-slate-900/50 ' +
        (isCritical ? 'border-red-500/30' : 'border-slate-800')
      }
    >
      <div className="text-sm text-slate-400">{label}</div>
      <div
        className={
          'text-3xl font-semibold mt-2 ' +
          (isCritical ? 'text-red-400' : 'text-slate-100')
        }
      >
        {value}
      </div>
      <div className="text-xs text-slate-500 mt-1">{sublabel}</div>
    </div>
  );
}
