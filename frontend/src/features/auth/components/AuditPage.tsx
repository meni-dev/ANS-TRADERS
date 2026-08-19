import {
  Box,
  Chip,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useDebouncedValue } from '@/lib/hooks/useDebouncedValue'
import { useAudit } from '../hooks'

/** The actions worth filtering by. The list is short on purpose — everything logged is unusual. */
const ACTIONS = [
  { value: '', label: 'Everything' },
  { value: 'Cancelled', label: 'Cancellations' },
  { value: 'StockAdjusted', label: 'Stock adjustments' },
  { value: 'BooksLocked', label: 'Books locked' },
  { value: 'BooksUnlocked', label: 'Books unlocked' },
  { value: 'UserCreated', label: 'People added' },
  { value: 'UserDeactivated', label: 'People switched off' },
  { value: 'PasswordChanged', label: 'Password changes' },
]

/** Audit rows carry a real timestamp, so unlike a document date the time of day matters. */
function formatMoment(value: string) {
  return new Date(value).toLocaleString('en-IN', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function AuditPage() {
  const [search, setSearch] = useState('')
  const [action, setAction] = useState('')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(50)

  const debouncedSearch = useDebouncedValue(search, 300)

  const { data, isLoading } = useAudit({
    search: debouncedSearch || undefined,
    action: action || undefined,
    page: page + 1,
    pageSize,
  })

  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary">
        Every cancellation, stock correction and lock change, with the name of whoever did it. Rows
        are written in the same transaction as the thing they describe, so nothing here can be
        removed without removing the document too.
      </Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          size="small"
          label="Search"
          placeholder="Bill number, part, person"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value)
            setPage(0)
          }}
          sx={{ minWidth: 260 }}
        />
        <TextField
          size="small"
          select
          label="Action"
          value={action}
          onChange={(event) => {
            setAction(event.target.value)
            setPage(0)
          }}
          sx={{ minWidth: 200 }}
        >
          {ACTIONS.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <Paper sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ whiteSpace: 'nowrap' }}>When</TableCell>
              <TableCell>Who</TableCell>
              <TableCell>What</TableCell>
              <TableCell>On</TableCell>
              <TableCell>Detail</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((row) => (
              <TableRow key={row.id}>
                <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatMoment(row.occurredAt)}</TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{row.userName}</TableCell>
                <TableCell>
                  <Chip size="small" label={row.actionLabel} />
                </TableCell>
                <TableCell>
                  {row.entityLabel ?? row.entityType}
                  {row.entityLabel && (
                    <Typography variant="caption" color="text.disabled" sx={{ display: 'block' }}>
                      {row.entityType}
                    </Typography>
                  )}
                </TableCell>
                <TableCell>{row.detail ?? '—'}</TableCell>
              </TableRow>
            ))}
            {!isLoading && !data?.items.length && (
              <TableRow>
                <TableCell colSpan={5}>
                  <Box sx={{ py: 4, textAlign: 'center' }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                      Nothing to show
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      An empty trail means nothing has been cancelled or corrected — that is the good
                      case, not a missing feature.
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
        <TablePagination
          component="div"
          count={data?.totalCount ?? 0}
          page={page}
          onPageChange={(_, next) => setPage(next)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value))
            setPage(0)
          }}
          rowsPerPageOptions={[25, 50, 100]}
        />
      </Paper>
    </Stack>
  )
}
