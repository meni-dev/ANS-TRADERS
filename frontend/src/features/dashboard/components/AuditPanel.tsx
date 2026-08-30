import { formatCurrency, formatQuantity } from '@/lib/format'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import { PanelCard } from '@/components/data/PanelCard'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import { Box, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import type { AuditChecksDto } from '../types'

type CheckState = 'clean' | 'attention' | 'serious' | 'info'

type CheckRowProps = {
  state: CheckState
  label: string
  detail: ReactNode
}

// Each state ships with its own icon and a worded detail line — the colour is a reinforcement,
// never the only thing carrying the meaning.
const stateStyle: Record<CheckState, { colour: string; icon: ReactNode }> = {
  clean: { colour: 'success.dark', icon: <CheckCircleOutlinedIcon sx={{ fontSize: 18 }} /> },
  attention: { colour: 'warning.dark', icon: <WarningAmberOutlinedIcon sx={{ fontSize: 18 }} /> },
  serious: { colour: 'error.dark', icon: <ErrorOutlineOutlinedIcon sx={{ fontSize: 18 }} /> },
  info: { colour: 'text.disabled', icon: <InfoOutlinedIcon sx={{ fontSize: 18 }} /> },
}

function CheckRow({ state, label, detail }: CheckRowProps) {
  const { colour, icon } = stateStyle[state]

  return (
    <Stack
      direction="row"
      spacing={1.25}
      sx={{
        alignItems: 'flex-start',
        py: 1.25,
        borderBottom: '1px solid',
        borderColor: 'grey.100',
        '&:last-of-type': { borderBottom: 'none' },
      }}
    >
      <Box sx={{ color: colour, display: 'flex', mt: 0.125, flexShrink: 0 }}>{icon}</Box>
      <Box sx={{ minWidth: 0, flexGrow: 1 }}>
        <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }}>{label}</Typography>
        <Typography sx={{ fontSize: 12.5, color: 'text.secondary', lineHeight: 1.5 }}>
          {detail}
        </Typography>
      </Box>
    </Stack>
  )
}

/**
 * The checks an auditor runs first, answered before they ask. Everything here is a question about
 * the documents themselves rather than about trade, which is why it sits apart from the figures.
 */
