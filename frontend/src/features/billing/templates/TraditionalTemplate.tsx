import { ShopLogo } from '@/components/brand/ShopLogo'
import { amountInWords } from '@/lib/documents/amountInWords'
import { formatCurrency, formatDate, formatQuantity } from '@/lib/format'
import { Box, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { PrintStyles } from './PrintStyles'
import { shopAddress } from './shared'
import type { InvoiceTemplateProps } from './types'

// Every cell is ruled on all sides, the way an invoice book from a local press is. The rules are
// what people expect to see on a bill here, and they survive a bad photocopy better than
// whitespace-only structure does.
const box = { border: '1px solid', borderColor: 'grey.500' }

const th = {
  ...box,
  fontSize: 10.5,
  fontWeight: 700,
  textTransform: 'uppercase' as const,
  letterSpacing: '0.03em',
  py: 0.75,
  whiteSpace: 'nowrap' as const,
}

const td = { ...box, fontSize: 12, py: 0.75 }

function Cell({ label, value }: { label: string; value: string }) {
  return (
    <Stack direction="row" spacing={1} sx={{ py: 0.25 }}>
      <Typography sx={{ fontSize: 11.5, color: 'text.secondary', minWidth: 96 }}>{label}</Typography>
      <Typography sx={{ fontSize: 11.5, fontWeight: 600 }}>: {value}</Typography>
    </Stack>
  )
}

/**
 * The format most shops here already hand over. A double-ruled masthead, boxed party panels, and a
 * fully ruled line table — recognisable to a customer who has been buying parts for thirty years,
 * and legible after the third photocopy.
 */
export function TraditionalTemplate({ invoice, shop }: InvoiceTemplateProps) {
  const address = shopAddress(shop)
  const cancelled = invoice.status === 'Cancelled'

  return (
    <>
      <PrintStyles page="A4" margin="10mm" />

      <Paper
        variant="outlined"
        className="print-sheet"
        sx={{ borderRadius: 0, p: { xs: 1.5, md: 2 }, border: '2px solid', borderColor: 'grey.600' }}
      >
        <Box sx={{ textAlign: 'center', borderBottom: '3px double', borderColor: 'grey.600', pb: 1, mb: 1 }}>
          <ShopLogo height={38} sx={{ mx: 'auto', mb: 0.5 }} />
          <Typography sx={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.22em' }}>
            TAX INVOICE
          </Typography>
          <Typography sx={{ fontSize: 22, fontWeight: 700, letterSpacing: '0.01em', mt: 0.25 }}>
            {shop.name}
          </Typography>
          {address && <Typography sx={{ fontSize: 11.5 }}>{address}</Typography>}
          <Typography sx={{ fontSize: 11.5 }}>
            {shop.phone && `Ph: ${shop.phone}`}
            {shop.email && ` · ${shop.email}`}
          </Typography>
          <Typography sx={{ fontSize: 12, fontWeight: 700, mt: 0.25 }}>
            {shop.gstin && `GSTIN: ${shop.gstin}`}
          </Typography>
        </Box>

        <Stack direction={{ xs: 'column', sm: 'row' }}>
          <Box sx={{ ...box, flex: 1, p: 1 }}>
            <Typography sx={{ fontSize: 10.5, fontWeight: 700, textTransform: 'uppercase', mb: 0.5 }}>
              Buyer
            </Typography>
            <Typography sx={{ fontSize: 13.5, fontWeight: 700 }}>{invoice.customerName}</Typography>
            <Cell label="Phone" value={invoice.customerPhone ?? '—'} />
            <Cell label="GSTIN" value={invoice.customerGstin ?? 'Unregistered'} />
            <Cell
              label="State Code"
              value={`${invoice.customerStateCode ?? shop.stateCode} · ${
                invoice.isInterState ? 'Inter-state' : 'Intra-state'
              }`}
            />
          </Box>

          <Box sx={{ ...box, flex: 1, p: 1, borderLeft: { sm: 'none' }, borderTop: { xs: 'none', sm: '1px solid' } }}>
            <Typography sx={{ fontSize: 10.5, fontWeight: 700, textTransform: 'uppercase', mb: 0.5 }}>
              Invoice Details
            </Typography>
            <Cell label="Invoice No" value={invoice.invoiceNumber} />
            <Cell label="Date" value={formatDate(invoice.invoiceDate)} />
            <Cell label="Payment" value={invoice.paymentMode} />
            {cancelled && (
              <Typography sx={{ fontSize: 12, fontWeight: 700, color: 'error.dark', mt: 0.5 }}>
                *** CANCELLED ***
              </Typography>
            )}
          </Box>
        </Stack>

        <Box sx={{ overflowX: 'auto', mt: 1 }}>
          <Table size="small" sx={{ minWidth: 720, borderCollapse: 'collapse' }}>
            <TableHead>
              <TableRow>
                <TableCell align="center" sx={{ ...th, width: 34 }}>S.No</TableCell>
                <TableCell sx={th}>Particulars</TableCell>
                <TableCell align="center" sx={{ ...th, width: 84 }}>HSN</TableCell>
                <TableCell align="center" sx={{ ...th, width: 80 }}>Qty</TableCell>
                <TableCell align="right" sx={{ ...th, width: 92 }}>Rate</TableCell>
                <TableCell align="right" sx={{ ...th, width: 104 }}>Taxable</TableCell>
                <TableCell align="center" sx={{ ...th, width: 62 }}>GST %</TableCell>
                <TableCell align="right" sx={{ ...th, width: 104 }}>Tax</TableCell>
                <TableCell align="right" sx={{ ...th, width: 112 }}>Amount</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {invoice.items.map((line, index) => (
                <TableRow key={line.id}>
                  <TableCell align="center" sx={td}>{index + 1}</TableCell>
                  <TableCell sx={td}>
                    <Typography sx={{ fontSize: 12.5, fontWeight: 600 }}>{line.itemName}</Typography>
                    <Typography sx={{ fontSize: 10.5, color: 'text.secondary' }}>
                      {line.partNumber}
                      {line.discountPercent > 0 && ` (less ${line.discountPercent}%)`}
                    </Typography>
                  </TableCell>
                  <TableCell align="center" sx={td}>{line.hsn || '—'}</TableCell>
                  <TableCell align="center" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatQuantity(line.quantity)} {line.uqc}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.rate)}
                  </TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.taxableAmount)}
                  </TableCell>
                  <TableCell align="center" sx={td}>{line.gstRate}%</TableCell>
                  <TableCell align="right" sx={{ ...td, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(line.cgstAmount + line.sgstAmount + line.igstAmount)}
                  </TableCell>
                  <TableCell
                    align="right"
                    sx={{ ...td, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}
                  >
                    {formatCurrency(line.lineTotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>

        <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ mt: -0.125 }} className="print-keep">
          <Box sx={{ ...box, flex: 1, p: 1 }}>
            <Typography sx={{ fontSize: 10.5, fontWeight: 700, textTransform: 'uppercase' }}>
              Rupees in Words
            </Typography>
            <Typography sx={{ fontSize: 12.5, fontWeight: 700, mt: 0.25 }}>
              {amountInWords(invoice.grandTotal)}
            </Typography>

            {invoice.notes && (
              <Typography sx={{ fontSize: 11.5, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 1 }}>
                {invoice.notes}
              </Typography>
            )}
            {shop.invoiceTerms && (
              <Typography sx={{ fontSize: 10.5, color: 'text.secondary', whiteSpace: 'pre-wrap', mt: 1 }}>
                {shop.invoiceTerms}
              </Typography>
            )}
          </Box>

          <Box sx={{ ...box, width: { sm: 300 }, p: 1, borderLeft: { sm: 'none' }, borderTop: { xs: 'none', sm: '1px solid' } }}>
            <Amount label="Sub Total" value={invoice.subTotal} />
            {invoice.discountAmount > 0 && <Amount label="Less Discount" value={-invoice.discountAmount} />}
            <Amount label="Taxable Value" value={invoice.taxableAmount} />
            {invoice.isInterState ? (
              <Amount label="IGST" value={invoice.igstAmount} />
            ) : (
              <>
                <Amount label="CGST" value={invoice.cgstAmount} />
                <Amount label="SGST" value={invoice.sgstAmount} />
              </>
            )}
            {invoice.roundOff !== 0 && <Amount label="Round Off" value={invoice.roundOff} />}

            <Box sx={{ borderTop: '3px double', borderColor: 'grey.600', mt: 0.5, pt: 0.5 }}>
              <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
                <Typography sx={{ fontSize: 13, fontWeight: 700 }}>GRAND TOTAL</Typography>
                <Typography sx={{ fontSize: 15, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(invoice.grandTotal)}
                </Typography>
              </Stack>
            </Box>

            <Amount label="Paid" value={invoice.amountPaid} />
            <Amount label="Balance" value={Math.max(invoice.balanceDue, 0)} />
          </Box>
        </Stack>

        <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ mt: 1, justifyContent: 'space-between', gap: 2 }}>
          <Box sx={{ maxWidth: 420 }}>
            {shop.bankDetails && (
              <Typography sx={{ fontSize: 10.5, color: 'text.secondary', whiteSpace: 'pre-wrap' }}>
                {shop.bankDetails}
              </Typography>
            )}
            {shop.invoiceFooter && (
              <Typography sx={{ fontSize: 10.5, color: 'text.secondary', mt: 0.5 }}>
                {shop.invoiceFooter}
              </Typography>
            )}
            <Typography sx={{ fontSize: 10.5, mt: 1 }}>Receiver's Signature</Typography>
            <Box sx={{ borderBottom: '1px solid', borderColor: 'grey.500', width: 160, mt: 2.5 }} />
          </Box>

          <Box sx={{ textAlign: 'right' }}>
            <Typography sx={{ fontSize: 11.5, fontWeight: 600 }}>For {shop.name}</Typography>
            <Box sx={{ borderBottom: '1px solid', borderColor: 'grey.500', width: 170, mt: 4, ml: 'auto' }} />
            <Typography sx={{ fontSize: 10.5, mt: 0.5 }}>Authorised Signatory</Typography>
          </Box>
        </Stack>
      </Paper>
    </>
  )
}

function Amount({ label, value }: { label: string; value: number }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', py: 0.125 }}>
      <Typography sx={{ fontSize: 11.5 }}>{label}</Typography>
      <Typography sx={{ fontSize: 11.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
        {value < 0 ? `(−) ${formatCurrency(Math.abs(value))}` : formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
