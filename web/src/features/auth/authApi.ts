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
