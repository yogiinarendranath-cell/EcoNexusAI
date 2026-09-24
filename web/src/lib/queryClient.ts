import { QueryClient } from '@tanstack/react-query';

/**
 * Shared TanStack Query client.
 *
 * Defaults chosen for a real ops dashboard:
 * - staleTime 30s: data is considered fresh for 30 seconds; subsequent
 *   mounts use the cache instead of refetching instantly.
 * - retry 1: one retry on failure. Enough to survive a blip without
 *   hammering the API during an outage.
 * - refetchOnWindowFocus true: when you come back to the tab, refetch.
 *   Standard for ops dashboards where data freshness matters.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
    mutations: {
      retry: 0,
    },
  },
});
