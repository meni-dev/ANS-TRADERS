import { formatCurrency, formatDate, formatQuantity } from '@/lib/format'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { PanelCard } from '@/components/data/PanelCard'
import LocalFireDepartmentOutlinedIcon from '@mui/icons-material/LocalFireDepartmentOutlined'
import ReceiptOutlinedIcon from '@mui/icons-material/ReceiptOutlined'
import AddShoppingCartOutlinedIcon from '@mui/icons-material/AddShoppingCartOutlined'
import { Box, Button, Chip, Stack, Typography } from '@mui/material'
import type { AccentTone } from '@/theme/theme'
import type { ReactNode } from 'react'
import type { RecentInvoiceDto, ReorderItemDto, TopSellingItemDto } from '../types'

type ListPanelProps = {
  title: string
  icon: ReactNode
  iconTone: AccentTone
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
  icon,
  iconTone,
  caption,
  emptyMessage,
  actionLabel,
  onAction,
  children,
  isEmpty,
}: ListPanelProps) {
  return (
    <PanelCard
      title={title}
      icon={icon}
      iconTone={iconTone}
      caption={caption}
      footer={
        <Button
          size="small"
          variant="text"
          endIcon={<ArrowForwardIcon sx={{ fontSize: 16 }} />}
          onClick={onAction}
          sx={{ ml: -1 }}
        >
          {actionLabel}
        </Button>
      }
    >
      {isEmpty ? (
        <Typography sx={{ fontSize: 13, color: 'text.disabled', py: 2 }}>{emptyMessage}</Typography>
      ) : (
        children
      )}
    </PanelCard>
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
      icon={<AddShoppingCartOutlinedIcon />}
      iconTone="amber"
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
      icon={<LocalFireDepartmentOutlinedIcon />}
      iconTone="rose"
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
      icon={<ReceiptOutlinedIcon />}
      iconTone="blue"
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
