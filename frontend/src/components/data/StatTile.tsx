import { accent, type AccentTone } from '@/theme/theme'
import { alpha, Box, Paper, Skeleton, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'

type StatTileProps = {
  label: string
  /** The headline figure, already formatted — currency, count, whatever the caller means. */
  value: string
  /** One line under the figure: a count, a comparison, whatever qualifies the number. */
  caption?: ReactNode
  tone?: 'default' | 'primary' | 'success' | 'warning' | 'error'
  loading?: boolean
  /** Renders the tile as a button. Every figure worth showing has a screen that explains it. */
  onClick?: () => void
  /** Optional chip in the corner. Names the tile at a glance on a screen full of numbers. */
  icon?: ReactNode
  /** Which hue the chip wears. Identifies the tile; it does not grade the figure. */
  iconTone?: AccentTone
  /**
   * Washes the whole card in the icon's hue instead of leaving it white.
   * <p>
   * For a row of tiles that opens a screen — a dashboard, a page's summary strip — where telling
   * four figures apart at a glance is worth more than the quiet. Not for tiles buried in a page
   * that is mostly table: there the colour would be the loudest thing on screen.
   * </p>
   */
  tinted?: boolean
}

const toneColour = {
  default: 'text.primary',
  primary: 'primary.dark',
  success: 'success.dark',
  warning: 'warning.dark',
  error: 'error.dark',
} as const

/**
 * The one stat tile. Two near-identical versions had grown up independently — one in the dashboard
 * and one on the stock screen — and had already drifted apart in type size and label colour.
 * <p>
 * The icon is optional and sits in the corner rather than beside the figure. On a row of four
 * tiles an icon in the reading path competes with the number it is meant to introduce; parked in
 * the corner it does the one job worth doing, which is telling you which tile you are looking at
 * before you have read a word.
 * </p>
 */
export function StatTile({
  label,
  value,
  caption,
  tone = 'default',
  loading,
  onClick,
  icon,
  iconTone = 'neutral',
  tinted = false,
}: StatTileProps) {
  const chip = accent[iconTone]
  const washed = tinted && iconTone !== 'neutral'

  return (
    <Paper
      variant="outlined"
      component={onClick ? 'button' : 'div'}
      onClick={onClick}
      type={onClick ? 'button' : undefined}
      sx={{
        p: 2,
        borderRadius: '10px',
        height: '100%',
        width: '100%',
        display: 'block',
        textAlign: 'left',
        font: 'inherit',
        color: 'inherit',
        ...(washed && {
          bgcolor: chip.tint,
          // Its own hue at rest, so the card reads as one piece rather than as a tinted panel
          // inside a grey frame.
          borderColor: alpha(chip.solid, 0.18),
        }),
        ...(onClick && {
          cursor: 'pointer',
          transition: 'border-color 120ms ease, box-shadow 120ms ease, transform 120ms ease',
          '&:hover': {
            borderColor: washed ? alpha(chip.solid, 0.4) : 'grey.300',
            boxShadow: '0 2px 4px -1px rgba(24, 29, 36, 0.04), 0 8px 16px -4px rgba(24, 29, 36, 0.08)',
            transform: 'translateY(-1px)',
          },
        }),
      }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
        <Box sx={{ minWidth: 0, flexGrow: 1 }}>
          <Typography
            sx={{
              fontSize: 11,
              fontWeight: 700,
              letterSpacing: '0.05em',
              textTransform: 'uppercase',
              color: washed ? alpha(chip.solid, 0.75) : 'text.disabled',
            }}
          >
            {label}
          </Typography>

          {loading ? (
            <Skeleton width={96} height={34} />
          ) : (
            <Typography
              sx={{
                fontSize: 22,
                fontWeight: 700,
                letterSpacing: '-0.02em',
                fontVariantNumeric: 'tabular-nums',
                color: toneColour[tone],
                mt: 0.25,
                lineHeight: 1.25,
              }}
            >
              {value}
            </Typography>
          )}
        </Box>

        {icon && (
          <Box
            sx={{
              flexShrink: 0,
              width: 34,
              height: 34,
              borderRadius: '9px',
              display: 'grid',
              placeItems: 'center',
              bgcolor: washed ? '#fff' : chip.tint,
              color: chip.solid,
              '& svg': { fontSize: 18 },
            }}
          >
            {icon}
          </Box>
        )}
      </Stack>

      {caption && !loading && (
        <Box
          sx={{
            fontSize: 12,
            color: washed ? alpha(chip.solid, 0.9) : 'text.secondary',
            lineHeight: 1.4,
            mt: 0.5,
            minHeight: 18,
          }}
        >
          {caption}
        </Box>
      )}
    </Paper>
  )
}
