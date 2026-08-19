import { ShopLogo } from '@/components/brand/ShopLogo'
import { amountInWords } from '@/lib/documents/amountInWords'
import { formatCurrency, formatDate, formatQuantity } from '@/lib/format'
import { Box, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { PrintStyles } from './PrintStyles'
import { customerLines, shopAddress } from './shared'
import type { InvoiceTemplateProps } from './types'

const th = {
  fontSize: 10,
  fontWeight: 600,
  letterSpacing: '0.1em',
  textTransform: 'uppercase' as const,
  color: 'text.disabled',
  borderBottom: '1px solid',
  borderColor: 'grey.200',
  py: 1,
  whiteSpace: 'nowrap' as const,
}

const td = { fontSize: 13, py: 1.75, borderBottom: 'none' }

/**
 * Everything a tax invoice must carry and nothing else. One hairline under the header, one above
 * the total, and white space doing the rest of the work. For a shop whose bill is often the only
 * piece of paper a customer keeps.
 */
export function MinimalTemplate({ invoice, shop }: InvoiceTemplateProps) {
  const address = shopAddress(shop)
  const cancelled = invoice.status === 'Cancelled'

  return (
    <>
      <PrintStyles page="A4" margin="18mm" />

      <Paper variant="outlined" className="print-sheet" sx={{ borderRadius: '8px', p: { xs: 3, md: 6 } }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between', alignItems: 'flex-start', pb: 2.5 }}
        >
          <Box>
            <ShopLogo height={26} sx={{ mb: 0.75 }} />
            <Typography sx={{ fontSize: 15, fontWeight: 700, letterSpacing: '0.02em' }}>
              {shop.name}
            </Typography>
            {address && (
              <Typography sx={{ fontSize: 12, color: 'text.secondary', maxWidth: 300, mt: 0.25 }}>
                {address}
              </Typography>
            )}
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
              {shop.gstin && `GSTIN ${shop.gstin}`}
              {shop.phone && ` · ${shop.phone}`}
            </Typography>
          </Box>

          <Box sx={{ textAlign: { sm: 'right' } }}>
            <Typography sx={{ fontSize: 10, letterSpacing: '0.22em', color: 'text.disabled' }}>
              TAX INVOICE
            </Typography>
            <Typography sx={{ fontSize: 14, fontWeight: 600, mt: 0.5 }}>
              {invoice.invoiceNumber}
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
              {formatDate(invoice.invoiceDate)}
            </Typography>
            {cancelled && (
              <Typography sx={{ fontSize: 11, fontWeight: 700, color: 'error.dark', mt: 0.5 }}>
                CANCELLED
              </Typography>
            )}
          </Box>
        </Stack>

        <Box sx={{ borderTop: '1px solid', borderColor: 'grey.300' }} />

        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between', py: 2.5 }}
        >
          <Box>
            <Typography sx={{ fontSize: 10, letterSpacing: '0.14em', color: 'text.disabled' }}>
              BILLED TO
            </Typography>
            <Typography sx={{ fontSize: 14, fontWeight: 600, mt: 0.5 }}>
              {invoice.customerName}
            </Typography>
            {customerLines(invoice).map((line) => (
              <Typography key={line} sx={{ fontSize: 12, color: 'text.secondary' }}>
                {line}
              </Typography>
            ))}
          </Box>

          <Box sx={{ textAlign: { sm: 'right' } }}>
            <Typography sx={{ fontSize: 10, letterSpacing: '0.14em', color: 'text.disabled' }}>
              SUPPLY
            </Typography>
            <Typography sx={{ fontSize: 12.5, mt: 0.5 }}>
              {invoice.isInterState ? 'Inter-state · IGST' : 'Intra-state · CGST + SGST'}
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>{invoice.paymentMode}</Typography>
          </Box>
        </Stack>

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" sx={{ minWidth: 560 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={th}>Item</TableCell>
                <TableCell sx={{ ...th, width: 90 }}>HSN</TableCell>
                <TableCell align="right" sx={{ ...th, width: 100 }}>Qty</TableCell>
                <TableCell align="right" sx={{ ...th, width: 110 }}>Rate</TableCell>
                <TableCell align="right" sx={{ ...th, width: 130 }}>Amount</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {invoice.items.map((line) => (
                <TableRow key={line.id}>
                  <TableCell sx={td}>
                    <Typography sx={{ fontSize: 13.5, lineHeight: 1.45 }}>{line.itemName}</Typography>
                    <Typography sx={{ fontSize: 11.5, color: 'text.disabled', lineHeight: 1.45 }}>
                      {line.partNumber} · GST {line.gstRate}%
                      {line.discountPercent > 0 && ` · ${line.discountPercent}% off`}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ ...td, color: 'text.secondary', fontSize: 12 }}>
                    {line.hsn || '—'}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatQuantity(line.quantity)} {line.uqc}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.rate)}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.lineTotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>

        <Stack sx={{ alignItems: 'flex-end', mt: 1 }} className="print-keep">
          <Box sx={{ width: { xs: '100%', sm: 300 }, borderTop: '1px solid', borderColor: 'grey.300', pt: 2 }}>
            <Stack spacing={0.75}>
              <Row label="Taxable value" value={invoice.taxableAmount} />
              {invoice.discountAmount > 0 && <Row label="Discount" value={-invoice.discountAmount} />}
              {invoice.isInterState ? (
                <Row label="IGST" value={invoice.igstAmount} />
              ) : (
                <>
                  <Row label="CGST" value={invoice.cgstAmount} />
                  <Row label="SGST" value={invoice.sgstAmount} />
                </>
              )}
              {invoice.roundOff !== 0 && <Row label="Round off" value={invoice.roundOff} />}

              <Stack
                direction="row"
                sx={{ justifyContent: 'space-between', alignItems: 'baseline', pt: 1.25 }}
              >
                <Typography sx={{ fontSize: 12.5, letterSpacing: '0.04em' }}>Total</Typography>
                <Typography
                  sx={{ fontSize: 22, fontWeight: 600, letterSpacing: '-0.01em', fontVariantNumeric: 'tabular-nums' }}
                >
                  {formatCurrency(invoice.grandTotal)}
                </Typography>
              </Stack>

              {invoice.balanceDue > 0 && (
                <Row label="Balance due" value={invoice.balanceDue} />
              )}
            </Stack>
          </Box>
        </Stack>

        <Box sx={{ mt: 4 }}>
          <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
            {amountInWords(invoice.grandTotal)}
          </Typography>

          {invoice.notes && (
            <Typography sx={{ fontSize: 12, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 2 }}>
              {invoice.notes}
            </Typography>
          )}

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            sx={{ justifyContent: 'space-between', alignItems: 'flex-end', mt: 5 }}
          >
            {shop.invoiceFooter && (
              <Typography sx={{ fontSize: 11, color: 'text.disabled', maxWidth: 340 }}>
                {shop.invoiceFooter}
              </Typography>
            )}
            <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>For {shop.name}</Typography>
          </Stack>
        </Box>
      </Paper>
    </>
  )
}

function Row({ label, value }: { label: string; value: number }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>{label}</Typography>
      <Typography sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
        {value < 0 ? `− ${formatCurrency(Math.abs(value))}` : formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
