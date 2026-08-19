import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  ListSubheader,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import { downloadCsv, toCsv } from '@/lib/csv'
import { formatCurrency, formatDate, todayIso } from '@/lib/format'
import { useRegister, useRegisterList } from '../hooks'
import type { Register, RegisterColumn } from '../types'

/** The financial year Apr–Mar that `today` falls in, which is the range a register usually wants. */
function financialYear(today: string) {
  const [year, month] = today.split('-').map(Number)
  const startYear = month >= 4 ? year : year - 1
  return { fromDate: `${startYear}-04-01`, toDate: `${startYear + 1}-03-31` }
}

/** The month `today` falls in — the other range anybody actually asks for. */
function thisMonth(today: string) {
  const [year, month] = today.split('-').map(Number)
  const lastDay = new Date(year, month, 0).getDate()
  const mm = `${month}`.padStart(2, '0')
  return { fromDate: `${year}-${mm}-01`, toDate: `${year}-${mm}-${lastDay}` }
}

const RIGHT_ALIGNED: RegisterColumn['type'][] = ['Money', 'Quantity', 'Number']

function renderCell(value: string | null, type: RegisterColumn['type']) {
  if (value === null || value === '') return '—'
  if (type === 'Date') return formatDate(value)
  if (type === 'Money') return formatCurrency(Number(value))
  // Quantity and Number keep the figure the server sent. A GST rate is 18, not ₹18.00, and a
  // quantity of 2.5 litres must not be dressed up as currency.
  return value
}

function RegisterTable({ register }: { register: Register }) {
  const totalsByColumn = useMemo(
    () => new Map(register.totals.map((total) => [total.columnKey, total.value])),
    [register.totals],
  )

  return (
    // A register runs to hundreds of rows and every column is a number somebody is reading across.
    // Scrolling the headings away turns "₹3,120.00" into a figure nobody can name, so the table
    // scrolls inside its own box with the headings and the totals pinned to its edges.
    <Paper sx={{ overflow: 'auto', maxHeight: 'calc(100vh - 240px)' }}>
      <Table size="small" stickyHeader sx={{ minWidth: 700 }}>
        <TableHead>
          <TableRow>
            {register.columns.map((column) => (
              <TableCell
                key={column.key}
                align={RIGHT_ALIGNED.includes(column.type) ? 'right' : 'left'}
                sx={{ whiteSpace: 'nowrap' }}
              >
                {column.label}
              </TableCell>
            ))}
          </TableRow>
        </TableHead>
        <TableBody>
          {register.rows.map((row, rowIndex) => (
            // Registers have no stable row id — a GST summary line is a grouping, not a record —
            // and the order is fixed by the server, so the index is the honest key here.
            <TableRow key={rowIndex} hover>
              {register.columns.map((column, cellIndex) => (
                <TableCell
                  key={column.key}
                  align={RIGHT_ALIGNED.includes(column.type) ? 'right' : 'left'}
                  sx={{
                    whiteSpace: 'nowrap',
                    fontVariantNumeric: RIGHT_ALIGNED.includes(column.type) ? 'tabular-nums' : undefined,
                  }}
                >
                  {renderCell(row[cellIndex], column.type)}
                </TableCell>
              ))}
            </TableRow>
          ))}
          {register.rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={register.columns.length}>
                <Box sx={{ py: 4, textAlign: 'center' }}>
                  <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                    Nothing in this range
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Widen the dates, or pick another register.
                  </Typography>
                </Box>
              </TableCell>
            </TableRow>
          )}
        </TableBody>
        {register.totals.length > 0 && register.rows.length > 0 && (
          <TableFooter>
            <TableRow>
              {register.columns.map((column, index) => (
                <TableCell
                  key={column.key}
                  align={RIGHT_ALIGNED.includes(column.type) ? 'right' : 'left'}
                  sx={{
                    fontWeight: 700,
                    color: 'text.primary',
                    fontSize: 13,
                    whiteSpace: 'nowrap',
                    fontVariantNumeric: 'tabular-nums',
                    position: 'sticky',
                    bottom: 0,
                    bgcolor: 'background.paper',
                    borderTop: '1px solid',
                    borderColor: 'divider',
                  }}
                >
                  {index === 0
                    ? 'Total'
                    : totalsByColumn.has(column.key)
                      ? renderCell(String(totalsByColumn.get(column.key)), column.type)
                      : ''}
                </TableCell>
              ))}
            </TableRow>
          </TableFooter>
        )}
      </Table>
    </Paper>
  )
}

