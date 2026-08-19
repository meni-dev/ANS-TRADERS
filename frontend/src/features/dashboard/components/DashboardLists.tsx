import { formatCurrency, formatDate, formatQuantity } from '@/lib/format'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import type { RecentInvoiceDto, ReorderItemDto, TopSellingItemDto } from '../types'

type ListPanelProps = {
  title: string
  caption: string
  emptyMessage: string
  actionLabel: string
  onAction: () => void
  children: ReactNode
  isEmpty: boolean
}

/** The shell the three bottom panels share — title, rows, and one way through to the full screen. */
function ListPanel({
  title,
  caption,
  emptyMessage,
  actionLabel,
  onAction,
  children,
  isEmpty,
}: ListPanelProps) {
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2.5, borderRadius: '8px', height: '100%', display: 'flex', flexDirection: 'column' }}
    >
      <Typography variant="h3">{title}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mt: 0.25, mb: 1.5 }}>
        {caption}
      </Typography>

      <Box sx={{ flexGrow: 1 }}>
        {isEmpty ? (
          <Typography sx={{ fontSize: 13, color: 'text.disabled', py: 2 }}>{emptyMessage}</Typography>
        ) : (
          children
        )}
      </Box>

      <Button
        size="small"
        variant="text"
        endIcon={<ArrowForwardIcon sx={{ fontSize: 16 }} />}
        onClick={onAction}
        sx={{ alignSelf: 'flex-start', mt: 1.5, ml: -1 }}
      >
        {actionLabel}
      </Button>
    </Paper>
  )
}

function Row({
  primary,
  secondary,
  value,
  valueColour = 'text.primary',
  onClick,
}: {
  primary: string
  secondary: string
  value: ReactNode
  valueColour?: string
  onClick?: () => void
}) {
  return (
    <Stack
      direction="row"
      spacing={1.5}
      onClick={onClick}
      sx={{
        alignItems: 'center',
        justifyContent: 'space-between',
        py: 0.875,
        borderBottom: '1px solid',
        borderColor: 'grey.100',
        '&:last-of-type': { borderBottom: 'none' },
        ...(onClick && {
          cursor: 'pointer',
          mx: -1,
          px: 1,
          borderRadius: '4px',
          '&:hover': { bgcolor: 'grey.50' },
        }),
      }}
    >
      <Box sx={{ minWidth: 0 }}>
        <Typography sx={{ fontSize: 13, fontWeight: 600, lineHeight: 1.4 }} noWrap>
          {primary}
        </Typography>
        <Typography sx={{ fontSize: 11.5, color: 'text.disabled', lineHeight: 1.4 }} noWrap>
          {secondary}
        </Typography>
      </Box>
      <Box
        sx={{
          fontSize: 13,
          fontWeight: 600,
          fontVariantNumeric: 'tabular-nums',
          color: valueColour,
          flexShrink: 0,
          textAlign: 'right',
        }}
      >
        {value}
      </Box>
    </Stack>
  )
}

export function ReorderPanel({
  items,
  onOpen,
}: {
  items: ReorderItemDto[]
  onOpen: () => void
}) {
  return (
    <ListPanel
      title="Needs reordering"
      caption="At or below the reorder level."
      emptyMessage="Everything is above its reorder level."
      actionLabel="Low stock"
      onAction={onOpen}
      isEmpty={items.length === 0}
    >
      {items.map((item) => (
        <Row
          key={item.productId}
          primary={item.itemName}
          secondary={`${item.partNumber} · reorder at ${formatQuantity(item.reorderLevel)}`}
          value={`${formatQuantity(item.stockOnHand)} ${item.uqc}`}
          valueColour={item.stockOnHand <= 0 ? 'error.dark' : 'warning.dark'}
        />
      ))}
    </ListPanel>
  )
}

export function TopSellersPanel({
  items,
  onOpen,
}: {
  items: TopSellingItemDto[]
  onOpen: () => void
}) {
  return (
    <ListPanel
      title="Top sellers"
      caption="This month, by value sold."
      emptyMessage="No sales this month yet."
      actionLabel="New invoice"
      onAction={onOpen}
      isEmpty={items.length === 0}
    >
      {items.map((item) => (
        <Row
          key={item.productId}
          primary={item.itemName}
          secondary={`${item.partNumber} · ${formatQuantity(item.quantity)} ${item.uqc} sold`}
          value={formatCurrency(item.salesValue)}
        />
      ))}
    </ListPanel>
  )
}

export function RecentInvoicesPanel({
  items,
  onOpen,
  onOpenInvoice,
}: {
  items: RecentInvoiceDto[]
  onOpen: () => void
  onOpenInvoice: (id: string) => void
}) {
  return (
    <ListPanel
      title="Recent invoices"
      caption="The last five raised."
      emptyMessage="No invoices raised yet."
      actionLabel="All invoices"
      onAction={onOpen}
      isEmpty={items.length === 0}
    >
      {items.map((invoice) => (
        <Row
          key={invoice.id}
          primary={invoice.customerName}
          secondary={`${invoice.invoiceNumber} · ${formatDate(invoice.invoiceDate)}`}
          onClick={() => onOpenInvoice(invoice.id)}
          value={
            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
              {invoice.status === 'Cancelled' ? (
                <Chip
                  label="Cancelled"
                  size="small"
                  sx={{ bgcolor: 'error.light', color: 'error.dark' }}
                />
              ) : invoice.balanceDue > 0 ? (
                <Chip label="Due" size="small" sx={{ bgcolor: 'warning.light', color: 'warning.dark' }} />
              ) : null}
              <span>{formatCurrency(invoice.grandTotal)}</span>
            </Stack>
          }
        />
      ))}
    </ListPanel>
  )
}
