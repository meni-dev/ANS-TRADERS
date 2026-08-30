import { accent, type AccentTone } from '@/theme/theme'
import { Box, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'

type PanelCardProps = {
  title: string
  /** Chip beside the title. Lets the eye find a module on a long dashboard without reading. */
  icon?: ReactNode
  /** Which hue the chip wears. Identifies the module; it does not grade its contents. */
  iconTone?: AccentTone
  /** One line under the title saying what is being counted, and over what period. */
  caption?: ReactNode
  /** Top-right: a total, a chip, a status. Not a second title. */
  action?: ReactNode
  /** Bottom: the one way through to the screen that owns this module. */
  footer?: ReactNode
  /** Turn off when the module draws to its own edges — a chart, a full-bleed table. */
  padded?: boolean
  children: ReactNode
}

/**
 * The shell every dashboard module wears.
 * <p>
 * Four panels had each grown their own header — same intent, four different type sizes and four
 * different paddings — and a dashboard whose cards do not line up reads as unfinished however good
 * each card is on its own. Modules differ in what they show, never in how they are framed.
 * </p>
 * <p>
 * <code>height: 100%</code> is on the card rather than left to the caller, so two modules sharing a
 * grid row end level regardless of which one has more rows in it.
 * </p>
 */
export function PanelCard({
  title,
  icon,
  iconTone = 'neutral',
  caption,
  action,
  footer,
  padded = true,
  children,
}: PanelCardProps) {
  const chip = accent[iconTone]

  return (
    <Paper
      variant="outlined"
      sx={{ borderRadius: '10px', height: '100%', display: 'flex', flexDirection: 'column' }}
    >
      <Stack
        direction="row"
        spacing={1.5}
        sx={{
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          px: 2.25,
          pt: 1.75,
          pb: caption ? 1.5 : 1.25,
        }}
      >
        <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', minWidth: 0 }}>
          {icon && (
            <Box
              sx={{
                flexShrink: 0,
                width: 30,
                height: 30,
                borderRadius: '8px',
                display: 'grid',
                placeItems: 'center',
                bgcolor: chip.tint,
                color: chip.solid,
                '& svg': { fontSize: 17 },
              }}
            >
              {icon}
            </Box>
          )}
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: 14.5, fontWeight: 650, letterSpacing: '-0.01em' }}>
              {title}
            </Typography>
            {caption && (
              <Typography
                sx={{ fontSize: 12.5, color: 'text.secondary', mt: 0.25, lineHeight: 1.45 }}
              >
                {caption}
              </Typography>
            )}
          </Box>
        </Stack>
        {action && <Box sx={{ flexShrink: 0 }}>{action}</Box>}
      </Stack>

      <Box sx={{ flexGrow: 1, minWidth: 0, px: padded ? 2.25 : 0, pb: footer ? 0 : 2 }}>
        {children}
      </Box>

      {footer && (
        <Box sx={{ px: 2.25, py: 1.25, borderTop: '1px solid', borderColor: 'grey.100', mt: 1.5 }}>
          {footer}
        </Box>
      )}
    </Paper>
  )
}
