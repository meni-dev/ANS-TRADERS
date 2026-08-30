import TrendingDownOutlinedIcon from '@mui/icons-material/TrendingDownOutlined'
import SavingsOutlinedIcon from '@mui/icons-material/SavingsOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { DataTable } from '@/components/data/DataTable'
import { useAuth } from '@/features/auth/AuthProvider'
import { StatTile } from '@/components/data/StatTile'
import { formatCurrency, formatQuantity } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import ClearIcon from '@mui/icons-material/Clear'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import SearchIcon from '@mui/icons-material/Search'
import SwapVertOutlinedIcon from '@mui/icons-material/SwapVertOutlined'
import {
  Box,
  Button,
  Chip,
  FormControlLabel,
  Grid,
  IconButton,
  InputAdornment,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useStock, useStockSummary } from '../hooks'
import type { ProductStockDto } from '../types'
import { AdjustStockDialog } from './AdjustStockDialog'

type StockListPageProps = {
  /** Opens the screen pre-filtered, for the Low Stock entry in the sidebar. */
  lowOnlyByDefault?: boolean
}

export function StockListPage({ lowOnlyByDefault = false }: StockListPageProps) {
  const navigate = useNavigate()
  const { can } = useAuth()
  const canAdjust = can('StockAdjust')
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [lowOnly, setLowOnly] = useState(lowOnlyByDefault)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [adjusting, setAdjusting] = useState<ProductStockDto | null>(null)

  const filters = {
    search: debouncedSearch || undefined,
    lowOnly: lowOnly || undefined,
  }

  const { data, isLoading, isFetching } = useStock({
    ...filters,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  // Headline figures describe the whole filtered set, not the page on screen, so they come from
  // their own endpoint rather than being summed off `data.items`.
  const { data: summary } = useStockSummary(filters)

  const columns: GridColDef<ProductStockDto>[] = useMemo(
    () => [
      {
        field: 'partNumber',
        headerName: 'Part Number',
        width: 150,
        renderCell: (params) => (
          <Typography
            sx={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace', fontSize: 12.5, fontWeight: 600 }}
          >
            {params.row.partNumber}
          </Typography>
        ),
      },
      {
        field: 'itemName',
        headerName: 'Item',
        flex: 1,
        minWidth: 220,
        renderCell: (params) => {
          const vehicle = [params.row.vehicleBrand, params.row.vehicleModel].filter(Boolean).join(' · ')
          return (
            <Box sx={{ minWidth: 0 }}>
              <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }} noWrap>
                {params.row.itemName}
              </Typography>
              <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
                {vehicle || 'No vehicle fitment'}
              </Typography>
            </Box>
          )
        },
      },
      {
        field: 'stockOnHand',
        headerName: 'On Hand',
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => {
          const out = params.row.stockOnHand <= 0
          const low = !out && params.row.stockOnHand <= params.row.reorderLevel
          return (
            <Box sx={{ textAlign: 'right' }}>
              <Typography
                sx={{
                  fontSize: 14,
                  fontWeight: 700,
                  fontVariantNumeric: 'tabular-nums',
                  color: out ? 'error.dark' : low ? 'warning.dark' : 'text.primary',
                }}
              >
                {formatQuantity(params.row.stockOnHand)}
              </Typography>
              <Typography sx={{ fontSize: 11, color: 'text.disabled', lineHeight: 1.3 }}>
                {params.row.uqc}
              </Typography>
            </Box>
          )
        },
      },
      {
        field: 'reorderLevel',
        headerName: 'Reorder At',
        width: 110,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
            {params.row.reorderLevel > 0 ? formatQuantity(params.row.reorderLevel) : '—'}
          </Typography>
        ),
      },
      {
        field: 'stockValue',
        headerName: 'Value at Cost',
        width: 140,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.stockValue)}
          </Typography>
        ),
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 150,
        sortable: false,
        // Discontinued parts stay on this screen — they still occupy shelf space and still count
        // towards the stock value — so the row says so rather than the list quietly omitting them.
        renderCell: (params) => (
          <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
            {params.row.stockOnHand <= 0 ? (
              <Chip label="Out of stock" size="small" sx={{ bgcolor: 'error.light', color: 'error.dark' }} />
            ) : params.row.stockOnHand <= params.row.reorderLevel ? (
              <Chip label="Low" size="small" sx={{ bgcolor: 'warning.light', color: 'warning.dark' }} />
            ) : (
              <Chip label="In stock" size="small" sx={{ bgcolor: 'success.light', color: 'success.dark' }} />
            )}
            {!params.row.isActive && (
              <Chip label="Inactive" size="small" sx={{ bgcolor: 'grey.100', color: 'text.secondary' }} />
            )}
          </Stack>
        ),
      },
      {
        field: 'actions',
        headerName: '',
        width: 92,
        sortable: false,
        filterable: false,
        align: 'right',
        renderCell: (params) => (
          <Stack direction="row" spacing={0.25} sx={{ justifyContent: 'flex-end', width: '100%' }}>
            {canAdjust && (
              <Tooltip title="Adjust stock">
                <IconButton
                  size="small"
                  onClick={(e) => {
                    e.stopPropagation()
                    setAdjusting(params.row)
                  }}
                >
                  <FactCheckOutlinedIcon sx={{ fontSize: 18 }} />
                </IconButton>
              </Tooltip>
            )}
            <Tooltip title="Movement history">
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation()
                  navigate(`/inventory/stock-ledger?productId=${params.row.id}`)
                }}
              >
                <SwapVertOutlinedIcon sx={{ fontSize: 18 }} />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [navigate, canAdjust],
  )

  const total = data?.totalCount ?? 0
  const isFiltering = debouncedSearch.length > 0 || lowOnly !== lowOnlyByDefault

  return (
    <Box>
      <PageHeader
        title={lowOnlyByDefault ? 'Low Stock' : 'Stock'}
        icon={<WarehouseOutlinedIcon />}
        iconTone="amber"
        caption={
          lowOnlyByDefault
            ? 'Items at or below their reorder level — what to put on the next supplier order.'
            : 'What is on the shelf right now. Purchases add to it, invoices take from it.'
        }
        actions={
          <Button
            variant="outlined"
            startIcon={<SwapVertOutlinedIcon sx={{ fontSize: 18 }} />}
            onClick={() => navigate('/inventory/stock-ledger')}
          >
            Stock Ledger
          </Button>
        }
      />

      <Grid container spacing={2} sx={{ mb: 2.5 }}>
        <Grid size={{ xs: 6, md: 3 }}>
          <StatTile
            label="Items tracked"
            value={`${summary?.totalItems ?? 0}`}
            icon={<Inventory2OutlinedIcon />}
            iconTone="blue"
            tinted
          />
        </Grid>
        <Grid size={{ xs: 6, md: 3 }}>
          <StatTile
            label="Low stock"
            value={`${summary?.lowStockCount ?? 0}`}
            tone="warning"
            icon={<TrendingDownOutlinedIcon />}
            iconTone="amber"
            tinted
          />
        </Grid>
        <Grid size={{ xs: 6, md: 3 }}>
          <StatTile
            label="Out of stock"
            value={`${summary?.outOfStockCount ?? 0}`}
            tone="error"
            icon={<ErrorOutlineOutlinedIcon />}
            iconTone="rose"
            tinted
          />
        </Grid>
        <Grid size={{ xs: 6, md: 3 }}>
          <StatTile
            label="Value at cost"
            value={formatCurrency(summary?.totalStockValue ?? 0)}
            icon={<SavingsOutlinedIcon />}
            iconTone="teal"
            tinted
          />
        </Grid>
      </Grid>

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1.5}
        sx={{ mb: 2, alignItems: { sm: 'center' } }}
      >
        <TextField
          placeholder="Search part number, item name, vehicle…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ width: 420, maxWidth: '100%' }}
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

        <FormControlLabel
          control={
            <Switch size="small" checked={lowOnly} onChange={(e) => setLowOnly(e.target.checked)} />
          }
          label={<Typography sx={{ fontSize: 13 }}>Low stock only</Typography>}
        />
      </Stack>

      <DataTable
        rows={data?.items ?? []}
        columns={columns}
        loading={isLoading || isFetching}
        rowCount={total}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        onRowClick={(row) => setAdjusting(row)}
        emptyTitle={
          lowOnly ? 'Nothing is running low' : isFiltering ? 'No matching items' : 'No stock tracked yet'
        }
        emptyDescription={
          lowOnly
            ? 'Every tracked item is above its reorder level.'
            : isFiltering
              ? 'Nothing matches the current search. Try a different part number or item name.'
              : 'Add products to the item master and record a purchase to start tracking stock.'
        }
        emptyAction={
          isFiltering ? (
            <Button
              variant="outlined"
              size="small"
              onClick={() => {
                setSearch('')
                setLowOnly(lowOnlyByDefault)
              }}
            >
              Clear filters
            </Button>
          ) : (
            <Button variant="outlined" size="small" onClick={() => navigate('/products')}>
              Go to Products
            </Button>
          )
        }
      />

      {adjusting && <AdjustStockDialog product={adjusting} onClose={() => setAdjusting(null)} />}
    </Box>
  )
}
