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
import { customerLines, shopAddress, taxByRate } from './shared'
import type { InvoiceTemplateProps } from './types'

const th = {
  fontSize: 10,
  fontWeight: 700,
  letterSpacing: '0.04em',
  textTransform: 'uppercase' as const,
  color: 'text.secondary',
  bgcolor: 'grey.100',
  py: 0.75,
  borderRight: '1px solid',
  borderColor: 'grey.300',
  whiteSpace: 'nowrap' as const,
}

const td = {
  fontSize: 12,
  py: 0.75,
  borderRight: '1px solid',
  borderColor: 'grey.200',
}

function Caption({ children }: { children: React.ReactNode }) {
  return (
    <Typography
      sx={{
        fontSize: 9.5,
        fontWeight: 700,
        letterSpacing: '0.06em',
        textTransform: 'uppercase',
        color: 'text.disabled',
      }}
    >
      {children}
    </Typography>
  )
}

/**
 * The accountant's copy. Everything Classic carries, plus the rate-wise tax summary a return is
 * checked against, the bank details a customer pays into, and the terms. Denser than Classic
 * because it has more to say and still has to fit one sheet.
 */
export function DetailedTemplate({ invoice, shop }: InvoiceTemplateProps) {
  const address = shopAddress(shop)
  const rates = taxByRate(invoice.items)
  const cancelled = invoice.status === 'Cancelled'

  return (
    <>
      <PrintStyles page="A4" margin="10mm" />

      <Paper
        variant="outlined"
        className="print-sheet"
        sx={{ borderRadius: '8px', p: { xs: 2, md: 3 }, borderColor: 'grey.400' }}
      >
        {/* Centred masthead: the shape a printed invoice book uses, and it leaves the corners free
            for the document identity block below. */}
        <Box sx={{ textAlign: 'center', pb: 1.5 }}>
          <ShopLogo height={46} sx={{ mx: 'auto', mb: 0.75 }} />
          <Typography sx={{ fontSize: 20, fontWeight: 700, letterSpacing: '-0.01em' }}>
            {shop.name}
          </Typography>
          {shop.legalName && shop.legalName !== shop.name && (
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>{shop.legalName}</Typography>
          )}
          {address && (
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>{address}</Typography>
          )}
          <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
            {shop.gstin && `GSTIN: ${shop.gstin}`}
            {shop.phone && ` · Ph: ${shop.phone}`}
            {shop.email && ` · ${shop.email}`}
          </Typography>
        </Box>

        <Box
          sx={{
            borderTop: '2px solid',
            borderBottom: '1px solid',
            borderColor: 'grey.400',
            py: 0.75,
            textAlign: 'center',
          }}
        >
          <Typography sx={{ fontSize: 12, fontWeight: 700, letterSpacing: '0.18em' }}>
            TAX INVOICE
          </Typography>
        </Box>

        <Grid container sx={{ border: '1px solid', borderColor: 'grey.300', borderTop: 'none' }}>
          <Grid size={{ xs: 12, sm: 6 }} sx={{ p: 1.5, borderRight: { sm: '1px solid' }, borderColor: 'grey.300' }}>
            <Caption>Bill To</Caption>
            <Typography sx={{ fontSize: 14, fontWeight: 700, mt: 0.25 }}>
              {invoice.customerName}
            </Typography>
            {customerLines(invoice).map((line) => (
              <Typography key={line} sx={{ fontSize: 12, color: 'text.secondary' }}>
                {line}
              </Typography>
            ))}
          </Grid>

          <Grid size={{ xs: 12, sm: 6 }} sx={{ p: 1.5 }}>
            <Stack spacing={0.5}>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Caption>Invoice No.</Caption>
                <Typography
                  sx={{
                    fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
                    fontSize: 12.5,
                    fontWeight: 700,
                  }}
                >
                  {invoice.invoiceNumber}
                </Typography>
              </Stack>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Caption>Date</Caption>
                <Typography sx={{ fontSize: 12.5 }}>{formatDate(invoice.invoiceDate)}</Typography>
              </Stack>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Caption>Place of Supply</Caption>
                <Typography sx={{ fontSize: 12.5 }}>
                  {invoice.customerStateCode ?? shop.stateCode} ·{' '}
                  {invoice.isInterState ? 'Inter-state' : 'Intra-state'}
                </Typography>
              </Stack>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Caption>Payment Mode</Caption>
                <Typography sx={{ fontSize: 12.5 }}>{invoice.paymentMode}</Typography>
              </Stack>
              {cancelled && (
                <Chip
                  label="CANCELLED"
                  size="small"
                  sx={{ alignSelf: 'flex-start', bgcolor: 'error.light', color: 'error.dark' }}
                />
              )}
            </Stack>
          </Grid>
        </Grid>

        <Box sx={{ overflowX: 'auto', border: '1px solid', borderColor: 'grey.300', borderTop: 'none' }}>
          <Table size="small" sx={{ minWidth: 760 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ ...th, width: 30 }}>#</TableCell>
                <TableCell sx={th}>Description</TableCell>
                <TableCell sx={{ ...th, width: 84 }}>HSN</TableCell>
                <TableCell align="right" sx={{ ...th, width: 74 }}>Qty</TableCell>
                <TableCell align="right" sx={{ ...th, width: 92 }}>Rate</TableCell>
                <TableCell align="right" sx={{ ...th, width: 78 }}>Disc</TableCell>
                <TableCell align="right" sx={{ ...th, width: 100 }}>Taxable</TableCell>
                <TableCell align="right" sx={{ ...th, width: 60 }}>GST</TableCell>
                <TableCell align="right" sx={{ ...th, width: 110, borderRight: 'none' }}>Amount</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {invoice.items.map((line, index) => (
                <TableRow key={line.id}>
                  <TableCell sx={{ ...td, color: 'text.disabled' }}>{index + 1}</TableCell>
                  <TableCell sx={td}>
                    <Typography sx={{ fontSize: 12.5, fontWeight: 600, lineHeight: 1.35 }}>
                      {line.itemName}
                    </Typography>
                    <Typography sx={{ fontSize: 11, color: 'text.disabled', lineHeight: 1.35 }}>
                      {line.partNumber}
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
                    {line.discountPercent > 0 ? `${line.discountPercent}%` : '—'}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.taxableAmount)}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {line.gstRate}%
                  </TableCell>
                  <TableCell
                    align="right"
                    sx={{ ...td, borderRight: 'none', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}
                  >
                    {formatCurrency(line.lineTotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>

        <Grid container sx={{ border: '1px solid', borderColor: 'grey.300', borderTop: 'none' }} className="print-keep">
          {/* The rate-wise summary is the reason this template exists: a bill mixing 18% and 28%
              parts can be tied to the return without adding lines up by hand. */}
          <Grid size={{ xs: 12, md: 7 }} sx={{ borderRight: { md: '1px solid' }, borderColor: 'grey.300' }}>
            <Box sx={{ p: 1.5, pb: 0.75 }}>
              <Caption>Tax Summary by Rate</Caption>
            </Box>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ ...th, borderRight: 'none' }}>Rate</TableCell>
                  <TableCell align="right" sx={{ ...th, borderRight: 'none' }}>Taxable</TableCell>
                  {invoice.isInterState ? (
                    <TableCell align="right" sx={{ ...th, borderRight: 'none' }}>IGST</TableCell>
                  ) : (
                    <>
                      <TableCell align="right" sx={{ ...th, borderRight: 'none' }}>CGST</TableCell>
                      <TableCell align="right" sx={{ ...th, borderRight: 'none' }}>SGST</TableCell>
                    </>
                  )}
                  <TableCell align="right" sx={{ ...th, borderRight: 'none' }}>Total Tax</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rates.map((row) => (
                  <TableRow key={row.gstRate}>
                    <TableCell sx={{ ...td, borderRight: 'none' }}>{row.gstRate}%</TableCell>
                    <TableCell align="right" sx={{ ...td, borderRight: 'none', fontVariantNumeric: 'tabular-nums' }}>
                      {formatCurrency(row.taxableValue)}
                    </TableCell>
                    {invoice.isInterState ? (
                      <TableCell align="right" sx={{ ...td, borderRight: 'none', fontVariantNumeric: 'tabular-nums' }}>
                        {formatCurrency(row.igstAmount)}
                      </TableCell>
                    ) : (
                      <>
                        <TableCell align="right" sx={{ ...td, borderRight: 'none', fontVariantNumeric: 'tabular-nums' }}>
                          {formatCurrency(row.cgstAmount)}
                        </TableCell>
                        <TableCell align="right" sx={{ ...td, borderRight: 'none', fontVariantNumeric: 'tabular-nums' }}>
                          {formatCurrency(row.sgstAmount)}
                        </TableCell>
                      </>
                    )}
                    <TableCell
                      align="right"
                      sx={{ ...td, borderRight: 'none', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}
                    >
                      {formatCurrency(row.totalTax)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>

            <Box sx={{ p: 1.5, borderTop: '1px solid', borderColor: 'grey.200' }}>
              <Caption>Amount in Words</Caption>
              <Typography sx={{ fontSize: 12.5, fontWeight: 600, mt: 0.25 }}>
                {amountInWords(invoice.grandTotal)}
              </Typography>
            </Box>
          </Grid>

          <Grid size={{ xs: 12, md: 5 }} sx={{ p: 1.5 }}>
            <Stack spacing={0.5}>
              <Line label="Sub total" value={invoice.subTotal} />
              {invoice.discountAmount > 0 && <Line label="Discount" value={-invoice.discountAmount} />}
              <Line label="Taxable value" value={invoice.taxableAmount} />
              {invoice.isInterState ? (
                <Line label="IGST" value={invoice.igstAmount} />
              ) : (
                <>
                  <Line label="CGST" value={invoice.cgstAmount} />
                  <Line label="SGST" value={invoice.sgstAmount} />
                </>
              )}
              {invoice.roundOff !== 0 && <Line label="Round off" value={invoice.roundOff} />}

              <Box sx={{ borderTop: '2px solid', borderColor: 'grey.400', mt: 0.75, pt: 0.75 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
                  <Typography sx={{ fontSize: 13, fontWeight: 700 }}>Grand Total</Typography>
                  <Typography sx={{ fontSize: 17, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(invoice.grandTotal)}
                  </Typography>
                </Stack>
              </Box>

              <Line label="Paid" value={invoice.amountPaid} />
              <Line label="Balance due" value={Math.max(invoice.balanceDue, 0)} bold />
            </Stack>
          </Grid>
        </Grid>

        <Grid container spacing={0} sx={{ mt: 2 }} className="print-keep">
          <Grid size={{ xs: 12, md: 8 }}>
            {shop.bankDetails && (
              <Box sx={{ mb: 1.5 }}>
                <Caption>Bank Details</Caption>
                <Typography sx={{ fontSize: 12, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 0.25 }}>
                  {shop.bankDetails}
                </Typography>
              </Box>
            )}
            {shop.invoiceTerms && (
              <Box sx={{ mb: 1.5 }}>
                <Caption>Terms &amp; Conditions</Caption>
                <Typography sx={{ fontSize: 11.5, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 0.25 }}>
                  {shop.invoiceTerms}
                </Typography>
              </Box>
            )}
            {invoice.notes && (
              <Box sx={{ mb: 1.5 }}>
                <Caption>Notes</Caption>
                <Typography sx={{ fontSize: 12, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 0.25 }}>
                  {invoice.notes}
                </Typography>
              </Box>
            )}
            {shop.invoiceFooter && (
              <Typography sx={{ fontSize: 11, color: 'text.disabled', maxWidth: 460 }}>
                {shop.invoiceFooter}
              </Typography>
            )}
          </Grid>

          <Grid size={{ xs: 12, md: 4 }}>
            <Box sx={{ textAlign: 'right', pt: 4 }}>
              <Box sx={{ borderTop: '1px solid', borderColor: 'grey.400', ml: 'auto', width: 180, pt: 0.5 }}>
                <Typography sx={{ fontSize: 11.5, color: 'text.secondary' }}>
                  For {shop.name}
                </Typography>
                <Typography sx={{ fontSize: 10.5, color: 'text.disabled', mt: 2 }}>
                  Authorised Signatory
                </Typography>
              </Box>
            </Box>
          </Grid>
        </Grid>
      </Paper>
    </>
  )
}

function Line({ label, value, bold }: { label: string; value: number; bold?: boolean }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography sx={{ fontSize: 12, color: 'text.secondary', fontWeight: bold ? 700 : 400 }}>
        {label}
      </Typography>
      <Typography
        sx={{ fontSize: 12.5, fontWeight: bold ? 700 : 500, fontVariantNumeric: 'tabular-nums' }}
      >
        {value < 0 ? `− ${formatCurrency(Math.abs(value))}` : formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
