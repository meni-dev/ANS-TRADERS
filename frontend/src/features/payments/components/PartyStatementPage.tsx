import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { PrintStyles } from '@/features/billing/templates/PrintStyles'
import { useShopSettings } from '@/features/settings/hooks'
import { formatCurrency, formatDate } from '@/lib/format'
import PrintOutlinedIcon from '@mui/icons-material/PrintOutlined'
import {
  Box,
  Button,
  Divider,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { usePartyStatement } from '../hooks'
import { LEDGER_ENTRY_LABELS } from '../types'

/**
 * A party's account as a dated statement. It is not a tax invoice, so it never goes through the
 * five-template picker — one layout, always, and the only thing the shop chooses is the range.
 */
export function PartyStatementPage() {
  const navigate = useNavigate()
  const { partyId } = useParams<{ partyId: string }>()
  const [searchParams] = useSearchParams()
  const isSupplier = searchParams.get('type') === 'supplier'

  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')

  const shop = useShopSettings()
  const party = isSupplier ? { supplierId: partyId } : { customerId: partyId }

  // A statement is read top to bottom in one go, so it is paged large rather than 20 at a time —
  // a customer asking "what do I owe" will not click through four pages to find out.
  const { data, isLoading } = usePartyStatement(party, {
    fromDate: fromDate || undefined,
    toDate: toDate || undefined,
    page: 1,
    pageSize: 200,
  })

  return (
    <Stack spacing={2.5}>
      <PrintStyles page="A4" />

      <PageHeader
        title="Statement of account"
        icon={<ReceiptLongOutlinedIcon />}
        iconTone="blue"
        caption={data?.partyName ?? '—'}
        onBack={() => navigate(-1)}
        align="flex-end"
        className="no-print"
        actions={
          <>
            <TextField
              size="small"
              type="date"
              label="From"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
            />
            <TextField
              size="small"
              type="date"
              label="To"
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
            />
            <Button variant="contained" startIcon={<PrintOutlinedIcon />} onClick={() => window.print()}>
              Print
            </Button>
          </>
        }
        flush
      />

      <Paper variant="outlined" className="print-sheet" sx={{ p: { xs: 2, sm: 4 } }}>
        <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', mb: 2 }}>
          <Box>
            <Typography sx={{ fontSize: 17, fontWeight: 700 }}>
              {shop.data?.name ?? 'Statement'}
            </Typography>
            {shop.data?.addressLine1 ? (
              <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
                {shop.data.addressLine1}
                {shop.data.city ? `, ${shop.data.city}` : ''}
              </Typography>
            ) : null}
            {shop.data?.phone ? (
              <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
                {shop.data.phone}
              </Typography>
            ) : null}
          </Box>
          <Box sx={{ textAlign: 'right' }}>
            <Typography sx={{ fontSize: 13, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6 }}>
              Statement of account
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
              {fromDate || toDate
                ? `${fromDate ? formatDate(fromDate) : 'Start'} — ${toDate ? formatDate(toDate) : 'Today'}`
                : 'All entries'}
            </Typography>
          </Box>
        </Stack>

        <Divider sx={{ mb: 1.5 }} />

        <Typography sx={{ fontSize: 14, fontWeight: 700, mb: 1.5 }}>{data?.partyName ?? ''}</Typography>

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" sx={{ minWidth: 620 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700, fontSize: 12 }}>Date</TableCell>
                <TableCell sx={{ fontWeight: 700, fontSize: 12 }}>Particulars</TableCell>
                <TableCell sx={{ fontWeight: 700, fontSize: 12 }}>Reference</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>
                  Debit
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>
                  Credit
                </TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>
                  Balance
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              <TableRow>
                <TableCell colSpan={5} sx={{ fontSize: 12.5, fontWeight: 600 }}>
                  {/* Never starts at zero unless the account did — a statement that pretends
                      otherwise is one the customer will simply not recognise. */}
                  Balance brought forward
                </TableCell>
                <TableCell align="right" sx={{ fontSize: 12.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(data?.openingBalance ?? 0)}
                </TableCell>
              </TableRow>

              {(data?.entries ?? []).map((entry) => (
                <TableRow key={entry.id}>
                  <TableCell sx={{ fontSize: 12.5, whiteSpace: 'nowrap' }}>
                    {formatDate(entry.entryDate)}
                  </TableCell>
                  <TableCell sx={{ fontSize: 12.5 }}>
                    {LEDGER_ENTRY_LABELS[entry.entryType]}
                    {/* The label already names the event, so a note repeating it is noise on a
                        document the customer reads line by line. */}
                    {entry.notes && entry.notes !== LEDGER_ENTRY_LABELS[entry.entryType] ? (
                      <Typography sx={{ fontSize: 11, color: 'text.secondary' }}>{entry.notes}</Typography>
                    ) : null}
                  </TableCell>
                  <TableCell sx={{ fontSize: 12, fontFamily: 'monospace' }}>
                    {entry.referenceNumber ?? ''}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, fontVariantNumeric: 'tabular-nums' }}>
                    {entry.amount > 0 ? formatCurrency(entry.amount) : ''}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, fontVariantNumeric: 'tabular-nums' }}>
                    {entry.amount < 0 ? formatCurrency(-entry.amount) : ''}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(entry.balanceAfter)}
                  </TableCell>
                </TableRow>
              ))}

              {!isLoading && (data?.entries.length ?? 0) === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} sx={{ fontSize: 12.5, color: 'text.secondary' }}>
                    No entries in this period.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </Box>

        <Divider sx={{ my: 1.5 }} />

        <Stack direction="row" spacing={3} className="print-keep" sx={{ justifyContent: 'flex-end' }}>
          <Typography sx={{ fontSize: 14, fontWeight: 700 }}>Balance due</Typography>
          <Typography sx={{ fontSize: 14, fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(data?.closingBalance ?? 0)}
          </Typography>
        </Stack>

        {(data?.closingBalance ?? 0) < 0 ? (
          <Typography sx={{ fontSize: 12, color: 'text.secondary', textAlign: 'right', mt: 0.5 }}>
            In credit — this is money held on account.
          </Typography>
        ) : null}
      </Paper>
    </Stack>
  )
}
