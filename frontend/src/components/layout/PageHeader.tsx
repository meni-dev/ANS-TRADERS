import { accent, type AccentTone } from '@/theme/theme'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Box, IconButton, Stack, Tooltip, Typography } from '@mui/material'
import type { ReactNode } from 'react'

type PageHeaderProps = {
  title: string
  /** One line saying what this screen is for. Not a tagline — what it does. */
  caption?: ReactNode
  /** Chip to the left of the title, in the same hue the sidebar and dashboard use for this area. */
  icon?: ReactNode
  iconTone?: AccentTone
  /** Beside the title: a count, a status. */
  badge?: ReactNode
  /** Buttons, on the right. */
  actions?: ReactNode
  /** Shows a back arrow. For a form or a document, never for a list. */
  onBack?: () => void
  /**
   * How the right-hand side lines up. Buttons centre against the title; a date filter carries its
   * own label above the box and wants its bottom edge level with the caption instead.
   */
  align?: 'center' | 'flex-end'
  /** Drops the bottom margin, for a page that lays its own sections out with a Stack. */
  flush?: boolean
  /** Passed through — a document screen sets `no-print` so its buttons stay off the paper. */
  className?: string
}

/**
 * The band every screen opens with.
 * <p>
 * Two dozen pages had each hand-rolled the same header out of a Stack, an h1 and a caption, and
 * they had already drifted on spacing and on whether the count chip sat beside the title or under
 * it. One component means a new screen looks like the app on the day it is written, without anybody
 * having to remember what the app looks like.
 * </p>
 * <p>
 * The hue is the same one the area wears on the dashboard and in its icon chip, so a screen tells
 * you which part of the shop you are in before you have read its title.
 * </p>
 */
export function PageHeader({
  title,
  caption,
  icon,
  iconTone = 'neutral',
  badge,
  actions,
  onBack,
  align = 'center',
  flush = false,
  className,
}: PageHeaderProps) {
  const chip = accent[iconTone]

  return (
    <Stack
      className={className}
      direction={{ xs: 'column', sm: 'row' }}
      spacing={2}
      sx={{ justifyContent: 'space-between', alignItems: { sm: align }, mb: flush ? 0 : 2.5 }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', minWidth: 0 }}>
        {onBack && (
          <Tooltip title="Back">
            <IconButton size="small" onClick={onBack} sx={{ ml: -0.5 }} aria-label="Back">
              <ArrowBackIcon sx={{ fontSize: 19 }} />
            </IconButton>
          </Tooltip>
        )}

        {icon && (
          <Box
            sx={{
              flexShrink: 0,
              width: 38,
              height: 38,
              borderRadius: '10px',
              display: 'grid',
              placeItems: 'center',
              bgcolor: chip.tint,
              color: chip.solid,
              '& svg': { fontSize: 21 },
            }}
          >
            {icon}
          </Box>
        )}

        <Box sx={{ minWidth: 0 }}>
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <Typography variant="h1" sx={{ minWidth: 0 }}>
              {title}
            </Typography>
            {badge}
          </Stack>
          {caption && (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
              {caption}
            </Typography>
          )}
        </Box>
      </Stack>

      {actions && (
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexShrink: 0 }}>
          {actions}
        </Stack>
      )}
    </Stack>
  )
}
