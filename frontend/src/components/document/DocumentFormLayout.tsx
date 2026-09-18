import { layout } from '@/theme/theme'
import { Box } from '@mui/material'
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
 *
 * The sidebar is a fixed width rather than a fraction of the row: Totals/Payment only need enough
 * room to read two fields side by side, and letting the center column take whatever is left over
 * (instead of a fixed share of it) is what actually buys the items table room at a narrower
 * viewport — a proportional split still starves it exactly where the room is scarce.
 */
const SIDEBAR_WIDTH = 360

export function DocumentFormLayout({ children, totals, payment }: DocumentFormLayoutProps) {
  return (
    // alignItems stays at its stretch default: the sidebar must be as tall as the center column
    // for the sticky Box inside it to have any room to stick within.
    <Box sx={{ display: 'flex', flexDirection: { xs: 'column', md: 'row' }, gap: 2.5 }}>
      <Box sx={{ flex: 1, minWidth: 0 }}>{children}</Box>

      <Box sx={{ width: { xs: '100%', md: SIDEBAR_WIDTH }, flexShrink: 0 }}>
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
      </Box>
    </Box>
  )
}
