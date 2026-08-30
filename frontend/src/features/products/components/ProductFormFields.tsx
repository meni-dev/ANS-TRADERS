import { RHFNumberField } from '@/components/form/RHFNumberField'
import { RHFSelectField } from '@/components/form/RHFSelectField'
import { SUPPLY_TYPES } from '../types'
import { FormSection } from '@/components/form/FormSection'
import { RHFTextField } from '@/components/form/RHFTextField'
import { Grid, InputAdornment, TextField } from '@mui/material'
import { useEffect, useMemo } from 'react'
import { useFormContext, useWatch } from 'react-hook-form'
import { UQC_OPTIONS, VEHICLE_BRANDS, VEHICLE_MODELS } from '../vehicleData'

export function ProductFormFields() {
  const { control, setValue, getValues } = useFormContext()

  const gstRate = useWatch({ control, name: 'gstRate' })
  const supplyType = useWatch({ control, name: 'supplyType' })
  const vehicleBrand = useWatch({ control, name: 'vehicleBrand' })

  // CGST and SGST are always an even split of GST. They mirror what the server stores, so they
  // are shown read-only rather than being editable form fields.
  const halfGst = useMemo(() => {
    const rate = Number(gstRate)
    return Number.isFinite(rate) ? (rate / 2).toFixed(2) : '0.00'
  }, [gstRate])

  const modelOptions = useMemo(() => {
    const models = VEHICLE_MODELS[vehicleBrand as string] ?? []
    return models.map((m) => ({ value: m, label: m }))
  }, [vehicleBrand])

  // A model from the previous brand must not linger once the brand changes.
  useEffect(() => {
    const models = VEHICLE_MODELS[vehicleBrand as string] ?? []
    const current = getValues('vehicleModel')
    if (current && !models.includes(current)) {
      setValue('vehicleModel', '')
    }
  }, [vehicleBrand, setValue, getValues])

  const rupee = {
    startAdornment: <InputAdornment position="start">₹</InputAdornment>,
  }

  return (
    <>
      <FormSection title="Identification" caption="How this part is found and referred to.">
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFTextField name="partNumber" label="Part Number" required />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFTextField name="itemCode" label="Item Code" required />
          </Grid>
          <Grid size={12}>
            <RHFTextField name="itemName" label="Item Name" required />
          </Grid>
          <Grid size={12}>
            <RHFTextField
              name="description"
              label="Description"
              multiline
              minRows={2}
              placeholder="Optional notes about this part"
            />
          </Grid>
        </Grid>
      </FormSection>

      <FormSection title="Pricing & Tax" caption="Rates used across billing and GST returns.">
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFTextField name="hsn" label="HSN Code" required />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFSelectField name="uqc" label="UQC" options={UQC_OPTIONS} required />
          </Grid>

          <Grid size={{ xs: 12, sm: 4 }}>
            <RHFSelectField
              name="supplyType"
              label="Supply Type"
              options={SUPPLY_TYPES.map((t) => ({ value: t.value, label: t.label }))}
              helperText="Nil rated and exempt goods are reported apart from taxable turnover"
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 4 }}>
            <RHFNumberField
              name="gstRate"
              label="GST Rate"
              required
              disabled={supplyType !== 'Taxable'}
              helperText={supplyType === 'Taxable' ? undefined : 'Untaxed goods carry no rate'}
              slotProps={{ input: { endAdornment: <InputAdornment position="end">%</InputAdornment> } }}
            />
          </Grid>
          <Grid size={{ xs: 6, sm: 4 }}>
            <TextField
              label="CGST"
              value={halfGst}
              disabled
              fullWidth
              helperText="Half of GST"
              slotProps={{ input: { endAdornment: <InputAdornment position="end">%</InputAdornment> } }}
            />
          </Grid>
          <Grid size={{ xs: 6, sm: 4 }}>
            <TextField
              label="SGST"
              value={halfGst}
              disabled
              fullWidth
              helperText="Half of GST"
              slotProps={{ input: { endAdornment: <InputAdornment position="end">%</InputAdornment> } }}
            />
          </Grid>

          <Grid size={{ xs: 12, sm: 4 }}>
            <RHFNumberField name="purchaseRate" label="Purchase Rate" required slotProps={{ input: rupee }} />
          </Grid>
          <Grid size={{ xs: 12, sm: 4 }}>
            <RHFNumberField name="sellingRate" label="Selling Rate" required slotProps={{ input: rupee }} />
          </Grid>
          <Grid size={{ xs: 12, sm: 4 }}>
            <RHFNumberField name="mrp" label="MRP" required slotProps={{ input: rupee }} />
          </Grid>
        </Grid>
      </FormSection>

      <FormSection title="Vehicle Fitment" caption="Optional — which bike this part belongs to." collapsible>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFSelectField
              name="vehicleBrand"
              label="Vehicle Brand"
              options={VEHICLE_BRANDS.map((b) => ({ value: b, label: b }))}
            />
          </Grid>
          <Grid size={{ xs: 12, sm: 6 }}>
            <RHFSelectField
              name="vehicleModel"
              label="Vehicle Model"
              options={modelOptions}
              disabled={modelOptions.length === 0}
              helperText={modelOptions.length === 0 ? 'Pick a vehicle brand first' : undefined}
            />
          </Grid>
        </Grid>
      </FormSection>
    </>
  )
}
