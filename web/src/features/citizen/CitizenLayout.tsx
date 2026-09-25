import { NavLink, Outlet } from 'react-router-dom';

const TABS = [
  { to: '/app', label: 'Home', icon: '🏠', end: true },
  { to: '/app/stations', label: 'Stations', icon: '📍', end: false },
  { to: '/app/report', label: 'Report', icon: '📸', end: false },
  { to: '/app/points', label: 'Points', icon: '⭐', end: false },
  { to: '/app/profile', label: 'Profile', icon: '👤', end: false },
];

export default function CitizenLayout() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans flex flex-col">
      {/* Main content area — leaves room for the bottom nav */}
      <main className="flex-1 pb-24">
        <Outlet />
      </main>

      {/* Bottom navigation — fixed on mobile, centered on desktop */}
      <nav className="fixed bottom-0 left-0 right-0 border-t border-slate-800 bg-slate-950/95 backdrop-blur">
        <div className="max-w-md mx-auto grid grid-cols-5">
          {TABS.map((tab) => (
            <NavLink
              key={tab.to}
              to={tab.to}
              end={tab.end}
              className={({ isActive }) =>
                'flex flex-col items-center justify-center py-3 text-xs transition ' +
                (isActive
                  ? 'text-emerald-400'
                  : 'text-slate-500 hover:text-slate-300')
              }
            >
              <span className="text-lg leading-none mb-0.5">{tab.icon}</span>
              <span>{tab.label}</span>
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  );
}
