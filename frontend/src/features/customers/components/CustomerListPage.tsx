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
import { useActivateCustomer, useCustomers, useDeactivateCustomer } from '../hooks'
import type { CustomerDto } from '../types'
import { CreateCustomerDialog } from './CreateCustomerDialog'
import { EditCustomerDialog } from './EditCustomerDialog'

export function CustomerListPage() {
  const navigate = useNavigate()
  const { notify } = useNotification()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [createOpen, setCreateOpen] = useState(false)
  const [editing, setEditing] = useState<CustomerDto | null>(null)
  const [toggleTarget, setToggleTarget] = useState<CustomerDto | null>(null)

  const { data, isLoading, isFetching } = useCustomers({
    search: debouncedSearch || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const deactivate = useDeactivateCustomer()
  const activate = useActivateCustomer()

  const columns: GridColDef<CustomerDto>[] = useMemo(
    () => [
      {
        field: 'name',
        headerName: 'Customer',
        flex: 1,
        minWidth: 220,
        // Name and locality read as one unit, so they share a cell rather than costing a column.
        renderCell: (params) => {
          const place = [params.row.city, params.row.state].filter(Boolean).join(', ')
          return (
            <Box sx={{ minWidth: 0 }}>
              <Typography sx={{ fontSize: 13.5, fontWeight: 600, lineHeight: 1.4 }} noWrap>
                {params.row.name}
              </Typography>
              <Typography sx={{ fontSize: 12, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
                {place || 'No address'}
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
        // An unregistered customer is a normal case, not missing data, so it gets a label
        // rather than an em dash.
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
        field: 'outstandingBalance',
        headerName: 'Outstanding',
        width: 140,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => {
          const balance = params.row.outstandingBalance
          return (
            <Typography
              sx={{
                fontSize: 13,
                fontWeight: balance > 0 ? 600 : 400,
                fontVariantNumeric: 'tabular-nums',
                color: balance > 0 ? 'text.primary' : 'text.disabled',
              }}
            >
              {/* Negative means they are in credit — money the shop is holding, not owed. */}
              {balance === 0
                ? '—'
                : balance > 0
                  ? formatCurrency(balance)
                  : `${formatCurrency(-balance)} advance`}
            </Typography>
          )
        },
      },
      {
        field: 'creditLimit',
        headerName: 'Credit Limit',
        width: 140,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography
            sx={{
              fontSize: 13,
              fontVariantNumeric: 'tabular-nums',
              color: params.row.creditLimit > 0 ? 'text.primary' : 'text.disabled',
            }}
          >
            {params.row.creditLimit > 0 ? formatCurrency(params.row.creditLimit) : '—'}
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
                  navigate(`/accounts/statements/${params.row.id}`)
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
        notify(`Customer "${toggleTarget.name}" deactivated`)
      } else {
        await activate.mutateAsync(toggleTarget.id)
        notify(`Customer "${toggleTarget.name}" activated`)
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
            <Typography variant="h1">Customers</Typography>
            {!isLoading && (
              <Chip
                label={`${total} ${total === 1 ? 'customer' : 'customers'}`}
                size="small"
                sx={{ bgcolor: 'grey.100', color: 'text.secondary' }}
              />
            )}
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Everyone you bill — search by name, phone, GSTIN or city.
          </Typography>
        </Box>

        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
          Add Customer
        </Button>
      </Stack>

      <TextField
        placeholder="Search name, phone, GSTIN, city…"
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
        emptyTitle={isSearching ? 'No matching customers' : 'No customers yet'}
        emptyDescription={
          isSearching
            ? `Nothing matches “${debouncedSearch}”. Try a different name or phone number.`
            : 'Add your first customer to start raising invoices against them.'
        }
        emptyAction={
          isSearching ? (
            <Button variant="outlined" size="small" onClick={() => setSearch('')}>
              Clear search
            </Button>
          ) : (
            <Button variant="contained" size="small" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Add Customer
            </Button>
          )
        }
      />

      <CreateCustomerDialog open={createOpen} onClose={() => setCreateOpen(false)} />

      {editing && <EditCustomerDialog customer={editing} onClose={() => setEditing(null)} />}

      <ConfirmDialog
        open={!!toggleTarget}
        title={toggleTarget?.isActive ? 'Deactivate customer?' : 'Activate customer?'}
        description={
          toggleTarget?.isActive
            ? `"${toggleTarget?.name}" will be hidden from billing search until reactivated.`
            : `"${toggleTarget?.name}" will become available again in billing search.`
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
