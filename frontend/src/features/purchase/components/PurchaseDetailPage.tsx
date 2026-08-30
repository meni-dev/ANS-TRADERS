import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { DocumentLinesTable } from '@/components/document/DocumentLinesTable'
import { useAuth } from '@/features/auth/AuthProvider'
import { BalanceChip, DocumentStatusChip } from '@/components/document/DocumentStatusChip'
import { DocumentTotals } from '@/components/document/DocumentTotals'
import { ConfirmDialog } from '@/components/feedback/ConfirmDialog'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { formatDate } from '@/lib/format'
import AssignmentReturnOutlinedIcon from '@mui/icons-material/AssignmentReturnOutlined'
import BlockIcon from '@mui/icons-material/Block'
import { Alert, Box, Button, Chip, CircularProgress, Grid, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useCancelPurchase, usePurchase } from '../hooks'

function Field({ label, value }: { label: string; value: string }) {
  return (
    <Box>
      <Typography sx={{ fontSize: 11, fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase', color: 'text.disabled' }}>
        {label}
      </Typography>
      <Typography sx={{ fontSize: 13.5, mt: 0.25 }}>{value}</Typography>
    </Box>
  )
}

export function PurchaseDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { can } = useAuth()
  const { notify } = useNotification()
  const { data: purchase, isLoading, isError } = usePurchase(id)
  const cancelPurchase = useCancelPurchase()
  const [confirmOpen, setConfirmOpen] = useState(false)

  if (isLoading) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: 320 }}>
        <CircularProgress size={28} />
      </Box>
    )
  }

  if (isError || !purchase) {
    return (
      <Alert severity="error" action={<Button size="small" onClick={() => navigate('/purchases')}>Back</Button>}>
        That purchase could not be loaded.
      </Alert>
    )
  }

  const handleCancel = async () => {
    try {
      await cancelPurchase.mutateAsync(purchase.id)
      notify(`Purchase ${purchase.purchaseNumber} cancelled`, 'info')
      setConfirmOpen(false)
    } catch {
      notify('Something went wrong. Please try again.', 'error')
    }
  }

  const isCancelled = purchase.status === 'Cancelled'

  return (
    <Box>
      <PageHeader
        title={purchase.purchaseNumber}
        icon={<ReceiptLongOutlinedIcon />}
        iconTone="violet"
        badge={
          <>
            <DocumentStatusChip status={purchase.status} />
            {!isCancelled && (
              <BalanceChip balanceDue={purchase.balanceDue} grandTotal={purchase.grandTotal} />
            )}
          </>
        }
        caption={`Supplier bill ${purchase.supplierInvoiceNumber} · ${formatDate(purchase.invoiceDate)}`}
        onBack={() => navigate('/purchases')}
        actions={
          <Stack direction="row" spacing={1}>
            {/* Goods go back from the shelf the bill filled, so the return starts from that bill. */}
            {!isCancelled && can('PurchaseReturn') && (
              <Button
                variant="outlined"
                startIcon={<AssignmentReturnOutlinedIcon sx={{ fontSize: 18 }} />}
                onClick={() => navigate(`/purchases/${purchase.id}/return`)}
              >
                Return items
              </Button>
            )}
            {!isCancelled && can('PurchaseCancel') && (
              <Button
                variant="outlined"
                color="error"
                startIcon={<BlockIcon sx={{ fontSize: 18 }} />}
                onClick={() => setConfirmOpen(true)}
              >
                Cancel Bill
              </Button>
            )}
          </Stack>
        }
      />

      {isCancelled && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          This purchase has been cancelled. It is kept for the audit trail but should not be claimed
          for input tax credit.
        </Alert>
      )}

      <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px', mb: 2 }}>
        <Grid container spacing={3}>
          <Grid size={{ xs: 6, sm: 3 }}>
            <Field label="Supplier" value={purchase.supplierName} />
          </Grid>
          <Grid size={{ xs: 6, sm: 3 }}>
            <Field label="GSTIN" value={purchase.supplierGstin ?? '—'} />
          </Grid>
          <Grid size={{ xs: 6, sm: 3 }}>
            <Field label="Bill Date" value={formatDate(purchase.invoiceDate)} />
          </Grid>
          <Grid size={{ xs: 6, sm: 3 }}>
            <Field label="Payment Mode" value={purchase.paymentMode} />
          </Grid>
        </Grid>

        <Stack direction="row" spacing={1} sx={{ mt: 2, alignItems: 'center' }}>
          <Chip
            size="small"
            label={purchase.isInterState ? 'Inter-state · IGST' : 'Intra-state · CGST + SGST'}
            sx={{ bgcolor: 'primary.light', color: 'primary.dark' }}
          />
          <Typography sx={{ fontSize: 12.5, color: 'text.disabled' }}>
            Financial year {purchase.financialYear}
          </Typography>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ borderRadius: '8px', overflow: 'hidden', mb: 2 }}>
        <DocumentLinesTable lines={purchase.items} isInterState={purchase.isInterState} />
      </Paper>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 7 }}>
          {purchase.notes && (
            <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px', height: '100%' }}>
              <Typography variant="overline" sx={{ color: 'text.secondary', display: 'block', mb: 1 }}>
                Notes
              </Typography>
              <Typography sx={{ fontSize: 13.5, whiteSpace: 'pre-wrap' }}>{purchase.notes}</Typography>
            </Paper>
          )}
        </Grid>

        <Grid size={{ xs: 12, md: 5 }}>
          <Paper variant="outlined" sx={{ p: 2.5, borderRadius: '8px' }}>
            <DocumentTotals
              amounts={{
                subTotal: purchase.subTotal,
                discountAmount: purchase.discountAmount,
                taxableAmount: purchase.taxableAmount,
                cgstAmount: purchase.cgstAmount,
                sgstAmount: purchase.sgstAmount,
                igstAmount: purchase.igstAmount,
                totalTax: purchase.totalTax,
                roundOff: purchase.roundOff,
                grandTotal: purchase.grandTotal,
              }}
              isInterState={purchase.isInterState}
              amountPaid={purchase.amountPaid}
            />
          </Paper>
        </Grid>
      </Grid>

      <ConfirmDialog
        open={confirmOpen}
        title="Cancel this purchase?"
        description={`${purchase.purchaseNumber} will be marked cancelled. The record stays for the audit trail, but it will no longer count towards input tax credit.`}
        confirmLabel="Cancel Bill"
        confirmColor="error"
        loading={cancelPurchase.isPending}
        onConfirm={handleCancel}
        onCancel={() => setConfirmOpen(false)}
      />
    </Box>
  )
}
