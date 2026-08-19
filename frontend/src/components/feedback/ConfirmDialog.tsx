import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import { Box, Button, Dialog, DialogActions, DialogContent, Stack, Typography } from '@mui/material'

type ConfirmDialogProps = {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  confirmColor?: 'primary' | 'error' | 'warning'
  loading?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  confirmColor = 'primary',
  loading,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const destructive = confirmColor === 'error'

  return (
    <Dialog open={open} onClose={loading ? undefined : onCancel} maxWidth="xs" fullWidth>
      <DialogContent sx={{ pt: 3, px: 3 }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
          <Box
            sx={{
              width: 40,
              height: 40,
              borderRadius: '8px',
              display: 'grid',
              placeItems: 'center',
              flexShrink: 0,
              bgcolor: destructive ? 'error.light' : 'primary.light',
              color: destructive ? 'error.main' : 'primary.dark',
            }}
          >
            <WarningAmberOutlinedIcon sx={{ fontSize: 20 }} />
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: '1rem', fontWeight: 700, mb: 0.5 }}>{title}</Typography>
            <Typography variant="body2" color="text.secondary">
              {description}
            </Typography>
          </Box>
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2.5, pt: 1, gap: 1 }}>
        <Button onClick={onCancel} disabled={loading} variant="outlined">
          Cancel
        </Button>
        <Button onClick={onConfirm} color={confirmColor} variant="contained" loading={loading}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
