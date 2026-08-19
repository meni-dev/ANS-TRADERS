import { useQuery } from '@tanstack/react-query'
import { fetchDashboard } from './api'

/**
 * The date is passed from the browser rather than left to the server clock, so "today" on the
 * dashboard is the shop's today even late in the evening.
 */
export function useDashboard(asOf: string) {
  return useQuery({
    queryKey: ['dashboard', asOf],
    queryFn: () => fetchDashboard(asOf),
    // Documents raised elsewhere in the app invalidate this through the shared 'dashboard' key;
    // between those, a minute-old figure is fine on a screen nobody stares at.
    staleTime: 60_000,
  })
}
