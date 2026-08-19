import { StatTile } from '@/components/data/StatTile'
import AccountBalanceWalletOutlinedIcon from '@mui/icons-material/AccountBalanceWalletOutlined'
import { usePaymentSummary } from '@/features/payments/hooks'
import { formatCurrency, todayIso } from '@/lib/format'
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward'
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward'
import { Alert, Box, Grid, Paper, Stack, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { AuditPanel } from './components/AuditPanel'
import { GstPanel } from './components/GstPanel'
import { RecentInvoicesPanel, ReorderPanel, TopSellersPanel } from './components/DashboardLists'
import { SalesTrendChart } from './components/SalesTrendChart'
import { useDashboard } from './hooks'

/** `1 customer` / `4 customers`. Every count on this screen can legitimately be one. */
function plural(count: number, noun: string): string {
  return `${count} ${noun}${count === 1 ? '' : 's'}`
}

/** `2026-08-17` → `August 2026`, for the panels scoped to the current month. */
function monthLabel(isoDate: string): string {
  const [year, month] = isoDate.split('-').map(Number)
  return new Date(year, month - 1, 1).toLocaleDateString('en-IN', { month: 'long', year: 'numeric' })
}

/** Signed, coloured by direction — sales falling is the thing worth noticing, so it is not green. */
function MonthDelta({ changePercent }: { changePercent: number | null }) {
  if (changePercent === null) {
    return <>no sales last month</>
  }

  const up = changePercent >= 0

  return (
    <Stack direction="row" spacing={0.25} sx={{ alignItems: 'center' }}>
      <Box sx={{ display: 'flex', color: up ? 'success.dark' : 'error.dark' }}>
        {up ? (
          <ArrowUpwardIcon sx={{ fontSize: 14 }} />
        ) : (
          <ArrowDownwardIcon sx={{ fontSize: 14 }} />
        )}
      </Box>
      <Box component="span" sx={{ color: up ? 'success.dark' : 'error.dark', fontWeight: 600 }}>
        {Math.abs(changePercent)}%
      </Box>
      <Box component="span">vs last month</Box>
    </Stack>
  )
}

export function DashboardPage() {
  const navigate = useNavigate()
  const cheques = usePaymentSummary({})

  // The date is settled once per render from the browser, so "today" is the shop's today.
  const asOf = todayIso()
  const { data, isLoading, isError } = useDashboard(asOf)

  const month = monthLabel(asOf)

  if (isError) {
    return (
      <Alert severity="error">
        The dashboard could not be loaded. Check that the API is running and try again.
      </Alert>
    )
  }

  const today = data?.today
  const monthData = data?.month
  const money = data?.money

  return (
    <Box>
      <Box sx={{ mb: 2.5 }}>
        <Typography variant="h1">Dashboard</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          Where the shop stands today — trade, money owed, GST and the document checks.
        </Typography>
      </Box>

      {/* Every tile links through to the screen that explains it. A dashboard figure with no way
          in is a dead end. */}
      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <StatTile
            label="Today's sales"
            value={formatCurrency(today?.salesTotal ?? 0)}
            caption={plural(today?.invoiceCount ?? 0, 'invoice')}
            tone="primary"
            loading={isLoading}
            onClick={() => navigate('/billing')}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <StatTile
            label={`This month · ${month.split(' ')[0]}`}
            value={formatCurrency(monthData?.salesTotal ?? 0)}
            caption={<MonthDelta changePercent={monthData?.changePercent ?? null} />}
            loading={isLoading}
            onClick={() => navigate('/billing')}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <StatTile
            label="Receivables"
            value={formatCurrency(money?.receivable ?? 0)}
            caption={
              money && money.receivableOver60 > 0
                ? `${formatCurrency(money.receivableOver60)} over 60 days past due`
                : money && money.receivableNotDue > 0
                  ? `${formatCurrency(money.receivableNotDue)} not due yet · ${plural(
                      money.customersWithDues,
                      'customer',
                    )}`
                  : `${money?.receivableInvoiceCount ?? 0} unpaid · ${plural(
                      money?.customersWithDues ?? 0,
                      'customer',
                    )}`
            }
            tone={money && money.receivableOver60 > 0 ? 'warning' : 'default'}
            loading={isLoading}
            // Straight to the outstanding filter, not the whole list — the tile is about what is unpaid.
            onClick={() => navigate('/billing?unpaid=1')}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <StatTile
            label="Payables"
            value={formatCurrency(money?.payable ?? 0)}
            caption={`${plural(money?.payableBillCount ?? 0, 'unpaid bill')} · ${plural(
              money?.suppliersWithDues ?? 0,
              'supplier',
            )}`}
            loading={isLoading}
            onClick={() => navigate('/purchases')}
          />
        </Grid>
      </Grid>

      {/* Without a link from here, post-dated cheques quietly rot in a tab nobody opens. This is
          the whole reason the module needs no scheduled job. */}
      {cheques.data && cheques.data.chequesInHandCount > 0 ? (
        <Paper
          variant="outlined"
          onClick={() => navigate('/accounts/cheques')}
          sx={{ p: 1.75, cursor: 'pointer', display: 'flex', gap: 1.25, alignItems: 'center' }}
        >
          <AccountBalanceWalletOutlinedIcon sx={{ fontSize: 20, color: 'warning.dark' }} />
          <Typography sx={{ fontSize: 13.5 }}>
            <strong>{cheques.data.chequesInHandCount} cheque(s)</strong> worth{' '}
            {formatCurrency(cheques.data.chequesInHand)} are still in hand — open the register to
            bank or clear them.
          </Typography>
        </Paper>
      ) : null}

      {money && money.advancesHeld > 0 ? (
        <Typography sx={{ fontSize: 12.5, color: 'text.secondary', mt: -1 }}>
          {formatCurrency(money.advancesHeld)} held on account against no bill — shown separately,
          because one customer's advance does not settle another customer's debt.
        </Typography>
      ) : null}

      <Box sx={{ mb: 2 }}>
        <SalesTrendChart points={data?.salesTrend ?? []} loading={isLoading} />
      </Box>

      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid size={{ xs: 12, lg: 7 }}>
          {data?.gst && <GstPanel gst={data.gst} monthLabel={month} />}
        </Grid>
        <Grid size={{ xs: 12, lg: 5 }}>
          {data && <AuditPanel audit={data.audit} monthLabel={month} />}
        </Grid>
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 4 }}>
          <ReorderPanel
            items={data?.reorder ?? []}
            onOpen={() => navigate('/inventory/low-stock')}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <TopSellersPanel
            items={data?.topSellers ?? []}
            onOpen={() => navigate('/billing/new')}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <RecentInvoicesPanel
            items={data?.recentInvoices ?? []}
            onOpen={() => navigate('/billing')}
            onOpenInvoice={(id) => navigate(`/billing/${id}`)}
          />
        </Grid>
      </Grid>
    </Box>
  )
}
