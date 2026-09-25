import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../auth/authStore';

/**
 * Route guard for /app/* — redirects unauthenticated visitors to /login.
 * Does not currently enforce the Citizen role: any authenticated user
 * can preview the citizen experience during development.
 */
export default function RequireCitizen({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <>{children}</>;
}
