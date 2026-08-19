import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../AuthProvider'
import type { Permission } from '../types'

/**
 * Keeps a whole screen behind a permission.
 * <p>
 * Hiding the nav row is not enough on its own — a typed URL, an old bookmark or a link in a
 * WhatsApp message all land straight on the route. Without this the page loads, every query it
 * makes comes back 403, and the screen renders its empty state: a purchase list that says
 * <i>no purchases yet</i> to somebody who simply is not allowed to look. Saying so plainly is the
 * difference between a rule and a bug.
 * </p>
 */
export function RequirePermission({ permission }: { permission: Permission }) {
  const { can } = useAuth()
  const navigate = useNavigate()

  if (can(permission)) {
    return <Outlet />
  }

  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '60vh', px: 2 }}>
      <Paper sx={{ p: 4, maxWidth: 420, textAlign: 'center' }}>
        <Stack spacing={1.5} sx={{ alignItems: 'center' }}>
          <LockOutlinedIcon sx={{ fontSize: 28, color: 'text.disabled' }} />
          <Typography sx={{ fontSize: 16, fontWeight: 700 }}>Not your screen</Typography>
          <Typography variant="body2" color="text.secondary">
            Your role does not include this. If you need it, ask whoever manages people at the shop
            to add it to your role.
          </Typography>
          <Button variant="outlined" size="small" onClick={() => navigate('/')}>
            Back to the dashboard
          </Button>
        </Stack>
      </Paper>
    </Box>
  )
}
