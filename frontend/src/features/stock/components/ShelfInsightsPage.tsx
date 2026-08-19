import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Grid,
  MenuItem,
  Paper,
  Stack,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { StatTile } from '@/components/data/StatTile'
import { downloadCsv, toCsv } from '@/lib/csv'
import { formatCurrency, formatDate, formatQuantity, todayIso } from '@/lib/format'
import { useDeadStock, useRateDrift, useReorder } from '../hooks'

const MONTH_OPTIONS = [3, 6, 9, 12]
const MARGIN_OPTIONS = [10, 15, 20, 25]
const COVER_OPTIONS = [15, 30, 45, 60, 90]

function Empty({ title, description }: { title: string; description: string }) {
  return (
    <Box sx={{ py: 5, textAlign: 'center' }}>
      <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
        {title}
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 460, mx: 'auto' }}>
        {description}
      </Typography>
    </Box>
  )
}

function ExportButton({ name, rows }: { name: string; rows: (string | number)[][] }) {
  return (
    <Button
      size="small"
      startIcon={<DownloadOutlinedIcon />}
      onClick={() => downloadCsv(`${name}-${todayIso()}.csv`, toCsv(rows))}
      disabled={rows.length <= 1}
    >
      Download
    </Button>
  )
}

function DeadStockTab() {
  const [months, setMonths] = useState(6)
  const { data } = useDeadStock(months)

  const csv: (string | number)[][] = [
    ['Part No', 'Item', 'Brand', 'On Hand', 'Purchase Rate', 'Value At Cost', 'Last Sold', 'Days Idle'],
    ...(data?.rows ?? []).map((row) => [
      row.partNumber,
      row.itemName,
      row.vehicleBrand ?? '',
      row.stockOnHand,
      row.purchaseRate,
      row.valueAtCost,
      row.lastSoldOn ?? 'Never',
      row.daysSinceLastSale ?? '',
    ]),
  ]

  return (
    <Stack spacing={2}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile
            label="Money standing still"
            value={formatCurrency(data?.totalValue ?? 0)}
            caption={`${data?.rows.length ?? 0} parts unsold for ${months} months or more`}
            tone="warning"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile
            label="Never sold at all"
            value={formatCurrency(data?.neverSoldValue ?? 0)}
            caption={`${data?.neverSoldCount ?? 0} parts that have never once left the shelf`}
            tone={data?.neverSoldCount ? 'error' : 'default'}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
            <TextField
              select
              size="small"
              label="Idle for"
              value={months}
              onChange={(event) => setMonths(Number(event.target.value))}
              sx={{ minWidth: 140 }}
            >
              {MONTH_OPTIONS.map((option) => (
                <MenuItem key={option} value={option}>
                  {option} months
                </MenuItem>
              ))}
            </TextField>
            <ExportButton name="dead-stock" rows={csv} />
          </Stack>
        </Grid>
      </Grid>

      <Paper sx={{ overflow: 'auto', maxHeight: 'calc(100vh - 400px)' }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Part No</TableCell>
              <TableCell>Item</TableCell>
              <TableCell>Brand</TableCell>
              <TableCell align="right">On Hand</TableCell>
              <TableCell align="right">Value At Cost</TableCell>
              <TableCell align="right">Last Sold</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.rows.map((row) => (
              <TableRow key={row.productId} hover>
                <TableCell sx={{ fontWeight: 600 }}>{row.partNumber}</TableCell>
                <TableCell>{row.itemName}</TableCell>
                <TableCell>{row.vehicleBrand ?? '—'}</TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatQuantity(row.stockOnHand)}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}>
                  {formatCurrency(row.valueAtCost)}
                </TableCell>
                <TableCell align="right">
                  {row.lastSoldOn ? (
                    <Stack sx={{ alignItems: 'flex-end' }}>
                      <span>{formatDate(row.lastSoldOn)}</span>
                      <Typography variant="caption" color="text.disabled">
                        {row.daysSinceLastSale} days ago
                      </Typography>
                    </Stack>
                  ) : (
                    <Chip size="small" color="error" label="Never sold" />
                  )}
                </TableCell>
              </TableRow>
            ))}
            {data?.rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>
                  <Empty
                    title="Nothing is sitting still"
                    description={`Every part with stock on hand has sold within the last ${months} months.`}
                  />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>
    </Stack>
  )
}

