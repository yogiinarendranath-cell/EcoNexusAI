import axios, { type AxiosInstance } from 'axios';

/**
 * Shared Axios instance for all API calls.
 *
 * Base URL is "/api" — the Vite dev server proxies that to the .NET
 * backend. In production, the same path will be served from the same
 * origin as the frontend, so no changes are needed when we deploy.
 *
 * The Authorization header is added automatically from localStorage
 * when an access token is present. Auth flows (login, refresh) land
 * in step 9.6 — the interceptor is prepared now so we don't have to
 * touch this file again.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000,
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('econexus.accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Centralized error logging. In step 9.6 we will add automatic
    // refresh-on-401 here. For now we just surface the error.
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
