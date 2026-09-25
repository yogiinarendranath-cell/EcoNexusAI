import { apiClient } from '../../lib/apiClient';
import type { AuthUser } from './authStore';

/**
 * Response shape from POST /api/v1/auth/login.
 * Mirrors EcoNexus.Contracts.Auth.AuthResponse.
 */
export type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
};

export type LoginRequest = {
  email: string;
  password: string;
};

/**
 * Authenticates a user and returns the token pair + user profile.
 * Throws an AxiosError with `response.status === 401` on bad credentials.
 */
export async function login(
  email: string,
  password: string,
): Promise<LoginResponse> {
  const { data } = await apiClient.post<LoginResponse>('/v1/auth/login', {
    email,
    password,
  } satisfies LoginRequest);
  return data;
}

/**
 * Response shape from POST /api/v1/auth/refresh.
 * Mirrors EcoNexus.Contracts.Auth.AuthResponse — the API returns a fresh
 * pair (new refresh token, new access token) on every successful refresh.
 */
export type RefreshResponse = {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
};

/**
 * Exchanges a refresh token for a fresh token pair. The API revokes the
 * incoming refresh token and returns a new one — callers must persist both.
 *
 * Note: `apiClient` already handles this flow automatically via its 401
 * interceptor. This function is exposed for explicit flows (e.g. on app
 * boot, refreshing before the access token expires).
 */
export async function refresh(refreshToken: string): Promise<RefreshResponse> {
  const { data } = await apiClient.post<RefreshResponse>('/v1/auth/refresh', {
    refreshToken,
  });
  return data;
}
/**
 * Revokes the refresh token server-side. Safe to call even if the token is
 * already expired — the API treats unknown/expired tokens as no-ops.
 */
export async function logout(refreshToken: string): Promise<void> {
  try {
    await apiClient.post('/v1/auth/logout', { refreshToken });
  } catch {
    // Never fail on logout. If the server rejects the token, we still want
    // the client to clear its local state.
  }
}

/**
 * Returns the current user's profile. Requires an Authorization header,
 * which `apiClient` attaches automatically from localStorage.
 */
export async function getCurrentUser(): Promise<AuthUser> {
  const { data } = await apiClient.get<AuthUser>('/v1/auth/me');
  return data;
}
