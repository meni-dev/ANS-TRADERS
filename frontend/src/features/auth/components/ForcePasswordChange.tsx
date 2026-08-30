import { describeError } from '@/lib/api/errors'
import { Alert, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useAuth } from '../AuthProvider'
import { useChangePassword } from '../hooks'

export function ForcePasswordChange() {
  const { user, refresh, signOut } = useAuth()
  const changePassword = useChangePassword()

  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)

  const mismatch = confirm.length > 0 && next !== confirm

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)

    try {
      await changePassword.mutateAsync({ currentPassword: current, newPassword: next })
      await refresh()
    } catch (caught) {
      setError(describeError(caught, 'Could not reach the server'))
    }
  }

  return (
    // Anchored, not centred — see the note on the sign-in card. Same reason: the error must grow
    // the card downward rather than shift the fields somebody is typing into.
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'flex-start',
        pt: { xs: 6, sm: '16vh' },
        pb: 6,
        px: 2,
        bgcolor: 'background.default',
      }}
    >
      <Paper sx={{ width: '100%', maxWidth: 420, p: 4 }}>
        <Stack spacing={2.5} component="form" onSubmit={submit}>
          <Box>
            <Typography sx={{ fontSize: 17, fontWeight: 700 }}>Choose a password</Typography>
            <Typography variant="body2" color="text.secondary">
              {user?.name}, the password you signed in with was handed to you by somebody else.
              Replace it before you start billing.
            </Typography>
          </Box>

          {error && <Alert severity="error">{error}</Alert>}

          <TextField
            label="Current password"
            type="password"
            value={current}
            onChange={(event) => setCurrent(event.target.value)}
            autoComplete="current-password"
            autoFocus
            fullWidth
          />
          <TextField
            label="New password"
            type="password"
            value={next}
            onChange={(event) => setNext(event.target.value)}
            helperText="At least eight characters"
            autoComplete="new-password"
            fullWidth
          />
          <TextField
            label="Repeat new password"
            type="password"
            value={confirm}
            onChange={(event) => setConfirm(event.target.value)}
            error={mismatch}
            helperText={mismatch ? 'These two do not match' : ' '}
            autoComplete="new-password"
            fullWidth
          />

          <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between' }}>
            <Button onClick={() => void signOut()}>Sign out</Button>
            <Button
              type="submit"
              variant="contained"
              disabled={changePassword.isPending || !current || next.length < 8 || mismatch}
            >
              {changePassword.isPending ? 'Saving…' : 'Save and continue'}
            </Button>
          </Stack>
        </Stack>
      </Paper>
    </Box>
  )
}
