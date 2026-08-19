import { DataTable } from '@/components/data/DataTable'
import { useAuth } from '@/features/auth/AuthProvider'
import { BalanceChip, DocumentStatusChip } from '@/components/document/DocumentStatusChip'
import { formatCurrency, formatDate } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import AddIcon from '@mui/icons-material/Add'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import {
  Box,
  Button,
  Chip,
  FormControlLabel,
  IconButton,
  InputAdornment,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid'
import { useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useInvoices } from '../hooks'
import type { InvoiceListItemDto } from '../types'

const STATUS_FILTERS = [
  { value: '', label: 'All statuses' },
  { value: 'Issued', label: 'Issued' },
  { value: 'Cancelled', label: 'Cancelled' },
]

export function InvoiceListPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [status, setStatus] = useState('')
  // Seeded from the URL so the dashboard's receivables tile can land on the outstanding list
  // rather than on everything ever billed.
  const [searchParams] = useSearchParams()
  const [unpaidOnly, setUnpaidOnly] = useState(searchParams.get('unpaid') === '1')
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })

  const { data, isLoading, isFetching } = useInvoices({
    search: debouncedSearch || undefined,
    status: status || undefined,
    unpaidOnly: unpaidOnly || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const columns: GridColDef<InvoiceListItemDto>[] = useMemo(
    () => [
      {
        field: 'invoiceNumber',
        headerName: 'Invoice No.',
        width: 165,
        renderCell: (params) => (
          <Typography
            sx={{
              fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
              fontSize: 12.5,
              fontWeight: 600,
            }}
            noWrap
          >
            {params.row.invoiceNumber}
          </Typography>
        ),
      },
      {
        field: 'invoiceDate',
        headerName: 'Date',
        width: 120,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13 }}>{formatDate(params.row.invoiceDate)}</Typography>
        ),
      },
      {
        field: 'customerName',
        headerName: 'Customer',
        flex: 1,
        minWidth: 200,
        // Phone sits under the name because that is how a counter tells two same-named customers
        // apart, and it is what they are called back on.
        renderCell: (params) => (
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }} noWrap>
              {params.row.customerName}
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
              {params.row.customerPhone ?? 'Walk-in'} · {params.row.itemCount}{' '}
              {params.row.itemCount === 1 ? 'item' : 'items'}
            </Typography>
          </Box>
        ),
      },
      {
        field: 'totalTax',
        headerName: 'GST',
        width: 110,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.totalTax)}
          </Typography>
        ),
      },
      {
        field: 'grandTotal',
        headerName: 'Total',
        width: 140,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.grandTotal)}
          </Typography>
        ),
      },
      {
        field: 'balanceDue',
        headerName: 'Payment',
        width: 160,
        renderCell: (params) => (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <BalanceChip balanceDue={params.row.balanceDue} grandTotal={params.row.grandTotal} />
            {params.row.balanceDue > 0 && (
              <Typography sx={{ fontSize: 12.5, color: 'warning.dark', fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(params.row.balanceDue)}
              </Typography>
            )}
          </Stack>
        ),
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 110,
        renderCell: (params) => <DocumentStatusChip status={params.row.status} />,
      },
    ],
    [],
  )

  const total = data?.totalCount ?? 0
  const isFiltering = debouncedSearch.length > 0 || status !== '' || unpaidOnly

  const clearFilters = () => {
    setSearch('')
    setStatus('')
    setUnpaidOnly(false)
  }

  return (
    <Box>
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' }, mb: 2.5 }}
      >
        <Box>
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <Typography variant="h1">Invoices</Typography>
            {!isLoading && (
              <Chip
                label={`${total} ${total === 1 ? 'invoice' : 'invoices'}`}
                size="small"
                sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
              />
            )}
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Every tax invoice raised at the counter, newest first.
          </Typography>
        </Box>

        {can('BillCreate') && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/billing/new')}>
            New Invoice
          </Button>
        )}
      </Stack>

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1.5}
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <TextField
          placeholder="Search invoice no., customer, phone…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ width: 380, maxWidth: '100%' }}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon sx={{ fontSize: 18, color: 'text.disabled' }} />
                </InputAdornment>
              ),
              endAdornment: search ? (
                <InputAdornment position="end">
                  <IconButton size="small" onClick={() => setSearch('')} aria-label="Clear search">
                    <ClearIcon sx={{ fontSize: 16 }} />
                  </IconButton>
                </InputAdornment>
              ) : undefined,
            },
          }}
        />

        <TextField
          select
          value={status}
          onChange={(e) => setStatus(e.target.value)}
          sx={{ width: 170 }}
          aria-label="Filter by status"
          // "All statuses" is the empty value, and MUI renders a blank box for it unless the
          // select is told that empty is a real choice rather than nothing selected.
          slotProps={{ select: { displayEmpty: true } }}
        >
          {STATUS_FILTERS.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>

        <FormControlLabel
          control={
            <Switch
              size="small"
              checked={unpaidOnly}
              onChange={(e) => setUnpaidOnly(e.target.checked)}
            />
          }
          label={<Typography sx={{ fontSize: 13 }}>Outstanding only</Typography>}
        />
      </Stack>

      <DataTable
        rows={data?.items ?? []}
        columns={columns}
        loading={isLoading || isFetching}
        rowCount={total}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        onRowClick={(row) => navigate(`/billing/${row.id}`)}
        emptyTitle={isFiltering ? 'No matching invoices' : 'No invoices yet'}
        emptyDescription={
          isFiltering
            ? 'Nothing matches the current search and filters. Try widening them.'
            : 'Raise your first bill — pick a customer, add the parts, and the GST works itself out.'
        }
        emptyAction={
          isFiltering ? (
            <Button variant="outlined" size="small" onClick={clearFilters}>
              Clear filters
            </Button>
          ) : (
            <Button
              variant="contained"
              size="small"
              startIcon={<AddIcon />}
              onClick={() => navigate('/billing/new')}
            >
              New Invoice
            </Button>
          )
        }
      />
    </Box>
  )
}
