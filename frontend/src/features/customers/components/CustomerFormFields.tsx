import { FormSection } from '@/components/form/FormSection'
import { PartyAddressFields } from '@/components/form/PartyAddressFields'
import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFTextField } from '@/components/form/RHFTextField'
import { Grid, InputAdornment } from '@mui/material'

export function CustomerFormFields() {
  return (
    <>
      <FormSection title="Contact" caption="How you reach this customer.">
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 7 }}>
            <RHFTextField name="name" label="Customer Name" required />
          </Grid>
          <Grid size={{ xs: 12, sm: 5 }}>
            <RHFTextField name="phone" label="Phone" required placeholder="9840012345" />
          </Grid>
          <Grid size={12}>
            <RHFTextField name="email" label="Email" placeholder="Optional" />
          </Grid>
        </Grid>
      </FormSection>

      <FormSection title="Tax & Credit" caption="GSTIN is required only for registered customers.">
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 5 }}>
            <RHFTextField name="gstin" label="GSTIN" placeholder="33AABCU9603R1ZM" />
          </Grid>
          <Grid size={{ xs: 12, sm: 4 }}>
            <RHFNumberField
              name="creditLimit"
              label="Credit Limit"
              required
              helperText="Leave at 0 for no limit"
              slotProps={{ input: { startAdornment: <InputAdornment position="start">₹</InputAdornment> } }}
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 3 }}>
            <RHFNumberField
              name="creditDays"
              label="Credit Days"
              required
              // Sets the due date on every bill. Without it, ageing counts from the invoice date and
              // a customer on 30-day terms reads as a month late on the day he is billed.
              helperText="0 = pay on delivery"
            />
          </Grid>
        </Grid>
      </FormSection>

      <FormSection title="Address" caption="Optional — needed on GST invoices." collapsible>
        <PartyAddressFields />
      </FormSection>
    </>
  )
}
