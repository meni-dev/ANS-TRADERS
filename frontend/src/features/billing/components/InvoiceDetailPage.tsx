import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { BalanceChip, DocumentStatusChip } from '@/components/document/DocumentStatusChip'
import { ConfirmDialog } from '@/components/feedback/ConfirmDialog'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { useShopSettings } from '@/features/settings/hooks'
import { formatDate } from '@/lib/format'
import BlockIcon from '@mui/icons-material/Block'
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined'
import AssignmentReturnOutlinedIcon from '@mui/icons-material/AssignmentReturnOutlined'
import PrintOutlinedIcon from '@mui/icons-material/PrintOutlined'
import { Alert, Box, Button, CircularProgress, Stack } from '@mui/material'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { templateComponent } from '../templates'
import { useCancelInvoice, useInvoice } from '../hooks'
import { useAuth } from '@/features/auth/AuthProvider'

export function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { can } = useAuth()
  const { notify } = useNotification()
  const { data: invoice, isLoading, isError } = useInvoice(id)
  const { data: shop } = useShopSettings()
  const cancelInvoice = useCancelInvoice()
  const [confirmOpen, setConfirmOpen] = useState(false)

  // The sheet needs both the document and the seller header, so it waits for both rather than
  // flashing a bill with a blank shop name on it.
  if (isLoading || !shop) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: 320 }}>
        <CircularProgress size={28} />
      </Box>
    )
  }

  if (isError || !invoice) {
    return (
      <Alert severity="error" action={<Button size="small" onClick={() => navigate('/billing')}>Back</Button>}>
        That invoice could not be loaded.
      </Alert>
    )
  }

  const handleCancel = async () => {
    try {
      await cancelInvoice.mutateAsync(invoice.id)
      notify(`Invoice ${invoice.invoiceNumber} cancelled`, 'info')
      setConfirmOpen(false)
    } catch {
      notify('Something went wrong. Please try again.', 'error')
    }
  }

  const isCancelled = invoice.status === 'Cancelled'

  // Which layout prints is a shop-wide setting, so the same bill looks the same from every terminal.
  const Template = templateComponent(shop.invoiceTemplate)

  return (
    <Box>
      <PageHeader
        title={invoice.invoiceNumber}
        icon={<ReceiptLongOutlinedIcon />}
        iconTone="blue"
        badge={
          <>
            <DocumentStatusChip status={invoice.status} />
            {!isCancelled && (
              <BalanceChip balanceDue={invoice.balanceDue} grandTotal={invoice.grandTotal} />
            )}
          </>
        }
        caption={
          <>
            {invoice.customerName} · {formatDate(invoice.invoiceDate)}
            {/* Only worth saying when it differs from the invoice date — otherwise it is noise. */}
            {invoice.dueDate && invoice.dueDate !== invoice.invoiceDate && invoice.balanceDue > 0
              ? ` · due ${formatDate(invoice.dueDate)}`
              : ''}
          </>
        }
        onBack={() => navigate('/billing')}
        className="no-print"
        actions={
          <Stack direction="row" spacing={1}>
            {/* The counter is standing here when the customer pays, so the receipt starts here. */}
            {!isCancelled && invoice.balanceDue > 0 && can('PaymentRecord') && (
              <Button
                variant="contained"
                startIcon={<PaymentsOutlinedIcon sx={{ fontSize: 18 }} />}
                onClick={() => navigate('/accounts/payments/new')}
              >
                Record receipt
              </Button>
            )}
            {/* Goods come back to the counter, not to a menu — so the return starts from the
                document the customer is holding. */}
            {!isCancelled && can('SalesReturn') && (
              <Button
                variant="outlined"
                startIcon={<AssignmentReturnOutlinedIcon sx={{ fontSize: 18 }} />}
                onClick={() => navigate(`/billing/${invoice.id}/return`)}
              >
                Return items
              </Button>
            )}
            <Button
              variant="outlined"
              startIcon={<PrintOutlinedIcon sx={{ fontSize: 18 }} />}
              onClick={() => window.print()}
            >
              Print
            </Button>
            {!isCancelled && can('BillCancel') && (
              <Button
                variant="outlined"
                color="error"
                startIcon={<BlockIcon sx={{ fontSize: 18 }} />}
                onClick={() => setConfirmOpen(true)}
              >
                Cancel
              </Button>
            )}
          </Stack>
        }
      />

      {isCancelled && (
        <Alert severity="warning" sx={{ mb: 2 }} className="no-print">
          This invoice has been cancelled. It is kept for the audit trail but must not be treated as
          a live tax document.
        </Alert>
      )}

      <Template invoice={invoice} shop={shop} />

      <ConfirmDialog
        open={confirmOpen}
        title="Cancel this invoice?"
        description={`${invoice.invoiceNumber} will be marked cancelled. The record stays for the audit trail, but it stops counting as a live tax document. Invoice numbers are never reused.`}
        confirmLabel="Cancel Invoice"
        confirmColor="error"
        loading={cancelInvoice.isPending}
        onConfirm={handleCancel}
        onCancel={() => setConfirmOpen(false)}
      />
    </Box>
  )
}
