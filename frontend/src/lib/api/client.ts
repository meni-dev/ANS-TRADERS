import { getToken, notifySessionExpired } from './session'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5266'

export class ApiError extends Error {
  code: string
  errors: Record<string, string[]>
  status: number

  constructor(status: number, message: string, code: string, errors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.code = code
    this.errors = errors
  }
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  params?: Record<string, string | number | boolean | undefined>
}

function buildUrl(path: string, params?: RequestOptions['params']) {
  const url = new URL(`${API_BASE_URL}${path}`)

  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== '') {
        url.searchParams.set(key, String(value))
      }
    }
  }

  return url.toString()
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken()
  const headers: Record<string, string> = {}

  if (options.body) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  const response = await fetch(buildUrl(path, options.params), {
    method: options.method ?? 'GET',
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
  })

  // Handled once here rather than in every screen: a dead token makes every query fail at the same
  // moment, and without this each one would surface its own error toast behind a page the user can
  // no longer use.
  if (response.status === 401 && !path.startsWith('/api/auth/sign-in')) {
    notifySessionExpired()
  }

  if (response.status === 204) {
    return undefined as T
  }

  const isJson = response.headers.get('content-type')?.includes('application/json')
  const payload = isJson ? await response.json() : undefined

  if (!response.ok) {
    throw new ApiError(
      response.status,
      payload?.message ?? 'Request failed',
      payload?.code ?? 'UNKNOWN_ERROR',
      payload?.errors ?? {},
    )
  }

  return payload as T
}
