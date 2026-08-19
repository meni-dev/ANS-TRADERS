import { Box, Paper, Skeleton, Stack, Typography } from '@mui/material'
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
 * and one on the stock screen — and had already drifted apart in type size and label colour. The
 * figure carries the tone; there is no coloured icon block, because on a row of four tiles the
 * icons compete with the numbers they are meant to introduce.
 */
export function StatTile({
  label,
  value,
  caption,
  tone = 'default',
  loading,
  onClick,
}: StatTileProps) {
  return (
    <Paper
      variant="outlined"
      component={onClick ? 'button' : 'div'}
      onClick={onClick}
      type={onClick ? 'button' : undefined}
      sx={{
        p: 2,
        borderRadius: '8px',
        height: '100%',
        width: '100%',
        display: 'block',
        textAlign: 'left',
        font: 'inherit',
        color: 'inherit',
        ...(onClick && {
          cursor: 'pointer',
          transition: 'border-color 120ms ease, background-color 120ms ease',
          '&:hover': { borderColor: 'grey.400', bgcolor: 'grey.50' },
        }),
      }}
    >
      <Typography
        sx={{
          fontSize: 11,
          fontWeight: 700,
          letterSpacing: '0.05em',
          textTransform: 'uppercase',
          color: 'text.disabled',
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

      {caption && !loading && (
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', mt: 0.25, minHeight: 18 }}>
          <Box sx={{ fontSize: 12, color: 'text.secondary', lineHeight: 1.4 }}>{caption}</Box>
        </Stack>
      )}
    </Paper>
  )
}
