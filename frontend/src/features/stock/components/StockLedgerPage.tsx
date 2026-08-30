import SwapVertOutlinedIcon from '@mui/icons-material/SwapVertOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { DataTable } from '@/components/data/DataTable'
import { formatDate, formatQuantity } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
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
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useStockMovements } from '../hooks'
import { MOVEMENT_TYPE_LABELS, type StockMovementDto, type StockMovementType } from '../types'

const TYPE_FILTERS = [
  { value: '', label: 'All movements' },
  ...(Object.keys(MOVEMENT_TYPE_LABELS) as StockMovementType[]).map((value) => ({
    value,
    label: MOVEMENT_TYPE_LABELS[value],
  })),
]

/** Where a movement's reference document lives, so the ledger can link back to it. */
function referenceRoute(movement: StockMovementDto): string | null {
  if (!movement.referenceId) return null
  if (movement.movementType === 'Sale' || movement.movementType === 'SaleCancelled') {
    return `/billing/${movement.referenceId}`
  }
  if (movement.movementType === 'Purchase' || movement.movementType === 'PurchaseCancelled') {
    return `/purchases/${movement.referenceId}`
  }
  return null
}

export function StockLedgerPage() {
  const navigate = useNavigate()
  // Deep-linked from the stock screen's history button, so the product filter comes off the URL
  // rather than component state — the link has to survive a refresh and a back button.
  const [searchParams, setSearchParams] = useSearchParams()
  const productId = searchParams.get('productId') ?? undefined

  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [movementType, setMovementType] = useState('')
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })

  const { data, isLoading, isFetching } = useStockMovements({
    search: debouncedSearch || undefined,
    productId,
    movementType: movementType || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const columns: GridColDef<StockMovementDto>[] = useMemo(
    () => [
      {
        field: 'movementDate',
        headerName: 'Date',
        width: 120,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13 }}>{formatDate(params.row.movementDate)}</Typography>
        ),
      },
      {
        field: 'itemName',
        headerName: 'Item',
        flex: 1,
        minWidth: 220,
        renderCell: (params) => (
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }} noWrap>
              {params.row.itemName}
            </Typography>
            <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
              {params.row.partNumber}
            </Typography>
          </Box>
        ),
      },
      {
        field: 'movementType',
        headerName: 'Movement',
        width: 160,
        renderCell: (params) => {
          const inward = params.row.quantity > 0
          return (
            <Chip
              label={MOVEMENT_TYPE_LABELS[params.row.movementType] ?? params.row.movementType}
              size="small"
              sx={{
                bgcolor: inward ? 'success.light' : 'grey.100',
                color: inward ? 'success.dark' : 'text.secondary',
              }}
            />
          )
        },
      },
      {
        field: 'referenceNumber',
        headerName: 'Reference',
        width: 170,
        renderCell: (params) => (
          <Typography
            sx={{
              fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
              fontSize: 12,
              color: params.row.referenceNumber ? 'text.secondary' : 'text.disabled',
            }}
            noWrap
          >
            {params.row.referenceNumber ?? params.row.notes ?? '—'}
          </Typography>
        ),
      },
      {
        field: 'quantity',
        headerName: 'Change',
        width: 110,
        align: 'right',
        headerAlign: 'right',
        // Signed and coloured: the direction is the first thing anyone reads off a ledger.
        renderCell: (params) => {
          const inward = params.row.quantity > 0
          return (
            <Typography
              sx={{
                fontSize: 13.5,
                fontWeight: 700,
                fontVariantNumeric: 'tabular-nums',
                color: inward ? 'success.dark' : 'error.dark',
              }}
            >
              {inward ? '+' : '−'}
              {formatQuantity(Math.abs(params.row.quantity))}
            </Typography>
          )
        },
      },
      {
        field: 'balanceAfter',
        headerName: 'Balance',
        width: 110,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
            {formatQuantity(params.row.balanceAfter)}
          </Typography>
        ),
      },
    ],
    [],
  )

  const total = data?.totalCount ?? 0
  const filteredProductName = productId ? data?.items[0]?.itemName : undefined
  const isFiltering = debouncedSearch.length > 0 || movementType !== '' || !!productId

  const clearFilters = () => {
    setSearch('')
    setMovementType('')
    setSearchParams({}, { replace: true })
  }

  return (
    <Box>
      <PageHeader
        title="Stock Ledger"
        icon={<SwapVertOutlinedIcon />}
        iconTone="violet"
        caption="Every quantity change, with the document behind it. Newest first."
        badge={
          !isLoading && (
            <Chip
              label={`${total} ${total === 1 ? 'movement' : 'movements'}`}
              size="small"
              sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
            />
          )
        }
        actions={
          <Button variant="outlined" onClick={() => navigate('/inventory/stock')}>
            Back to Stock
          </Button>
        }
      />

      {productId && (
        <Chip
          label={`Filtered to ${filteredProductName ?? 'one item'}`}
          size="small"
          onDelete={() => setSearchParams({}, { replace: true })}
          sx={{ mb: 2, bgcolor: 'primary.light', color: 'primary.dark' }}
        />
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mb: 2 }}>
        <TextField
          placeholder="Search item, part number, document no.…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ width: 400, maxWidth: '100%' }}
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
          value={movementType}
          onChange={(e) => setMovementType(e.target.value)}
          sx={{ width: 200 }}
          aria-label="Filter by movement type"
          // See the note in InvoiceListPage: an empty value needs displayEmpty to render its label.
          slotProps={{ select: { displayEmpty: true } }}
        >
          {TYPE_FILTERS.map((option) => (
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
        onRowClick={(row) => {
          const route = referenceRoute(row)
          if (route) navigate(route)
        }}
        emptyTitle={isFiltering ? 'No matching movements' : 'No stock movements yet'}
        emptyDescription={
          isFiltering
            ? 'Nothing matches the current filters. Try widening them.'
            : 'Record a purchase or raise an invoice and the movement will appear here.'
        }
        emptyAction={
          isFiltering ? (
            <Button variant="outlined" size="small" onClick={clearFilters}>
              Clear filters
            </Button>
          ) : undefined
        }
      />
    </Box>
  )
}
