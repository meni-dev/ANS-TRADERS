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
    // One field per row rather than the sm:6 pairing an earlier, wider sidebar used: a fixed
    // ~360px column halves to under 160px per field, which is enough width for the input itself
    // but not for its helper text ("A flat amount off the whole bill" and the like), which then
    // wraps to two or three cramped lines. Full width fits nearly all of it on one line instead.
    <PanelCard title="Payment">
      <Grid container spacing={2}>
        <Grid size={12}>
          <RHFSelectField
            name="paymentMode"
            label="Payment Mode"
            options={[...PAYMENT_MODES]}
            // Targeted by the item table's "done adding items" exit — see handleEnterAsTab.ts.
            id="payment-mode-trigger"
          />
        </Grid>
        {extraField && <Grid size={12}>{extraField}</Grid>}
        <Grid size={12}>
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
