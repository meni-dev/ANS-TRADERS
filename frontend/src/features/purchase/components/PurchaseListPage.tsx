import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
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
import { usePurchases } from '../hooks'
import type { PurchaseListItemDto } from '../types'

const STATUS_FILTERS = [
  { value: '', label: 'All statuses' },
  { value: 'Received', label: 'Received' },
  { value: 'Cancelled', label: 'Cancelled' },
]

export function PurchaseListPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [status, setStatus] = useState('')
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })

  const { data, isLoading, isFetching } = usePurchases({
    search: debouncedSearch || undefined,
    status: status || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const columns: GridColDef<PurchaseListItemDto>[] = useMemo(
    () => [
      {
        field: 'purchaseNumber',
        headerName: 'Purchase No.',
        width: 160,
        // Our number and the supplier's live in one cell: they are two names for the same bill, and
        // the counter reconciles against whichever one it happens to be holding.
        renderCell: (params) => (
          <Box sx={{ minWidth: 0 }}>
            <Typography
              sx={{
                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
                fontSize: 12.5,
                fontWeight: 600,
                lineHeight: 1.4,
              }}
              noWrap
            >
              {params.row.purchaseNumber}
            </Typography>
            <Typography sx={{ fontSize: 11.5, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
              Bill {params.row.supplierInvoiceNumber}
            </Typography>
          </Box>
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
        field: 'supplierName',
        headerName: 'Supplier',
        flex: 1,
        minWidth: 200,
        renderCell: (params) => (
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }} noWrap>
              {params.row.supplierName}
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }}>
              {params.row.itemCount} {params.row.itemCount === 1 ? 'item' : 'items'}
            </Typography>
          </Box>
        ),
      },
      {
        field: 'totalTax',
        headerName: 'GST',
        width: 120,
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
        width: 150,
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
        width: 120,
        renderCell: (params) => <DocumentStatusChip status={params.row.status} />,
      },
    ],
    [],
  )

  const total = data?.totalCount ?? 0
  const isFiltering = debouncedSearch.length > 0 || status !== ''

  return (
    <Box>
      <PageHeader
        title="Purchases"
        icon={<ShoppingCartOutlinedIcon />}
        iconTone="violet"
        caption="Supplier bills as they come in — the input tax credit side of your GST return."
        badge={
          !isLoading && (
            <Chip
              label={`${total} ${total === 1 ? 'bill' : 'bills'}`}
              size="small"
              sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
            />
          )
        }
        actions={
          can('PurchaseCreate') && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/purchases/new')}>
              Record Purchase
            </Button>
          )
        }
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mb: 2 }}>
        <TextField
          placeholder="Search purchase no., bill no., supplier…"
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
          sx={{ width: 180 }}
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
      </Stack>

      <DataTable
        rows={data?.items ?? []}
        columns={columns}
        loading={isLoading || isFetching}
        rowCount={total}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        onRowClick={(row) => navigate(`/purchases/${row.id}`)}
        emptyTitle={isFiltering ? 'No matching purchases' : 'No purchases yet'}
        emptyDescription={
          isFiltering
            ? 'Nothing matches the current search and filter. Try widening them.'
            : 'Record your first supplier bill to start tracking stock coming in and the GST paid on it.'
        }
        emptyAction={
          isFiltering ? (
            <Button
              variant="outlined"
              size="small"
              onClick={() => {
                setSearch('')
                setStatus('')
              }}
            >
              Clear filters
            </Button>
          ) : (
            <Button
              variant="contained"
              size="small"
              startIcon={<AddIcon />}
              onClick={() => navigate('/purchases/new')}
            >
              Record Purchase
            </Button>
          )
        }
      />
    </Box>
  )
}
