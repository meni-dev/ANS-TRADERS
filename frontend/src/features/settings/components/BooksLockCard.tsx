import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import { Alert, Button, Chip, Paper, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { useAuth } from '@/features/auth/AuthProvider'
import { ApiError } from '@/lib/api/client'
import { formatDate, todayIso } from '@/lib/format'
import { useSetBooksLock } from '../hooks'
import type { ShopSettingsDto } from '../types'

/**
 * Freezes everything up to a date, so a month already filed with GST cannot quietly change.
 * <p>
 * Kept out of the settings form on purpose: this is not a preference, it is an action with a
 * consequence, and folding it into the same Save button would let it move every time somebody
 * corrected a phone number.
 * </p>
 */
export function BooksLockCard({ settings }: { settings: ShopSettingsDto }) {
  const { can } = useAuth()
  const setBooksLock = useSetBooksLock()
  const { notify } = useNotification()

  const [date, setDate] = useState(settings.booksLockedUpTo ?? '')
  const [error, setError] = useState<string | null>(null)

  async function apply(value: string | null) {
    setError(null)

    try {
      await setBooksLock.mutateAsync(value)
      setDate(value ?? '')
      notify(value ? `Books locked up to ${formatDate(value)}` : 'Books unlocked')
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? Object.values(caught.errors)[0]?.[0] ?? caught.message
          : 'Could not reach the server',
      )
    }
  }

  return (
    <Paper sx={{ p: 3 }}>
      <Stack spacing={2}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <LockOutlinedIcon sx={{ fontSize: 20, color: 'text.disabled' }} />
          <Stack sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography sx={{ fontSize: 15, fontWeight: 600 }}>Books Lock</Typography>
            <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
              Once a month has been filed, nothing dated inside it should move. Lock up to the last
              day you filed and the app will refuse bills, purchases, notes, receipts, expenses and
              cancellations dated on or before it.
            </Typography>
          </Stack>
          <Chip
            size="small"
            color={settings.booksLockedUpTo ? 'success' : 'default'}
            label={
              settings.booksLockedUpTo
                ? `Locked to ${formatDate(settings.booksLockedUpTo)}`
                : 'Open'
            }
          />
        </Stack>

        {error && <Alert severity="error">{error}</Alert>}

        {!can('BooksLock') ? (
          <Typography sx={{ fontSize: 12.5, color: 'text.disabled' }}>
            Your role does not let you move this.
          </Typography>
        ) : (
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ alignItems: 'center' }}>
            <TextField
              size="small"
              type="date"
              label="Locked up to"
              value={date}
              onChange={(event) => setDate(event.target.value)}
              slotProps={{ inputLabel: { shrink: true }, htmlInput: { max: todayIso() } }}
            />
            <Button
              variant="contained"
              onClick={() => void apply(date || null)}
              disabled={setBooksLock.isPending || !date || date === settings.booksLockedUpTo}
            >
              Lock
            </Button>
            <Button
              color="warning"
              onClick={() => void apply(null)}
              disabled={setBooksLock.isPending || !settings.booksLockedUpTo}
            >
              Unlock
            </Button>
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
