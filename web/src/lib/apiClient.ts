import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

/**
 * Shared Axios instance for all API calls.
 *
 * Base URL is "/api" — the Vite dev server proxies that to the .NET
 * backend. In production, the same path will be served from the same
 * origin as the frontend, so no changes are needed when we deploy.
 *
 * Token handling:
 *  - Request interceptor attaches `Authorization: Bearer <access>` from
 *    localStorage on every call.
 *  - Response interceptor catches 401 responses, calls the refresh
 *    endpoint once, and retries the original request with the new token.
 *  - If refresh fails, we clear the auth mirror and redirect to /login.
 *
 * Race safety: if N requests 401 simultaneously, only the FIRST triggers
 * a refresh. The others await the same in-flight promise. This is
 * critical — otherwise a page that fires 5 parallel requests would fire
 * 5 refresh calls and rotate the refresh token 5 times, invalidating
 * itself.
 */

const ACCESS_TOKEN_KEY = 'econexus.accessToken';
const REFRESH_TOKEN_KEY = 'econexus.refreshToken';
const REFRESH_ENDPOINT = '/v1/auth/refresh';

export const apiClient: AxiosInstance = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000,
});

// ============================================================
// Request interceptor — attach bearer token
// ============================================================
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem(ACCESS_TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ============================================================
// Response interceptor — refresh-on-401 with race safety
// ============================================================

/** In-flight refresh promise. Non-null while a refresh is happening. */
let refreshPromise: Promise<string> | null = null;

/**
 * Calls the refresh endpoint with the stored refresh token.
 * On success: stores new tokens and resolves with the new access token.
 * On failure: clears all auth state and rejects.
 */
async function performRefresh(): Promise<string> {
  const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
  if (!refreshToken) {
    throw new Error('No refresh token available');
  }

  // Use a bare axios call (not `apiClient`) to avoid recursive
  // interceptors. The refresh endpoint does not require auth.
  const response = await axios.post(
    `/api${REFRESH_ENDPOINT}`,
    { refreshToken },
    { headers: { 'Content-Type': 'application/json' }, timeout: 15000 },
  );

  const data = response.data as {
    accessToken: string;
    refreshToken: string;
  };

  localStorage.setItem(ACCESS_TOKEN_KEY, data.accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);

  return data.accessToken;
}

/**
 * Clears tokens and redirects to /login. Called only when refresh fails.
 * Also clears the Zustand-persisted auth blob so the UI resets on reload.
 */
function handleRefreshFailure(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem('econexus.auth');

  if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalConfig = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
    const status = error.response?.status;

    // Only handle 401s, only once per request, and only if we have a refresh token.
    const isRefreshable =
      status === 401 &&
      originalConfig &&
      !originalConfig._retry &&
      localStorage.getItem(REFRESH_TOKEN_KEY) !== null &&
      originalConfig.url !== REFRESH_ENDPOINT;

    if (isRefreshable && originalConfig) {
      originalConfig._retry = true;

      try {
        // If a refresh is already in flight, wait for it. Otherwise, start one.
        if (!refreshPromise) {
          refreshPromise = performRefresh().finally(() => {
            refreshPromise = null;
          });
        }

        const newAccessToken = await refreshPromise;

        // Attach the new token and retry the original request.
        originalConfig.headers = originalConfig.headers ?? {};
        (originalConfig.headers as Record<string, string>).Authorization =
          `Bearer ${newAccessToken}`;

        return apiClient(originalConfig);
      } catch (refreshError) {
        handleRefreshFailure();
        return Promise.reject(refreshError);
      }
    }

    // Non-refreshable error — surface it as usual.
    if (error.response) {
      console.error(
        `[apiClient] ${error.config?.method?.toUpperCase()} ${error.config?.url} -> ${error.response.status}`,
        error.response.data,
      );
    } else {
      console.error('[apiClient] network error', error.message);
    }

    return Promise.reject(error);
  },
);