export function AuditPanel({ audit, monthLabel }: { audit: AuditChecksDto; monthLabel: string }) {
  const numberingClean = audit.missingInvoiceCount === 0 && audit.missingPurchaseCount === 0
  const recon = audit.reconciliation

  const missingSample = [...audit.missingInvoiceNumbers, ...audit.missingPurchaseNumbers]
    .slice(0, 4)
    .join(', ')

  const totalSales = audit.b2BSales + audit.b2CSales
  const b2bShare = totalSales > 0 ? Math.round((audit.b2BSales / totalSales) * 100) : 0

  return (
    <PanelCard
      title="Audit checks"
      icon={<FactCheckOutlinedIcon />}
      iconTone="teal"
      caption={`Numbering across ${audit.financialYear}; everything else for ${monthLabel}.`}
    >

      {/* Ahead even of the numbering gaps, because this is the only check whose failure means a
          figure already on screen is wrong. Everything else reports on the documents; this reports
          on whether the app's own totals still agree with the entries they were built from. */}
      <CheckRow
        state={recon.isClean ? 'clean' : 'serious'}
        label="Balances reconcile"
        detail={
          recon.isClean
            ? 'Every balance still matches the entries behind it.'
            : [
                recon.partyBalanceMismatches > 0
                  ? `${recon.partyBalanceMismatches} party balance(s) disagree with their ledger`
                  : null,
                recon.documentBalanceMismatches > 0
                  ? `${recon.documentBalanceMismatches} document(s) with a wrong balance due`
                  : null,
                recon.allocationMismatches > 0
                  ? `${recon.allocationMismatches} document(s) whose paid amount does not match its receipts`
                  : null,
                recon.stockMismatches > 0
                  ? `${recon.stockMismatches} item(s) whose stock disagrees with its movements`
                  : null,
              ]
                .filter(Boolean)
                .join(' · ')
        }
      />

      {/* A gap in the series is the first thing an auditor looks for, so it is next.
          Cancelled documents keep their number, so a gap means a row that was never written. */}
      <CheckRow
        state={numberingClean ? 'clean' : 'serious'}
        label="Document numbering"
        detail={
          numberingClean
            ? 'Unbroken — no invoice or purchase number is missing.'
            : `${audit.missingInvoiceCount + audit.missingPurchaseCount} missing${
                missingSample ? ` — ${missingSample}${
                  audit.missingInvoiceCount + audit.missingPurchaseCount > 4 ? ' and more' : ''
                }` : ''
              }. A gap has to be explained.`
        }
      />

      <CheckRow
        state={audit.cancelledInvoiceCount + audit.cancelledPurchaseCount === 0 ? 'clean' : 'attention'}
        label="Cancelled documents"
        detail={
          audit.cancelledInvoiceCount + audit.cancelledPurchaseCount === 0
            ? 'None cancelled this month.'
            : `${audit.cancelledInvoiceCount} invoice${audit.cancelledInvoiceCount === 1 ? '' : 's'}, ` +
              `${audit.cancelledPurchaseCount} purchase${audit.cancelledPurchaseCount === 1 ? '' : 's'}. ` +
              'Kept on record and excluded from the return.'
        }
      />

      {/* Stock that moved with no document behind it — every one is a human decision. */}
      <CheckRow
        state={audit.stockAdjustmentCount === 0 ? 'clean' : 'attention'}
        label="Manual stock adjustments"
        detail={
          audit.stockAdjustmentCount === 0
            ? 'None — all stock moved through a purchase or an invoice.'
            : `${audit.stockAdjustmentCount} adjustment${audit.stockAdjustmentCount === 1 ? '' : 's'}, ` +
              `net ${audit.stockAdjustmentNetQuantity > 0 ? '+' : ''}` +
              `${formatQuantity(audit.stockAdjustmentNetQuantity)}. Each needs a reason on record.`
        }
      />

      <CheckRow
        state="info"
        label="B2B vs B2C split"
        detail={
          totalSales === 0
            ? 'No sales this month.'
            : `${audit.b2BInvoiceCount} registered (${formatCurrency(audit.b2BSales)}, ${b2bShare}%) · ` +
              `${audit.b2CInvoiceCount} unregistered (${formatCurrency(audit.b2CSales)}). ` +
              'They file as separate tables in GSTR-1.'
        }
      />

      {/* A line billed with no HSN cannot go into GSTR-1 Table 12 at all, so this is a filing
          blocker rather than something to keep an eye on. */}
      <CheckRow
        state={audit.itemsSoldWithoutHsnCount === 0 ? 'clean' : 'serious'}
        label="Items sold without an HSN"
        detail={
          audit.itemsSoldWithoutHsnCount === 0
            ? 'Every item sold this month has an HSN code.'
            : `${audit.itemsSoldWithoutHsnCount} item${
                audit.itemsSoldWithoutHsnCount === 1 ? '' : 's'
              } worth ${formatCurrency(audit.salesWithoutHsn)}. ` +
              'They cannot be reported in Table 12 until the master is fixed.'
        }
      />

      <CheckRow
        state={audit.highValueWithoutGstinCount === 0 ? 'clean' : 'attention'}
        label="Large sales without a GSTIN"
        detail={
          audit.highValueWithoutGstinCount === 0
            ? `None above ${formatCurrency(audit.highValueWithoutGstinThreshold)}.`
            : `${audit.highValueWithoutGstinCount} bill${
                audit.highValueWithoutGstinCount === 1 ? '' : 's'
              } over ${formatCurrency(audit.highValueWithoutGstinThreshold)} with no GSTIN captured. ` +
              'The buyer is likely a business that could have claimed credit.'
        }
      />
    </PanelCard>
  )
}
