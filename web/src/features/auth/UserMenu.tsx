import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from './authStore';
import { logout } from './authApi';

/**
 * Header widget that reflects the current auth state.
 *
 * Logged out: shows a "Sign in" link to /login.
 * Logged in:  shows the user's display name and a "Sign out" button
 *             which revokes the refresh token on the server and clears
 *             the local store, then redirects to the landing page.
 */
export default function UserMenu() {
  const navigate = useNavigate();
  const { isAuthenticated, user, refreshToken, clear } = useAuthStore();

  async function handleSignOut() {
    if (refreshToken) {
      await logout(refreshToken);
    }
    clear();
    navigate('/');
  }

  if (isAuthenticated && user) {
    return (
      <div className="flex items-center gap-3 text-sm">
        <span className="text-slate-300">
          Welcome, <span className="text-slate-100 font-medium">{user.displayName}</span>
        </span>
        <button
          type="button"
          onClick={handleSignOut}
          className="px-3 py-1.5 rounded-lg border border-slate-700 text-slate-300 hover:bg-slate-900 hover:text-white transition"
        >
          Sign out
        </button>
      </div>
    );
  }

  return (
    <Link
      to="/login"
      className="text-sm text-slate-300 hover:text-white transition"
    >
      Sign in
    </Link>
  );
}
