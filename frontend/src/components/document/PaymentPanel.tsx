import { PanelCard } from '@/components/data/PanelCard'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { PAYMENT_MODES } from '@/lib/documents/types'
import { Grid } from '@mui/material'
import type { ReactNode } from 'react'

type PaymentPanelProps = {
  /** Extra field between payment mode and amount — invoice's bill discount, purchase has none. */
  extraField?: ReactNode
  amountPaidDisabled?: boolean
  amountPaidHelperText?: ReactNode
  amountPaidLabel?: string
  notesPlaceholder?: string
}

/**
 * The payment block shared by the invoice and purchase create screens. Reads paymentMode /
 * amountPaid / notes off whichever FormProvider is in context, same as LineItemsEditor — callers
 * differ only in the amount-paid copy/disabled state and an optional extra field (invoice's bill
 * discount), so those are passed in rather than branching on document type here.
 */
export function PaymentPanel({
  extraField,
  amountPaidDisabled,
  amountPaidHelperText,
  amountPaidLabel = 'Amount Paid',
  notesPlaceholder,
}: PaymentPanelProps) {
  return (
    <PanelCard title="Payment">
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <RHFSelectField name="paymentMode" label="Payment Mode" options={[...PAYMENT_MODES]} />
        </Grid>
        {extraField && <Grid size={{ xs: 12, sm: 6 }}>{extraField}</Grid>}
        <Grid size={{ xs: 12, sm: 6 }}>
          <RHFNumberField
            name="amountPaid"
            label={amountPaidLabel}
            disabled={amountPaidDisabled}
            helperText={amountPaidHelperText}
          />
        </Grid>
        <Grid size={12}>
          <RHFTextField
            name="notes"
            label="Notes"
            multiline
            minRows={2}
            placeholder={notesPlaceholder}
          />
        </Grid>
      </Grid>
    </PanelCard>
  )
}
