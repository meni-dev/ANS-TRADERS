import { FormSection } from '@/components/form/FormSection'
import { PartyAddressFields } from '@/components/form/PartyAddressFields'
import { RHFTextField } from '@/components/form/RHFTextField'
import { Grid } from '@mui/material'

export function SupplierFormFields() {
  return (
    <>
      <FormSection title="Contact" caption="How you reach this supplier.">
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 7 }}>
            <RHFTextField name="name" label="Supplier Name" required />
          </Grid>
          <Grid size={{ xs: 12, sm: 5 }}>
            <RHFTextField name="phone" label="Phone" required placeholder="9012345678" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFTextField name="contactPerson" label="Contact Person" placeholder="Optional" />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFTextField name="email" label="Email" placeholder="Optional" />
          </Grid>
        </Grid>
      </FormSection>

      <FormSection title="Tax & Terms" caption="GSTIN is needed to claim input tax credit.">
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 7 }}>
            <RHFTextField name="gstin" label="GSTIN" placeholder="29AABCB1234C1Z5" />
          </Grid>
          <Grid size={{ xs: 12, sm: 5 }}>
            <RHFTextField name="paymentTerms" label="Payment Terms" placeholder="e.g. 30 days" />
          </Grid>
        </Grid>
      </FormSection>

      <FormSection title="Address" caption="Optional — needed on purchase records." collapsible>
        <PartyAddressFields />
      </FormSection>
    </>
  )
}
