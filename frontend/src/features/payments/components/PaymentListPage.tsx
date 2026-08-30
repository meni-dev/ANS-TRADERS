import AccountBalanceWalletOutlinedIcon from '@mui/icons-material/AccountBalanceWalletOutlined'
import CallReceivedIcon from '@mui/icons-material/CallReceived'
import CallMadeIcon from '@mui/icons-material/CallMade'
import AccountBalanceOutlinedIcon from '@mui/icons-material/AccountBalanceOutlined'
import { DataTable } from '@/components/data/DataTable'
import { useAuth } from '@/features/auth/AuthProvider'
import { StatTile } from '@/components/data/StatTile'
import { formatCurrency, formatDate } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import {
  Box,
  Button,
  Chip,
  IconButton,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { usePaymentSummary, usePayments } from '../hooks'
import type { PaymentListItemDto, PaymentStatus } from '../types'

const DIRECTIONS = [
  { value: '', label: 'Money in and out' },
  { value: 'Received', label: 'Received' },
  { value: 'Paid', label: 'Paid out' },
]

const STATUS_COLOURS: Record<PaymentStatus, 'success' | 'warning' | 'default'> = {
  Posted: 'success',
  Pending: 'warning',
  Reversed: 'default',
}

/** Says what the status means, rather than repeating the word already in the chip. */
const STATUS_HINTS: Record<PaymentStatus, string> = {
  Posted: 'Settled',
  Pending: 'Not banked yet',
  Reversed: 'Reversed',
}

/** The enum value is `Upi`; nobody writes it that way. */
function modeLabel(mode: string): string {
  return mode === 'Upi' ? 'UPI' : mode === 'BankTransfer' ? 'Bank Transfer' : mode
}

function financialYearStart(): string {
  const today = new Date()
  const year = today.getMonth() >= 3 ? today.getFullYear() : today.getFullYear() - 1
  return `${year}-04-01`
}

export function PaymentListPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [direction, setDirection] = useState('')
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })

  const today = new Date().toISOString().slice(0, 10)
  const summary = usePaymentSummary({ fromDate: financialYearStart(), toDate: today })

  const { data, isLoading, isFetching } = usePayments({
    search: debouncedSearch || undefined,
    direction: direction || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const columns: GridColDef<PaymentListItemDto>[] = useMemo(
    () => [
      {
        field: 'paymentDate',
        headerName: 'Date',
        width: 118,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13 }}>{formatDate(params.row.paymentDate)}</Typography>
        ),
      },
      {
        field: 'receiptNumber',
        headerName: 'Receipt',
        width: 170,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, fontFamily: 'monospace' }}>
            {/* Counter money has no receipt of its own — the customer was handed the bill. */}
            {params.row.receiptNumber ?? (params.row.isCounterPayment ? 'On the bill' : '—')}
          </Typography>
        ),
      },
      {
        field: 'partyName',
        headerName: 'Party',
        flex: 1,
        minWidth: 180,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600 }} noWrap>
            {params.row.partyName}
          </Typography>
        ),
      },
      {
        field: 'mode',
        headerName: 'Mode',
        width: 150,
        renderCell: (params) => (
          <Stack sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: 13 }}>{modeLabel(params.row.mode)}</Typography>
            {params.row.chequeNumber ? (
              <Typography sx={{ fontSize: 11.5, color: 'text.secondary' }} noWrap>
                {params.row.chequeNumber} · {params.row.chequeStatus}
              </Typography>
            ) : null}
          </Stack>
        ),
      },
      {
        field: 'amount',
        headerName: 'Amount',
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography
            sx={{
              fontSize: 13.5,
              fontWeight: 600,
              fontVariantNumeric: 'tabular-nums',
              // Direction is also written in the header filter and the status column, so colour is
              // never the only thing carrying it.
              color: params.row.direction === 'Received' ? 'success.main' : 'text.primary',
            }}
          >
            {params.row.direction === 'Received' ? '+' : '−'}
            {formatCurrency(params.row.amount)}
          </Typography>
        ),
      },
      {
        field: 'unallocatedAmount',
        headerName: 'On account',
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) =>
          // A reversed payment keeps its figures as a record of what it once did, so showing them
          // here would offer the counter money it cannot actually spend.
          params.row.unallocatedAmount > 0 && params.row.status !== 'Reversed' ? (
            <Typography sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
              {formatCurrency(params.row.unallocatedAmount)}
            </Typography>
          ) : (
            <Typography sx={{ fontSize: 13, color: 'text.disabled' }}>—</Typography>
          ),
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 150,
        renderCell: (params) => (
          <Chip
            size="small"
            variant="outlined"
            color={STATUS_COLOURS[params.row.status]}
            label={STATUS_HINTS[params.row.status]}
          />
        ),
      },
    ],
    [],
  )

  return (
    <Stack spacing={2.5}>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            Receipts &amp; Payments
          </Typography>
          <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
            Every rupee in and out, this financial year
          </Typography>
        </Box>
        {can('PaymentRecord') && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/accounts/payments/new')}>
            Record receipt
          </Button>
        )}
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(4, 1fr)' },
        }}
      >
        <StatTile
          label="Collected"
          value={formatCurrency(summary.data?.collected ?? 0)}
          icon={<CallReceivedIcon />}
          iconTone="teal"
          tinted
        />
        <StatTile
          label="Paid out"
          value={formatCurrency(summary.data?.paidOut ?? 0)}
          icon={<CallMadeIcon />}
          iconTone="rose"
          tinted
        />
        <StatTile
          label="Net cash"
          value={formatCurrency(summary.data?.netCash ?? 0)}
          icon={<AccountBalanceOutlinedIcon />}
          iconTone="blue"
          tinted
        />
        {/* Deliberately its own tile: paper the shop is holding is not money it can spend, and
            adding it to Collected is how a cash figure starts lying. */}
        <StatTile
          label="Cheques in hand"
          value={formatCurrency(summary.data?.chequesInHand ?? 0)}
          caption={`${summary.data?.chequesInHandCount ?? 0} not yet cleared`}
          icon={<AccountBalanceWalletOutlinedIcon />}
          iconTone="violet"
          tinted
        />
      </Box>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <TextField
          size="small"
          placeholder="Receipt no., party or cheque number"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          sx={{ flex: 1, maxWidth: 380 }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
              endAdornment: search ? (
                <InputAdornment position="end">
                  <IconButton size="small" onClick={() => setSearch('')} aria-label="Clear search">
                    <ClearIcon fontSize="small" />
                  </IconButton>
                </InputAdornment>
              ) : null,
            },
          }}
        />
        <TextField
          select
          size="small"
          value={direction}
          onChange={(event) => setDirection(event.target.value)}
          sx={{ minWidth: 190 }}
          slotProps={{ select: { displayEmpty: true } }}
        >
          {DIRECTIONS.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <DataTable
        rows={data?.items ?? []}
        columns={columns}
        rowCount={data?.totalCount ?? 0}
        loading={isLoading || isFetching}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        emptyTitle="No money recorded yet"
        emptyDescription="Receipts you take against a bill, and money you pay a supplier, both land here."
      />
    </Stack>
  )
}
