import { layout } from '@/theme/theme'
import { Box, Grid } from '@mui/material'
import type { ReactNode } from 'react'

type DocumentFormLayoutProps = {
  /** Header fields + items table — everything that scrolls normally. */
  children: ReactNode
  /** Totals panel — sits on top of the sidebar. */
  totals: ReactNode
  /** Payment panel — sits below totals. */
  payment: ReactNode
}

/**
 * The two-column shell shared by the invoice and purchase create screens: a center "document"
 * column that scrolls, and a right sidebar (totals over payment) pinned in view while it does —
 * the total is never scrolled out of sight while adding lines. Collapses to a single stacked
 * column under `md`, where a sticky sidebar has nowhere useful to pin itself against.
 */
export function DocumentFormLayout({ children, totals, payment }: DocumentFormLayoutProps) {
  return (
    // alignItems stays at its stretch default: the right Grid item must be as tall as the
    // center column for the sticky Box inside it to have any room to stick within.
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, md: 7.5, lg: 8 }} sx={{ minWidth: 0 }}>
        {children}
      </Grid>

      <Grid size={{ xs: 12, md: 4.5, lg: 4 }}>
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            gap: 2.5,
            position: { xs: 'static', md: 'sticky' },
            top: { md: `${layout.appBarHeight + 20}px` },
          }}
        >
          {totals}
          {payment}
        </Box>
      </Grid>
    </Grid>
  )
}
