import { Alert, Snackbar } from '@mui/material'
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'

type Severity = 'success' | 'error' | 'info' | 'warning'

type Notification = { message: string; severity: Severity }

type NotificationContextValue = {
  notify: (message: string, severity?: Severity) => void
}

const NotificationContext = createContext<NotificationContextValue | undefined>(undefined)

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [notification, setNotification] = useState<Notification | null>(null)

  const notify = useCallback((message: string, severity: Severity = 'success') => {
    setNotification({ message, severity })
  }, [])

  const value = useMemo(() => ({ notify }), [notify])

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <Snackbar
        open={!!notification}
        autoHideDuration={4000}
        onClose={() => setNotification(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        {notification ? (
          <Alert severity={notification.severity} onClose={() => setNotification(null)} variant="filled">
            {notification.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </NotificationContext.Provider>
  )
}

export function useNotification() {
  const context = useContext(NotificationContext)
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider')
  }
  return context
}
