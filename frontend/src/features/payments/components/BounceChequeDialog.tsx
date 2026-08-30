import { describeError } from '@/lib/api/errors'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { formatCurrency, todayIso } from '@/lib/format'
import { zodResolver } from '@hookform/resolvers/zod'
import ReportProblemOutlinedIcon from '@mui/icons-material/ReportProblemOutlined'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  Grid,
  Stack,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { FormProvider, useForm } from 'react-hook-form'
import { useBounceCheque } from '../hooks'
import { bounceChequeSchema, type BounceChequeFormValues, type PaymentListItemDto } from '../types'

type BounceChequeDialogProps = {
  cheque: PaymentListItemDto | null
  onClose: () => void
}

/**
 * Records that the bank refused a cheque. Deliberately not a cancellation: the money genuinely
 * arrived and then failed, and the shop needs that visible before it takes this customer's cheque
 * again. The bills the cheque had settled all reopen.
 */
export function BounceChequeDialog({ cheque, onClose }: BounceChequeDialogProps) {
  const { notify } = useNotification()
  const [serverError, setServerError] = useState<string | null>(null)
  const bounceCheque = useBounceCheque()

  const form = useForm<BounceChequeFormValues>({
    resolver: zodResolver(bounceChequeSchema),
    defaultValues: { bouncedOn: todayIso(), reason: '', chargeAmount: 0 },
  })

  if (!cheque) return null

  async function onSubmit(values: BounceChequeFormValues) {
    setServerError(null)
    try {
      await bounceCheque.mutateAsync({ paymentId: cheque!.id, values })
      notify(`Cheque ${cheque!.chequeNumber} recorded as returned`, 'success')
      form.reset()
      onClose()
    } catch (error) {
      setServerError(describeError(error, 'Could not record the bounce'))
    }
  }

  return (
    <Dialog open fullWidth maxWidth="sm" onClose={bounceCheque.isPending ? undefined : onClose}>
      <DialogHeader
        title="Cheque returned"
        subtitle={`${cheque.chequeNumber} · ${cheque.partyName} · ${formatCurrency(cheque.amount)}`}
        icon={<ReportProblemOutlinedIcon fontSize="small" />}
        onClose={onClose}
        disabled={bounceCheque.isPending}
      />
      <FormProvider {...form}>
        <Box component="form" onSubmit={form.handleSubmit(onSubmit)}>
          <DialogContent dividers>
            <Stack spacing={2}>
              {serverError ? <Alert severity="error">{serverError}</Alert> : null}

              <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
                The bills this cheque had settled go back to unpaid and the customer's balance climbs
                again. The receipt stays on the statement — a balance that reappears with no line
                explaining it is what starts an argument at the counter.
              </Typography>

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFTextField
                    name="bouncedOn"
                    label="Returned on"
                    type="date"
                    required
                  />
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                  <RHFNumberField
                    name="chargeAmount"
                    label="Bank charge to recover"
                    // Defaults to nothing rather than to a stored figure the shop forgot it set.
                    helperText="Leave at zero if you are not charging it on"
                  />
                </Grid>
                <Grid size={12}>
                  <RHFTextField
                    name="reason"
                    label="What the bank said"
                    required
                    placeholder="Funds insufficient"
                  />
                </Grid>
              </Grid>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, py: 2 }}>
            <Button onClick={onClose} disabled={bounceCheque.isPending}>
              Cancel
            </Button>
            <Button type="submit" variant="contained" color="error" disabled={bounceCheque.isPending}>
              {bounceCheque.isPending ? 'Recording…' : 'Record the bounce'}
            </Button>
          </DialogActions>
        </Box>
      </FormProvider>
    </Dialog>
  )
}