function RateDriftTab() {
  const [floor, setFloor] = useState(15)
  const { data } = useRateDrift(floor)

  const csv: (string | number)[][] = [
    ['Part No', 'Item', 'On Hand', 'Last Bought At', 'Bought On', 'Selling At', 'MRP', 'Margin %'],
    ...(data?.rows ?? []).map((row) => [
      row.partNumber,
      row.itemName,
      row.stockOnHand,
      row.lastPurchaseRate,
      row.lastPurchasedOn ?? '',
      row.sellingRate,
      row.mrp,
      row.marginPercent ?? 'Not priced',
    ]),
  ]

  return (
    <Stack spacing={2}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 3 }}>
          <StatTile
            label="Selling below cost"
            value={String(data?.belowCostCount ?? 0)}
            caption="Every sale of these loses money"
            tone={data?.belowCostCount ? 'error' : 'success'}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 3 }}>
          <StatTile
            label={`Margin under ${floor}%`}
            value={String(data?.thinMarginCount ?? 0)}
            caption="Supplier rate has moved, the price has not"
            tone={data?.thinMarginCount ? 'warning' : 'default'}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 3 }}>
          <StatTile
            label="No selling price"
            value={String(data?.unpricedCount ?? 0)}
            caption="Bought, but nobody has priced them"
            tone={data?.unpricedCount ? 'warning' : 'default'}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 3 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
            <TextField
              select
              size="small"
              label="Margin floor"
              value={floor}
              onChange={(event) => setFloor(Number(event.target.value))}
              sx={{ minWidth: 130 }}
            >
              {MARGIN_OPTIONS.map((option) => (
                <MenuItem key={option} value={option}>
                  {option}%
                </MenuItem>
              ))}
            </TextField>
            <ExportButton name="rate-drift" rows={csv} />
          </Stack>
        </Grid>
      </Grid>

      <Alert severity="info">
        Margin is worked out against the rate on the newest purchase bill, not the rate in the
        catalogue. The bill is what the supplier charges now, and that is what the next box will cost.
      </Alert>

      <Paper sx={{ overflow: 'auto', maxHeight: 'calc(100vh - 460px)' }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Part No</TableCell>
              <TableCell>Item</TableCell>
              <TableCell align="right">On Hand</TableCell>
              <TableCell align="right">Last Bought At</TableCell>
              <TableCell align="right">Selling At</TableCell>
              <TableCell align="right">MRP</TableCell>
              <TableCell align="right">Margin</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.rows.map((row) => (
              <TableRow key={row.productId} hover>
                <TableCell sx={{ fontWeight: 600 }}>{row.partNumber}</TableCell>
                <TableCell>{row.itemName}</TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatQuantity(row.stockOnHand)}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  <Stack sx={{ alignItems: 'flex-end' }}>
                    <span>{formatCurrency(row.lastPurchaseRate)}</span>
                    {row.lastPurchasedOn && (
                      <Typography variant="caption" color="text.disabled">
                        {formatDate(row.lastPurchasedOn)}
                      </Typography>
                    )}
                  </Stack>
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {row.sellingRateMissing ? '—' : formatCurrency(row.sellingRate)}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {row.mrp > 0 ? formatCurrency(row.mrp) : '—'}
                </TableCell>
                <TableCell align="right">
                  {row.sellingRateMissing ? (
                    <Chip size="small" color="warning" label="Not priced" />
                  ) : row.sellingBelowCost ? (
                    <Chip size="small" color="error" label={`${row.marginPercent}%`} />
                  ) : (
                    <Typography
                      component="span"
                      sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}
                    >
                      {row.marginPercent}%
                    </Typography>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {data?.rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={7}>
                  <Empty
                    title="Every part is earning its margin"
                    description={`Nothing is selling below cost or under ${floor}%, and everything bought has a price on it.`}
                  />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>
    </Stack>
  )
}

function ReorderTab() {
  const [cover, setCover] = useState(45)
  const { data } = useReorder(cover)

  const csv: (string | number)[][] = [
    ['Part No', 'Item', 'On Hand', 'Sold Per Day', 'Days Of Cover', 'Suggested Qty', 'Rate', 'Value'],
    ...(data?.rows ?? []).map((row) => [
      row.partNumber,
      row.itemName,
      row.stockOnHand,
      row.dailyVelocity,
      row.daysOfCover ?? '',
      row.suggestedQuantity,
      row.lastPurchaseRate,
      row.suggestedValue,
    ]),
  ]

  return (
    <Stack spacing={2}>
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile
            label="Buying list"
            value={formatCurrency(data?.totalSuggestedValue ?? 0)}
            caption={`${data?.rows.length ?? 0} parts to bring back up`}
            tone="primary"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <StatTile
            label="Already empty"
            value={String(data?.outOfStockCount ?? 0)}
            caption="Nothing on the shelf and still selling"
            tone={data?.outOfStockCount ? 'error' : 'success'}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', height: '100%' }}>
            <TextField
              select
              size="small"
              label="Stock to hold"
              value={cover}
              onChange={(event) => setCover(Number(event.target.value))}
              sx={{ minWidth: 150 }}
            >
              {COVER_OPTIONS.map((option) => (
                <MenuItem key={option} value={option}>
                  {option} days
                </MenuItem>
              ))}
            </TextField>
            <ExportButton name="reorder" rows={csv} />
          </Stack>
        </Grid>
      </Grid>

      <Alert severity="info">
        Worked out from what actually sold over the last {data?.windowDays ?? 90} days, net of
        returns — not from a fixed level. A part still gets on the list if it is under its reorder
        level, because that is the shop&apos;s own standing instruction.
      </Alert>

      <Paper sx={{ overflow: 'auto', maxHeight: 'calc(100vh - 460px)' }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Part No</TableCell>
              <TableCell>Item</TableCell>
              <TableCell align="right">On Hand</TableCell>
              <TableCell align="right">Sold Per Day</TableCell>
              <TableCell align="right">Runs Out In</TableCell>
              <TableCell align="right">Buy</TableCell>
              <TableCell align="right">Value</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.rows.map((row) => (
              <TableRow key={row.productId} hover>
                <TableCell sx={{ fontWeight: 600 }}>{row.partNumber}</TableCell>
                <TableCell>{row.itemName}</TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatQuantity(row.stockOnHand)}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {row.dailyVelocity > 0 ? row.dailyVelocity.toFixed(2) : '—'}
                </TableCell>
                <TableCell align="right">
                  {row.daysOfCover === null ? (
                    // Not "0 days" — a part that is not selling does not run out, and saying it
                    // does would push the shop to buy something nobody is asking for.
                    <Typography variant="caption" color="text.disabled">
                      Not moving
                    </Typography>
                  ) : row.daysOfCover <= 7 ? (
                    <Chip size="small" color="error" label={`${row.daysOfCover} days`} />
                  ) : (
                    <span>{row.daysOfCover} days</span>
                  )}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}>
                  {formatQuantity(row.suggestedQuantity)}
                </TableCell>
                <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(row.suggestedValue)}
                </TableCell>
              </TableRow>
            ))}
            {data?.rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={7}>
                  <Empty
                    title="Nothing to buy"
                    description={`Every part has enough on the shelf for the next ${cover} days at the rate it is selling.`}
                  />
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>
    </Stack>
  )
}

/**
 * The three questions a stock list cannot answer: what is not moving, what is no longer worth what
 * it costs, and what is about to run out.
 */
export function ShelfInsightsPage() {
  const [tab, setTab] = useState(0)

  return (
    <Stack spacing={2}>
      <Tabs value={tab} onChange={(_, next) => setTab(next)}>
        <Tab label="Dead Stock" />
        <Tab label="Rate Drift" />
        <Tab label="What To Buy" />
      </Tabs>

      {tab === 0 && <DeadStockTab />}
      {tab === 1 && <RateDriftTab />}
      {tab === 2 && <ReorderTab />}
    </Stack>
  )
}
