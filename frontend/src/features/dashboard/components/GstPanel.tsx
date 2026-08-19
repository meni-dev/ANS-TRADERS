import { formatCurrency, formatQuantity } from '@/lib/format'
import {
  Box,
  Divider,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import type { GstSummaryDto } from '../types'

type GstPanelProps = {
  gst: GstSummaryDto
  monthLabel: string
}

function TaxRow({
  label,
  value,
  muted,
}: {
  label: string
  value: number
  muted?: boolean
}) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography sx={{ fontSize: 12.5, color: muted ? 'text.disabled' : 'text.secondary' }}>
        {label}
      </Typography>
      <Typography
        sx={{
          fontSize: 13,
          fontWeight: muted ? 400 : 500,
          fontVariantNumeric: 'tabular-nums',
          color: muted ? 'text.disabled' : 'text.primary',
        }}
      >
        {formatCurrency(value)}
      </Typography>
    </Stack>
  )
}

const headerCell = {
  fontSize: 11,
  fontWeight: 700,
  letterSpacing: '0.03em',
  textTransform: 'uppercase' as const,
  color: 'text.secondary',
  bgcolor: 'grey.50',
  py: 1,
  whiteSpace: 'nowrap' as const,
}

/**
 * The month's GST position, in the two shapes filing actually needs: the output/input/net summary
 * that decides what gets remitted, and the per-HSN breakup GSTR-1 Table 12 asks for.
 */
export function GstPanel({ gst, monthLabel }: GstPanelProps) {
  // Negative net means input credit exceeded output tax — a month of heavy stocking. Shown as
  // credit carried forward rather than as a negative payable, which would read as a mistake.
  const isCredit = gst.netPayable < 0

  return (
    <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px', height: '100%' }}>
      <Typography variant="h3">GST — {monthLabel}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25, mb: 2 }}>
        Cancelled documents excluded, as they are from the return.
      </Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} sx={{ mb: 2.5 }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography
            sx={{
              fontSize: 11,
              fontWeight: 700,
              letterSpacing: '0.05em',
              textTransform: 'uppercase',
              color: 'text.disabled',
              mb: 1,
            }}
          >
            Output — on sales
          </Typography>
          <Stack spacing={0.75}>
            <TaxRow label="Taxable value" value={gst.outputTaxable} muted />
            <TaxRow label="CGST" value={gst.outputCgst} />
            <TaxRow label="SGST" value={gst.outputSgst} />
            <TaxRow label="IGST" value={gst.outputIgst} />
            <Divider sx={{ my: 0.5 }} />
            <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2 }}>
              <Typography sx={{ fontSize: 12.5, fontWeight: 700 }}>Total</Typography>
              <Typography sx={{ fontSize: 14, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(gst.outputTotal)}
              </Typography>
            </Stack>
          </Stack>
        </Box>

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography
            sx={{
              fontSize: 11,
              fontWeight: 700,
              letterSpacing: '0.05em',
              textTransform: 'uppercase',
              color: 'text.disabled',
              mb: 1,
            }}
          >
            Input — on purchases
          </Typography>
          <Stack spacing={0.75}>
            <TaxRow label="Taxable value" value={gst.inputTaxable} muted />
            <TaxRow label="CGST" value={gst.inputCgst} />
            <TaxRow label="SGST" value={gst.inputSgst} />
            <TaxRow label="IGST" value={gst.inputIgst} />
            <Divider sx={{ my: 0.5 }} />
            <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 2 }}>
              <Typography sx={{ fontSize: 12.5, fontWeight: 700 }}>Total</Typography>
              <Typography sx={{ fontSize: 14, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(gst.inputTotal)}
              </Typography>
            </Stack>
          </Stack>
        </Box>
      </Stack>

      <Box
        sx={{
          bgcolor: isCredit ? 'success.light' : 'primary.light',
          borderRadius: '6px',
          px: 2,
          py: 1.5,
          mb: 2.5,
        }}
      >
        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
          <Typography
            sx={{ fontSize: 12.5, fontWeight: 700, color: isCredit ? 'success.dark' : 'primary.dark' }}
          >
            {isCredit ? 'Input credit carried forward' : 'Net GST payable'}
          </Typography>
          <Typography
            sx={{
              fontSize: 18,
              fontWeight: 700,
              fontVariantNumeric: 'tabular-nums',
              color: isCredit ? 'success.dark' : 'primary.dark',
            }}
          >
            {formatCurrency(Math.abs(gst.netPayable))}
          </Typography>
        </Stack>
      </Box>

      <Typography
        sx={{
          fontSize: 11,
          fontWeight: 700,
          letterSpacing: '0.05em',
          textTransform: 'uppercase',
          color: 'text.disabled',
          mb: 1,
        }}
      >
        HSN summary — GSTR-1 Table 12
      </Typography>

      {gst.hsn.length === 0 ? (
        <Typography sx={{ fontSize: 13, color: 'text.disabled', py: 2 }}>
          No sales this month.
        </Typography>
      ) : (
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" sx={{ minWidth: 520 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={headerCell}>HSN</TableCell>
                <TableCell sx={headerCell}>UQC</TableCell>
                <TableCell align="right" sx={headerCell}>Qty</TableCell>
                <TableCell align="right" sx={headerCell}>Taxable</TableCell>
                <TableCell align="right" sx={headerCell}>Tax</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {gst.hsn.map((row) => (
                <TableRow key={`${row.hsn}-${row.uqc}`} sx={{ '& td': { borderColor: 'grey.100', py: 1 } }}>
                  <TableCell
                    sx={{
                      fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
                      fontSize: 12.5,
                      fontWeight: 600,
                      color: row.hsn ? 'text.primary' : 'text.disabled',
                    }}
                  >
                    {/* An item saved without an HSN still sells; the return will need one. */}
                    {row.hsn || 'Not set'}
                  </TableCell>
                  <TableCell sx={{ fontSize: 12.5, color: 'text.secondary' }}>{row.uqc}</TableCell>
                  <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                    {formatQuantity(row.quantity)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(row.taxableValue)}
                  </TableCell>
                  <TableCell
                    align="right"
                    sx={{ fontSize: 13, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}
                  >
                    {formatCurrency(row.totalTax)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
      )}
    </Paper>
  )
}
