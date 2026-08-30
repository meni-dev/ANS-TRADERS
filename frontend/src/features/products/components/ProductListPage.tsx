import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { DataTable } from '@/components/data/DataTable'
import { useAuth } from '@/features/auth/AuthProvider'
import { ConfirmDialog } from '@/components/feedback/ConfirmDialog'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { formatCurrency, formatQuantity } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import AddIcon from '@mui/icons-material/Add'
import BlockIcon from '@mui/icons-material/Block'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlined'
import ClearIcon from '@mui/icons-material/Clear'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import SearchIcon from '@mui/icons-material/Search'
import {
  Box,
  Button,
  Chip,
  IconButton,
  InputAdornment,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid'
import { useNavigate } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { useActivateProduct, useDeactivateProduct, useProducts } from '../hooks'
import type { ProductDto } from '../types'
import { CreateProductDialog } from './CreateProductDialog'
import { EditProductDialog } from './EditProductDialog'

export function ProductListPage() {
  const navigate = useNavigate()
  const { can } = useAuth()
  const { notify } = useNotification()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [createOpen, setCreateOpen] = useState(false)
  const [editingProduct, setEditingProduct] = useState<ProductDto | null>(null)
  const [toggleTarget, setToggleTarget] = useState<ProductDto | null>(null)

  const { data, isLoading, isFetching } = useProducts({
    search: debouncedSearch || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const deactivateProduct = useDeactivateProduct()
  const activateProduct = useActivateProduct()

  const columns: GridColDef<ProductDto>[] = useMemo(
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
        // Name and fitment travel together, so pairing them in one cell saves a column and
        // keeps the row scannable as a single unit.
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
        field: 'hsn',
        headerName: 'HSN',
        width: 110,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, color: params.row.hsn ? 'text.primary' : 'text.disabled' }}>
            {params.row.hsn || '—'}
          </Typography>
        ),
      },
      {
        field: 'gstRate',
        headerName: 'GST',
        width: 90,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
            {params.row.gstRate}%
          </Typography>
        ),
      },
      {
        field: 'sellingRate',
        headerName: 'Selling Rate',
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.sellingRate)}
          </Typography>
        ),
      },
      {
        field: 'mrp',
        headerName: 'MRP',
        width: 120,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, color: 'text.secondary', fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.mrp)}
          </Typography>
        ),
      },
      {
        field: 'stockOnHand',
        headerName: 'Stock',
        width: 100,
        align: 'right',
        headerAlign: 'right',
        // Live stock, not the opening figure — the number a counter needs is what is on the shelf
        // right now. Out and low are the two states worth colouring; everything else is normal.
        renderCell: (params) => {
          const stock = Number(params.row.stockOnHand)
          const out = stock <= 0
          const low = !out && stock <= Number(params.row.reorderLevel)
          return (
            <Typography
              sx={{
                fontSize: 13,
                fontWeight: 600,
                fontVariantNumeric: 'tabular-nums',
                color: out ? 'error.dark' : low ? 'warning.dark' : 'text.primary',
              }}
            >
              {formatQuantity(stock)}
            </Typography>
          )
        },
      },
      {
        field: 'isActive',
        headerName: 'Status',
        width: 110,
        renderCell: (params) => (
          <Chip
            label={params.row.isActive ? 'Active' : 'Inactive'}
            size="small"
            sx={{
              bgcolor: params.row.isActive ? 'success.light' : 'grey.100',
              color: params.row.isActive ? 'success.dark' : 'text.secondary',
            }}
          />
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
            <Tooltip title="Edit">
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation()
                  setEditingProduct(params.row)
                }}
              >
                <EditOutlinedIcon sx={{ fontSize: 18 }} />
              </IconButton>
            </Tooltip>
            <Tooltip title={params.row.isActive ? 'Deactivate' : 'Activate'}>
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation()
                  setToggleTarget(params.row)
                }}
              >
                {params.row.isActive ? (
                  <BlockIcon sx={{ fontSize: 18 }} />
                ) : (
                  <CheckCircleOutlineIcon sx={{ fontSize: 18 }} />
                )}
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      },
    ],
    [],
  )

  const handleToggleConfirm = async () => {
    if (!toggleTarget) return
    try {
      if (toggleTarget.isActive) {
        await deactivateProduct.mutateAsync(toggleTarget.id)
        notify(`Product "${toggleTarget.itemName}" deactivated`)
      } else {
        await activateProduct.mutateAsync(toggleTarget.id)
        notify(`Product "${toggleTarget.itemName}" activated`)
      }
      setToggleTarget(null)
    } catch {
      notify('Something went wrong. Please try again.', 'error')
    }
  }

  const total = data?.totalCount ?? 0
  const isSearching = debouncedSearch.length > 0

  return (
    <Box>
      <PageHeader
        title="Products"
        icon={<CategoryOutlinedIcon />}
        iconTone="teal"
        caption="Every spare part you stock — search by part number, item name or vehicle."
        badge={
          !isLoading && (
            <Chip
              label={`${total} ${total === 1 ? 'item' : 'items'}`}
              size="small"
              sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
            />
          )
        }
        actions={
          can('ProductManage') && (
            <Stack direction="row" spacing={1}>
              {/* A catalogue is thousands of parts — typing them in one at a time is not a plan. */}
              <Button
                variant="outlined"
                startIcon={<UploadFileOutlinedIcon />}
                onClick={() => navigate('/products/import')}
              >
                Import
              </Button>
              <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
                Add Product
              </Button>
            </Stack>
          )
        }
      />

      <TextField
        placeholder="Search part number, item name, vehicle…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        sx={{ mb: 2, width: 440, maxWidth: '100%' }}
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

      <DataTable
        rows={data?.items ?? []}
        columns={columns}
        loading={isLoading || isFetching}
        rowCount={total}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        onRowClick={(row) => setEditingProduct(row)}
        emptyTitle={isSearching ? 'No matching products' : 'No products yet'}
        emptyDescription={
          isSearching
            ? `Nothing matches “${debouncedSearch}”. Try a different part number or item name.`
            : 'Add your first spare part to start building the item master.'
        }
        emptyAction={
          isSearching ? (
            <Button variant="outlined" size="small" onClick={() => setSearch('')}>
              Clear search
            </Button>
          ) : can('ProductManage') ? (
            <Button variant="contained" size="small" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Add Product
            </Button>
          ) : undefined
        }
      />

      <CreateProductDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      {editingProduct && (
        <EditProductDialog product={editingProduct} onClose={() => setEditingProduct(null)} />
      )}

      <ConfirmDialog
        open={!!toggleTarget}
        title={toggleTarget?.isActive ? 'Deactivate product?' : 'Activate product?'}
        description={
          toggleTarget?.isActive
            ? `"${toggleTarget?.itemName}" will be hidden from billing search until reactivated.`
            : `"${toggleTarget?.itemName}" will become available again in billing search.`
        }
        confirmLabel={toggleTarget?.isActive ? 'Deactivate' : 'Activate'}
        confirmColor={toggleTarget?.isActive ? 'error' : 'primary'}
        loading={deactivateProduct.isPending || activateProduct.isPending}
        onConfirm={handleToggleConfirm}
        onCancel={() => setToggleTarget(null)}
      />
    </Box>
  )
}
