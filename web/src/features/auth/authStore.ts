import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/**
 * Authenticated user profile — mirrors the `user` field of the login response.
 */
export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
};

/**
 * Persisted auth state. Stored in localStorage under `econexus.auth`.
 *
 * The access token is ALSO mirrored to a separate localStorage key
 * (`econexus.accessToken`) so the existing axios request interceptor in
 * `apiClient.ts` can read it without knowing about Zustand. One source
 * of truth, two read paths. This keeps `apiClient.ts` decoupled from
 * the state library.
 */
type AuthState = {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;

  setAuth: (payload: {
    user: AuthUser;
    accessToken: string;
    refreshToken: string;
  }) => void;

  clear: () => void;
};

const ACCESS_TOKEN_KEY = 'econexus.accessToken';

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      setAuth: ({ user, accessToken, refreshToken }) => {
        // Mirror the access token for the axios interceptor.
        localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);

        set({
          user,
          accessToken,
          refreshToken,
          isAuthenticated: true,
        });
      },

      clear: () => {
        // Remove the mirror too, otherwise the interceptor keeps sending a
        // dead token after logout.
        localStorage.removeItem(ACCESS_TOKEN_KEY);

        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
        });
      },
    }),
    {
      name: 'econexus.auth',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
);
