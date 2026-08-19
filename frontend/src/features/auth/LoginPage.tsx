import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { ShopLogo } from '@/components/brand/ShopLogo'
import { ApiError } from '@/lib/api/client'
import { useAuth } from './AuthProvider'

export function LoginPage() {
  const { user, restoring, signIn } = useAuth()
  const navigate = useNavigate()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (restoring) return null
  if (user) return <Navigate to="/" replace />

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)
    setBusy(true)

    try {
      await signIn(username.trim(), password)
      navigate('/', { replace: true })
    } catch (caught) {
      // The server deliberately gives one message for a wrong username and a wrong password, so
      // there is nothing to unpack per field here.
      setError(
        caught instanceof ApiError
          ? Object.values(caught.errors)[0]?.[0] ?? caught.message
          : 'Could not reach the server',
      )
    } finally {
      setBusy(false)
    }
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', bgcolor: 'background.default', p: 2 }}>
      <Paper sx={{ width: '100%', maxWidth: 380, p: 4 }}>
        <Stack spacing={3} component="form" onSubmit={submit}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <ShopLogo height={38} />
            <Box>
              <Typography sx={{ fontSize: 17, fontWeight: 700, lineHeight: 1.2 }}>ANS Traders</Typography>
              <Typography sx={{ fontSize: 12, color: 'text.disabled' }}>Sign in to the counter</Typography>
            </Box>
          </Stack>

          {error && <Alert severity="error">{error}</Alert>}

          <TextField
            label="Username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            autoFocus
            autoComplete="username"
            fullWidth
          />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
            fullWidth
          />

          <Button type="submit" variant="contained" size="large" disabled={busy || !username || !password}>
            {busy ? 'Signing in…' : 'Sign in'}
          </Button>
        </Stack>
      </Paper>
    </Box>
  )
}
