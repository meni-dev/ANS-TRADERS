import { DataTable } from '@/components/data/DataTable'
import { ConfirmDialog } from '@/components/feedback/ConfirmDialog'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { formatCurrency } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import AddIcon from '@mui/icons-material/Add'
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
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
import { useActivateSupplier, useDeactivateSupplier, useSuppliers } from '../hooks'
import type { SupplierDto } from '../types'
import { CreateSupplierDialog } from './CreateSupplierDialog'
import { EditSupplierDialog } from './EditSupplierDialog'

export function SupplierListPage() {
  const navigate = useNavigate()
  const { notify } = useNotification()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [createOpen, setCreateOpen] = useState(false)
  const [editing, setEditing] = useState<SupplierDto | null>(null)
  const [toggleTarget, setToggleTarget] = useState<SupplierDto | null>(null)

  const { data, isLoading, isFetching } = useSuppliers({
    search: debouncedSearch || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const deactivate = useDeactivateSupplier()
  const activate = useActivateSupplier()

  const columns: GridColDef<SupplierDto>[] = useMemo(
    () => [
      {
        field: 'name',
        headerName: 'Supplier',
        flex: 1,
        minWidth: 220,
        // The contact person is who you actually call, so it rides along under the firm name.
        renderCell: (params) => {
          const sub = params.row.contactPerson
            ? params.row.contactPerson
            : [params.row.city, params.row.state].filter(Boolean).join(', ')
          return (
            <Box sx={{ minWidth: 0 }}>
              <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }} noWrap>
                {params.row.name}
              </Typography>
              <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
                {sub || 'No contact person'}
              </Typography>
            </Box>
          )
        },
      },
      {
        field: 'phone',
        headerName: 'Phone',
        width: 140,
        renderCell: (params) => (
          <Typography
            sx={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace', fontSize: 12.5, fontWeight: 600 }}
          >
            {params.row.phone}
          </Typography>
        ),
      },
      {
        field: 'gstin',
        headerName: 'GSTIN',
        width: 180,
        renderCell: (params) =>
          params.row.gstin ? (
            <Typography
              sx={{ fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace', fontSize: 12 }}
            >
              {params.row.gstin}
            </Typography>
          ) : (
            <Chip label="Unregistered" size="small" sx={{ bgcolor: 'grey.100', color: 'text.disabled' }} />
          ),
      },
      {
        field: 'paymentTerms',
        headerName: 'Terms',
        width: 130,
        renderCell: (params) => (
          <Typography
            sx={{ fontSize: 13, color: params.row.paymentTerms ? 'text.primary' : 'text.disabled' }}
            noWrap
          >
            {params.row.paymentTerms || '—'}
          </Typography>
        ),
      },
      {
        field: 'openingBalance',
        headerName: 'Opening Bal.',
        width: 140,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography
            sx={{
              fontSize: 13,
              fontWeight: params.row.openingBalance > 0 ? 600 : 400,
              fontVariantNumeric: 'tabular-nums',
              color: params.row.openingBalance > 0 ? 'warning.dark' : 'text.disabled',
            }}
          >
            {params.row.openingBalance > 0 ? formatCurrency(params.row.openingBalance) : '—'}
          </Typography>
        ),
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
        width: 128,
        sortable: false,
        filterable: false,
        align: 'right',
        renderCell: (params) => (
          <Stack direction="row" spacing={0.25} sx={{ justifyContent: 'flex-end', width: '100%' }}>
            {/* The statement is where an argument about a balance gets settled, so it lives one
                click from the party, not buried under Accounts. */}
            <Tooltip title="Statement">
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation()
                  navigate(`/accounts/statements/${params.row.id}?type=supplier`)
                }}
              >
                <ReceiptLongOutlinedIcon sx={{ fontSize: 18 }} />
              </IconButton>
            </Tooltip>
            <Tooltip title="Edit">
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation()
                  setEditing(params.row)
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
    [navigate],
  )

  const handleToggleConfirm = async () => {
    if (!toggleTarget) return
    try {
      if (toggleTarget.isActive) {
        await deactivate.mutateAsync(toggleTarget.id)
        notify(`Supplier "${toggleTarget.name}" deactivated`)
      } else {
        await activate.mutateAsync(toggleTarget.id)
        notify(`Supplier "${toggleTarget.name}" activated`)
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
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'flex-start' }, mb: 2.5 }}
      >
        <Box>
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <Typography variant="h1">Suppliers</Typography>
            {!isLoading && (
              <Chip
                label={`${total} ${total === 1 ? 'supplier' : 'suppliers'}`}
                size="small"
                sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
              />
            )}
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Everyone you buy from — search by name, phone, GSTIN or contact.
          </Typography>
        </Box>

        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          Add Supplier
        </Button>
      </Stack>

      <TextField
        placeholder="Search name, phone, GSTIN, contact…"
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
        onRowClick={(row) => setEditing(row)}
        emptyTitle={isSearching ? 'No matching suppliers' : 'No suppliers yet'}
        emptyDescription={
          isSearching
            ? `Nothing matches “${debouncedSearch}”. Try a different name or phone number.`
            : 'Add your first supplier to start recording purchases against them.'
        }
        emptyAction={
          isSearching ? (
            <Button variant="outlined" size="small" onClick={() => setSearch('')}>
              Clear search
            </Button>
          ) : (
            <Button variant="contained" size="small" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Add Supplier
            </Button>
          )
        }
      />

      <CreateSupplierDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      {editing && <EditSupplierDialog supplier={editing} onClose={() => setEditing(null)} />}

      <ConfirmDialog
        open={!!toggleTarget}
        title={toggleTarget?.isActive ? 'Deactivate supplier?' : 'Activate supplier?'}
        description={
          toggleTarget?.isActive
            ? `"${toggleTarget?.name}" will be hidden from purchase search until reactivated.`
            : `"${toggleTarget?.name}" will become available again in purchase search.`
        }
        confirmLabel={toggleTarget?.isActive ? 'Deactivate' : 'Activate'}
        confirmColor={toggleTarget?.isActive ? 'error' : 'primary'}
        loading={deactivate.isPending || activate.isPending}
        onConfirm={handleToggleConfirm}
        onCancel={() => setToggleTarget(null)}
      />
    </Box>
  )
}
