import { ShopLogo } from '@/components/brand/ShopLogo'
import { amountInWords } from '@/lib/documents/amountInWords'
import { formatCurrency, formatDate, formatQuantity } from '@/lib/format'
import {
  Box,
  Chip,
  Grid,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { PrintStyles } from './PrintStyles'
import { customerLines, shopAddress } from './shared'
import type { InvoiceTemplateProps } from './types'

const th = {
  fontSize: 10,
  fontWeight: 700,
  letterSpacing: '0.08em',
  textTransform: 'uppercase' as const,
  color: 'text.disabled',
  borderBottom: '1px solid',
  borderColor: 'grey.300',
  py: 1,
  whiteSpace: 'nowrap' as const,
}

const td = { fontSize: 12.5, py: 1.5, borderBottom: '1px solid', borderColor: 'grey.100' }

/**
 * For a shop that wants the bill to look designed. A colour band carries the identity, the number
 * is set large enough to read across a counter, and the line table drops its rules so the type does
 * the structuring. Same fields as Classic — this is styling, not a different document.
 */
export function ModernTemplate({ invoice, shop }: InvoiceTemplateProps) {
  const address = shopAddress(shop)
  const cancelled = invoice.status === 'Cancelled'

  return (
    <>
      <PrintStyles page="A4" margin="0" />
      {/* Browsers drop background fills when printing unless told otherwise, and this template is
          mostly a coloured band — without this it prints as an empty rectangle. */}
      <style>{`
        @media print {
          .modern-band { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
        }
      `}</style>

      <Paper variant="outlined" className="print-sheet" sx={{ borderRadius: '8px', overflow: 'hidden' }}>
        <Box
          className="modern-band"
          sx={{
            bgcolor: 'primary.main',
            color: 'primary.contrastText',
            px: { xs: 3, md: 5 },
            py: 3,
          }}
        >
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={2}
            sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' } }}
          >
            <Box>
              <Box
                sx={{
                  display: 'inline-flex',
                  bgcolor: 'common.white',
                  borderRadius: '8px',
                  px: 1.25,
                  py: 0.75,
                  mb: 1.25,
                }}
              >
                <ShopLogo height={34} />
              </Box>
              <Typography sx={{ fontSize: 24, fontWeight: 700, letterSpacing: '-0.02em', lineHeight: 1.2 }}>
                {shop.name}
              </Typography>
              {address && (
                <Typography sx={{ fontSize: 12.5, opacity: 0.85, mt: 0.5, maxWidth: 380 }}>
                  {address}
                </Typography>
              )}
              <Typography sx={{ fontSize: 12.5, opacity: 0.85 }}>
                {shop.gstin && `GSTIN ${shop.gstin}`}
                {shop.phone && ` · ${shop.phone}`}
              </Typography>
            </Box>

            <Box sx={{ textAlign: { sm: 'right' } }}>
              <Typography sx={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.18em', opacity: 0.8 }}>
                TAX INVOICE
              </Typography>
              <Typography sx={{ fontSize: 22, fontWeight: 700, letterSpacing: '-0.01em', lineHeight: 1.25 }}>
                {invoice.invoiceNumber}
              </Typography>
              <Typography sx={{ fontSize: 12.5, opacity: 0.85 }}>
                {formatDate(invoice.invoiceDate)}
              </Typography>
            </Box>
          </Stack>
        </Box>

        <Box sx={{ px: { xs: 3, md: 5 }, py: 3 }}>
          {cancelled && (
            <Chip
              label="CANCELLED"
              size="small"
              sx={{ mb: 2, bgcolor: 'error.light', color: 'error.dark' }}
            />
          )}

          <Grid container spacing={3} sx={{ mb: 3 }}>
            <Grid size={{ xs: 12, sm: 6 }}>
              <Typography
                sx={{
                  fontSize: 10.5,
                  fontWeight: 700,
                  letterSpacing: '0.08em',
                  textTransform: 'uppercase',
                  color: 'text.disabled',
                }}
              >
                Billed to
              </Typography>
              <Typography sx={{ fontSize: 16, fontWeight: 700, mt: 0.5 }}>
                {invoice.customerName}
              </Typography>
              {customerLines(invoice).map((line) => (
                <Typography key={line} sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                  {line}
                </Typography>
              ))}
            </Grid>

            <Grid size={{ xs: 12, sm: 6 }}>
              <Stack spacing={1} sx={{ alignItems: { sm: 'flex-end' } }}>
                <Box sx={{ textAlign: { sm: 'right' } }}>
                  <Typography
                    sx={{
                      fontSize: 10.5,
                      fontWeight: 700,
                      letterSpacing: '0.08em',
                      textTransform: 'uppercase',
                      color: 'text.disabled',
                    }}
                  >
                    Place of supply
                  </Typography>
                  <Typography sx={{ fontSize: 13 }}>
                    {invoice.isInterState ? 'Inter-state · IGST' : 'Intra-state · CGST + SGST'}
                  </Typography>
                </Box>
                <Box sx={{ textAlign: { sm: 'right' } }}>
                  <Typography
                    sx={{
                      fontSize: 10.5,
                      fontWeight: 700,
                      letterSpacing: '0.08em',
                      textTransform: 'uppercase',
                      color: 'text.disabled',
                    }}
                  >
                    Payment
                  </Typography>
                  <Typography sx={{ fontSize: 13 }}>{invoice.paymentMode}</Typography>
                </Box>
              </Stack>
            </Grid>
          </Grid>

          {/* No rules on the table: the type hierarchy and the row spacing do the structuring. */}
          <Box sx={{ overflowX: 'auto' }}>
            <Table size="small" sx={{ minWidth: 660 }}>
              <TableHead>
                <TableRow>
                  <TableCell sx={th}>Item</TableCell>
                  <TableCell sx={{ ...th, width: 90 }}>HSN</TableCell>
                  <TableCell align="right" sx={{ ...th, width: 90 }}>Qty</TableCell>
                  <TableCell align="right" sx={{ ...th, width: 110 }}>Rate</TableCell>
                  <TableCell align="right" sx={{ ...th, width: 90 }}>GST</TableCell>
                  <TableCell align="right" sx={{ ...th, width: 130 }}>Amount</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {invoice.items.map((line) => (
                  <TableRow key={line.id}>
                    <TableCell sx={td}>
                      <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }}>
                        {line.itemName}
                      </Typography>
                      <Typography sx={{ fontSize: 11.5, color: 'text.disabled', lineHeight: 1.4 }}>
                        {line.partNumber}
                        {line.discountPercent > 0 && ` · ${line.discountPercent}% off`}
                      </Typography>
                    </TableCell>
                    <TableCell sx={{ ...td, color: 'text.secondary' }}>{line.hsn || '—'}</TableCell>
                    <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                      {formatQuantity(line.quantity)} {line.uqc}
                    </TableCell>
                    <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                      {formatCurrency(line.rate)}
                    </TableCell>
                    <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                      {line.gstRate}%
                      <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>
                        {formatCurrency(line.cgstAmount + line.sgstAmount + line.igstAmount)}
                      </Typography>
                    </TableCell>
                    <TableCell
                      align="right"
                      sx={{ ...td, fontSize: 14, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}
                    >
                      {formatCurrency(line.lineTotal)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Box>

          <Grid container spacing={3} sx={{ mt: 1 }} className="print-keep">
            <Grid size={{ xs: 12, md: 6 }}>
              <Typography
                sx={{
                  fontSize: 10.5,
                  fontWeight: 700,
                  letterSpacing: '0.08em',
                  textTransform: 'uppercase',
                  color: 'text.disabled',
                }}
              >
                Amount in words
              </Typography>
              <Typography sx={{ fontSize: 13, fontWeight: 600, mt: 0.5 }}>
                {amountInWords(invoice.grandTotal)}
              </Typography>

              {invoice.notes && (
                <Typography sx={{ fontSize: 12.5, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 2 }}>
                  {invoice.notes}
                </Typography>
              )}
              {shop.invoiceFooter && (
                <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 2, maxWidth: 380 }}>
                  {shop.invoiceFooter}
                </Typography>
              )}
            </Grid>

            <Grid size={{ xs: 12, md: 6 }}>
              <Box
                className="modern-band"
                sx={{ bgcolor: 'grey.50', borderRadius: '8px', p: 2.5 }}
              >
                <Stack spacing={0.75}>
                  <Row label="Sub total" value={invoice.subTotal} />
                  {invoice.discountAmount > 0 && (
                    <Row label="Discount" value={-invoice.discountAmount} />
                  )}
                  <Row label="Taxable value" value={invoice.taxableAmount} />
                  {invoice.isInterState ? (
                    <Row label="IGST" value={invoice.igstAmount} />
                  ) : (
                    <>
                      <Row label="CGST" value={invoice.cgstAmount} />
                      <Row label="SGST" value={invoice.sgstAmount} />
                    </>
                  )}
                  {invoice.roundOff !== 0 && <Row label="Round off" value={invoice.roundOff} />}

                  <Box sx={{ borderTop: '1px solid', borderColor: 'grey.300', mt: 1, pt: 1.25 }}>
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
                      <Typography sx={{ fontSize: 13, fontWeight: 700 }}>Grand total</Typography>
                      <Typography
                        sx={{
                          fontSize: 24,
                          fontWeight: 700,
                          letterSpacing: '-0.02em',
                          color: 'primary.dark',
                          fontVariantNumeric: 'tabular-nums',
                        }}
                      >
                        {formatCurrency(invoice.grandTotal)}
                      </Typography>
                    </Stack>
                  </Box>

                  <Row label="Paid" value={invoice.amountPaid} />
                  {invoice.balanceDue > 0 && (
                    <Row label="Balance due" value={invoice.balanceDue} tone="warning.dark" />
                  )}
                </Stack>
              </Box>

              <Box sx={{ mt: 4, textAlign: 'right' }}>
                <Box sx={{ borderTop: '1px solid', borderColor: 'grey.300', ml: 'auto', width: 180, pt: 0.75 }}>
                  <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>For {shop.name}</Typography>
                </Box>
              </Box>
            </Grid>
          </Grid>
        </Box>
      </Paper>
    </>
  )
}

function Row({ label, value, tone }: { label: string; value: number; tone?: string }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography sx={{ fontSize: 12.5, color: tone ?? 'text.secondary', fontWeight: tone ? 600 : 400 }}>
        {label}
      </Typography>
      <Typography
        sx={{
          fontSize: 13,
          fontWeight: tone ? 700 : 500,
          color: tone ?? 'text.primary',
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        {value < 0 ? `− ${formatCurrency(Math.abs(value))}` : formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
