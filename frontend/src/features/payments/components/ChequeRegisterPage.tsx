import { describeError } from '@/lib/api/errors'
import { DataTable } from '@/components/data/DataTable'
import { formatCurrency, formatDate, todayIso } from '@/lib/format'
import { Alert, Button, Chip, Stack, Tab, Tabs, Typography } from '@mui/material'
import type { GridColDef, GridPaginationModel } from '@mui/x-data-grid'
import { useMemo, useState } from 'react'
import { BounceChequeDialog } from './BounceChequeDialog'
import { useCheques, useMoveCheque } from '../hooks'
import type { ChequeStatus, PaymentListItemDto } from '../types'

/**
 * The register is a to-do list before it is a history, so the tabs are ordered by what needs doing.
 * "Due for banking" is the one that replaces a scheduled job: the shop has to physically take the
 * cheque to the bank anyway, so the human action already exists and only needs a list.
 */
const TABS = [
  {
    key: 'due',
    label: 'Due for banking',
    status: 'Pending' as ChequeStatus,
    bankableOnly: true,
    blurb: 'Dated today or earlier and still in the drawer.',
  },
  {
    key: 'pdc',
    label: 'In hand (post-dated)',
    status: 'Pending' as ChequeStatus,
    bankableOnly: false,
    blurb: 'Taken, but not bankable yet. These have settled nothing.',
  },
  {
    key: 'clearing',
    label: 'In clearing',
    status: 'Deposited' as ChequeStatus,
    blurb: 'With the bank. Anything sitting here over a week is worth chasing.',
  },
  { key: 'cleared', label: 'Cleared', status: 'Cleared' as ChequeStatus, blurb: 'Paid by the bank.' },
  {
    key: 'bounced',
    label: 'Bounced',
    status: 'Bounced' as ChequeStatus,
    blurb: 'Returned unpaid. The bills they had settled are open again.',
  },
]

const CHEQUE_STATUS_COLOURS: Record<ChequeStatus, 'default' | 'info' | 'success' | 'error' | 'warning'> = {
  Pending: 'warning',
  Deposited: 'info',
  Cleared: 'success',
  Bounced: 'error',
  Cancelled: 'default',
}

export function ChequeRegisterPage() {
  const [tabIndex, setTabIndex] = useState(0)
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize: 20 })
  const [bouncing, setBouncing] = useState<PaymentListItemDto | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const tab = TABS[tabIndex]
  const today = todayIso()

  const { data, isLoading, isFetching } = useCheques({
    status: tab.status,
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
  })

  const moveCheque = useMoveCheque()

  // The two Pending tabs split on a date the server does not filter by, because "bankable" is a
  // question about today and would make the response uncacheable for the sake of one comparison.
  const rows = useMemo(() => {
    const items = data?.items ?? []
    if (tab.bankableOnly === undefined) return items
    return items.filter((row) =>
      tab.bankableOnly ? (row.chequeDate ?? '') <= today : (row.chequeDate ?? '') > today,
    )
  }, [data?.items, tab.bankableOnly, today])

  async function move(row: PaymentListItemDto, action: 'deposit' | 'clear' | 'post' | 'cancel') {
    setActionError(null)
    try {
      await moveCheque.mutateAsync({ paymentId: row.id, action, onDate: today })
    } catch (error) {
      setActionError(describeError(error, 'Could not update this cheque'))
    }
  }

  const columns: GridColDef<PaymentListItemDto>[] = useMemo(
    () => [
      {
        field: 'chequeDate',
        headerName: 'Cheque date',
        width: 130,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13 }}>{formatDate(params.row.chequeDate)}</Typography>
        ),
      },
      {
        field: 'partyName',
        headerName: 'Party',
        flex: 1,
        minWidth: 170,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600 }} noWrap>
            {params.row.partyName}
          </Typography>
        ),
      },
      {
        field: 'chequeNumber',
        headerName: 'Cheque no.',
        width: 130,
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13, fontFamily: 'monospace' }}>
            {params.row.chequeNumber ?? '—'}
          </Typography>
        ),
      },
      {
        field: 'amount',
        headerName: 'Amount',
        width: 130,
        align: 'right',
        headerAlign: 'right',
        renderCell: (params) => (
          <Typography sx={{ fontSize: 13.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(params.row.amount)}
          </Typography>
        ),
      },
      {
        field: 'chequeStatus',
        headerName: 'Status',
        width: 120,
        renderCell: (params) =>
          params.row.chequeStatus ? (
            <Chip
              size="small"
              variant="outlined"
              color={CHEQUE_STATUS_COLOURS[params.row.chequeStatus]}
              label={params.row.chequeStatus}
            />
          ) : null,
      },
      {
        field: 'actions',
        headerName: '',
        width: 300,
        sortable: false,
        renderCell: (params) => {
          const row = params.row
          const status = row.chequeStatus
          if (!status || status === 'Cleared' || status === 'Bounced' || status === 'Cancelled') {
            return null
          }

          const notBankableYet = (row.chequeDate ?? '') > today

          return (
            <Stack direction="row" spacing={0.75}>
              {status === 'Pending' ? (
                <Button
                  size="small"
                  variant="outlined"
                  disabled={notBankableYet || moveCheque.isPending}
                  // Banking a post-dated cheque is what posts it, so the same button does both.
                  onClick={() => move(row, row.status === 'Pending' ? 'post' : 'deposit')}
                >
                  Bank it
                </Button>
              ) : null}
              {status === 'Deposited' ? (
                <Button
                  size="small"
                  variant="outlined"
                  disabled={moveCheque.isPending}
                  onClick={() => move(row, 'clear')}
                >
                  Cleared
                </Button>
              ) : null}
              <Button
                size="small"
                variant="outlined"
                color="error"
                disabled={moveCheque.isPending}
                onClick={() => setBouncing(row)}
              >
                Bounced
              </Button>
              {status === 'Pending' ? (
                <Button size="small" disabled={moveCheque.isPending} onClick={() => move(row, 'cancel')}>
                  Returned
                </Button>
              ) : null}
            </Stack>
          )
        },
      },
    ],
    // `move` closes over today and the mutation, both stable for a render; listing it would only
    // rebuild the columns on every mutation state change.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [today, moveCheque.isPending],
  )

  return (
    <Stack spacing={2.5}>
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          Cheque register
        </Typography>
        <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
          Every cheque taken, from the drawer to the bank and back
        </Typography>
      </Box>

      {actionError ? <Alert severity="error">{actionError}</Alert> : null}

      <Tabs
        value={tabIndex}
        onChange={(_, next) => {
          setTabIndex(next)
          setPaginationModel((model) => ({ ...model, page: 0 }))
        }}
        variant="scrollable"
        scrollButtons="auto"
      >
        {TABS.map((item) => (
          <Tab key={item.key} label={item.label} />
        ))}
      </Tabs>

      <Typography sx={{ fontSize: 13, color: 'text.secondary', mt: -1 }}>{tab.blurb}</Typography>

      <DataTable
        rows={rows}
        columns={columns}
        rowCount={tab.bankableOnly === undefined ? (data?.totalCount ?? 0) : rows.length}
        loading={isLoading || isFetching}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        emptyTitle="Nothing here"
        emptyDescription="No cheques in this state right now."
      />

      <BounceChequeDialog cheque={bouncing} onClose={() => setBouncing(null)} />
    </Stack>
  )
}
