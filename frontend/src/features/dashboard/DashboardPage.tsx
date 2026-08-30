import { StatTile } from '@/components/data/StatTile'
import AccountBalanceWalletOutlinedIcon from '@mui/icons-material/AccountBalanceWalletOutlined'
import CallMadeIcon from '@mui/icons-material/CallMade'
import CallReceivedIcon from '@mui/icons-material/CallReceived'
import ChevronRightIcon from '@mui/icons-material/ChevronRight'
import PointOfSaleOutlinedIcon from '@mui/icons-material/PointOfSaleOutlined'
import ShowChartOutlinedIcon from '@mui/icons-material/ShowChartOutlined'
import { usePaymentSummary } from '@/features/payments/hooks'
import { formatCurrency, todayIso } from '@/lib/format'
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward'
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward'
import { Alert, Box, Grid, Paper, Stack, Typography } from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { AuditPanel } from './components/AuditPanel'
import { GstPanel } from './components/GstPanel'
import { RecentInvoicesPanel, ReorderPanel, TopSellersPanel } from './components/DashboardLists'
import { QuickActionsBar } from './components/QuickActionsBar'
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

/**
 * Signed, in a tinted pill so the direction reads before the number does.
 * <p>
 * Sales falling is the thing worth noticing, so a fall is red rather than green — the pill grades
 * the movement, which is the one place on this screen where colour is allowed to mean something.
 * </p>
 */
function MonthDelta({ changePercent }: { changePercent: number | null }) {
  if (changePercent === null) {
    return <Box component="span">no sales last month</Box>
  }

  const up = changePercent >= 0

  return (
    <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
      <Stack
        direction="row"
        spacing={0.125}
        sx={{
          alignItems: 'center',
          px: 0.625,
          py: 0.125,
          borderRadius: '5px',
          bgcolor: up ? 'success.light' : 'error.light',
          color: up ? 'success.dark' : 'error.dark',
          fontWeight: 700,
          fontSize: 11.5,
        }}
      >
        {up ? (
          <ArrowUpwardIcon sx={{ fontSize: 13 }} />
        ) : (
          <ArrowDownwardIcon sx={{ fontSize: 13 }} />
        )}
        {Math.abs(changePercent)}%
      </Stack>
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
    <Stack spacing={2}>
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={2}
        sx={{ alignItems: { md: 'center' }, justifyContent: 'space-between' }}
      >
        <Box>
          <Typography variant="h1">Dashboard</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25 }}>
            Where the shop stands today — trade, money owed, GST and the document checks.
          </Typography>
        </Box>
        <QuickActionsBar />
      </Stack>

      {/* Every tile links through to the screen that explains it. A dashboard figure with no way
          in is a dead end. */}
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <StatTile
            label="Today's sales"
            icon={<PointOfSaleOutlinedIcon />}
            iconTone="blue"
            tinted
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
            icon={<ShowChartOutlinedIcon />}
            iconTone="violet"
            tinted
            value={formatCurrency(monthData?.salesTotal ?? 0)}
            caption={<MonthDelta changePercent={monthData?.changePercent ?? null} />}
            loading={isLoading}
            onClick={() => navigate('/billing')}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <StatTile
            label="Receivables"
            icon={<CallReceivedIcon />}
            iconTone="amber"
            tinted
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
            icon={<CallMadeIcon />}
            iconTone="rose"
            tinted
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
          sx={{
            p: 1.5,
            borderRadius: '10px',
            cursor: 'pointer',
            display: 'flex',
            gap: 1.5,
            alignItems: 'center',
            borderColor: '#F8E4C4',
            bgcolor: '#FFFDF8',
            transition: 'background-color 120ms ease',
            '&:hover': { bgcolor: '#FDF3E4' },
          }}
        >
          <Box
            sx={{
              width: 30,
              height: 30,
              flexShrink: 0,
              borderRadius: '8px',
              display: 'grid',
              placeItems: 'center',
              bgcolor: '#FDF3E4',
              color: 'warning.dark',
            }}
          >
            <AccountBalanceWalletOutlinedIcon sx={{ fontSize: 17 }} />
          </Box>
          <Typography sx={{ fontSize: 13.5, flexGrow: 1 }}>
            <strong>{cheques.data.chequesInHandCount} cheque(s)</strong> worth{' '}
            {formatCurrency(cheques.data.chequesInHand)} are still in hand — open the register to
            bank or clear them.
          </Typography>
          <ChevronRightIcon sx={{ fontSize: 19, color: 'text.disabled', flexShrink: 0 }} />
        </Paper>
      ) : null}

      {money && money.advancesHeld > 0 ? (
        <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
          {formatCurrency(money.advancesHeld)} held on account against no bill — shown separately,
          because one customer's advance does not settle another customer's debt.
        </Typography>
      ) : null}

      {/* One grid for every module below the tiles. Panels that share a row are the same height
          because the shell stretches, not because their contents happen to match. */}
      <Grid container spacing={2}>
        <Grid size={12}>
          <SalesTrendChart points={data?.salesTrend ?? []} loading={isLoading} />
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          {data?.gst && <GstPanel gst={data.gst} monthLabel={month} />}
        </Grid>
        <Grid size={{ xs: 12, lg: 5 }}>
          {data && <AuditPanel audit={data.audit} monthLabel={month} />}
        </Grid>

        <Grid size={{ xs: 12, md: 4 }}>
          <ReorderPanel
            items={data?.reorder ?? []}
            onOpen={() => navigate('/inventory/low-stock')}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <TopSellersPanel items={data?.topSellers ?? []} onOpen={() => navigate('/billing/new')} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <RecentInvoicesPanel
            items={data?.recentInvoices ?? []}
            onOpen={() => navigate('/billing')}
            onOpenInvoice={(id) => navigate(`/billing/${id}`)}
          />
        </Grid>
      </Grid>
    </Stack>
  )
}
