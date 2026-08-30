import { describeError } from '@/lib/api/errors'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { FormErrorSummary } from '@/components/form/FormErrorSummary'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { formatQuantity } from '@/lib/format'
import { zodResolver } from '@hookform/resolvers/zod'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  Grid,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm, useWatch } from 'react-hook-form'
import { useAdjustStock } from '../hooks'
import { ADJUSTMENT_REASONS, adjustStockSchema, type AdjustStockFormValues, type ProductStockDto } from '../types'

type AdjustStockDialogProps = {
  product: ProductStockDto
  onClose: () => void
}

/**
 * Corrects stock to a physical count. The form asks what is actually on the shelf rather than for
 * a delta — a recount is the thing the user just did, and making them subtract turns one number
 * into two chances to get it wrong. The difference is shown so the correction is never a surprise.
 */
export function AdjustStockDialog({ product, onClose }: AdjustStockDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)

  const form = useForm<AdjustStockFormValues>({
    resolver: zodResolver(adjustStockSchema),
    defaultValues: {
      productId: product.id,
      countedQuantity: product.stockOnHand,
      reason: 'CountingError',
      notes: '',
    },
  })

  const adjustStock = useAdjustStock()

  const countedQuantity = useWatch({ control: form.control, name: 'countedQuantity' })
  const difference = (Number(countedQuantity) || 0) - product.stockOnHand

  const handleClose = () => {
    if (adjustStock.isPending) return
    onClose()
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setServerError(null)
    try {
      await adjustStock.mutateAsync(values)
      notify(`Stock for "${product.itemName}" corrected to ${formatQuantity(values.countedQuantity)}`)
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Something went wrong. Please try again.'))
    }
  })

  return (
    <Dialog open onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogHeader
        title="Adjust Stock"
        subtitle={`${product.partNumber} · ${product.itemName}`}
        icon={<FactCheckOutlinedIcon sx={{ fontSize: 20 }} />}
        onClose={handleClose}
        disabled={adjustStock.isPending}
      />

      <FormProvider {...form}>
        <form onSubmit={onSubmit} noValidate>
          <DialogContent dividers sx={{ px: 3, py: 2.5, bgcolor: 'grey.50' }}>
            {serverError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {serverError}
              </Alert>
            )}
            <FormErrorSummary />

            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 6 }}>
                <TextField
                  label="Recorded Stock"
                  value={formatQuantity(product.stockOnHand)}
                  disabled
                  fullWidth
                  helperText="What the system has today"
                />
              </Grid>
              <Grid size={{ xs: 12, sm: 6 }}>
                <RHFNumberField
                  name="countedQuantity"
                  label="Counted Stock"
                  required
                  helperText={`Actual count, in ${product.uqc}`}
                />
              </Grid>
              <Grid size={12}>
                {/* A code, not a sentence: "how much did I lose to damage this year" is a question
                    free text can never answer. The sentence goes in Notes, underneath. */}
                <RHFSelectField
                  name="reason"
                  label="Reason"
                  options={ADJUSTMENT_REASONS.map((r) => ({ value: r.value, label: r.label }))}
                  required
                />
              </Grid>
              <Grid size={12}>
                <RHFTextField
                  name="notes"
                  label="Notes"
                  placeholder="Two boxes crushed in the rack"
                  helperText="Optional — what happened this time"
                />
              </Grid>
            </Grid>

            <Stack
              direction="row"
              spacing={1}
              sx={{ mt: 2.5, alignItems: 'baseline', justifyContent: 'space-between' }}
            >
              <Typography sx={{ fontSize: 13, color: 'text.secondary' }}>
                Ledger entry
              </Typography>
              <Typography
                sx={{
                  fontSize: 16,
                  fontWeight: 700,
                  fontVariantNumeric: 'tabular-nums',
                  color: difference === 0 ? 'text.disabled' : difference > 0 ? 'success.dark' : 'error.dark',
                }}
              >
                {difference === 0
                  ? 'No change'
                  : `${difference > 0 ? '+' : '−'}${formatQuantity(Math.abs(difference))} ${product.uqc}`}
              </Typography>
            </Stack>
          </DialogContent>

          <DialogActions sx={{ px: 3, py: 2, gap: 1 }}>
            <Box sx={{ flexGrow: 1 }} />
            <Button onClick={handleClose} disabled={adjustStock.isPending} variant="outlined">
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              loading={adjustStock.isPending}
              disabled={difference === 0}
            >
              Record Adjustment
            </Button>
          </DialogActions>
        </form>
      </FormProvider>
    </Dialog>
  )
}
