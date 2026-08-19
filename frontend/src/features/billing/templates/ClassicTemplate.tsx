import { ShopLogo } from '@/components/brand/ShopLogo'
import { amountInWords } from '@/lib/documents/amountInWords'
import { formatCurrency, formatDate, formatQuantity } from '@/lib/format'
import {
  Box,
  Chip,
  Divider,
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

function Field({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography
        sx={{
          fontSize: 10.5,
          fontWeight: 700,
          letterSpacing: '0.05em',
          textTransform: 'uppercase',
          color: 'text.disabled',
        }}
      >
        {label}
      </Typography>
      <Typography sx={{ fontSize: 13, mt: 0.25 }}>{value}</Typography>
    </Box>
  )
}

/**
 * The default. A clean A4 sheet with the tax split carried as table columns — the shape most
 * software bills arrive in, and the one that reads fastest across a counter.
 */
export function ClassicTemplate({ invoice, shop }: InvoiceTemplateProps) {
  const address = shopAddress(shop)
  const cancelled = invoice.status === 'Cancelled'

  return (
    <>
      <PrintStyles page="A4" margin="14mm" />

      <Paper variant="outlined" className="print-sheet" sx={{ borderRadius: '8px', p: { xs: 2.5, md: 4 } }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}
        >
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
            <ShopLogo height={42} sx={{ mt: 0.25 }} />
            <Box>
              <Typography sx={{ fontSize: 18, fontWeight: 700, letterSpacing: '-0.015em' }}>
                {shop.name}
              </Typography>
              {address && (
                <Typography sx={{ fontSize: 12.5, color: 'text.secondary', maxWidth: 340 }}>
                  {address}
                </Typography>
              )}
              <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                {shop.gstin && `GSTIN ${shop.gstin}`}
                {shop.phone && ` · ${shop.phone}`}
              </Typography>
            </Box>
          </Stack>

          <Box sx={{ textAlign: { sm: 'right' } }}>
            <Typography
              sx={{
                fontSize: 11,
                fontWeight: 700,
                letterSpacing: '0.1em',
                textTransform: 'uppercase',
                color: 'text.disabled',
              }}
            >
              Tax Invoice
            </Typography>
            <Typography
              sx={{
                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
                fontSize: 15,
                fontWeight: 700,
              }}
            >
              {invoice.invoiceNumber}
            </Typography>
            <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
              {formatDate(invoice.invoiceDate)}
            </Typography>
            {cancelled && (
              <Chip
                label="CANCELLED"
                size="small"
                sx={{ mt: 0.75, bgcolor: 'error.light', color: 'error.dark' }}
              />
            )}
          </Box>
        </Stack>

        <Divider sx={{ my: 3 }} />

        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <Typography
              sx={{
                fontSize: 10.5,
                fontWeight: 700,
                letterSpacing: '0.05em',
                textTransform: 'uppercase',
                color: 'text.disabled',
                mb: 0.5,
              }}
            >
              Bill To
            </Typography>
            <Typography sx={{ fontSize: 14.5, fontWeight: 600 }}>{invoice.customerName}</Typography>
            {customerLines(invoice).map((line) => (
              <Typography key={line} sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                {line}
              </Typography>
            ))}
          </Grid>

          <Grid size={{ xs: 6, sm: 3 }}>
            <Field label="Place of Supply" value={invoice.isInterState ? 'Inter-state' : 'Intra-state'} />
          </Grid>
          <Grid size={{ xs: 6, sm: 3 }}>
            <Field label="Payment Mode" value={invoice.paymentMode} />
          </Grid>
        </Grid>

        <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: '8px', overflow: 'hidden' }}>
          <Box sx={{ overflowX: 'auto' }}>
            <Table size="small" sx={{ minWidth: 700 }}>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ ...headerCell, width: 30 }}>#</TableCell>
                  <TableCell sx={headerCell}>Item</TableCell>
                  <TableCell sx={{ ...headerCell, width: 78 }}>HSN</TableCell>
                  <TableCell align="right" sx={{ ...headerCell, width: 74 }}>Qty</TableCell>
                  <TableCell align="right" sx={{ ...headerCell, width: 88 }}>Rate</TableCell>
                  <TableCell align="right" sx={{ ...headerCell, width: 96 }}>Taxable</TableCell>
                  {invoice.isInterState ? (
                    <TableCell align="right" sx={{ ...headerCell, width: 96 }}>IGST</TableCell>
                  ) : (
                    <>
                      <TableCell align="right" sx={{ ...headerCell, width: 84 }}>CGST</TableCell>
                      <TableCell align="right" sx={{ ...headerCell, width: 84 }}>SGST</TableCell>
                    </>
                  )}
                  <TableCell align="right" sx={{ ...headerCell, width: 104 }}>Total</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {invoice.items.map((line, index) => (
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
                    <TableCell sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                      {line.hsn || '—'}
                    </TableCell>
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
                    {invoice.isInterState ? (
                      <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                        {formatCurrency(line.igstAmount)}
                        <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>{line.gstRate}%</Typography>
                      </TableCell>
                    ) : (
                      <>
                        <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                          {formatCurrency(line.cgstAmount)}
                          <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>
                            {line.gstRate / 2}%
                          </Typography>
                        </TableCell>
                        <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                          {formatCurrency(line.sgstAmount)}
                          <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>
                            {line.gstRate / 2}%
                          </Typography>
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
        </Box>

        <Grid container spacing={3} sx={{ mt: 0.5 }} className="print-keep">
          <Grid size={{ xs: 12, md: 7 }}>
            <Typography
              sx={{
                fontSize: 10.5,
                fontWeight: 700,
                letterSpacing: '0.05em',
                textTransform: 'uppercase',
                color: 'text.disabled',
              }}
            >
              Amount in Words
            </Typography>
            <Typography sx={{ fontSize: 13, fontWeight: 600, mt: 0.25 }}>
              {amountInWords(invoice.grandTotal)}
            </Typography>

            {invoice.notes && (
              <Box sx={{ mt: 2.5 }}>
                <Typography
                  sx={{
                    fontSize: 10.5,
                    fontWeight: 700,
                    letterSpacing: '0.05em',
                    textTransform: 'uppercase',
                    color: 'text.disabled',
                  }}
                >
                  Notes
                </Typography>
                <Typography sx={{ fontSize: 12.5, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 0.25 }}>
                  {invoice.notes}
                </Typography>
              </Box>
            )}

            {shop.invoiceFooter && (
              <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 2.5, maxWidth: 420 }}>
                {shop.invoiceFooter}
              </Typography>
            )}
          </Grid>

          <Grid size={{ xs: 12, md: 5 }}>
            <Stack spacing={1}>
              <TotalRow label="Sub total" value={invoice.subTotal} />
              {invoice.discountAmount > 0 && (
                <TotalRow label="Discount" value={-invoice.discountAmount} />
              )}
              <TotalRow label="Taxable value" value={invoice.taxableAmount} />
              {invoice.isInterState ? (
                <TotalRow label="IGST" value={invoice.igstAmount} />
              ) : (
                <>
                  <TotalRow label="CGST" value={invoice.cgstAmount} />
                  <TotalRow label="SGST" value={invoice.sgstAmount} />
                </>
              )}
              {invoice.roundOff !== 0 && <TotalRow label="Round off" value={invoice.roundOff} muted />}

              <Divider sx={{ my: 0.5 }} />

              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
                <Typography sx={{ fontSize: 13.5, fontWeight: 700 }}>Grand total</Typography>
                <Typography
                  sx={{ fontSize: 20, fontWeight: 700, letterSpacing: '-0.02em', fontVariantNumeric: 'tabular-nums' }}
                >
                  {formatCurrency(invoice.grandTotal)}
                </Typography>
              </Stack>

              <TotalRow label="Paid" value={invoice.amountPaid} />
              <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
                <Typography
                  sx={{
                    fontSize: 13,
                    fontWeight: 600,
                    color: invoice.balanceDue > 0 ? 'warning.dark' : 'success.dark',
                  }}
                >
                  {invoice.balanceDue > 0 ? 'Balance due' : 'Settled'}
                </Typography>
                <Typography
                  sx={{
                    fontSize: 14,
                    fontWeight: 700,
                    fontVariantNumeric: 'tabular-nums',
                    color: invoice.balanceDue > 0 ? 'warning.dark' : 'success.dark',
                  }}
                >
                  {formatCurrency(Math.max(invoice.balanceDue, 0))}
                </Typography>
              </Stack>
            </Stack>

            <Box sx={{ mt: 5, textAlign: 'right' }}>
              <Divider sx={{ mb: 0.75, ml: 'auto', width: 180 }} />
              <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>For {shop.name}</Typography>
            </Box>
          </Grid>
        </Grid>
      </Paper>
    </>
  )
}

function TotalRow({ label, value, muted }: { label: string; value: number; muted?: boolean }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography sx={{ fontSize: 13, color: muted ? 'text.disabled' : 'text.secondary' }}>
        {label}
      </Typography>
      <Typography
        sx={{
          fontSize: 13.5,
          fontWeight: 500,
          fontVariantNumeric: 'tabular-nums',
          color: muted ? 'text.disabled' : 'text.primary',
        }}
      >
        {value < 0 ? `− ${formatCurrency(Math.abs(value))}` : formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
