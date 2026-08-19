import { RHFSelectField } from '@/components/form/RHFSelectField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { STATE_OPTIONS, stateCodeFor, stateFromGstin } from '@/lib/indianStates'
import { Grid, TextField } from '@mui/material'
import { useEffect } from 'react'
import { useFormContext, useWatch } from 'react-hook-form'

/**
 * Address block shared by the customer and supplier forms. The GST state code is derived rather
 * than typed: it must match the chosen state exactly for billing to pick CGST+SGST over IGST,
 * and a hand-entered code is the obvious place for that to go wrong.
 */
export function PartyAddressFields() {
  const { control, setValue, getValues } = useFormContext()

  const state = useWatch({ control, name: 'state' })
  const gstin = useWatch({ control, name: 'gstin' })

  // Keep the stored code in step with the selected state.
  useEffect(() => {
    const code = stateCodeFor(state as string)
    if (getValues('stateCode') !== code) {
      setValue('stateCode', code, { shouldValidate: true })
    }
  }, [state, setValue, getValues])

  // A GSTIN opens with its own state code, so pasting one fills the state in. Only applied when
  // the state is still blank — a user who picked a state deliberately should not be overridden.
  useEffect(() => {
    const matched = stateFromGstin(gstin as string)
    if (matched && !getValues('state')) {
      setValue('state', matched.name, { shouldValidate: true })
    }
  }, [gstin, setValue, getValues])

  return (
    <Grid container spacing={2}>
      <Grid size={12}>
        <RHFTextField name="addressLine1" label="Address Line 1" />
      </Grid>
      <Grid size={12}>
        <RHFTextField name="addressLine2" label="Address Line 2" />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <RHFTextField name="city" label="City" />
      </Grid>
      <Grid size={{ xs: 12, sm: 6 }}>
        <RHFTextField name="pincode" label="Pincode" placeholder="600001" />
      </Grid>
      <Grid size={{ xs: 12, sm: 8 }}>
        <RHFSelectField name="state" label="State" options={STATE_OPTIONS} />
      </Grid>
      <Grid size={{ xs: 12, sm: 4 }}>
        <TextField
          label="GST State Code"
          value={stateCodeFor(state as string) || ''}
          disabled
          fullWidth
          helperText="From state"
        />
      </Grid>
    </Grid>
  )
}
