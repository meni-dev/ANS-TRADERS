import { formatCurrency, formatDate } from '@/lib/format'
import { Box, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import type { SalesTrendPointDto } from '../types'

type SalesTrendChartProps = {
  points: SalesTrendPointDto[]
  loading?: boolean
}

// Plot geometry, in viewBox units. The SVG scales to its container; only the height is fixed, so
// the bars stay legible on a laptop without the panel dominating the page.
const WIDTH = 900
const HEIGHT = 220
const PAD = { top: 16, right: 8, bottom: 28, left: 64 }
const PLOT_W = WIDTH - PAD.left - PAD.right
const PLOT_H = HEIGHT - PAD.top - PAD.bottom

/** Cap from the mark spec — never let a bar fill its whole band; the leftover is air. */
const MAX_BAR_W = 24

/** The surface gap that separates touching bars. White does the separating, not a stroke. */
const BAR_GAP = 2

const SERIES = '#4880FF'

/**
 * Rounds an axis maximum up to a clean 1 / 2 / 5 × 10^n, so ticks land on numbers a person would
 * say out loud rather than on whatever the data happened to peak at.
 */
function niceCeiling(value: number): number {
  if (value <= 0) return 1000

  const magnitude = 10 ** Math.floor(Math.log10(value))
  const normalised = value / magnitude

  const step = normalised <= 1 ? 1 : normalised <= 2 ? 2 : normalised <= 5 ? 5 : 10
  return step * magnitude
}

/** Compact rupee for axis ticks — `₹1.2L` beats `₹1,20,000` on a 64px gutter. */
function compactRupee(value: number): string {
  if (value === 0) return '0'
  if (value >= 10_000_000) return `₹${(value / 10_000_000).toFixed(1)}Cr`
  if (value >= 100_000) return `₹${(value / 100_000).toFixed(1)}L`
  if (value >= 1_000) return `₹${Math.round(value / 1_000)}k`
  return `₹${value}`
}

export function SalesTrendChart({ points, loading }: SalesTrendChartProps) {
  const [hovered, setHovered] = useState<number | null>(null)

  const total = points.reduce((sum, p) => sum + p.salesTotal, 0)
  const busiest = points.reduce<SalesTrendPointDto | null>(
    (best, p) => (best === null || p.salesTotal > best.salesTotal ? p : best),
    null,
  )

  const max = niceCeiling(Math.max(...points.map((p) => p.salesTotal), 0))
  const ticks = [0, max / 2, max]

  const band = points.length > 0 ? PLOT_W / points.length : PLOT_W
  const barW = Math.max(2, Math.min(MAX_BAR_W, band - BAR_GAP))

  const x = (index: number) => PAD.left + index * band + (band - barW) / 2
  const y = (value: number) => PAD.top + PLOT_H - (value / max) * PLOT_H

  const active = hovered === null ? null : points[hovered]

  return (
    <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px' }}>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'baseline' }, mb: 2 }}
      >
        <Box>
          {/* Single series, so the title names what is plotted and no legend box is needed. */}
          <Typography variant="h3">Sales — last 30 days</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
            Billed value per day, cancelled invoices excluded.
          </Typography>
        </Box>
        <Typography sx={{ fontSize: 13, color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
          {formatCurrency(total)} total
        </Typography>
      </Stack>

      <Box sx={{ position: 'relative' }}>
        <Box
          component="svg"
          viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
          role="img"
          aria-label={`Daily sales for the last 30 days, totalling ${formatCurrency(total)}`}
          sx={{ width: '100%', height: 220, display: 'block', opacity: loading ? 0.35 : 1 }}
          onMouseLeave={() => setHovered(null)}
        >
          {/* Gridlines: hairline, solid, one step off the surface — present but recessive. */}
          {ticks.map((tick) => (
            <g key={tick}>
              <line
                x1={PAD.left}
                x2={WIDTH - PAD.right}
                y1={y(tick)}
                y2={y(tick)}
                stroke="#E7EAF0"
                strokeWidth={1}
              />
              <text
                x={PAD.left - 10}
                y={y(tick) + 4}
                textAnchor="end"
                fontSize={11}
                fill="#7A8394"
              >
                {compactRupee(tick)}
              </text>
            </g>
          ))}

          {points.map((point, index) => {
            const barH = point.salesTotal > 0 ? Math.max(2, PLOT_H - (y(point.salesTotal) - PAD.top)) : 0
            const isHovered = hovered === index

            return (
              <g key={point.date}>
                {barH > 0 && (
                  <rect
                    x={x(index)}
                    y={y(point.salesTotal)}
                    width={barW}
                    height={barH}
                    // Rounded data-end, square at the baseline: the radius is drawn on all corners
                    // and the overhanging rect below re-squares the two that sit on the axis.
                    rx={4}
                    fill={SERIES}
                    opacity={hovered === null || isHovered ? 1 : 0.45}
                  />
                )}
                {barH > 4 && (
                  <rect
                    x={x(index)}
                    y={PAD.top + PLOT_H - 4}
                    width={barW}
                    height={4}
                    fill={SERIES}
                    opacity={hovered === null || isHovered ? 1 : 0.45}
                  />
                )}

                {/* Full-height hit target — the bar itself is far too thin to hover reliably. */}
                <rect
                  x={PAD.left + index * band}
                  y={PAD.top}
                  width={band}
                  height={PLOT_H}
                  fill="transparent"
                  onMouseEnter={() => setHovered(index)}
                />
              </g>
            )
          })}

          {/* Baseline sits above the bars so their square feet read as one line. */}
          <line
            x1={PAD.left}
            x2={WIDTH - PAD.right}
            y1={PAD.top + PLOT_H}
            y2={PAD.top + PLOT_H}
            stroke="#D6DBE4"
            strokeWidth={1}
          />

          {/* Only the ends of the window are labelled — thirty dates would be unreadable. */}
          {points.length > 0 && (
            <>
              <text x={PAD.left} y={HEIGHT - 8} fontSize={11} fill="#7A8394">
                {formatDate(points[0].date)}
              </text>
              <text x={WIDTH - PAD.right} y={HEIGHT - 8} fontSize={11} fill="#7A8394" textAnchor="end">
                {formatDate(points[points.length - 1].date)}
              </text>
            </>
          )}
        </Box>

        {/* Tooltip rides above the plot rather than following the cursor, so it never covers the
            bar being read. */}
        {active && (
          <Box
            sx={{
              position: 'absolute',
              top: 0,
              left: `${(((hovered ?? 0) + 0.5) / points.length) * 100}%`,
              transform: 'translateX(-50%)',
              bgcolor: 'grey.800',
              color: '#fff',
              borderRadius: '5px',
              px: 1.25,
              py: 0.75,
              pointerEvents: 'none',
              whiteSpace: 'nowrap',
            }}
          >
            <Typography sx={{ fontSize: 11, opacity: 0.75 }}>{formatDate(active.date)}</Typography>
            <Typography sx={{ fontSize: 13, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
              {formatCurrency(active.salesTotal)}
            </Typography>
            <Typography sx={{ fontSize: 11, opacity: 0.75 }}>
              {active.invoiceCount} {active.invoiceCount === 1 ? 'invoice' : 'invoices'}
            </Typography>
          </Box>
        )}
      </Box>

      {busiest && busiest.salesTotal > 0 && (
        <Typography sx={{ fontSize: 12, color: 'text.disabled', mt: 1 }}>
          Busiest day {formatDate(busiest.date)} · {formatCurrency(busiest.salesTotal)}
        </Typography>
      )}
    </Paper>
  )
}
