import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ApiError, apiRequest } from '@/lib/api/client'
import { getToken, notifySessionExpired, onSessionExpired, setToken } from '@/lib/api/session'
import { signIn as signInRequest, signOut as signOutRequest } from './api'
import type { Permission, SignedInUser } from './types'

type AuthState = {
  user: SignedInUser | null
  /** True until the stored token has been checked, so a reload does not flash the login screen. */
  restoring: boolean
  signIn: (username: string, password: string) => Promise<SignedInUser>
  signOut: () => Promise<void>
  /** Called after a password change, so the forced-change gate lifts without a re-login. */
  refresh: () => Promise<void>
  /**
   * True when the signed-in person holds this permission.
   * <p>
   * For hiding doors, never for guarding them. The server refuses the action either way; this only
   * keeps somebody from being offered a button that will answer "your role does not let you".
   * </p>
   */
  can: (permission: Permission) => boolean
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<SignedInUser | null>(null)
  const [restoring, setRestoring] = useState(true)

  const refresh = useCallback(async () => {
    if (!getToken()) {
      setUser(null)
      return
    }

    try {
      setUser(await apiRequest<SignedInUser>('/api/auth/me'))
    } catch (error) {
      // A 401 has already cleared the token inside the client, and saying so again here costs
      // nothing. An unreachable server is a different thing and used to be treated the same: the
      // token was thrown away, so a server that was merely restarting signed the counter out for
      // good, and the one screen left to explain it was a login form that could not reach the
      // server either. The token is still valid — keep it, so coming back is a reload.
      if (!(error instanceof ApiError && error.code === 'NETWORK_UNREACHABLE')) {
        notifySessionExpired()
      }

      setUser(null)
    }
  }, [])

  useEffect(() => {
    onSessionExpired(() => setUser(null))
    refresh().finally(() => setRestoring(false))

    return () => onSessionExpired(null)
  }, [refresh])

  const value = useMemo<AuthState>(
    () => ({
      user,
      restoring,
      can: (permission) => user?.permissions.includes(permission) ?? false,
      signIn: async (username, password) => {
        const result = await signInRequest(username, password)
        setToken(result.token)
        setUser(result.user)
        return result.user
      },
      signOut: async () => {
        // Told to the server first so the row goes away; the local clear happens either way, since
        // a signed-out counter must not stay usable because the network blinked.
        try {
          await signOutRequest()
        } finally {
          setToken(null)
          setUser(null)
        }
      },
      refresh,
    }),
    [user, restoring, refresh],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error('useAuth must be used inside AuthProvider')
  return context
}
