import { DataTable } from '@/components/data/DataTable'
import { formatCurrency, formatDate } from '@/lib/format'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import ClearIcon from '@mui/icons-material/Clear'
import SearchIcon from '@mui/icons-material/Search'
import { Chip, IconButton, InputAdornment, Stack, TextField, Typography } from '@mui/material'
import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid'
import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useCreditNotes, useDebitNotes } from '../hooks'
import type { CreditNoteListItemDto, DebitNoteListItemDto } from '../types'

type ReturnListPageProps = { side: 'sales' | 'purchase' }

/** One row shape for both sides, so the grid does not need two nearly identical column sets. */
type Row = {
  id: string
  number: string
  noteDate: string
  against: string
  partyName: string
  itemCount: number
  grandTotal: number
  refundedAmount: number
  status: string
}

export function ReturnListPage({ side }: ReturnListPageProps) {
  const navigate = useNavigate()
  const isSales = side === 'sales'

  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })

  const params = {
    search: debouncedSearch || undefined,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  }

  // Only the side being shown is fetched; the other hook stands by so the component keeps one
  // shape rather than branching its whole body.
  const creditNotes = useCreditNotes(params, isSales)
  const debitNotes = useDebitNotes(params, !isSales)
  const query = isSales ? creditNotes : debitNotes

  const rows: Row[] = useMemo(() => {
    if (isSales) {
      return ((creditNotes.data?.items ?? []) as CreditNoteListItemDto[]).map((n) => ({
        id: n.id,
        number: n.creditNoteNumber,
        noteDate: n.noteDate,
        against: n.invoiceNumber,
        partyName: n.customerName,
        itemCount: n.itemCount,
        grandTotal: n.grandTotal,
        refundedAmount: n.refundedAmount,
        status: n.status,
      }))
    }

    return ((debitNotes.data?.items ?? []) as DebitNoteListItemDto[]).map((n) => ({
      id: n.id,
      number: n.debitNoteNumber,
      noteDate: n.noteDate,
      against: n.purchaseNumber,
      partyName: n.supplierName,
      itemCount: n.itemCount,
      grandTotal: n.grandTotal,
      refundedAmount: n.refundedAmount,
      status: n.status,
    }))
  }, [isSales, creditNotes.data, debitNotes.data])

  const columns: GridColDef<Row>[] = useMemo(
    () => [
      {
        field: 'noteDate',
        headerName: 'Date',
        width: 118,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13 }}>{formatDate(params.row.noteDate)}</Typography>
        ),
      },
      {
        field: 'number',
        headerName: 'Note',
        width: 175,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, fontFamily: 'monospace' }}>{params.row.number}</Typography>
        ),
      },
      {
        field: 'against',
        headerName: 'Against',
        width: 175,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, fontFamily: 'monospace', color: 'text.secondary' }}>
            {params.row.against}
          </Typography>
        ),
      },
      {
        field: 'partyName',
        headerName: isSales ? 'Customer' : 'Supplier',
        flex: 1,
        minWidth: 170,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600 }} noWrap>
            {params.row.partyName}
          </Typography>
        ),
      },
      {
        field: 'grandTotal',
        headerName: 'Value',
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.grandTotal)}
          </Typography>
        ),
      },
      {
        field: 'refundedAmount',
        headerName: 'Paid back',
        width: 120,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) =>
          params.row.refundedAmount > 0 ? (
            <Typography sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
              {formatCurrency(params.row.refundedAmount)}
            </Typography>
          ) : (
            <Typography sx={{ fontSize: 13, color: 'text.disabled' }}>—</Typography>
          ),
      },
      {
        field: 'status',
        headerName: 'Status',
        width: 120,
        renderCell: (params) =>
          params.row.status === 'Cancelled' ? (
            <Chip size="small" variant="outlined" label="Cancelled" />
          ) : (
            <Chip size="small" variant="outlined" color="success" label="Issued" />
          ),
      },
    ],
    [isSales],
  )

  return (
    <Stack spacing={2.5}>
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {isSales ? 'Sales Returns' : 'Purchase Returns'}
        </Typography>
        <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
          {isSales
            ? 'Credit notes raised against bills — goods a customer brought back'
            : 'Debit notes raised against supplier bills — goods sent back'}
        </Typography>
      </Box>

      <TextField
        size="small"
        placeholder={isSales ? 'Note number, bill number or customer' : 'Note number, bill number or supplier'}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        sx={{ maxWidth: 380 }}
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

      <DataTable
        rows={rows}
        columns={columns}
        rowCount={query.data?.totalCount ?? 0}
        loading={query.isLoading || query.isFetching}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        onRowClick={(row) =>
          navigate(isSales ? `/billing/returns/${row.id}` : `/purchases/returns/${row.id}`)
        }
        emptyTitle="No returns yet"
        emptyDescription={
          isSales
            ? 'Open a bill and use Return items when a customer brings something back.'
            : 'Open a supplier bill and use Return items when goods go back.'
        }
      />
    </Stack>
  )
}
