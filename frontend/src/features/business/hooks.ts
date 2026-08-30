import { apiRequest } from '@/lib/api/client'
import { useQuery } from '@tanstack/react-query'

/** The shop's own identity, served from the API's `Business` configuration section. */
export type BusinessProfileDto = {
  name: string
  legalName?: string | null
  gstin?: string | null
  stateCode: string
  state: string
  addressLine1?: string | null
  addressLine2?: string | null
  city?: string | null
  pincode?: string | null
  phone?: string | null
  email?: string | null
  invoiceFooter?: string | null
}

export function useBusinessProfile() {
  return useQuery({
    queryKey: ['business-profile'],
    queryFn: () => apiRequest<BusinessProfileDto>('/api/business-profile'),
    // Configuration, not data: it only changes when the server is redeployed.
    staleTime: Infinity,
  })
}
