import CloseIcon from '@mui/icons-material/Close'
import { Box, DialogTitle, IconButton, Stack, Tooltip, Typography } from '@mui/material'
import type { ReactNode } from 'react'

type DialogHeaderProps = {
  title: string
  /** Optional line under the title explaining what the form is for. */
  subtitle?: string
  /** Leading badge or icon, shown to the left of the title. */
  icon?: ReactNode
  onClose: () => void
  /** Blocks the close affordance while a submit is in flight. */
  disabled?: boolean
}

/**
 * Shared dialog header giving every modal a title block and a close button in the top-right.
 * The dialog body scrolls under it, so this stays pinned and the user always has a visible way
 * out without hunting for the Cancel button below the fold.
 */
export function DialogHeader({ title, subtitle, icon, onClose, disabled }: DialogHeaderProps) {
  return (
    <DialogTitle
      component="div"
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: 1.5,
        px: 3,
        py: 2,
        flexShrink: 0,
      }}
    >
      {icon && (
        <Box
          sx={{
            width: 36,
            height: 36,
            borderRadius: '8px',
            display: 'grid',
            placeItems: 'center',
            bgcolor: 'primary.light',
            color: 'primary.dark',
            flexShrink: 0,
            mt: 0.25,
          }}
        >
          {icon}
        </Box>
      )}

      <Box sx={{ flexGrow: 1, minWidth: 0 }}>
        <Typography sx={{ fontSize: '1.0625rem', fontWeight: 700, letterSpacing: '-0.01em', lineHeight: 1.35 }}>
          {title}
        </Typography>
        {subtitle && (
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
            {subtitle}
          </Typography>
        )}
      </Box>

      <Stack direction="row" sx={{ flexShrink: 0, mt: -0.5, mr: -1 }}>
        <Tooltip title="Close">
          {/* Wrapper keeps the tooltip alive while the button is disabled mid-save. */}
          <Box component="span">
            <IconButton onClick={onClose} disabled={disabled} size="small" aria-label="Close dialog">
              <CloseIcon sx={{ fontSize: 20 }} />
            </IconButton>
          </Box>
        </Tooltip>
      </Stack>
    </DialogTitle>
  )
}
