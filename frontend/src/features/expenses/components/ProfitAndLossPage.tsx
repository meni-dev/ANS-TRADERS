import { StatTile } from '@/components/data/StatTile'
import { useAuth } from '@/features/auth/AuthProvider'
import { formatCurrency, formatDate } from '@/lib/format'
import AddIcon from '@mui/icons-material/Add'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import {
  Alert,
  Box,
  Button,
  Divider,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useProfitAndLoss } from '../hooks'
import { RecordExpenseDialog } from './RecordExpenseDialog'

function monthStart(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`
}

function today(): string {
  return new Date().toISOString().slice(0, 10)
}

/**
 * What the shop earned, and what it spent to earn it.
 * <p>
 * The coverage line under the gross figure is not a footnote. Cost is snapshotted on a sale line
 * from the day that was built, so anything sold before then contributes revenue with no cost
 * against it — and a gross profit drawn mostly from uncosted lines reads as a very good month when
 * it is really an unknown one.
 * </p>
 */
export function ProfitAndLossPage() {
  const { can } = useAuth()
  const [fromDate, setFromDate] = useState(monthStart())
  const [toDate, setToDate] = useState(today())
  const [recordOpen, setRecordOpen] = useState(false)

  const { data, isLoading } = useProfitAndLoss({ fromDate, toDate })

  return (
    <Stack spacing={2.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: { sm: 'center' } }}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            Profit &amp; Loss
          </Typography>
          <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
            What you sold, what the goods cost, and what it cost to keep the shop open
          </Typography>
        </Box>
        <TextField
          size="small"
          type="date"
          label="From"
          value={fromDate}
          onChange={(e) => setFromDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          size="small"
          type="date"
          label="To"
          value={toDate}
          onChange={(e) => setToDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        {can('ExpenseRecord') && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setRecordOpen(true)}>
            Record spend
          </Button>
        )}
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(4, 1fr)' },
        }}
      >
        <StatTile label="Revenue" value={formatCurrency(data?.revenue ?? 0)} caption="Taxable value, GST excluded" loading={isLoading} />
        <StatTile label="Cost of goods" value={formatCurrency(data?.costOfGoods ?? 0)} loading={isLoading} />
        <StatTile
          label="Gross profit"
          value={formatCurrency(data?.grossProfit ?? 0)}
          caption={
            data && !data.isComplete
              ? `Cost known on ${data.costCoveragePercent}% of lines`
              : undefined
          }
          tone={data && !data.isComplete ? 'warning' : 'default'}
          loading={isLoading}
        />
        <StatTile
          label="Net profit"
          value={formatCurrency(data?.netProfit ?? 0)}
          tone={data && data.netProfit < 0 ? 'error' : 'success'}
          loading={isLoading}
        />
      </Box>

      {data && !data.isComplete ? (
        <Alert severity="warning" icon={<InfoOutlinedIcon fontSize="small" />}>
          <strong>{data.uncostedLines}</strong> of {data.costedLines + data.uncostedLines} sale lines
          in this period were billed before the shop started recording what its goods cost, so they
          contribute revenue with nothing against it. The gross and net figures above are that much
          too high. Bills raised from now on carry their cost.
        </Alert>
      ) : null}

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        <Typography sx={{ fontWeight: 700, mb: 1.5 }}>
          {formatDate(fromDate)} to {formatDate(toDate)}
        </Typography>

        <Line label="Revenue" value={data?.revenue ?? 0} />
        <Line label="Less cost of goods" value={-(data?.costOfGoods ?? 0)} />
        <Divider sx={{ my: 1 }} />
        <Line label="Gross profit" value={data?.grossProfit ?? 0} bold />

        <Box sx={{ mt: 2.5 }}>
          <Typography sx={{ fontSize: 12, letterSpacing: 0.6, textTransform: 'uppercase', color: 'text.secondary', mb: 0.5 }}>
            Running the shop
          </Typography>
          {(data?.expensesByCategory ?? []).map((c) => (
            <Line key={c.category} label={c.categoryLabel} value={-c.amount} muted />
          ))}
          {(data?.expensesByCategory.length ?? 0) === 0 ? (
            <Typography sx={{ fontSize: 13.5, color: 'text.secondary', py: 1 }}>
              Nothing recorded in this period. Rent, salary and electricity all belong here — without
              them the figure below is not profit.
            </Typography>
          ) : null}
          <Line label="Total expenses" value={-(data?.expenses ?? 0)} />
        </Box>

        <Divider sx={{ my: 1.5, borderColor: 'text.primary' }} />
        <Line label="Net profit" value={data?.netProfit ?? 0} bold large />
      </Paper>

      {recordOpen ? <RecordExpenseDialog onClose={() => setRecordOpen(false)} /> : null}
    </Stack>
  )
}

function Line({
  label,
  value,
  bold,
  muted,
  large,
}: {
  label: string
  value: number
  bold?: boolean
  muted?: boolean
  large?: boolean
}) {
  return (
    <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', py: 0.6 }}>
      <Typography
        sx={{
          fontSize: large ? 16 : 14.5,
          fontWeight: bold ? 700 : 400,
          color: muted ? 'text.secondary' : 'text.primary',
          pl: muted ? 1.5 : 0,
        }}
      >
        {label}
      </Typography>
      <Typography
        sx={{
          fontSize: large ? 16 : 14.5,
          fontWeight: bold ? 700 : 400,
          fontVariantNumeric: 'tabular-nums',
          color: value < 0 && bold ? 'error.main' : muted ? 'text.secondary' : 'text.primary',
        }}
      >
        {formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
