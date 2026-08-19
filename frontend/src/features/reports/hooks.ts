import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { fetchRegister, fetchRegisters } from './api'

export function useRegisterList() {
  return useQuery({
    queryKey: ['registers'],
    queryFn: fetchRegisters,
    // The list of registers is code, not data — it cannot change while the tab is open.
    staleTime: Infinity,
  })
}

export function useRegister(key: string, fromDate: string, toDate: string) {
  return useQuery({
    queryKey: ['register', key, fromDate, toDate],
    queryFn: () => fetchRegister(key, fromDate, toDate),
    placeholderData: keepPreviousData,
  })
}