export function RegistersPage() {
  const today = todayIso()
  const [range, setRange] = useState(() => thisMonth(today))
  const [selected, setSelected] = useState('sales')

  const { data: registers } = useRegisterList()
  const { data: register, isFetching } = useRegister(selected, range.fromDate, range.toDate)

  // Read off the picker rather than off the loaded register, so the controls do not flicker between
  // the two shapes while the next one is still on its way.
  const asAt = registers?.find((summary) => summary.key === selected)?.isAsAt ?? false

  const groups = useMemo(() => {
    const byGroup = new Map<string, typeof registers>()
    for (const summary of registers ?? []) {
      byGroup.set(summary.group, [...(byGroup.get(summary.group) ?? []), summary])
    }
    return [...byGroup.entries()]
  }, [registers])

  function exportCsv() {
    if (!register) return

    // The exported file carries the raw values the server sent, not what the screen shows. A CA
    // opening it needs 1234.50 in a cell they can sum, not "₹1,234.50" as text.
    const rows: (string | number)[][] = [
      [register.title],
      [register.isAsAt ? `As at ${register.toDate}` : `${register.fromDate} to ${register.toDate}`],
      [],
      register.columns.map((column) => column.label),
      ...register.rows.map((row) => row.map((cell) => cell ?? '')),
    ]

    if (register.totals.length > 0) {
      const totalsByColumn = new Map(register.totals.map((t) => [t.columnKey, t.value]))
      rows.push(
        register.columns.map((column, index) => {
          if (index === 0) return 'Total'

          const total = totalsByColumn.get(column.key)
          if (total === undefined) return ''

          // Written to the same precision as the rows above it. The server sends the rows as
          // "3700.00" and the total arrives as a number, so left alone the file ends with a column
          // of 3700.00 under a total of 43830 — the same figures, in two shapes.
          return column.type === 'Money' ? total.toFixed(2) : String(total)
        }),
      )
    }

    downloadCsv(
      register.isAsAt
        ? `${register.key}-as-at-${register.toDate}.csv`
        : `${register.key}-${register.fromDate}-to-${register.toDate}.csv`,
      toCsv(rows),
    )
  }

  return (
    <Stack spacing={2}>
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        sx={{ alignItems: { md: 'center' } }}
      >
        <TextField
          select
          size="small"
          label="Register"
          value={selected}
          onChange={(event) => setSelected(event.target.value)}
          sx={{ minWidth: 280 }}
        >
          {groups.flatMap(([group, items]) => [
            <ListSubheader key={group}>{group}</ListSubheader>,
            ...(items ?? []).map((summary) => (
              <MenuItem key={summary.key} value={summary.key}>
                {summary.title}
              </MenuItem>
            )),
          ])}
        </TextField>

        {/* Stock has one current level and a party has one current balance, so a date range would
            be a control that changes nothing. Better to take it away than to leave it there
            answering the same figures whatever it is set to. */}
        {asAt ? (
          <Typography variant="body2" color="text.secondary">
            As at {formatDate(today)}
          </Typography>
        ) : (
          <>
            <TextField
              size="small"
              type="date"
              label="From"
              value={range.fromDate}
              onChange={(event) => setRange({ ...range, fromDate: event.target.value })}
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              size="small"
              type="date"
              label="To"
              value={range.toDate}
              onChange={(event) => setRange({ ...range, toDate: event.target.value })}
              slotProps={{ inputLabel: { shrink: true } }}
            />

            <Button size="small" onClick={() => setRange(thisMonth(today))}>
              This month
            </Button>
            <Button size="small" onClick={() => setRange(financialYear(today))}>
              This year
            </Button>
          </>
        )}

        <Box sx={{ flexGrow: 1 }} />

        <Button
          variant="contained"
          startIcon={<DownloadOutlinedIcon />}
          onClick={exportCsv}
          disabled={!register || register.rowCount === 0}
        >
          Download
        </Button>
      </Stack>

      {register && (
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            {register.caption}
          </Typography>
          <Chip size="small" label={`${register.rowCount} rows`} />
          {isFetching && <CircularProgress size={14} />}
        </Stack>
      )}

      {register && <RegisterTable register={register} />}
    </Stack>
  )
}
