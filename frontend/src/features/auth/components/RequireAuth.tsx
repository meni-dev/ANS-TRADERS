import { Box, CircularProgress } from '@mui/material'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../AuthProvider'
import { ForcePasswordChange } from './ForcePasswordChange'

/**
 * Wraps every screen that is not the login page.
 * <p>
 * The server refuses unsigned requests on its own — this exists so the user sees a login form
 * instead of a dashboard full of failed queries.
 * </p>
 */
export function RequireAuth() {
  const { user, restoring } = useAuth()
  const location = useLocation()

  if (restoring) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center' }}>
        <CircularProgress size={28} />
      </Box>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  // A temporary password is a password somebody else has read out loud. Nothing else opens until it
  // has been replaced.
  if (user.mustChangePassword) {
    return <ForcePasswordChange />
  }

  return <Outlet />
}
