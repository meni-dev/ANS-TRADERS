import { formatCurrency, formatQuantity } from '@/lib/format'
import { Box, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'

/** The line shape purchases and invoices share once saved. */
export type DocumentLine = {
  id: string
  partNumber: string
  itemName: string
  hsn: string
  uqc: string
  quantity: number
  rate: number
  discountPercent: number
  discountAmount: number
  taxableAmount: number
  gstRate: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  lineTotal: number
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
 * Read-only line table for a saved document, laid out the way a GST invoice is expected to read:
 * description, HSN, quantity, rate, taxable value, then the tax split. Only the applicable tax
 * columns are rendered — IGST and CGST+SGST never appear on the same bill.
 */
export function DocumentLinesTable({
  lines,
  isInterState,
}: {
  lines: DocumentLine[]
  isInterState: boolean
}) {
  return (
    <Box sx={{ overflowX: 'auto' }}>
      <Table size="small" sx={{ minWidth: 760 }}>
        <TableHead>
          <TableRow>
            <TableCell sx={{ ...headerCell, width: 36 }}>#</TableCell>
            <TableCell sx={headerCell}>Item</TableCell>
            <TableCell sx={{ ...headerCell, width: 96 }}>HSN</TableCell>
            <TableCell align="right" sx={{ ...headerCell, width: 90 }}>Qty</TableCell>
            <TableCell align="right" sx={{ ...headerCell, width: 110 }}>Rate</TableCell>
            <TableCell align="right" sx={{ ...headerCell, width: 120 }}>Taxable</TableCell>
            {isInterState ? (
              <TableCell align="right" sx={{ ...headerCell, width: 120 }}>IGST</TableCell>
            ) : (
              <>
                <TableCell align="right" sx={{ ...headerCell, width: 110 }}>CGST</TableCell>
                <TableCell align="right" sx={{ ...headerCell, width: 110 }}>SGST</TableCell>
              </>
            )}
            <TableCell align="right" sx={{ ...headerCell, width: 130 }}>Total</TableCell>
          </TableRow>
        </TableHead>

        <TableBody>
          {lines.map((line, index) => (
            <TableRow key={line.id} sx={{ '& td': { borderColor: 'grey.100', py: 1.25 } }}>
              <TableCell sx={{ color: 'text.disabled', fontSize: 12.5 }}>{index + 1}</TableCell>

              <TableCell>
                <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }}>
                  {line.itemName}
                </Typography>
                <Typography sx={{ fontSize: 11.5, color: 'text.disabled', lineHeight: 1.4 }}>
                  {line.partNumber}
                  {line.discountPercent > 0 && ` · ${line.discountPercent}% off`}
                </Typography>
              </TableCell>

              <TableCell sx={{ fontSize: 12.5, color: 'text.secondary' }}>{line.hsn || '—'}</TableCell>

              <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                {formatQuantity(line.quantity)}
                <Typography component="span" sx={{ fontSize: 11, color: 'text.disabled', ml: 0.5 }}>
                  {line.uqc}
                </Typography>
              </TableCell>

              <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(line.rate)}
              </TableCell>

              <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(line.taxableAmount)}
              </TableCell>

              {isInterState ? (
                <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(line.igstAmount)}
                  <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>{line.gstRate}%</Typography>
                </TableCell>
              ) : (
                <>
                  <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.cgstAmount)}
                    <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>{line.gstRate / 2}%</Typography>
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.sgstAmount)}
                    <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>{line.gstRate / 2}%</Typography>
                  </TableCell>
                </>
              )}

              <TableCell
                align="right"
                sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}
              >
                {formatCurrency(line.lineTotal)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  )
}
